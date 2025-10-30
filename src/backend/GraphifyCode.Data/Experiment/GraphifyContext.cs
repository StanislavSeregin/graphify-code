using GraphifyCode.Data.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GraphifyCode.Data.Experiment;

public class GraphifyContext(
    IOptions<MarkdownStorageSettings> settings
) : DbContext
{
    private static readonly SemaphoreSlim _fileSystemLock = new(1, 1);
    private readonly string _pathContext = settings.Value.Path;
    private bool _isDataLoaded = false;

    public DbSet<Service> Services { get; set; } = null!;

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseInMemoryDatabase(Guid.NewGuid().ToString());
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<Service>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Name).IsRequired();
            entity.Property(e => e.Description).IsRequired();

            entity.HasOne(e => e.Endpoints)
                .WithOne(e => e.Parent)
                .HasForeignKey<Endpoints>("ServiceId")
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.UseCases)
                .WithOne(e => e.Parent)
                .HasForeignKey("ServiceId")
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Endpoints>(entity =>
        {
            entity.HasKey("ServiceId");

            entity.OwnsMany(e => e.EndpointList, endpoint =>
            {
                endpoint.Property(e => e.Id);
                endpoint.Property(e => e.Name).IsRequired();
                endpoint.Property(e => e.Description).IsRequired();
                endpoint.Property(e => e.Type).IsRequired();
            });
        });

        modelBuilder.Entity<UseCase>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Name).IsRequired();
            entity.Property(e => e.Description).IsRequired();

            entity.OwnsMany(e => e.Steps, step =>
            {
                step.Property(s => s.Name).IsRequired();
                step.Property(s => s.Description).IsRequired();
            });
        });
    }

    public async Task EnsureDataLoadedAsync(CancellationToken cancellationToken = default)
    {
        if (!_isDataLoaded)
        {
            await foreach (var service in EntityDriver.Load<Service>(_pathContext, cancellationToken))
            {
                Services.Add(service);
            }

            await base.SaveChangesAsync(cancellationToken);
            foreach (var entry in ChangeTracker.Entries())
            {
                entry.State = EntityState.Unchanged;
            }

            _isDataLoaded = true;
        }
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _fileSystemLock.WaitAsync(cancellationToken);
        try
        {
            ChangeTracker.DetectChanges();
            var entries = ChangeTracker.Entries().ToArray();
            MarkParentsWithOwnedChangesAsModified(entries);
            SyncDeletedOwnedEntitiesFromCollections(entries);
            await PersistChangesToFileSystem(entries, cancellationToken);
            return await base.SaveChangesAsync(cancellationToken);
        }
        finally
        {
            _fileSystemLock.Release();
        }
    }

    private void MarkParentsWithOwnedChangesAsModified(EntityEntry[] entries)
    {
        foreach (var entry in entries.Where(e => e.State is EntityState.Unchanged))
        {
            var ownedCollectionNames = EntityDriver.GetOwnedCollectionNames(entry.Entity);
            if (HasChangesInOwnedCollections(entry, ownedCollectionNames, entries))
            {
                entry.State = EntityState.Modified;
            }
        }
    }

    private bool HasChangesInOwnedCollections(EntityEntry parentEntry, IEnumerable<string> ownedCollectionNames, EntityEntry[] allEntries)
    {
        return ownedCollectionNames.Any(collectionName =>
        {
            var hasModifiedInCurrent = parentEntry
                .Collection(collectionName).CurrentValue
                ?.Cast<object>()
                .Any(item => Entry(item).State is not EntityState.Unchanged) is true;

            var hasAddedOrDeleted = allEntries.Any(e => IsOwnedEntityWithChanges(e, collectionName, parentEntry));
            return hasModifiedInCurrent || hasAddedOrDeleted;
        });
    }

    private static bool IsOwnedEntityWithChanges(EntityEntry ownedEntry, string expectedCollectionName, EntityEntry parentEntry)
    {

        return ownedEntry.State is EntityState.Added or EntityState.Deleted
            && ownedEntry.Metadata.IsOwned()
            && ownedEntry.Metadata.FindOwnership() is { } ownership
            && ownership.PrincipalToDependent?.Name == expectedCollectionName
            && ReferenceEquals(ownership.PrincipalToDependent.DeclaringEntityType.ClrType, parentEntry.Metadata.ClrType)
            && HasMatchingForeignKey(ownedEntry, parentEntry, ownership);
    }

    private static bool HasMatchingForeignKey(EntityEntry ownedEntry, EntityEntry parentEntry, IForeignKey ownership)
    {
        return ownership.PrincipalKey.Properties
            .Zip(ownership.Properties)
            .Select(item => (
                ParentKeyValue: parentEntry.Property(item.First.Name).CurrentValue,
                OwnedFkValue: ownedEntry.Property(item.Second.Name).CurrentValue
            ))
            .All(item => Equals(item.ParentKeyValue, item.OwnedFkValue));
    }

    private static void SyncDeletedOwnedEntitiesFromCollections(EntityEntry[] entries)
    {
        var deletedOwnedEntities = entries.Where(e => e.State == EntityState.Deleted && e.Metadata.IsOwned());
        foreach (var deletedEntry in deletedOwnedEntities)
        {
            if (deletedEntry.Metadata.FindOwnership() is { } ownership
                && FindParentEntry(deletedEntry, ownership, entries) is { } parentEntry)
            {
                RemoveFromParentCollection(deletedEntry, parentEntry, ownership);
            }
        }
    }

    private static EntityEntry? FindParentEntry(EntityEntry ownedEntry, IForeignKey ownership, EntityEntry[] allEntries)
    {
        var parentEntityType = ownership.PrincipalEntityType;
        var principalKey = ownership.PrincipalKey.Properties;
        var foreignKeyProperties = ownership.Properties;
        return allEntries.FirstOrDefault(e => e.Metadata.ClrType == parentEntityType.ClrType && principalKey
            .Zip(foreignKeyProperties)
            .All(pair => Equals(e.Property(pair.First.Name).CurrentValue, ownedEntry.Property(pair.Second.Name).CurrentValue))
        );
    }

    private static void RemoveFromParentCollection(EntityEntry deletedEntry, EntityEntry parentEntry, IForeignKey ownership)
    {
        if (ownership.PrincipalToDependent?.Name is { } collectionName
            && parentEntry.Collection(collectionName) is { } collectionEntry
            && collectionEntry.CurrentValue is System.Collections.IList list)
        {
            list.Remove(deletedEntry.Entity);
        }
    }

    private async Task PersistChangesToFileSystem(EntityEntry[] entries, CancellationToken cancellationToken)
    {
        foreach (var entry in entries)
        {
            await (entry.State switch
            {
                EntityState.Added or EntityState.Modified => EntityDriver.Write(_pathContext, entry.Entity, cancellationToken),
                EntityState.Deleted => EntityDriver.Remove(_pathContext, entry.Entity, cancellationToken),
                _ => Task.CompletedTask
            });
        }
    }
}
