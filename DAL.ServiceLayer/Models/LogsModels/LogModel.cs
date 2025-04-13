namespace DAL.ServiceLayer.Models.LogsModels;

public class LogModel
{
    public string Method { get; set; }
    public string Path { get; set; }
    public string QueryString { get; set; }
    public DateTime StartTime { get; set; }
    public string UserId { get; set; }
    public string Action { get; set; }
    public string ReqBody { get; set; }
    public string ResBody { get; set; }
    public string Controller { get; set; }
    public bool IsExceptionFromRequest { get; set; }
    public bool IsExceptionFromResponse { get; set; }
    public string RequestHeaders { get; set; }
}
