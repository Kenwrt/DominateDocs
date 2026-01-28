using DominateDocsData.Models;

namespace DominateDocsData.Database;

public interface IDocumentLibraryRepository : IBaseRepository<DocumentLibrary>
{
    Task<IEnumerable<DocumentLibrary>> GetForLoanAsync(Guid loanApplicationId);
}

public class DocumentLibraryRepository : BaseRepository<DocumentLibrary>, IDocumentLibraryRepository
{
    public DocumentLibraryRepository(IMongoDatabaseRepo db) : base(db)
    {
    }

    public async Task<IEnumerable<DocumentLibrary>> GetForLoanAsync(Guid loanApplicationId)
    {
        var all = await Db.GetRecordsAsync<DocumentLibrary>();
        return all;
    }
}