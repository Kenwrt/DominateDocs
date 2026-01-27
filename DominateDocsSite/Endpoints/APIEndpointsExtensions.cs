//using Azure.Extensions.AspNetCore.Configuration.Secrets;
//using Azure.Identity;
//using Azure.Security.KeyVault.Secrets;

using DocumentManager.Services;
using DominateDocsData.Models;
using DominateDocsData.Database;
using Newtonsoft.Json;

namespace DominateDocsSite.Endpoints;

public static class ApiEndpointsExtensions
{
    private static string webPath = "";

    public static void AddApiEndpointsServices(this IServiceCollection services, IConfiguration config, Action<ApiConfigOptions>? options = null)
    {
        ApiConfigOptions configOptions = new ApiConfigOptions();

        if (options is null)
        {
            services.Configure<ApiConfigOptions>(options =>
            {
            });

            options?.Invoke(configOptions);
        }
        else
        {
            options?.Invoke(configOptions);

            services.Configure<ApiConfigOptions>(options);

            services.Configure<HostOptions>(x =>
            {
                x.ServicesStartConcurrently = true;
                x.ServicesStopConcurrently = true;
            });

            //var cred = new ClientSecretCredential(configOptions.TenantId, configOptions.ClientId, configOptions.ClientSecret);

            //var azureClient = new SecretClient(new Uri(configOptions.EndPoint), cred);

            //configManager.AddAzureKeyVault(azureClient, new AzureKeyVaultConfigurationOptions());

            //if (configOptions.LocalAuthenication)
            //{
            //    configOptions.ApiKey = configManager.GetValue<string>("ApiAzureKeyVault:DefaultKey");
            //}
            //else
            //{
            //    configOptions.ApiKey = configManager.GetValue<string>("InternalApiToken");
            //}

            webPath = Path.Combine(configOptions.WebPath, "Uploads");

            if (!Directory.Exists(webPath))
                Directory.CreateDirectory(webPath);
        }
    }

    public static void UseApiEndpoints(this IEndpointRouteBuilder app)
    {
        MapApiEndpoints(app.MapGroup("api/v1/"));
    }

