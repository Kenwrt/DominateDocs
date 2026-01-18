namespace DominateDocsNotify.Models;

public class AppMsgEnums
{
    public enum MsgTypes
    {
        LogMsg,
        ReportMsg,
        SupportMsg,
        None
    }

    public enum MsgLogLevels
    {
        Error,
        Debug,
        Info,
        None
    }
}