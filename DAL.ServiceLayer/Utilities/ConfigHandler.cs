using Microsoft.Extensions.Configuration;

namespace DAL.ServiceLayer.Utilities;

public class ConfigHandler
{
    #region Variables
    public IConfiguration _config { get; private set; }
    public HttpClient _client { get; private set; }
    public IHttpClientFactory _httpClientFactory { get; private set; }

    public string LogId { get; set; }
    public string RequestedDateTime { get; set; }
    public string UserId { get; set; }

    #endregion

    #region IConfiguration
    public ConfigHandler(IConfiguration config, HttpClient client, IHttpClientFactory httpClientFactory)
    {
        _config = config;
        _client = client;
        _httpClientFactory = httpClientFactory;
    }

    #endregion 
}
