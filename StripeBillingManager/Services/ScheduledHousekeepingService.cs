using Coravel.Invocable;
using StripeBillingManager.State;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace StripeBillingManager.Services;

public class ScheduledHousekeepingService : IInvocable
{
    private readonly ILogger<ScheduledHousekeepingService> logger;
    private readonly IOptions<StripeBillingManagerConfigOptions> options;

    private IStripeBillingManagerState billingState;
    private CancellationToken stoppingToken;

    public ScheduledHousekeepingService(ILogger<ScheduledHousekeepingService> logger, IOptions<StripeBillingManagerConfigOptions> options, IStripeBillingManagerState billingState)
    {
        this.logger = logger;
        this.options = options;
        this.billingState = billingState;

        billingState.IsRunBackgroundHousekeeperServiceChanged += OnBackgroundHousekeeperServiceChanged;
    }

    public async Task Invoke()
    {
        List<Task> HousekeepingTasks = new();

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (options.Value.IsActive)
                {
                    if (billingState.IsHousekeeperActive)
                    {
                        List<Task> housekeeingTasks = new();

                        logger.LogDebug($"Stripe Billing Manager Scheduled Housekeeper Service running at: {DateTime.Now}");

                        housekeeingTasks.Add(Task.Run(async () => await ExecuteHousingkeepingTask()));

                        if (housekeeingTasks.Count > 0) await Task.WhenAll(housekeeingTasks);
                    }
                    else
                    {
                        logger.LogDebug($"Stripe Billing Manager Scheduled Housekeeper Service PAUSED at: {DateTime.Now}");
                    }
                }
                else
                {
                    logger.LogDebug($"Stripe Billing Manager Scheduled Housekeeper Service NOT Active");
                }

                billingState.IsReadyForProcessing = true;
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
            }

            await Task.Delay(TimeSpan.FromMinutes(options.Value.HouseKeepingIntervalMin));
        }
    }

    private async Task ExecuteHousingkeepingTask()
    {
        try
        {
        }
        catch (SystemException ex)
        {
            logger.LogError($"Error when executing Houskeeping Task : {ex.Message}");
        }
    }

    private void OnBackgroundHousekeeperServiceChanged(object? sender, bool e)
    {
    }
}