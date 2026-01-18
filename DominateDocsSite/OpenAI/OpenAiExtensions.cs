using DominateDocsSite.OpenAI.Services;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DominateDocsSite.OpenAI;

public static class OpenAiExtensions
{
    public static void AddOpenAiServices(this IServiceCollection services, IConfiguration config, Action<OpenApiConfigOptions>? options = null)
    {
        OpenApiConfigOptions configOptions = new OpenApiConfigOptions();

        if (options is null)
        {
            services.Configure<OpenApiConfigOptions>(options =>
            {
            });

            options?.Invoke(configOptions);
        }
        else
        {
            options?.Invoke(configOptions);

            services.Configure<OpenApiConfigOptions>(options);

            services.TryAddSingleton<IChatGptService, ChatGptService>();

            //services.Configure<HostOptions>(x =>
            //{
            //    x.ServicesStartConcurrently = true;
            //    x.ServicesStopConcurrently = true;
            //});

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
        }
    }

    //public static void UseApiEndpoints(this IEndpointRouteBuilder app)
    //{
    //    MapApiEndpoints(app.MapGroup("api/v1/"));
    //}

    //private static void MapApiEndpoints(this IEndpointRouteBuilder app)
    //{
    //    app.MapGet("/Test", () =>
    //    {
    //        return Results.Ok($"Ken's Test Message successfully");
    //    })
    //    .Produces(200)
    //    .Produces(404);

    //    //app.MapPost("/auth/login", async (DominateDocsData.Models.UserProfile dto, SignInManager<IdentityUser> signInManager, UserManager<IdentityUser> userManager, IDataProtectionProvider dataProtectionProvider, UserSession Session, HttpContextAccessor httpContextA) =>
    //    //{
    //    //    try
    //    //    {
    //    //        var user = await userManager.FindByNameAsync(dto.UserName);
    //    //        if (user is null || !await userManager.CheckPasswordAsync(user, dto.Password))
    //    //            return Results.Unauthorized();

    //    //        // This writes the auth cookie on the HTTP response
    //    //        await signInManager.SignInAsync(
    //    //            user,
    //    //            new AuthenticationProperties
    //    //            {
    //    //                IsPersistent = true,
    //    //                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(2)
    //    //            });

    //    //        // Create a session payload and protect it (encrypt/sign)
    //    //        var protector = dataProtectionProvider.CreateProtector("DominateDocs.SessionCookie.v1");
    //    //        var payload = new UserSession
    //    //        {
    //    //            UserName = user.UserName!,
    //    //            SessionId = Guid.NewGuid().ToString("N"),
    //    //            ExpUtc = DateTimeOffset.UtcNow.AddHours(2)
    //    //        };
    //    //        var json = System.Text.Json.JsonSerializer.Serialize(payload);
    //    //        var protectedValue = protector.Protect(json);

    //    //        Session.SessionId = payload.SessionId;
    //    //        Session.UserName = payload.UserName;

    //    //        var cookieOptions = new CookieOptions
    //    //        {
    //    //            HttpOnly = true,             // not readable from JS
    //    //            Secure = true,               // HTTPS only in production
    //    //            SameSite = SameSiteMode.Lax, // adjust if you need cross-site
    //    //            Path = "/",
    //    //            MaxAge = TimeSpan.FromHours(2) // or Expires = payload.ExpUtc
    //    //        };

    //    //        httpContextA.HttpContext!.Response.Cookies.Append(".DominateDocs.Session", protectedValue, cookieOptions);

    //    //    }
    //    //    catch (Exception ex)
    //    //    {
    //    //        throw;
    //    //    }

    //    //    return Results.Ok(new { ok = true });

    //    //}).Produces<string>(200);

    //    //.AddEndpointFilter<ApiEndpointFilter>();

    //    app.MapGet("/ServiceControl/{action}", DominateDocsServiceControlAsync)
    //           .Produces<string>(200)
    //           .Produces(404);
    //    //.AddEndpointFilter<ApiEndpointFilter>();

