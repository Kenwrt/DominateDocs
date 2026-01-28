using Coravel;
using DominateDocsNotify.Services;
using DominateDocsNotify.State;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Twilio;

namespace DominateDocsNotify;

public static class DominateDocsNotifyExtensions
{
    public static void AddNotifyServices(this IServiceCollection services, Action<NotifyConfigOptions>? options = null)
    {
        NotifyConfigOptions configOptions = new NotifyConfigOptions();

        if (options is null)
        {
            services.Configure<NotifyConfigOptions>(options =>
            {
            });

            options?.Invoke(configOptions);
        }
        else
        {
            options?.Invoke(configOptions);

            services.Configure<NotifyConfigOptions>(options);

            TwilioClient.Init(configOptions.TwilioAccountSid, configOptions.TwilioAuthToken);

            services.TryAddSingleton<INotifyState, NotifyState>();
           

            services.Configure<HostOptions>(x =>
            {
                x.ServicesStartConcurrently = true;
                x.ServicesStopConcurrently = true;
            });

          

            //Coravel Registration Services go Here!
            services.AddScheduler();
            services.TryAddSingleton<ScheduledHousekeepingService>();
        }
    }

    public static void UseNotifyScheduler(this IApplicationBuilder app, Action<NotifyConfigOptions>? options = null)
    {
        app.ApplicationServices.UseScheduler(scheduler =>
        {
            var scheduledHousekeepingService = scheduler.OnWorker("ScheduledHousekeepingService").Schedule<ScheduledHousekeepingService>();
            scheduledHousekeepingService.EveryMinute().PreventOverlapping("ScheduledHousekeepingService");
        });
    }

    public class NotifyConfigOptions
    {
        public string SMTPServerHost { get; set; }
        public int SMTPServerPort { get; set; } = 25;
        public string SmtpUser { get; set; }
        public string SmtpPassword { get; set; }
        public string EmailAccountDisplay { get; set; }
        public string EmailAccountPassword { get; set; }
        public bool EnableAuthenication { get; set; }
        public bool IsSmtpAuthentication { get; set; } = false;
        public bool EnableCertificatValidation { get; set; } = false;
        public bool EnableGoogleOAuth { get; set; } = false;
        public bool SecureSocketOption { get; set; } = false;
        public string EmailAccountDomain { get; set; }
        public string EmailAccount { get; set; }
        public string FromName { get; set; }
        public string TwilioFromPhoneNumber { get; set; }
        public string TwilioAuthToken { get; set; }
        public string TwilioAccountSid { get; set; }
        public bool IsRunBackgroundEmailService { get; set; } = false;
        public bool IsRunBackgroundTextService { get; set; } = false;
        public string LocalNotificationTemplatesLocation { get; set; } = "c:\\temp\\NotificationTemplates";
        public int MaxEmailThreads { get; set; } = 10;
        public int MaxTextThreads { get; set; } = 10;
        public int HouseKeepingIntervalMin { get; set; } = 1;
        public bool IsHousekeeperActive { get; set; } = true;
        public bool IsActive { get; set; } = true;
    }
}