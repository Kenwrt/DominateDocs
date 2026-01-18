namespace LiquidDocsSite.Components.Pages.FluentValidation;

public interface IDocumentSetRepository
{
    Task<bool> ExistsByNameAsync(Guid userId, string name, CancellationToken ct);
}