    //    app.MapPost("/UploadWordDocumentBase64", UploadWordDocumentBase64Async)
    //        .Produces(200)
    //        .Produces(404);
    //    //.AddEndpointFilter<ApiEndpointFilter>();

    //    app.MapPost("/UploadDocument", async (HttpRequest request) =>
    //    {
    //        if (!request.HasFormContentType)
    //            return Results.BadRequest("Content-Type must be multipart/form-data");

    //        var form = await request.ReadFormAsync();
    //        var file = form.Files["file"];

    //        if (file == null || file.Length == 0)
    //            return Results.BadRequest("No file received");

    //        var savePath = Path.Combine("UploadedDocs", file.FileName);
    //        Directory.CreateDirectory("UploadedDocs");

    //        using var stream = File.Create(savePath);
    //        await file.CopyToAsync(stream);

    //        return Results.Ok($"File {file.FileName} uploaded successfully");
    //    })
    //    .Produces(200)
    //    .Produces(404);
    //    //.AddEndpointFilter<ApiEndpointFilter>();

    //    //app.MapPost("/UploadDocument", async (HttpRequest request) =>
    //    //{
    //    //    if (!request.HasFormContentType)
    //    //        return Results.BadRequest("Form content type required.");

    //    //    var form = await request.ReadFormAsync();
    //    //    //var file = form.Files.GetFile("file");
    //    //    var file = form.Files.GetFile("file");
    //    //    if (file == null)
    //    //        return Results.BadRequest("No file uploaded.");

    //    //    var filePath = Path.Combine(webPath, Path.GetFileName(file.FileName));

    //    //    using (var stream = System.IO.File.Create(filePath))
    //    //    {
    //    //        await file.CopyToAsync(stream);
    //    //    }

    //    //    return Results.Ok(new { fileName = file.FileName, url = $"/DownloadDocument/{Uri.EscapeDataString(file.FileName)}" });
    //    //})
    //    //.Accepts<IFormFile>("multipart/form-data")
    //    //.Produces(200)
    //    //.Produces(400)
    //    //.AddEndpointFilter<ApiEndpointFilter>();

    //    app.MapGet("/DownloadDocument/{documentId}", DownloadDocumentAsync)
    //    .Produces(200)
    //    .Produces(404);
    //    //.AddEndpointFilter<ApiEndpointFilter>();

    //    app.MapGet("/DownloadDocumentLink/{documentId}", DownloadDocumentLinkAsync)
    //    .Produces(200)
    //    .Produces(404);
    //    //.AddEndpointFilter<ApiEndpointFilter>();
    //}

    //internal static async Task<IResult> DominateDocsServiceControlAsync(string action)
    //{
    //    try
    //    {
    //        if (action is not null && !string.IsNullOrWhiteSpace(action))
    //        {
    //            switch (action)
    //            {
    //                //case nameof(CasEnums.SDMTasks.StopServices):

    //                //    await casServices.StopServiceAsync("SDM", 5000);

    //                //    logger.LogDebug("SDM Service Stopped");

    //                //    break;

    //                //case nameof(CasEnums.SDMTasks.StartServices):

    //                //    await casServices.StartServiceAsync("SDM", 3000);

    //                //    logger.LogDebug("SDM Service Started");
    //                //    break;

    //                //case nameof(CasEnums.SDMTasks.ReStartServices):

    //                //    await casServices.ReStartServiceAsync("SDM", 5000);

    //                //    logger.LogDebug("SDM Service Restart");
    //                //    break;

    //                default:

    //                    break;
    //            }
    //        }

    //        //var status = casServices.ServiceStatus("SDM");

    //        //await Task.Delay(2000);
    //        var result = true;
    //        return Results.Ok(result);
    //    }
    //    catch (System.Exception ex)
    //    {
    //        //logger.LogError($"{ex.Message}");

    //        return Results.BadRequest();
    //    }
    //}

