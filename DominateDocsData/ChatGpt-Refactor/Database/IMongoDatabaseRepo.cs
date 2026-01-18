
namespace LiquidDocsSite.Database;

public interface IMongoDatabaseRepo
{
    void DeleteRecord<T>(T record) where T : class;
    void DropCollection<T>(T record) where T : class;
    Task DropCollectionAsync<T>();
    T GetRecordById<T>(Guid id) where T : class;
    Task<T> GetRecordByUserNameAsync<T>(string userName) where T : class;
    IEnumerable<T> GetRecords<T>() where T : class;
    Task<IEnumerable<T>> GetRecordsAsync<T>() where T : class;
    bool IfRecordExists<T>(Guid id) where T : class;
    T UpSertRecord<T>(T record) where T : class;
    Task<T> UpSertRecordAsync<T>(T record) where T : class;
}