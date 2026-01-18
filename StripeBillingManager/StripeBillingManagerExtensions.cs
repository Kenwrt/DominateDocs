using Coravel;
using StripeBillingManager.Services;
using StripeBillingManager.State;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace StripeBillingManager;

public static class StripeBillingManagerExtensions
{
    public static void AddStripeBillingManagerServices(this IServiceCollection services, Action<StripeBillingManagerConfigOptions>? options = null)
    {
        StripeBillingManagerConfigOptions configOptions = new StripeBillingManagerConfigOptions();

        if (options is null)
        {
            services.Configure<StripeBillingManagerConfigOptions>(options =>
            {
            });

            options?.Invoke(configOptions);
        }
        else
        {
            options?.Invoke(configOptions);

            services.Configure<StripeBillingManagerConfigOptions>(options);

            services.TryAddSingleton<IStripeBillingManagerState, StripeBillingManagerState>();

            services.TryAddSingleton<IStripeBillingManagerBackgroundService, StripeBillingManagerBackgroundService>();
            
            services.Configure<HostOptions>(x =>
            {
                x.ServicesStartConcurrently = true;
                x.ServicesStopConcurrently = true;
            });

           
            services.AddHostedService<StripeBillingManagerBackgroundService>();

            ////Coravel Registration Services go Here!
            services.AddScheduler();
            services.TryAddSingleton<ScheduledHousekeepingService>();
        }
    }

    public static void UseDocumentManagerScheduler(this IApplicationBuilder app, Action<StripeBillingManagerConfigOptions>? options = null)
    {
        app.ApplicationServices.UseScheduler(scheduler =>
        {
            var scheduledHousekeepingService = scheduler.OnWorker("ScheduledHousekeepingService").Schedule<ScheduledHousekeepingService>();
            scheduledHousekeepingService.EveryMinute().PreventOverlapping("ScheduledHousekeepingService");
        });
    }
}

public class StripeBillingManagerConfigOptions
{
    public string? APIKey { get; set; } = "{Secret_APIKey}";

    public string? WebhookSigning { get; set; } = "{Secret_Webhook_Verication}";

    public bool IsRunBackgroundBillingService { get; set; } = false;
      
    public int MaxBillingThreads { get; set; } = 10;

    public int HouseKeepingIntervalMin { get; set; } = 1;

    public bool IsHousekeeperActive { get; set; } = false;

    public bool IsActive { get; set; } = true;

  

}