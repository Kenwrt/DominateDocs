using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;

namespace DocumentManager.Database;

public class MongoDatabaseRepoDocuments : IMongoDatabaseRepoDocuments
{
    private ILogger<MongoDatabaseRepoDocuments> logger;
    private IOptions<DocumentManagerConfigOptions> options;
    private IMongoDatabase mongoConn;

    public MongoDatabaseRepoDocuments(ILogger<MongoDatabaseRepoDocuments> logger, IOptions<DocumentManagerConfigOptions> options)
    {
        this.logger = logger;
        this.options = options;

        GetConnection();
    }

    private void GetConnection()
    {
        if (!String.IsNullOrEmpty(options.Value.DbConnectionString))
        {
            var databaseName = options.Value.DbName;

            BsonSerializer.RegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));

            var client = new MongoClient(options.Value.DbConnectionString);

            if (client != null)
            {
                mongoConn = client.GetDatabase(databaseName);
            }
        }
    }

    public T GetRecordById<T>(Guid id) where T : class
    {
        T record = default(T);

        try
        {
            string table = typeof(T).Name;

            var collection = mongoConn.GetCollection<T>(table);

            var filter = Builders<T>.Filter.Eq("Id", id);

            record = collection.Find(filter).FirstOrDefault();
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message);
        }

        return record;
    }

    public IEnumerable<T> GetRecords<T>() where T : class
    {
        IEnumerable<T> resultList = null;

        try
        {
            string collectionName = typeof(T).Name;
            var collection = mongoConn.GetCollection<T>(collectionName);

            resultList = collection.Find<T>(new BsonDocument()).ToEnumerable();
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message);
        }

        return resultList;
    }

    public async Task DropCollectionAsync<T>()
    {
        try
        {
            string collectionName = typeof(T).Name;
            Type objType = typeof(T);

            var collection = mongoConn.GetCollection<T>(collectionName);

            await mongoConn.DropCollectionAsync(collectionName);
        }
        catch (SystemException ex)
        {
            logger.LogError(ex.Message);
        }
    }

    public async Task<IEnumerable<T>> GetRecordsAsync<T>() where T : class
    {
        IEnumerable<T> resultList = null;

        try
        {
            string collectionName = typeof(T).Name;

            var collection = mongoConn.GetCollection<T>(collectionName);

            resultList = collection.FindAsync<T>(new BsonDocument()).Result.ToEnumerable();
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message);
        }

        return resultList;
    }

    public T UpSertRecord<T>(T record) where T : class
    {
        Guid guidId = Guid.Empty;

        try
        {
            string table = typeof(T).Name;
            Type objType = typeof(T);

            // Safely retrieve the Id property value and handle null cases
            var idValue = GetIdPropertyValue(record);
            if (idValue is Guid id)
            {
                guidId = id;
            }
            else
            {
                throw new InvalidOperationException("The Id property is either null or not of type Guid.");
            }

            var collection = mongoConn.GetCollection<T>(table);

            var result = collection.ReplaceOne(Builders<T>.Filter.Eq("Id", guidId), record, new ReplaceOptions { IsUpsert = true });
        }
        catch (SystemException ex)
        {
            logger.LogError(ex.Message);
        }

        return record;
    }

    public async Task<T> UpSertRecordAsync<T>(T record) where T : class
    {
        Guid guidId = Guid.Empty;

        try
        {
            string table = typeof(T).Name;
            Type objType = typeof(T);

            // Safely retrieve the Id property value and handle null cases
            var idValue = GetIdPropertyValue(record);
            if (idValue is Guid id)
            {
                guidId = id;
            }
            else
            {
                throw new InvalidOperationException("The Id property is either null or not of type Guid.");
            }

            var collection = mongoConn.GetCollection<T>(table);

            await collection.ReplaceOneAsync(Builders<T>.Filter.Eq("Id", guidId), record, new ReplaceOptions { IsUpsert = true });
        }
        catch (SystemException ex)
        {
            logger.LogError(ex.Message);
        }

        return record;
    }

    public void DropCollection<T>(T record) where T : class
    {
        try
        {
            string collectionName = typeof(T).Name;
            Type objType = typeof(T);

            var collection = mongoConn.GetCollection<T>(collectionName);

            mongoConn.DropCollection(collectionName);
        }
        catch (SystemException ex)
        {
            logger.LogError(ex.Message);
        }
    }

    public bool DeleteRecord<T>(T record) where T : class
    {
        Guid guidId = Guid.Empty;
        bool sucessful = false;

        try
        {
            string collectionName = typeof(T).Name;
            Type objType = typeof(T);

            // Safely retrieve the Id property value and handle null cases
            var idValue = GetIdPropertyValue(record);
            if (idValue is Guid id)
            {
                guidId = id;
            }
            else
            {
                throw new InvalidOperationException("The Id property is either null or not of type Guid.");
            }

            var collection = mongoConn.GetCollection<T>(collectionName);

            var filter = Builders<T>.Filter.Eq("Id", guidId);
            collection.DeleteOne(filter);

            sucessful = true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message);
        }

        return sucessful;
    }

    public bool IfRecordExists<T>(Guid id) where T : class
    {
        Guid guidId = Guid.Empty;
        bool exists = false;

        try
        {
            string table = typeof(T).Name;

            var collection = mongoConn.GetCollection<T>(table);

            var filter = Builders<T>.Filter.Eq("Id", id);

            long recordCount = collection.Count(filter);

            if (recordCount > 0)
            {
                exists = true;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message);
        }

        return exists;
    }

    private Guid? GetGuidIdValue(object obj)
    {
        var value = GetIdPropertyValue(obj);

        if (value is Guid guidValue)
            return guidValue;

        if (value is string str && Guid.TryParse(str, out var parsedGuid))
            return parsedGuid;

        return null;
    }

    private object? GetIdPropertyValue(object obj)
    {
        if (obj == null) return null;

        var idProperty = obj.GetType().GetProperty("Id");
        if (idProperty == null) return null;

        return idProperty.GetValue(obj);
    }
}