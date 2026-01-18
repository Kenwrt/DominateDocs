namespace DominateDocsNotify.Services;

public class SendNotifierService
{
    //private readonly ILogger<SendNotifierService> logger;
    //private readonly IConfiguration config;
    ////private IAppConfiguration app;

    //private HttpClient httpClient = new HttpClient();

    //private string notifyUrl = string.Empty;

    //private bool sendReportNotifications = false;

    //private bool sendLogNotifications = false;

    //private bool sendSupportNotifications = false;

    //private int appRegId = -1;

    //private string deviceId = string.Empty;

    //public SendNotifierService(ILogger<SendNotifierService> logger, IConfiguration config)
    //{
    //    this.logger = logger;
    //    this.config = config;

    //    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(config.GetSection("InternalApiToken").Value);

    //    notifyUrl = this.config.GetSection("NotificationServices:URL").Value;

    //    appRegId = Convert.ToInt32(this.config.GetSection("NotificationServices:AppRegId").Value);

    //    deviceId = this.config.GetSection("NotificationServices:DeviceId").Value;

    //    sendReportNotifications = Convert.ToBoolean(this.config.GetSection("NotificationServices:SendReportNotifications").Value);

    //    sendLogNotifications = Convert.ToBoolean(this.config.GetSection("NotificationServices:SendLogNotifications").Value);

    //    sendSupportNotifications = Convert.ToBoolean(this.config.GetSection("NotificationServices:SendSupportNotifications").Value);
    //}

    //public async Task SendNotificationAsync(NotifyMsg notifyMsg)
    //{
    //    var options = new JsonSerializerOptions();
    //    options.Converters.Add(new JsonStringEnumConverter());

    //    try
    //    {
    //        //appMsg.AppName = "Notifier Test";
    //        //appMsg.MsgType = AppMsgEnums.MsgTypes.SupportMsg;
    //        //appMsg.MessageBody = msgText;
    //        //appMsg.To = msgEmail;
    //        //appMsg.PhoneNumber = msgPhone;

    //        //if (!String.IsNullOrEmpty(msgEmail)) await sendEmail.SendTestMailAsync(appMsg);
    //        //if (!String.IsNullOrEmpty(msgPhone)) await sendSms.SendTestMessageAsync(appMsg);

    //        NotifyMsgDTO notifyMsgDTO = new();

    //        notifyMsgDTO.AppId = appRegId;
    //        notifyMsgDTO.DeviceId = deviceId;
    //        notifyMsgDTO.MessageBody = notifyMsg.MessageBody;
    //        notifyMsgDTO.MsgLoglevel = notifyMsg.MsgLoglevel;
    //        notifyMsgDTO.MsgType = notifyMsg.MsgType;

    //        StringContent content = new StringContent(JsonConvert.SerializeObject(notifyMsgDTO), Encoding.UTF8, "application/json");

    //        HttpResponseMessage HttpResponseMsg = await httpClient.PostAsync($@"{notifyUrl}/api/notification", content);

    //        if (HttpResponseMsg.StatusCode == System.Net.HttpStatusCode.OK)
    //        {
    //            logger.LogDebug($"NotificationMsg sent sucessfully");
    //        }
    //        else
    //        {
    //            logger.LogError($"NotificationMsg failed");
    //        }
    //    }
    //    catch (SystemException ex)
    //    {
    //        logger.LogError(ex.Message);

    //        sendReportNotifications = false;
    //    }
    //}
}