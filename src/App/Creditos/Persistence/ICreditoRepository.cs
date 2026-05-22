namespace Creditos;

public interface ICreditoRepository
{
    Task<bool> ExistsByNumeroCreditoAsync(string numeroCredito, CancellationToken ct = default);
    Task AddAsync(Domain.Credito credito, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Domain.Credito>> GetByNumeroNfseAsync(string numeroNfse, CancellationToken ct = default);
    Task<Domain.Credito?> GetByNumeroCreditoAsync(string numeroCredito, CancellationToken ct = default);
}
