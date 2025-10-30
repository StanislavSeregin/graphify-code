using GraphifyCode.Data.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GraphifyCode.Data.Experiment;

public class GraphifyContext(IOptions<MarkdownStorageSettings> settings) : DbContext
{
    private static readonly SemaphoreSlim _fileSystemLock = new(1, 1);
    private readonly string _pathContext = settings.Value.Path;
    private bool _isDataLoaded = false;

    public DbSet<Service> Services { get; set; }

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
            var hasChangesInOwnedCollections = EntityDriver
                .GetOwnedCollectionNames(entry.Entity)
                .Any(collectionName =>
                {
                    var hasModifiedInCurrent = entry
                        .Collection(collectionName).CurrentValue
                        ?.Cast<object>()
                        .Any(item => Entry(item).State is not EntityState.Unchanged) is true;

                    var hasAddedOrDeleted = entries.Any(e => e.State is EntityState.Added or EntityState.Deleted
                        && e.Metadata.IsOwned()
                        && e.Metadata.FindOwnership() is { } ownership
                        && ownership.PrincipalToDependent?.Name == collectionName
                        && ReferenceEquals(ownership.PrincipalToDependent.DeclaringEntityType.ClrType, entry.Metadata.ClrType)
                        && ownership.PrincipalKey.Properties
                            .Zip(ownership.Properties)
                            .Select(item => (
                                ParentKeyValue: entry.Property(item.First.Name).CurrentValue,
                                OwnedFkValue: e.Property(item.Second.Name).CurrentValue
                            ))
                            .All(item => Equals(item.ParentKeyValue, item.OwnedFkValue)));

                    return hasModifiedInCurrent || hasAddedOrDeleted;
                });

            if (hasChangesInOwnedCollections)
            {
                entry.State = EntityState.Modified;
            }
        }
    }

    private static void SyncDeletedOwnedEntitiesFromCollections(EntityEntry[] entries)
    {
        foreach (var deletedEntry in entries.Where(e => e.State == EntityState.Deleted && e.Metadata.IsOwned()))
        {
            if (deletedEntry.Metadata.FindOwnership() is { } ownership)
            {
                var parentEntry = entries
                    .Where(e => e.Metadata.ClrType == ownership.PrincipalEntityType.ClrType && ownership.PrincipalKey.Properties
                        .Zip(ownership.Properties)
                        .All(pair => Equals(e.Property(pair.First.Name).CurrentValue, deletedEntry.Property(pair.Second.Name).CurrentValue)))
                    .FirstOrDefault();

                if (parentEntry is not null
                    && ownership.PrincipalToDependent?.Name is { } collectionName
                    && parentEntry.Collection(collectionName) is { } collectionEntry
                    && collectionEntry.CurrentValue is System.Collections.IList list)
                {
                    list.Remove(deletedEntry.Entity);
                }
            }
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
