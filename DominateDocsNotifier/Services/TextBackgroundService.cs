using DominateDocsNotify.Models;
using DominateDocsNotify.State;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;
using static DominateDocsNotify.DominateDocsNotifyExtensions;

namespace DominateDocsNotify.Services;

public class TextBackgroundService : BackgroundService, ITextBackgroundService
{
    private readonly ILogger<TextBackgroundService> logger;

    private readonly IOptions<NotifyConfigOptions> options;

    private readonly INotifyState notifyState;

    private SemaphoreSlim textSemaphoreSlim = null;

    public event EventHandler<TextMsg> OnTextCompletedEvent;

    public event EventHandler<TextMsg> OnTextErrorEvent;

    public TextBackgroundService(ILogger<TextBackgroundService> logger, IOptions<NotifyConfigOptions> options, INotifyState notifyState)
    {
        this.logger = logger;
        this.options = options;
        this.notifyState = notifyState;

        textSemaphoreSlim = new(options.Value.MaxTextThreads);

        notifyState.IsRunBackgroundTextServiceChanged += OnIsRunTextBackgroundServiceChanged;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (options.Value.IsActive)
                {
                    if (notifyState.IsRunBackgroundTextService)
                    {
                        List<Task> textMsgTasks = new();

                        while (notifyState.TextMsgProcessingQueue.Count > 0 && !stoppingToken.IsCancellationRequested)
                        {
                            logger.LogDebug("Text Msg Processing Queued Item Found Job at: {time} Processong Queue Entry", DateTimeOffset.Now);

                            try
                            {
                                TextMsg textMsg = null;

                                notifyState.TextMsgProcessingQueue.TryDequeue(out textMsg);

                                if (textMsg is not null) textMsgTasks.Add(Task.Run(async () => await TextMsgProcessingAsync(textMsg, stoppingToken)));
                            }
                            catch (Exception ex)
                            {
                                logger.LogError($"{ex.Message}");
                            }
                        }

                        if (textMsgTasks.Count > 0) await Task.WhenAll(textMsgTasks);

                        if (notifyState.TextMsgProcessingQueue.Count == 0) logger.LogDebug("Text Msg Processing Background Service running at: {time}  Nothing Queued", DateTimeOffset.Now);
                    }
                    else
                    {
                        logger.LogDebug("Text Msg Processing Background Service PAUSED at: {time}", DateTimeOffset.Now);
                    }
                }
                else
                {
                    logger.LogDebug($"Text Msg Processing Background Service NOT Active");
                }

                notifyState.IsReadyForProcessing = true;
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
            }

            await Task.Delay(TimeSpan.FromMinutes(2));
        }
    }

    public async Task TextMsgProcessingAsync(TextMsg textMsg, CancellationToken stoppingToken)
    {
        TextMsg textMsgState = null;

        try
        {
            await textSemaphoreSlim.WaitAsync();

            notifyState.TextMsgList.TryAdd(textMsg.Id, textMsg);

            var result = await MessageResource.CreateAsync(
                body: textMsg.MessageBody,
                from: new PhoneNumber(textMsg.From),
                to: new PhoneNumber(textMsg.To)
    );

            notifyState.StateHasChanged();

            OnTextCompletedEvent?.Invoke(this, textMsg);
        }
        catch (Exception)
        {
            OnTextErrorEvent?.Invoke(this, textMsg);
            notifyState.TextMsgList.Remove(textMsg.Id, out textMsgState);
            throw;
        }
        finally
        {
            textSemaphoreSlim?.Release();
        }
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        await base.StartAsync(cancellationToken);

        await DoSomeInitializationAsync();

        await DoSomeRecoveryAsync();
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await base.StopAsync(cancellationToken);

        await DoSomeCleanupAsync();
    }

    public async Task DoSomeInitializationAsync()
    {
        logger.LogDebug($"Text Msg Processing Background Service Initialization");
    }

    public async Task DoSomeRecoveryAsync()
    {
        logger.LogDebug($"Text Msg Processing Background Service Recovering Tranactions");
    }

    public async Task DoSomeCleanupAsync()
    {
        logger.LogDebug($"Text Msg Processing Background Service Performing Cleanup tasks");
    }

    //public async Task DoSomeTaskAsync()
    //{ }

    private void OnIsRunTextBackgroundServiceChanged(object? sender, bool e)
    {
        if (notifyState.IsRunBackgroundTextService)
        {
            DoSomeInitializationAsync();

            DoSomeRecoveryAsync();
        }
        else
        {
            DoSomeCleanupAsync();
        }
    }
}