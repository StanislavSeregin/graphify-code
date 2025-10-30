using GraphifyCode.Data.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System;
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
            var entries = ChangeTracker.Entries();
            foreach (var entry in entries.Where(e => e.State is EntityState.Unchanged))
            {
                var ownedCollectionNames = EntityDriver.GetOwnedCollectionNames(entry.Entity);
                var shouldMarkedAsModified = ownedCollectionNames.Any(collectionName => entry
                    .Collection(collectionName).CurrentValue
                    ?.Cast<object>()
                    .Any(item => Entry(item).State is not EntityState.Unchanged) is true);

                if (shouldMarkedAsModified)
                {
                    entry.State = EntityState.Modified;
                }
            }

            foreach (var entry in entries)
            {
                await (entry.State switch
                {
                    EntityState.Added or EntityState.Modified => EntityDriver.Write(_pathContext, entry.Entity, cancellationToken),
                    EntityState.Deleted => EntityDriver.Remove(_pathContext, entry.Entity, cancellationToken),
                    _ => Task.CompletedTask
                });
            }

            return await base.SaveChangesAsync(cancellationToken);
        }
        finally
        {
            _fileSystemLock.Release();
        }
    }
}
