namespace DAL.ServiceLayer.Models.LogsModels;

public class Headers
{
    public string Accept { get; set; }
    public string Host { get; set; }
    public string IP { get; set; }
    public string DeviceId { get; set; }
    public string Latitude { get; set; }
    public string Longitude { get; set; }
    public string Platform { get; set; }
    public string MobileModel { get; set; }
    public string AppVersion { get; set; }
    public string IsRefreshToken { get; set; }
    public string OSVersion { get; set; }
    public string Language { get; set; }
    public string XForwardedFor { get; set; }
    public string XRealIP { get; set; }
}
