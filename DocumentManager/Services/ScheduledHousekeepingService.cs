using Coravel.Invocable;
using DocumentManager.State;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DocumentManager.Services;

public class ScheduledHousekeepingService : IInvocable
{
    private readonly ILogger<ScheduledHousekeepingService> logger;
    private readonly IOptions<DocumentManagerConfigOptions> options;

    private IDocumentManagerState docState;
    private CancellationToken stoppingToken;

    public ScheduledHousekeepingService(ILogger<ScheduledHousekeepingService> logger, IOptions<DocumentManagerConfigOptions> options, IDocumentManagerState docState)
    {
        this.logger = logger;
        this.options = options;
        this.docState = docState;

        docState.IsRunBackgroundHousekeeperServiceChanged += OnBackgroundHousekeeperServiceChanged;
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
                    if (docState.IsHousekeeperActive)
                    {
                        List<Task> housekeeingTasks = new();

                        logger.LogDebug($"Document Scheduled Housekeeper Service running at: {DateTime.Now}");

                        housekeeingTasks.Add(Task.Run(async () => await ExecuteHousingkeepingTask()));

                        if (housekeeingTasks.Count > 0) await Task.WhenAll(housekeeingTasks);
                    }
                    else
                    {
                        logger.LogDebug($"Document Scheduled Housekeeper Service PAUSED at: {DateTime.Now}");
                    }
                }
                else
                {
                    logger.LogDebug($"Document Scheduled Housekeeper Service NOT Active");
                }

                docState.IsReadyForProcessing = true;
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