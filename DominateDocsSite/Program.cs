using DocumentManager;
using FluentValidation;
using GemBox.Document;
using DominateDocsData.FluentValidation;
using DominateDocsNotify;
using DominateDocsSite.Components;
using DominateDocsSite.Components.Account;
using DominateDocsSite.Data;
using DominateDocsData.Database;
using DominateDocsSite.Endpoints;
using DominateDocsData.Helpers;
using DominateDocsSite.OpenAI;
using DominateDocsSite.State;
using DominateDocsSite.ViewModels;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.OpenApi.Models;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using MudBlazor.Services;
using Serilog;
using Serilog.Events;
using StripeBillingManager;
using System.Runtime.InteropServices;
using DominateDocsData.Helpers;

var builder = WebApplication.CreateBuilder(args);

EncryptAes encrypt = new();

[DllImport("kernel32.dll", SetLastError = true)]
static extern bool AllocConsole();

builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json",
                 optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

Log.Logger = new LoggerConfiguration()
                 .ReadFrom.Configuration(builder.Configuration)
                    .MinimumLevel.Debug()
                    .MinimumLevel.Override("Microsoft", LogEventLevel.Error)
                    .MinimumLevel.Override("System.Net.Http.HttpClient", LogEventLevel.Error)
                    .CreateLogger();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddHubOptions(h => { h.EnableDetailedErrors = true; })
    .AddCircuitOptions(o => o.DetailedErrors = true);

builder.Services.AddMudServices();
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
// .AddAuthenticationStateSerialization();

builder.Services.AddLogging(builder =>
{
    builder.ClearProviders();
    builder.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.None);
    builder.AddSerilog();
});

builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.Limits.MaxRequestBodySize = 2L * 1024 * 1024 * 1024; // 2 GB limit
});

builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

builder.Services.AddCascadingAuthenticationState();
builder.Services.TryAddScoped<IdentityUserAccessor>();
builder.Services.TryAddScoped<IdentityRedirectManager>();
builder.Services.TryAddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();
builder.Services.TryAddScoped<UserSession>();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.ExpireTimeSpan = TimeSpan.FromHours(2);
    options.SlidingExpiration = true; // refresh lifetime on activity
    // options.Cookie.Name = ".MyApp.Auth";
    // options.Cookie.SameSite = SameSiteMode.Lax;
    // options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = IdentityConstants.ApplicationScheme;
    options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
})
    //.AddCookie();
    .AddCookie(IdentityConstants.ApplicationScheme, o =>
    {
        o.LoginPath = "/Account/Login";
        o.AccessDeniedPath = "/AccessDenied";
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequiresUser", policy =>
        policy.RequireRole("User", "Admin", "DevAdmin"));

    options.AddPolicy("RequiresAdmin", policy =>
        policy.RequireRole("Admin", "DevAdmin"));

    options.AddPolicy("RequiresDevAdmin", policy =>
        policy.RequireRole("DevAdmin"));
});

var connectionString = builder.Configuration.GetConnectionString("AzureSqlConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

connectionString = encrypt.Decrypt(connectionString);

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        connectionString,
        sqlOptions =>
        {
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,                         // how many retries
                maxRetryDelay: TimeSpan.FromSeconds(10),  // max delay between retries
                errorNumbersToAdd: null);                 // additional SQL error codes (optional)
        }));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddIdentityCore<DominateDocsSite.Data.ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services.AddSingleton<IEmailSender<DominateDocsSite.Data.ApplicationUser>, IdentityNoOpEmailSender>();

var env = builder.Environment;        // this IS an IWebHostEnvironment
var webPath = env.WebRootPath;

builder.Services.AddApiEndpointsServices(builder.Configuration, options =>
{
    options.ApplicationName = "";
    options.SqlConString = "";
    options.MongoConString = "";
    options.WebPath = webPath;
    options.ApiKey = "123456";
});

builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
    {
        Description = "API Key needed to access the endpoints. ApiKey: X-Api-Key",
        Type = SecuritySchemeType.ApiKey,
        Name = "X-Api-Key",
        In = ParameterLocation.Header,
        Scheme = "ApiKeyScheme"
    });

    var scheme = new OpenApiSecurityScheme
    {
        Reference = new OpenApiReference
        {
            Type = ReferenceType.SecurityScheme,
            Id = "ApiKey"
        },

        In = ParameterLocation.Header,
    };

    var requirement = new OpenApiSecurityRequirement
    {
        { scheme, Array.Empty<string>() }
    };

    options.AddSecurityRequirement(requirement);
});

builder.Services.TryAddSingleton<IEncryptAes, EncryptAes>();

//builder.Services.TryAddTransient<IFakeData, FakeData>();
//builder.Services.TryAddTransient<IFakeLoanAgreement, FakeLoanAgreement>();

