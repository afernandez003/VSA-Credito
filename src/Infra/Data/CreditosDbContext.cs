using Creditos.Infra.Data.Configurations;
using Microsoft.EntityFrameworkCore;

namespace Creditos.Infra.Data;

public sealed class CreditosDbContext(DbContextOptions<CreditosDbContext> options) : DbContext(options)
{
    public DbSet<Domain.Credito> Creditos => Set<Domain.Credito>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        _ = modelBuilder.ApplyConfiguration(new CreditoConfiguration());
    }
}