    //internal static async Task<IResult> UploadWordDocumentBase64Async(UploadRecord record, IMongoDatabaseRepo dbApp, IWebHostEnvironment env, IWordServices wordServices)
    //{
    //    try
    //    {
    //        if (record is null)
    //            return Results.BadRequest("No JASON Body");

    //        var bytes = Convert.FromBase64String(record.FileData);

    //        string path = Path.Combine(env.WebRootPath, "TempStaging", record.FileName);

    //        var fs = new MemoryStream(bytes);

    //        await using var fileStream = new FileStream(path, FileMode.Create, FileAccess.Write);

    //        await fs.CopyToAsync(fileStream);

    //        // Optionally, reset position to the beginning
    //        fs.Position = 0; // Reset the stream position to the beginning

    //        DocumentTag documentTag = JsonConvert.DeserializeObject<DocumentTag>(await wordServices.RetrieveHiddenTagsAsync(fs, "DominateDocsTag"));

    //        if (documentTag is null)
    //            return Results.NotFound($"Document with ID {documentTag.DocumentId} not found.");

    //        DocumentTemplate docTemp = dbApp.GetRecordById<DocumentTemplate>(documentTag.DocumentId);

    //        if (docTemp is null)
    //            return Results.NotFound($"Document with ID {documentTag.DocumentId} not found.");

    //        docTemp.TemplateBytes = bytes;
    //        docTemp.UpdatedAt = System.DateTime.UtcNow;

    //        dbApp.UpSertRecord<DocumentTemplate>(docTemp);

    //        return Results.Ok($"File {record.FileName} uploaded successfully");
    //    }
    //    catch (System.Exception)
    //    {
    //        return Results.BadRequest();
    //    }
    //}

    //internal static async Task<IResult> DownloadDocumentAsync(Guid documentId, IMongoDatabaseRepo dbApp)
    //{
    //    try
    //    {
    //        // Await the task to get the result of the asynchronous operation
    //        var documentRecord = dbApp.GetRecordById<DocumentTemplate>(documentId);

    //        string fileName = $"{documentRecord.Name}--{documentId.ToString().Substring(0, 12)}--{System.DateTime.UtcNow.ToString("MM-dd-yyyy-HH-MM")}.docm";

    //        byte[] documentBytes = documentRecord.TemplateBytes;

    //        if (documentBytes == null)
    //            return Results.NotFound();

    //        var fileStream = new MemoryStream(documentBytes);
    //        string contentType = "application/octet-stream"; // or more specific
    //        return Results.File(fileStream, contentType, fileDownloadName: fileName);
    //    }
    //    catch (System.Exception)
    //    {
    //        return Results.BadRequest();
    //    }
    //}

    //internal static async Task<IResult> DownloadDocumentLinkAsync(Guid documentId, IMongoDatabaseRepo dbApp, IWebHostEnvironment env)
    //{
    //    try
    //    {
    //        // Await the task to get the result of the asynchronous operation
    //        var documentRecord = dbApp.GetRecordById<DocumentTemplate>(documentId);

    //        string fileName = $"{documentId.ToString()}.docm";

    //        byte[] documentBytes = documentRecord.TemplateBytes;

    //        if (documentBytes == null)
    //            return Results.NotFound();

    //        string path = Path.Combine(env.WebRootPath, "TempStaging", fileName);

    //        var fs = new MemoryStream(documentBytes);

    //        await using var fileStream = new FileStream(path, FileMode.Create, FileAccess.Write);
    //        await fs.CopyToAsync(fileStream);

    //        //var fileStream = new MemoryStream(documentBytes);
    //        //string contentType = "application/octet-stream"; // or more specific
    //        //return Results.File(fileStream, contentType, fileDownloadName: fileName);
    //    }
    //    catch (System.Exception)
    //    {
    //        return Results.BadRequest();
    //    }

    //    return Results.Ok();
    //}
}

public class OpenApiConfigOptions
{
    public string ApiKeyName { get; set; }
    public string KeyValue { get; set; }
}