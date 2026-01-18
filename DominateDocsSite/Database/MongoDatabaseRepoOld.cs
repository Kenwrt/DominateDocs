using DocumentFormat.OpenXml.Office2010.Excel;
using LiquidDocsSite.Helpers;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using System.Reflection;

namespace LiquidDocsSite.Database;

public class MongoDatabaseRepo : IMongoDatabaseRepo
{
    private ILogger<MongoDatabaseRepo> logger;
    private IConfiguration config;
    private IMongoDatabase mongoConn;
    private IEncryptAes aes;
    private IMongoClient client;

    public MongoDatabaseRepo(ILogger<MongoDatabaseRepo> logger, IConfiguration config, IEncryptAes aes, IMongoClient client)
    {
        this.logger = logger;
        this.config = config;
        this.aes = aes;
        this.client = client;

        mongoConn = client.GetDatabase("LiquidDocsSite");
    }

    private void GetConnection()
    {
        try
        {
            var atlas = aes.Decrypt(config.GetConnectionString("AtlasMongoConnection"));
            var app = aes.Decrypt(config.GetConnectionString("ApplicationMongoConnection"));

            var connString = !string.IsNullOrWhiteSpace(atlas) ? atlas : app;

            if (string.IsNullOrWhiteSpace(connString))
                throw new InvalidOperationException("No Mongo connection string found. Set AtlasMongoConnection or ApplicationMongoConnection.");

            const string databaseName = "LiquidDocsSite";

            BsonSerializer.RegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));

            var client = new MongoClient(connString);
            mongoConn = client.GetDatabase(databaseName);

            logger.LogInformation("Connected to Mongo database {db}", databaseName);

        }
        catch (Exception ex)
        {

            logger.LogError(ex.Message);
        }


    }

    public T GetRecordById<T>(Guid id) where T : class
    {
        T record = default(T);

        try
        {
            string table = typeof(T).Name;

            var collection = mongoConn.GetCollection<T>(table);

            var filter = Builders<T>.Filter.Eq(
            "_id",
            new MongoDB.Bson.BsonBinaryData(id, GuidRepresentation.Standard));
            var doc = collection.Find(filter).FirstOrDefault();

            if (doc == null)
            {
                // fall back to "Id" for old docs
                filter = Builders<T>.Filter.Eq(
                    "Id",
                    new MongoDB.Bson.BsonBinaryData(id, GuidRepresentation.Standard)
                );
                doc = collection.Find(filter).FirstOrDefault();
            }

            record = collection.Find(filter).FirstOrDefault();
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message);
        }

        return record;
    }

    public async Task<T> GetRecordByUserNameAsync<T>(string userName) where T : class
    {
        T record = default(T);

        try
        {
            string table = typeof(T).Name;

            var collection = mongoConn.GetCollection<T>(table);

            var filter = Builders<T>.Filter.Or(Builders<T>.Filter.Eq("UserName", userName), Builders<T>.Filter.Eq("userName", userName));

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

        return resultList ?? Enumerable.Empty<T>();
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

    private object? GetIdPropertyValue(object obj)
    {
        if (obj == null) return null;

        var idProperty = obj.GetType().GetProperty("Id");
        if (idProperty == null) return null;

        return idProperty.GetValue(obj);
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

    public T UpSertRecord<T>(T record) where T : class
    {
        try
        {
            var id = GetIdPropertyValue(record);
            if (id is not Guid guidId || guidId == Guid.Empty)
                throw new InvalidOperationException("Record must have a non-empty Guid Id.");

            var collection = mongoConn.GetCollection<T>(typeof(T).Name);

            var filter = Builders<T>.Filter.Eq(
             "_id",
             new BsonBinaryData(guidId, GuidRepresentation.Standard)
            );

            // fallback to "Id" if the document was saved that way
            var doc = collection.Find(filter).FirstOrDefault();
            if (doc == null)
            {
                filter = Builders<T>.Filter.Eq(
                    "Id",
                    new BsonBinaryData(guidId, GuidRepresentation.Standard)
                );
            }

            collection.ReplaceOne(filter, record, new ReplaceOptions { IsUpsert = true });

            return record;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Upsert failed");
            return record;
        }
    }

    public async Task<T> UpSertRecordAsync<T>(T record) where T : class
    {
        try
        {
            var id = GetIdPropertyValue(record);
            if (id is not Guid guidId || guidId == Guid.Empty)
                throw new InvalidOperationException("Record must have a non-empty Guid Id.");

            var collection = mongoConn.GetCollection<T>(typeof(T).Name);

            var filter = Builders<T>.Filter.Eq(
             "_id",
             new BsonBinaryData(guidId, GuidRepresentation.Standard)
            );

            // fallback to "Id" if the document was saved that way
            var doc = collection.Find(filter).FirstOrDefault();
            if (doc == null)
            {
                filter = Builders<T>.Filter.Eq(
                    "Id",
                    new BsonBinaryData(guidId, GuidRepresentation.Standard)
                );
            }

            collection.ReplaceOne(filter, record, new ReplaceOptions { IsUpsert = true });

            return record;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Upsert failed");
            return record;
        }
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

    public void DeleteRecord<T>(T record) where T : class
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

            var filter = Builders<T>.Filter.Eq(
            "_id",
            new MongoDB.Bson.BsonBinaryData(id, GuidRepresentation.Standard));
            var doc = collection.Find(filter).FirstOrDefault();

            if (doc == null)
            {
                // fall back to "Id" for old docs
                filter = Builders<T>.Filter.Eq(
                    "Id",
                    new MongoDB.Bson.BsonBinaryData(id, GuidRepresentation.Standard)
                );
                doc = collection.Find(filter).FirstOrDefault();
            }

            collection.DeleteOne(filter);
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message);
        }
    }

    public bool IfRecordExists<T>(Guid id) where T : class
    {
        bool exists = false;

        try
        {
            string table = typeof(T).Name;

            var collection = mongoConn.GetCollection<T>(table);

            var filter = Builders<T>.Filter.Eq(
           "_id",
           new MongoDB.Bson.BsonBinaryData(id, GuidRepresentation.Standard));
            var doc = collection.Find(filter).FirstOrDefault();

            if (doc == null)
            {
                // fall back to "Id" for old docs
                filter = Builders<T>.Filter.Eq(
                    "Id",
                    new MongoDB.Bson.BsonBinaryData(id, GuidRepresentation.Standard)
                );
                doc = collection.Find(filter).FirstOrDefault();
            }

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
}
