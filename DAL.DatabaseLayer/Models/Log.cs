namespace DAL.DatabaseLayer.Models;

public class Log
{
    public string LogId { get; set; }
    public string Controller { get; set; }
    public string Action { get; set; }
    public string Path { get; set; }
    public string Method { get; set; }
    public string QueryString { get; set; }
    public string RequestBody { get; set; }
    public string ResponseBody { get; set; }
    public string UserId { get; set; }
    public DateTime EndTime { get; set; }
    public bool IsException { get; set; }
    public string Exception { get; set; }
    public string RequestHeaderJson { get; set; }
    public DateTime? StartTime { get; set; }
    public bool? Status { get; set; }

}
