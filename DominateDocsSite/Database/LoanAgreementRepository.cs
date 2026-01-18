using DominateDocsData.Models;

namespace DominateDocsSite.Database;

public interface ILoanAgreementRepository : IBaseRepository<LoanAgreementDocument>
{
    Task<LoanAgreementDocument?> GetByLoanNumberAsync(string loanNumber);
}

public class LoanAgreementRepository : BaseRepository<LoanAgreementDocument>, ILoanAgreementRepository
{
    public LoanAgreementRepository(IMongoDatabaseRepo db) : base(db)
    {
    }

    public override async Task<LoanAgreementDocument> UpsertAsync(LoanAgreementDocument entity)
    {
        if (entity.Id == Guid.Empty)
            entity.Id = Guid.NewGuid();

        if (entity.CreatedAtUtc == default)
            entity.CreatedAtUtc = DateTime.UtcNow;

        entity.LastUpdatedAtUtc = DateTime.UtcNow;

        if (string.IsNullOrWhiteSpace(entity.LoanNumber) && entity.LoanAgreement != null)
        {
            entity.LoanNumber = entity.LoanAgreement.LoanNumber;
        }

        return await base.UpsertAsync(entity);
    }

    public async Task<LoanAgreementDocument?> GetByLoanNumberAsync(string loanNumber)
    {
        var all = await Db.GetRecordsAsync<LoanAgreementDocument>();
        return all.FirstOrDefault(x => x.LoanNumber == loanNumber);
    }
}