builder.Services.TryAddSingleton<AliasValidator>();
builder.Services.TryAddSingleton<BorrowerValidator>();
builder.Services.TryAddSingleton<GuarantorValidator>();
builder.Services.TryAddSingleton<LienValidator>();
builder.Services.TryAddSingleton<LenderValidator>();
builder.Services.TryAddSingleton<ServicerValidator>();
builder.Services.TryAddSingleton<RegistrationValidator>();
builder.Services.TryAddSingleton<PropertyOwnerValidator>();
builder.Services.TryAddSingleton<PropertyValidator>();
builder.Services.TryAddSingleton<EntityOwnerValidator>();
builder.Services.TryAddSingleton<SigningAuthorityValidator>();
builder.Services.TryAddSingleton<LoanAgreementValidator>();
builder.Services.TryAddSingleton<PaymentOptionsValidator>();
builder.Services.TryAddSingleton<QuickLoanAgreementValidator>();
builder.Services.TryAddSingleton<QuickBorrowerValidator>();
builder.Services.TryAddSingleton<QuickBrokerValidator>();
builder.Services.TryAddSingleton<QuickGuarantorValidator>();
builder.Services.TryAddSingleton<QuickLenderValidator>();
builder.Services.TryAddSingleton<QuickServicerValidator>();
builder.Services.TryAddSingleton<QuickPropertyValidator>();
builder.Services.TryAddSingleton<DocumentLibraryValidator>();
//builder.Services.TryAddSingleton<DocumentSetValidator>();
builder.Services.TryAddSingleton<DocumentValidator>();

builder.Services.AddValidatorsFromAssemblyContaining<AliasValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<PaymentOptionsValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<BorrowerValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<BrokerValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<ServicerValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<GuarantorValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<LienValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<LenderValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<RegistrationValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<PropertyOwnerValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<PropertyValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<EntityOwnerValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<SigningAuthorityValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<LoanAgreementValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<QuickLoanAgreementValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<QuickBrokerValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<QuickServicerValidator>();

builder.Services.AddValidatorsFromAssemblyContaining<QuickBorrowerValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<QuickGuarantorValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<QuickLenderValidator>();

builder.Services.AddValidatorsFromAssemblyContaining<DocumentLibraryValidator>();
//builder.Services.AddValidatorsFromAssemblyContaining<DocumentSetValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<DocumentValidator>();

builder.Services.TryAddSingleton<IEmailSender<DominateDocsSite.Data.ApplicationUser>, IdentityNoOpEmailSender>();

//Ken Added Here
builder.Services.AddEndpointsApiExplorer();

//
builder.Services.TryAddSingleton<IApplicationStateManager, ApplicationStateManager>();

//View Models

builder.Services.TryAddScoped<DocumentLibraryViewModel>();
builder.Services.TryAddScoped<DocumentViewModel>();
builder.Services.TryAddScoped<DocumentSetAssignmentViewModel>();
builder.Services.TryAddScoped<DocumentSetViewModel>();
builder.Services.TryAddScoped<AkaViewModel>();
builder.Services.TryAddScoped<BorrowerViewModel>();
builder.Services.TryAddScoped<EntityOwnerViewModel>();
builder.Services.TryAddScoped<BrokerViewModel>();
builder.Services.TryAddScoped<GuarantorViewModel>();
builder.Services.TryAddScoped<LienViewModel>();
builder.Services.TryAddScoped<LenderViewModel>();
builder.Services.TryAddScoped<RegistrationViewModel>();
builder.Services.TryAddScoped<PropertyViewModel>();
builder.Services.TryAddScoped<PropertyOwnerViewModel>();
builder.Services.TryAddScoped<CreditCardViewModel>();
builder.Services.TryAddScoped<StateLendingLicenseViewModel>();
builder.Services.TryAddScoped<SigningAuthorityViewModel>();
builder.Services.TryAddScoped<LoanAgreementViewModel>();
builder.Services.TryAddScoped<TestMergeViewModel>();
builder.Services.TryAddScoped<ContactUsViewModel>();
builder.Services.TryAddScoped<LoanTypeViewModel>();
builder.Services.TryAddScoped<LoanTypeListViewModel>();
builder.Services.TryAddScoped<DashboardViewModel>();
builder.Services.TryAddScoped<QuickBorrowerViewModel>();
builder.Services.TryAddScoped<QuickBrokerViewModel>();
builder.Services.TryAddScoped<QuickGuarantorViewModel>();
builder.Services.TryAddScoped<QuickLenderViewModel>();
builder.Services.TryAddScoped<QuickPropertyViewModel>();
builder.Services.TryAddScoped<QuickServicerViewModel>();
builder.Services.TryAddScoped<QuickLoanAgreementViewModel>();
builder.Services.TryAddScoped<UserDefaultProfileViewModel>();
builder.Services.TryAddScoped<UserDefaultLenderViewModel>();
builder.Services.TryAddScoped<UserDefaultBrokerViewModel>();
builder.Services.TryAddScoped<UserDefaultServicerViewModel>();
builder.Services.TryAddScoped<UserProfileViewModel>();



