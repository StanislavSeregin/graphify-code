using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace GraphifyCode.Data.Experiment;

public class GraphifyContext(string pathContext) : DbContext
{
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseInMemoryDatabase("Dummy");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // TODO: Implements
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // TODO: Implements

        return Task.FromResult(0);
    }
}
