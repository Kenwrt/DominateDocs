using Coravel.Invocable;
using DominateDocsNotify.State;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using static DominateDocsNotify.DominateDocsNotifyExtensions;

namespace DominateDocsNotify.Services;

public class ScheduledHousekeepingService : IInvocable
{
    private readonly ILogger<ScheduledHousekeepingService> logger;
    private readonly IOptions<NotifyConfigOptions> options;

    private INotifyState notifyState;
    private CancellationToken stoppingToken;

    public ScheduledHousekeepingService(ILogger<ScheduledHousekeepingService> logger, IOptions<NotifyConfigOptions> options, INotifyState notifyState)
    {
        this.logger = logger;
        this.options = options;
        this.notifyState = notifyState;

        notifyState.IsRunBackgroundHousekeeperServiceChanged += OnBackgroundHousekeeperServiceChanged;
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
                    if (notifyState.IsHousekeeperActive)
                    {
                        List<Task> housekeeingTasks = new();

                        logger.LogDebug($"Notify Scheduled Housekeeper Service running at: {DateTime.Now}");

                        housekeeingTasks.Add(Task.Run(async () => await ExecuteHousingkeepingTask()));

                        if (housekeeingTasks.Count > 0) await Task.WhenAll(housekeeingTasks);
                    }
                    else
                    {
                        logger.LogDebug($"Notify Scheduled Housekeeper Service PAUSED at: {DateTime.Now}");
                    }
                }
                else
                {
                    logger.LogDebug($"Notify Scheduled Housekeeper Service NOT Active");
                }

                notifyState.IsReadyForProcessing = true;
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