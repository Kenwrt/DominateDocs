using FluentEmail.Core;
using FluentEmail.Razor;
using FluentEmail.Smtp;
using DominateDocsNotify.Models;
using DominateDocsNotify.State;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PostmarkDotNet;
using System.Net.Mail;
using static DominateDocsNotify.DominateDocsNotifyExtensions;

namespace DominateDocsNotify.Services;

public class EmailBackgroundService : BackgroundService, IEmailBackgroundService
{
    private readonly ILogger<EmailBackgroundService> logger;

    private readonly IOptions<NotifyConfigOptions> options;

    private readonly INotifyState notifyState;

    private SemaphoreSlim emailSemaphoreSlim = null;

    public event EventHandler<EmailMsg> OnEmailCompletedEvent;

    public event EventHandler<EmailMsg> OnEmailErrorEvent;

    private readonly IWebHostEnvironment webEnv;

    public EmailBackgroundService(ILogger<EmailBackgroundService> logger, IOptions<NotifyConfigOptions> options, INotifyState notifyState, IWebHostEnvironment webEnv)
    {
        this.logger = logger;
        this.options = options;
        this.notifyState = notifyState;
        this.webEnv = webEnv;

        emailSemaphoreSlim = new(options.Value.MaxEmailThreads);

        notifyState.IsRunBackgroundEmailServiceChanged += OnIsRunEmailBackgroundServiceChanged;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (options.Value.IsActive)
                {
                    if (notifyState.IsRunBackgroundEmailService)
                    {
                        List<Task> emailTasks = new();

                        while (notifyState.EmailMsgProcessingQueue.Count > 0 && !stoppingToken.IsCancellationRequested)
                        {
                            logger.LogDebug("Email Processing Queued Item Found Job at: {time} Processong Queue Entry", DateTimeOffset.Now);

                            try
                            {
                                EmailMsg email = null;

                                notifyState.EmailMsgProcessingQueue.TryDequeue(out email);

                                if (email is not null) emailTasks.Add(Task.Run(async () => await EmailSendAsync(email, stoppingToken)));
                            }
                            catch (Exception ex)
                            {
                                logger.LogError($"{ex.Message}");
                            }
                        }

                        if (emailTasks.Count > 0) await Task.WhenAll(emailTasks);

                        if (notifyState.EmailMsgProcessingQueue.Count == 0) logger.LogDebug("Email Processing Background Service running at: {time}  Nothing Queued", DateTimeOffset.Now);
                    }
                    else
                    {
                        logger.LogDebug("Email Processing Background Service PAUSED at: {time}", DateTimeOffset.Now);
                    }
                }
                else
                {
                    logger.LogDebug($"Email Processing Background Service NOT Active");
                }

                notifyState.IsReadyForProcessing = true;
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
            }