    private static void MapApiEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/Test", () =>
        {
            return Results.Ok($"Ken's Test Message successfully");
        })
        .Produces(200)
        .Produces(404);

        app.MapGet("/ServiceControl/{action}", DominateDocsServiceControlAsync)
               .Produces<string>(200)
               .Produces(404);
        //.AddEndpointFilter<ApiEndpointFilter>();

        app.MapPost("/UploadWordDocumentBase64", UploadWordDocumentBase64Async)
            .Produces(200)
            .Produces(404);
        //.AddEndpointFilter<ApiEndpointFilter>();

        app.MapPost("/UploadDocument", async (HttpRequest request) =>
        {
            if (!request.HasFormContentType)
                return Results.BadRequest("Content-Type must be multipart/form-data");

            var form = await request.ReadFormAsync();
            var file = form.Files["file"];

            if (file == null || file.Length == 0)
                return Results.BadRequest("No file received");

            var savePath = Path.Combine(webPath, "UploadedDocs", file.FileName);
            Directory.CreateDirectory(Path.Combine(webPath, "UploadedDocs"));

            using var stream = File.Create(savePath);
            await file.CopyToAsync(stream);

            return Results.Ok($"File {file.FileName} uploaded successfully");
        })
        .Produces(200)
        .Produces(404);

        app.MapGet("/DownloadDocument/{documentId}", DownloadDocumentAsync)
        .Produces(200)
        .Produces(404);
        //.AddEndpointFilter<ApiEndpointFilter>();

        app.MapGet("/DownloadDocumentLink/{documentId}", DownloadDocumentLinkAsync)
        .Produces(200)
        .Produces(404);
        //.AddEndpointFilter<ApiEndpointFilter>();
    }

    internal static async Task<IResult> DominateDocsServiceControlAsync(string action)
    {
        try
        {
            if (action is not null && !string.IsNullOrWhiteSpace(action))
            {
                switch (action)
                {
                    //case nameof(CasEnums.SDMTasks.StopServices):

                    //    await casServices.StopServiceAsync("SDM", 5000);

                    //    logger.LogDebug("SDM Service Stopped");

                    //    break;

                    //case nameof(CasEnums.SDMTasks.StartServices):

                    //    await casServices.StartServiceAsync("SDM", 3000);

                    //    logger.LogDebug("SDM Service Started");
                    //    break;

                    //case nameof(CasEnums.SDMTasks.ReStartServices):

                    //    await casServices.ReStartServiceAsync("SDM", 5000);

                    //    logger.LogDebug("SDM Service Restart");
                    //    break;

                    default:

                        break;
                }
            }

            //var status = casServices.ServiceStatus("SDM");

            //await Task.Delay(2000);
            var result = true;
            return Results.Ok(result);
        }
        catch (System.Exception ex)
        {
            //logger.LogError($"{ex.Message}");

            return Results.BadRequest();
        }
    }

    internal static async Task<IResult> UploadWordDocumentBase64Async(UploadRecord record, IMongoDatabaseRepo dbApp, IWordServices wordServices)
    {
        try
        {
            if (record is null)
                return Results.BadRequest("No JASON Body");

            var bytes = Convert.FromBase64String(record.FileData);

            string path = Path.Combine(webPath, "TempStaging", record.FileName);

            if (!Directory.Exists(Path.Combine(webPath, "TempStaging"))) Directory.CreateDirectory(Path.Combine(webPath, "TempStaging"));

            if (!File.Exists(path)) File.Delete(path);

            var fs = new MemoryStream(bytes);

            await using var fileStream = new FileStream(path, FileMode.Create, FileAccess.Write);

            await fs.CopyToAsync(fileStream);

            // Optionally, reset position to the beginning
            fs.Position = 0; // Reset the stream position to the beginning

            DocumentTag documentTag = JsonConvert.DeserializeObject<DocumentTag>(await wordServices.RetrieveHiddenTagsAsync(fs, "DominateDocsTag"));

            if (documentTag is null) documentTag = JsonConvert.DeserializeObject<DocumentTag>(await wordServices.RetrieveHiddenTagsAsync(fs, "LiquidDocsTag"));

            if (documentTag is null) return Results.NotFound($"Document with ID {documentTag.DocumentId} not found.");

            Document docTemp = dbApp.GetRecordById<Document>(documentTag.DocumentId);

            if (docTemp is null) return Results.NotFound($"Document with ID {documentTag.DocumentId} not found.");

            DocumentStore docStore = dbApp.GetRecordById<DocumentStore>(docTemp.DocStoreId);

            docStore.DocumentBytes = bytes;

            docStore.UpdatedAt = System.DateTime.UtcNow;

            dbApp.UpSertRecord<DocumentStore>(docStore);

            docTemp.UpdatedAt = System.DateTime.UtcNow;

            dbApp.UpSertRecord<Document>(docTemp);

            return Results.Ok($"File {record.FileName} uploaded successfully");
        }
        catch (System.Exception)
        {
            return Results.BadRequest();
        }
    }

    internal static async Task<IResult> DownloadDocumentAsync(Guid documentId, IMongoDatabaseRepo dbApp)
    {
        try
        {
           
            // Await the task to get the result of the asynchronous operation
            var documentRecord = dbApp.GetRecordById<Document>(documentId);

            DocumentStore documentStore = dbApp.GetRecordById<DocumentStore>(documentRecord.DocStoreId);
            


            string fileName = $"{documentRecord.Name}--{documentId.ToString().Substring(0, 12)}--{System.DateTime.UtcNow.ToString("MM-dd-yyyy-HH-MM")}.docm";

            byte[] documentBytes = documentStore.DocumentBytes;

            if (documentBytes == null)
                return Results.NotFound();

            var fileStream = new MemoryStream(documentBytes);
            string contentType = "application/octet-stream"; // or more specific
            return Results.File(fileStream, contentType, fileDownloadName: fileName);
        }
        catch (System.Exception)
        {
            return Results.BadRequest();
        }
    }

    internal static async Task<IResult> DownloadDocumentLinkAsync(Guid documentId, IMongoDatabaseRepo dbApp, IWebHostEnvironment env)
    {
        try
        {
            // Await the task to get the result of the asynchronous operation
            var documentRecord = dbApp.GetRecordById<Document>(documentId);

            string fileName = $"{documentId.ToString()}.docm";

            DocumentStore documentStore = dbApp.GetRecordById<DocumentStore>(documentRecord.DocStoreId);

            byte[] documentBytes = documentStore.DocumentBytes;

            if (documentBytes == null)
                return Results.NotFound();

            string path = Path.Combine(webPath, "TempStaging", fileName);

            var fs = new MemoryStream(documentBytes);

            await using var fileStream = new FileStream(path, FileMode.Create, FileAccess.Write);
            await fs.CopyToAsync(fileStream);

            //var fileStream = new MemoryStream(documentBytes);
            //string contentType = "application/octet-stream"; // or more specific
            //return Results.File(fileStream, contentType, fileDownloadName: fileName);
        }
        catch (System.Exception)
        {
            return Results.BadRequest();
        }

        return Results.Ok();
    }
}

public class ApiConfigOptions
{
    public string ApplicationName { get; set; }
    public string SqlConString { get; set; }
    public string MongoConString { get; set; }
    public string WebPath { get; set; }
    public string ApiKey { get; set; }
}