using Creditos.Infra.Data;
using Microsoft.EntityFrameworkCore;

namespace Creditos.Infra.Persistence;

public sealed class EfCoreCreditoRepository(CreditosDbContext context) : ICreditoRepository
{
    private static readonly Func<CreditosDbContext, string, Task<bool>> ExistsCompiled =
        EF.CompileAsyncQuery((CreditosDbContext db, string numeroCredito) =>
            db.Set<Domain.Credito>().Any(c => c.NumeroCredito == numeroCredito));

    private static readonly Func<CreditosDbContext, string, Task<Domain.Credito?>> GetByNumeroCreditoCompiled =
        EF.CompileAsyncQuery((CreditosDbContext db, string numeroCredito) =>
            db.Set<Domain.Credito>().FirstOrDefault(c => c.NumeroCredito == numeroCredito));

    public Task<bool> ExistsByNumeroCreditoAsync(string numeroCredito, CancellationToken ct = default)
        => ExistsCompiled(context, numeroCredito);

    public async Task AddAsync(Domain.Credito credito, CancellationToken ct = default)
        => await context.Creditos.AddAsync(credito, ct);

    public Task SaveChangesAsync(CancellationToken ct = default)
        => context.SaveChangesAsync(ct);

    public async Task<IReadOnlyList<Domain.Credito>> GetByNumeroNfseAsync(string numeroNfse, CancellationToken ct = default)
        => await context.Creditos
            .AsNoTracking()
            .Where(c => c.NumeroNfse == numeroNfse)
            .OrderBy(c => c.DataConstituicao)
            .ThenBy(c => c.NumeroCredito)
            .ToListAsync(ct);

    public Task<Domain.Credito?> GetByNumeroCreditoAsync(string numeroCredito, CancellationToken ct = default)
        => GetByNumeroCreditoCompiled(context, numeroCredito);
}
