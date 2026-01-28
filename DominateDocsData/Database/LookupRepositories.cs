using DominateDocsData.Database;
using DominateDocsData.Models;

namespace DominateDocsData.Database;

public interface ILenderRepository : IBaseRepository<Lender>
{ }

public interface IBorrowerRepository : IBaseRepository<Borrower>
{ }

public interface IGuarantorRepository : IBaseRepository<Guarantor>
{ }

public interface IBrokerRepository : IBaseRepository<Broker>
{ }

public interface IPropertyRecordRepository : IBaseRepository<PropertyRecord>
{ }

public class LenderRepository : BaseRepository<Lender>, ILenderRepository
{
    public LenderRepository(IMongoDatabaseRepo db) : base(db)
    {
    }
}

public class BorrowerRepository : BaseRepository<Borrower>, IBorrowerRepository
{
    public BorrowerRepository(IMongoDatabaseRepo db) : base(db)
    {
    }
}

public class GuarantorRepository : BaseRepository<Guarantor>, IGuarantorRepository
{
    public GuarantorRepository(IMongoDatabaseRepo db) : base(db)
    {
    }
}

public class BrokerRepository : BaseRepository<Broker>, IBrokerRepository
{
    public BrokerRepository(IMongoDatabaseRepo db) : base(db)
    {
    }
}

public class PropertyRecordRepository : BaseRepository<PropertyRecord>, IPropertyRecordRepository
{
    public PropertyRecordRepository(IMongoDatabaseRepo db) : base(db)
    {
    }
}