//Mongo Stuff
BsonSerializer.RegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));

var baseConn = encrypt.Decrypt(builder.Configuration.GetConnectionString("AtlasMongoConnection"))
                      ?? encrypt.Decrypt(builder.Configuration.GetConnectionString("AtlasMongoConnection"));

var connWithUuid = baseConn.Contains("uuidRepresentation=", StringComparison.OrdinalIgnoreCase)
    ? baseConn
    : $"{baseConn}{(baseConn.Contains("?") ? "&" : "?")}uuidRepresentation=standard";

builder.Services.AddSingleton<IMongoClient>(_ => new MongoClient(connWithUuid));
builder.Services.AddSingleton<IMongoDatabaseRepo, MongoDatabaseRepo>();

builder.Services.AddNotifyServices(options =>
{
    options.EmailAccountDomain = builder.Configuration.GetValue<string>("NotifyServices:EmailAccountDomain")?.Trim();
    options.EmailAccount = builder.Configuration.GetValue<string>("NotifyServices:EmailAccount")?.Trim();
    options.EmailAccountDisplay = builder.Configuration.GetValue<string>("NotifyServices:EmailAccountDisplay")?.Trim();
    options.EmailAccountPassword = builder.Configuration.GetValue<string>("NotifyServices:EmailAccountPassword")?.Trim();
    options.EnableAuthenication = builder.Configuration.GetValue<bool>("NotifyServices:EnableAuthenication");
    options.EnableCertificatValidation = builder.Configuration.GetValue<bool>("NotifyServices:EnableCertificatValidation");
    options.EnableGoogleOAuth = builder.Configuration.GetValue<bool>("NotifyServices:EnableGoogleOAuth");
    options.SecureSocketOption = builder.Configuration.GetValue<bool>("NotifyServices:SecureSocketOption");
    options.SMTPServerHost = builder.Configuration.GetValue<string>("NotifyServices:SMTPServerHost")?.Trim();
    options.SMTPServerPort = builder.Configuration.GetValue<int>("NotifyServices:SMTPServerPort");
    options.TwilioFromPhoneNumber = builder.Configuration.GetValue<string>("NotifyServices:TwilioFromPhoneNumber")?.Trim();
    options.TwilioAuthToken = builder.Configuration.GetValue<string>("NotifyServices:TwilioAuthToken")?.Trim();
    options.TwilioAccountSid = builder.Configuration.GetValue<string>("NotifyServices:TwilioAccountSid")?.Trim();
    options.IsRunBackgroundEmailService = builder.Configuration.GetValue<bool>("NotifyServices:IsRunBackgroundEmailService");
    options.IsRunBackgroundTextService = builder.Configuration.GetValue<bool>("NotifyServices:IsRunBackgroundTextService");
    options.LocalNotificationTemplatesLocation = builder.Configuration.GetValue<string>("NotifyServices:LocalNotificationTemplatesLocation")?.Trim();
    options.MaxEmailThreads = builder.Configuration.GetValue<int>("NotifyServices:MaxEmailThreads");
    options.MaxTextThreads = builder.Configuration.GetValue<int>("NotifyServices:MaxTextThreads");
    options.IsHousekeeperActive = builder.Configuration.GetValue<bool>("NotifyServices:IsHousekeeperActive");
    options.IsActive = builder.Configuration.GetValue<bool>("NotifyServices:IsActive");
    options.HouseKeepingIntervalMin = builder.Configuration.GetValue<int>("NotifyServices:HouseKeepingIntervalMin");
});

builder.Services.AddStripeBillingManagerServices(options =>
{
    options.APIKey = builder.Configuration.GetValue<string>("StripeBillingManager:APIKey")?.Trim();
    options.WebhookSigning = builder.Configuration.GetValue<string>("StripeBillingManager:WebhookSigning")?.Trim();
    options.IsRunBackgroundBillingService = builder.Configuration.GetValue<bool>("StripeBillingManager:IsRunBackgroundEmailService");
    options.MaxBillingThreads = builder.Configuration.GetValue<int>("StripeBillingManager:MaxBillingThreads");
    options.IsHousekeeperActive = builder.Configuration.GetValue<bool>("StripeBillingManager:IsHousekeeperActive");
    options.IsActive = builder.Configuration.GetValue<bool>("StripeBillingManager:IsActive");
    options.HouseKeepingIntervalMin = builder.Configuration.GetValue<int>("StripeBillingManager:HouseKeepingIntervalMin");
});

