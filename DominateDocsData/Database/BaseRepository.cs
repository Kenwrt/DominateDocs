namespace DominateDocsData.Database;

public interface IBaseRepository<T> where T : class
{
    Task<T> UpsertAsync(T entity);

    Task<T?> GetByIdAsync(Guid id);

    Task<IEnumerable<T>> GetAllAsync();

    Task<IEnumerable<T>> GetByUserAsync(Guid userId);

    Task<bool> DeleteAsync(Guid id);
}

public abstract class BaseRepository<T> : IBaseRepository<T> where T : class
{
    protected readonly IMongoDatabaseRepo Db;

    protected BaseRepository(IMongoDatabaseRepo db)
    {
        Db = db;
    }

    public virtual async Task<T> UpsertAsync(T entity)
    {
        var result = await Db.UpSertRecordAsync(entity);
        return result;
    }

    public virtual async Task<T?> GetByIdAsync(Guid id)
    {
        var result = Db.GetRecordById<T>(id);
        return await Task.FromResult(result);
    }

    public virtual async Task<IEnumerable<T>> GetAllAsync()
    {
        var results = await Db.GetRecordsAsync<T>();
        return results;
    }

    public virtual async Task<IEnumerable<T>> GetByUserAsync(Guid userId)
    {
        var results = await Db.GetRecordsAsync<T>();
        var prop = typeof(T).GetProperty("UserId");
        if (prop == null || prop.PropertyType != typeof(Guid))
            return results;

        return results.Where(x =>
        {
            var value = prop.GetValue(x);
            return value is Guid g && g == userId;
        });
    }

    public virtual async Task<bool> DeleteAsync(Guid id)
    {
        var existing = Db.GetRecordById<T>(id);
        if (existing is null)
            return await Task.FromResult(false);

        Db.DeleteRecord(existing);
        return await Task.FromResult(true);
    }
}