            await Task.Delay(TimeSpan.FromMinutes(1));
        }
    }

    public async Task EmailSendAsync(EmailMsg email, CancellationToken stoppingToken)
    {
        EmailMsg emailState = null;

        string razorTemplatesBasePath = Path.Combine(AppContext.BaseDirectory, "EmailTemplates");

        string logoBasePath = Path.Combine(webEnv.WebRootPath, "images");

        string defaultRazorTemplate = "ForwarderJobAuditReport.cshtml";

        string defaultLogo = "images/Dominate Docs Logo_Horizontal-03.png";

        try
        {
            await emailSemaphoreSlim.WaitAsync();

            notifyState.EmailMsgList.TryAdd(email.Id, email);

            if (email.ProviderType == Enums.EmailEnums.Providers.Fluent)
            {
                var smtpClient = new SmtpClient(options.Value.SMTPServerHost)
                {
                    Port = options.Value.SMTPServerPort,
                    EnableSsl = options.Value.SecureSocketOption
                };

                Email.DefaultSender = new SmtpSender(() => smtpClient);
                Email.DefaultRenderer = new RazorRenderer();

                var fromAddress = string.IsNullOrWhiteSpace(email.From)
                    ? $"{options.Value.EmailAccountDisplay}@{options.Value.EmailAccountDomain}"
                    : email.From;

                var emailMsg = Email
                    .From(fromAddress)
                    .To(email.To, email.Name ?? "Recipient Name")
                    .Subject(email.Subject);

                if (!string.IsNullOrEmpty(email.EmailTemplateName))
                {
                    email.EmailTemplateName = Path.Combine(razorTemplatesBasePath, email.EmailTemplateName);
                    emailMsg = emailMsg.UsingTemplateFromFile(email.EmailTemplateName, email);
                }
                else
                {
                    emailMsg = emailMsg.Body(email.MessageBody, true);
                }

                if (email.Attachments is { Count: > 0 })
                {
                    foreach (var att in email.Attachments)
                    {
                        var stream = att.ToStream();

                        emailMsg.Attach(new FluentEmail.Core.Models.Attachment
                        {
                            Data = stream,
                            Filename = att.FileName,
                            ContentType = att.ContentType ?? GetContentType(att.FileName)
                        });
                    }
                }

                var result = await emailMsg.SendAsync();
            }

            if (email.ProviderType == Enums.EmailEnums.Providers.FluentPostMark)
            {
            }

            if (email.ProviderType == Enums.EmailEnums.Providers.SendGrid)
            {
            }

            if (email.ProviderType == Enums.EmailEnums.Providers.PostMark)
            {
                var attachments = new List<PostmarkMessageAttachment>();

                TemplatedPostmarkMessage message = new PostmarkDotNet.TemplatedPostmarkMessage
                {
                    To = email.To,
                    From = "no-reply@DominateDocs.law",
                    ReplyTo = email.From,
                    TrackOpens = true,
                    TemplateId = email.PostMarkTemplateId,
                    TemplateModel = email.TemplateModel
                };

                var logoBytes = File.ReadAllBytes(Path.Combine(logoBasePath, defaultLogo));

                // New attachments
                foreach (var att in email.Attachments)
                {
                    var bytes = att.ToBytes();

                    attachments.Add(new PostmarkMessageAttachment
                    {
                        Name = att.FileName,
                        Content = Convert.ToBase64String(bytes),
                        ContentType = att.ContentType ?? GetContentType(att.FileName),
                        ContentId = att.IsInline ? att.ContentId : null
                    });
                }

                if (attachments.Count > 0)
                {
                    message.Attachments = attachments;
                }

                var client = new PostmarkClient("94a27931-8519-4319-a2ed-955322ee9122");
                var sendResult = await client.SendMessageAsync(message);
            }

            logger.LogInformation("Sending email to {To} with subject {Subject}", email.To, email.Subject);

            OnEmailCompletedEvent?.Invoke(this, email);
        }
        catch (Exception ex)
        {
            OnEmailErrorEvent?.Invoke(this, email);
            notifyState.EmailMsgList.Remove(email.Id, out emailState);
            throw;
        }
        finally
        {
            emailSemaphoreSlim?.Release();
        }
    }

    private string GetContentType(string fileName)
    {
        var ext = Path.GetExtension(fileName).ToLower();
        return ext switch
        {
            ".pdf" => "application/pdf",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ".txt" => "text/plain",
            _ => "application/octet-stream"
        };
    }

    // --------- Helper methods for provider-specific conversions ---------

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
        logger.LogDebug($"Email Processing Background Service Initialization");
    }

    public async Task DoSomeRecoveryAsync()
    {
        logger.LogDebug($"Email Processing Background Service Recovering Tranactions");
    }

    public async Task DoSomeCleanupAsync()
    {
        logger.LogDebug($"Email Processing Background Service Performing Cleanup tasks");
    }

    //public async Task DoSomeTaskAsync()
    //{ }

    private void OnIsRunEmailBackgroundServiceChanged(object? sender, bool e)
    {
        if (notifyState.IsRunBackgroundEmailService)
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