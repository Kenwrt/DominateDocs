using DominateDocsData.Enums;
using DominateDocsData.Models;
using DominateDocsData.Models.Stripe;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StripeBillingManager.State;
using System.Collections.Generic;
using System.Text;

namespace StripeBillingManager.Services;

public class StripeBillingManagerBackgroundService : BackgroundService, IStripeBillingManagerBackgroundService
{
    private readonly ILogger<StripeBillingManagerBackgroundService> logger;

    private readonly IOptions<StripeBillingManagerConfigOptions> options;


    private readonly IStripeBillingManagerState billingState;

    private SemaphoreSlim billingSemaphoreSlim = null;

    public event EventHandler<LoanDocumentSetGeneratedEvent>? OnDocSetBillingCompletedEvent;

    public event EventHandler<LoanDocumentSetGeneratedEvent>? OnDocSetBillingErrorEvent;

    public event EventHandler<Subscription>? OnSubscriptionBillingCompletedEvent;

    public event EventHandler<Subscription>? OnSubscriptionBillingErrorEvent;

    public StripeBillingManagerBackgroundService(ILogger<StripeBillingManagerBackgroundService> logger, IOptions<StripeBillingManagerConfigOptions> options, IStripeBillingManagerState billingState)
    {
        this.logger = logger;
        this.options = options;
        this.billingState = billingState;


        billingSemaphoreSlim = new(options.Value.MaxBillingThreads);

        billingState.IsRunBackgroundBillingServiceChanged += OnRunBillingServiceChanged;

    }

    private async Task DocSetBillingTransactionAsync(LoanDocumentSetGeneratedEvent DocSetBillingEvent, CancellationToken stoppingToken)
    {
        try
        {
            await billingSemaphoreSlim.WaitAsync();

            billingState.DocSetEventBillingList.TryAdd(DocSetBillingEvent.Id, DocSetBillingEvent);


            //DocSet Billing Code goes Here


            OnDocSetBillingCompletedEvent?.Invoke(this, DocSetBillingEvent);

            billingState.StateHasChanged();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Error processing document {DocSetBillingEvent.Id}: {ex.Message}");

            OnDocSetBillingErrorEvent?.Invoke(this, DocSetBillingEvent);
            billingState.DocSetEventBillingList.TryRemove(DocSetBillingEvent.Id, out DocSetBillingEvent);
            throw;
        }
        finally
        {
            billingSemaphoreSlim?.Release();
        }
    }

    private async Task SubscriptionBillingTransactionAsync(Subscription subscription, CancellationToken stoppingToken)
    {
        try
        {
            await billingSemaphoreSlim.WaitAsync();

            billingState.SubscriptionList.TryAdd(subscription.Id, subscription);


            //Subscription Billing Code goes Here


            OnSubscriptionBillingCompletedEvent?.Invoke(this, subscription);

            billingState.StateHasChanged();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Error processing document {subscription.Id}: {ex.Message}");

            OnSubscriptionBillingErrorEvent?.Invoke(this, subscription);
            billingState.SubscriptionList.TryRemove(subscription.Id, out subscription);
            throw;
        }
        finally
        {
            billingSemaphoreSlim?.Release();
        }
    }



    // Background Service Maintenance Area
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (options.Value.IsActive)
                {
                    if (billingState.IsRunBackgroundBillingService)
                    {
                        //DocSet Billing
                        List<Task> docSetBillingTasks = new();

                        while (billingState.DocSetEventBillingProcessingQueue.Count > 0 && !stoppingToken.IsCancellationRequested)
                        {
                            logger.LogDebug("Billing for DocSets Transaction Queued Item Found Job at: {time} Billing Event Queue Entry", DateTimeOffset.Now);

                            try
                            {
                                LoanDocumentSetGeneratedEvent DocSetBillingEvent = null;

                                billingState.DocSetEventBillingProcessingQueue.TryDequeue(out DocSetBillingEvent);

                                if (DocSetBillingEvent is not null) docSetBillingTasks.Add(Task.Run(async () => await DocSetBillingTransactionAsync(DocSetBillingEvent, stoppingToken)));
                            }
                            catch (Exception ex)
                            {
                                logger.LogError($"{ex.Message}");
                            }
                        }

                        if (docSetBillingTasks.Count > 0) await Task.WhenAll(docSetBillingTasks);

                        if (billingState.DocSetEventBillingProcessingQueue.Count == 0) logger.LogDebug("Stripe DocSet Billing Background Service running at: {time}  Nothing Queued", DateTimeOffset.Now);


                        //Subscription Billing
                        List<Task> subscriptBillingTasks = new();

                        while (billingState.SubscriptionProcessQueue.Count > 0 && !stoppingToken.IsCancellationRequested)
                        {
                            logger.LogDebug("Billing for SUbscription Transaction Queued Item Found Job at: {time} Billing Event Queue Entry", DateTimeOffset.Now);

                            try
                            {
                                Subscription subscription = null;

                                billingState.SubscriptionProcessQueue.TryDequeue(out subscription);

                                if (subscription is not null) subscriptBillingTasks.Add(Task.Run(async () => await SubscriptionBillingTransactionAsync(subscription, stoppingToken)));
                            }
                            catch (Exception ex)
                            {
                                logger.LogError($"{ex.Message}");
                            }
                        }

                        if (subscriptBillingTasks.Count > 0) await Task.WhenAll(subscriptBillingTasks);

                        if (billingState.SubscriptionProcessQueue.Count == 0) logger.LogDebug("Stripe Subscription Billing Background Service running at: {time}  Nothing Queued", DateTimeOffset.Now);


                    }
                    else
                    {
                        logger.LogDebug("Stripe DocSet Billing Background Service PAUSED at: {time}", DateTimeOffset.Now);
                    }
                }
                else
                {
                    logger.LogDebug($"Stripe Billing Background Service NOT Active");
                }

                billingState.IsReadyForProcessing = true;
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
            }

            await Task.Delay(TimeSpan.FromMinutes(1));
        }
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        await base.StartAsync(cancellationToken);

        await DoRecoveryAsync();
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await base.StopAsync(cancellationToken);

        await DoCleanupAsync();
    }

    private async Task DoRecoveryAsync()
    {
        logger.LogDebug($"Stripe Billing Manager Background Service Recovering Tranactions");
    }

    private async Task DoCleanupAsync()
    {
        logger.LogDebug($"Stripe Billing Manager Background Service Performing Cleanup tasks");
    }

    private void OnRunBillingServiceChanged(object? sender, bool e)
    {
        if (billingState.IsRunBackgroundBillingService)
        {
            DoRecoveryAsync();
        }
        else
        {
            DoCleanupAsync();
        }
    }
}

