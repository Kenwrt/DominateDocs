using Coravel;
using DocumentManager.CalculatorsSchedulers;
using DocumentManager.Database;
using DocumentManager.Services;
using DocumentManager.State;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace DocumentManager;

public static class DocumentManagerExtensions
{
    public static void AddDocumentManagerServices(this IServiceCollection services, Action<DocumentManagerConfigOptions>? options = null)
    {
        DocumentManagerConfigOptions configOptions = new DocumentManagerConfigOptions();

        if (options is null)
        {
            services.Configure<DocumentManagerConfigOptions>(options =>
            {
            });

            options?.Invoke(configOptions);
        }
        else
        {
            options?.Invoke(configOptions);

            services.Configure<DocumentManagerConfigOptions>(options);

            services.TryAddSingleton<IDocumentManagerState, DocumentManagerState>();

            services.TryAddSingleton<IWordServices, WordServices>();

            services.TryAddSingleton<ILoanScheduler, LoanScheduler>();

            services.TryAddSingleton<IBalloonPaymentCalculater, BalloonPaymentCalculater>();

            services.TryAddSingleton<IRazorLiteService, RazorLiteService>();

            services.TryAddSingleton<IDocumentMergeBackgroundService, DocumentMergeBackgroundService>();

            services.TryAddSingleton<IMongoDatabaseRepoDocuments, MongoDatabaseRepoDocuments>();

            services.TryAddSingleton<IFetchCurrentIndexRatesAndSchedulesService, FetchCurrentIndexRatesAndSchedulesService>();

            services.AddHttpClient<FetchCurrentIndexRatesAndSchedulesService>();

            services.Configure<HostOptions>(x =>
            {
                x.ServicesStartConcurrently = true;
                x.ServicesStopConcurrently = true;
            });

            services.AddHostedService<LoanApplicationBackgroundService>();
            services.AddHostedService<DocumentMergeBackgroundService>();

            ////Coravel Registration Services go Here!
            services.AddScheduler();
            services.TryAddSingleton<ScheduledHousekeepingService>();
        }
    }

    public static void UseDocumentManagerScheduler(this IApplicationBuilder app, Action<DocumentManagerConfigOptions>? options = null)
    {
        app.ApplicationServices.UseScheduler(scheduler =>
        {
            var scheduledHousekeepingService = scheduler.OnWorker("ScheduledHousekeepingService").Schedule<ScheduledHousekeepingService>();
            scheduledHousekeepingService.EveryMinute().PreventOverlapping("ScheduledHousekeepingService");
        });
    }
}

public class DocumentManagerConfigOptions
{
    public string? DbName { get; set; } = "DocumentDb";

    public string? DbConnectionString { get; set; }

    public string? StorageName { get; set; } = string.Empty;

    public string? EndPoint { get; set; } = "localhost:9000";

    public string? AccessKey { get; set; } = "uploaduser1";

    public string? SecretKey { get; set; } = "uploaduser1secretkey";

    public bool UseSSL { get; set; } = false;

    public bool UseObjectCloudStore { get; set; } = false;

    public bool IsRunBackgroundDocumentMergeService { get; set; } = false;

    public bool IsRunBackgroundLoanApplicationService { get; set; } = false;

    public string? LocalDocumentStore { get; set; } = "c:\\temp\\DocumentStore";

    public string? MasterTemplate { get; set; }

    public int MaxDocumentMergeThreads { get; set; } = 10;

    public int MaxLoanApplicationThreads { get; set; } = 10;

    public int HouseKeepingIntervalMin { get; set; } = 1;

    public bool IsHousekeeperActive { get; set; } = false;

    public List<string> TestDocumentNames { get; set; }

    public bool IsActive { get; set; } = false;
}