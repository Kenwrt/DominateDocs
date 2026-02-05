using Coravel;
using DocumentManager.CalculatorsSchedulers;
using DocumentManager.Email;
using DocumentManager.Infrastructure;
using DocumentManager.Jobs;
using DocumentManager.Services;
using DocumentManager.State;
using DocumentManager.Workers;
using DominateDocsData.Database;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace DocumentManager;

public static class DocumentManagerExtensions
{
    public static void AddDocumentManagerServices(this IServiceCollection services, Action<DocumentManagerConfigOptions>? options = null)
    {
        // ✅ Correct options wiring (your prior version was inverted)
        if (options is not null)
            services.Configure(options);
        else
            services.Configure<DocumentManagerConfigOptions>(_ => { });

        // Core state/services
        services.TryAddSingleton<IDocumentManagerState, DocumentManagerState>();
        services.TryAddScoped<IDocumentOutputService, DocumentOutputService>();

        services.TryAddSingleton<IWordServices, WordServices>();
        services.TryAddSingleton<ILoanScheduler, LoanScheduler>();
        services.TryAddSingleton<IBalloonPaymentCalculater, BalloonPaymentCalculater>();
        services.TryAddSingleton<IRazorLiteService, RazorLiteService>();
        services.AddScoped<ILoanThenGenerateEvaluator, DebugThenGenerateEvaluator>();
                
        // Typed client so PostmarkEmailSender gets HttpClient + options
        services.AddHttpClient<IEmailSender, PostmarkEmailSender>();


        services.TryAddSingleton<IMongoDatabaseRepo, MongoDatabaseRepo>();

        services.TryAddSingleton<IFetchCurrentIndexRatesAndSchedulesService, FetchCurrentIndexRatesAndSchedulesService>();
        services.AddHttpClient<FetchCurrentIndexRatesAndSchedulesService>();

        // ✅ Channel queues (replace ConcurrentQueue polling)
        services.TryAddSingleton<IJobQueue<LoanJob>>(_ => new ChannelJobQueue<LoanJob>(capacity: 500));
        services.TryAddSingleton<IJobQueue<MergeJob>>(_ => new ChannelJobQueue<MergeJob>(capacity: 2000));
        services.TryAddSingleton<IJobQueue<EmailJob>>(_ => new ChannelJobQueue<EmailJob>(capacity: 2000));

        // ✅ Dispatcher for producers (UI/Admin bench uses this)
        services.TryAddSingleton<IJobDispatcher, JobDispatcher>();

        // ✅ Email sender abstraction (NOOP for now, Postmark later)
        services.TryAddSingleton<IEmailSender, NoopEmailSender>();

        services.Configure<HostOptions>(x =>
        {
            x.ServicesStartConcurrently = true;
            x.ServicesStopConcurrently = true;
        });

        // ✅ Hosted workers (new pipeline)
        services.AddHostedService<LoanWorker>();
        services.AddHostedService<MergeWorker>();
        services.AddHostedService<EmailWorker>();

        // Scheduler (keep as-is)
        services.AddScheduler();
        services.TryAddSingleton<ScheduledHousekeepingService>();
    }

    public static void UseDocumentManagerScheduler(this IApplicationBuilder app, Action<DocumentManagerConfigOptions>? options = null)
    {
        app.ApplicationServices.UseScheduler(scheduler =>
        {
            var scheduledHousekeepingService = scheduler
                .OnWorker("ScheduledHousekeepingService")
                .Schedule<ScheduledHousekeepingService>();

            scheduledHousekeepingService.EveryMinute()
                .PreventOverlapping("ScheduledHousekeepingService");
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

    public string PostMarkApiKey { get; set; } = "";
    public string FromEmail { get; set; } = "";
    public string? FromName { get; set; }
    public string? MessageStream { get; set; } = "outbound";

    public string? StripeAPIKey { get; set; }
    public string? StripeSecretKey { get; set; }
    public string? StripeWebhook { get; set; }

    public List<string> TestDocumentNames { get; set; } = new();
    public bool IsActive { get; set; } = false;
}
