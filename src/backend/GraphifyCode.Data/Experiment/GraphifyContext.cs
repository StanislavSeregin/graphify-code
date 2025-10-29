using GraphifyCode.Data.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System;
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
            await foreach (var service in EntityLoader.Load<Service>(_pathContext, cancellationToken))
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
            foreach (var entry in ChangeTracker.Entries())
            {
                await (entry.State switch
                {
                    EntityState.Added or EntityState.Modified => EntityLoader.Write(_pathContext, entry.Entity, cancellationToken),
                    EntityState.Deleted => EntityLoader.Remove(_pathContext, entry.Entity, cancellationToken),
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