var aes = new EncryptAes();

builder.Services.AddDocumentManagerServices(options =>
{
    options.DbName = builder.Configuration.GetValue<string>("DocumentManager:DbName", "DefaultDBNotNamed")?.Trim();
    options.DbConnectionString = aes.Decrypt(builder.Configuration.GetValue<string>("ConnectionStrings:AtlasMongoConnection")?.Trim());
    options.StorageName = builder.Configuration.GetValue<string>("DocumentManager:StorageName")?.Trim();
    options.EndPoint = builder.Configuration.GetValue<string>("DocumentManager:EndPoint")?.Trim();
    options.AccessKey = builder.Configuration.GetValue<string>("DocumentManager:AccessKey")?.Trim();
    options.SecretKey = builder.Configuration.GetValue<string>("DocumentManager:SecretKey")?.Trim();
    options.UseSSL = builder.Configuration.GetValue<bool>("DocumentManager:UseSSL");
    options.UseObjectCloudStore = builder.Configuration.GetValue<bool>("DocumentManager:UseObjectCloudStore");
    options.IsRunBackgroundDocumentMergeService = builder.Configuration.GetValue<bool>("DocumentManager:IsRunBackgroundDocumentMergeService");
    options.IsRunBackgroundLoanApplicationService = builder.Configuration.GetValue<bool>("DocumentManager:IsRunBackgroundLoanApplicationService");
    options.LocalDocumentStore = builder.Configuration.GetValue<string>("DocumentManager:LocalDocumentStore")?.Trim();
    options.MasterTemplate = builder.Configuration.GetValue<string>("DocumentManager:MasterTemplate")?.Trim();
    options.MaxDocumentMergeThreads = builder.Configuration.GetValue<int>("DocumentManager:MaxDocumentMergeThreads");
    options.MaxLoanApplicationThreads = builder.Configuration.GetValue<int>("DocumentManager:MaxLoanApplicationThreads");
    options.IsHousekeeperActive = builder.Configuration.GetValue<bool>("DocumentManager:IsHousekeeperActive");
    options.IsActive = builder.Configuration.GetValue<bool>("DocumentManager:IsActive");

    var names = builder.Configuration
    .GetSection("DocumentManager:TestDocumentNames")
    .Get<List<string>>() ?? new();

    options.HouseKeepingIntervalMin = builder.Configuration.GetValue<int>("DocumentManager:HouseKeepingIntervalMin");
    options.TestDocumentNames = names;
});

builder.Services.AddApiEndpointsServices(builder.Configuration, options =>
{
    options.ApplicationName = builder.Configuration.GetValue<string>("ApplicationName").Trim();
    options.SqlConString = builder.Configuration.GetValue<string>("ConnectionStrings:AzureSqlConnection", "").Trim();
    options.MongoConString = builder.Configuration.GetValue<string>("ConnectionStrings:AtlasMongoConnection", "").Trim();
    options.WebPath = builder.Environment.WebRootPath.Trim();
    options.ApiKey = "123456";
});

builder.Services.AddOpenAiServices(builder.Configuration, options =>
{
    options.ApiKeyName = builder.Configuration.GetValue<string>("OpenAI:ApiKeyName", "")?.Trim(); ;
    options.KeyValue = encrypt.Decrypt(builder.Configuration.GetValue<string>("OpenAI:KeyValue", "")?.Trim());
});

ComponentInfo.SetLicense("DN-2025Dec15-ndx5IcVcdUwVQvvKLJ8dvUE4VUx4EN6GXcLPtyuMMwjgV90qZ11gmGRM80jomcm3LRNW7gQHSlOKa2QUusaVE9HOdVQ==A");

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthentication();
app.UseAuthorization();

app.UseApiEndpoints();

// Add additional endpoints required by the Identity /Account Razor components.
app.MapAdditionalIdentityEndpoints();


app.MapPost("/auth/logout", async (SignInManager<ApplicationUser> signInManager) =>
{
    await signInManager.SignOutAsync();
    return Results.Redirect("/");
})
.AllowAnonymous()
.DisableAntiforgery();


//using (var scope = app.Services.CreateScope())
//{
//    await SeedRoles(scope.ServiceProvider);
//}

app.Run();

//static async Task SeedRoles(IServiceProvider serviceProvider)
//{
//    var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

//    string[] roleNames = { "Admin", "User", "Support" };

//    foreach (var roleName in roleNames)
//    {
//        if (!await roleManager.RoleExistsAsync(roleName))
//        {
//            await roleManager.CreateAsync(new IdentityRole(roleName));
//        }
//    }
//}