namespace DominateDocsNotify.Models;

public class NotifyMsg
{
    public AppMsgEnums.MsgTypes MsgType { get; set; } = AppMsgEnums.MsgTypes.LogMsg;

    public AppMsgEnums.MsgLogLevels MsgLoglevel { get; set; } = AppMsgEnums.MsgLogLevels.None;

    public string EmailTemplateName { get; set; } = "PlainEmailTemplate.cshtml";

    public string MessageBody { get; set; } = string.Empty;
}