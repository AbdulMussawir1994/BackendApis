using Microsoft.Extensions.Configuration;
using Moq;

namespace TestProject.Mocks;

public static class MockConfigHandler
{
    public static DAL.ServiceLayer.Utilities.ConfigHandler Create()
    {
        var configurationMock = new Mock<IConfiguration>();
        var httpClientFactoryMock = new Mock<IHttpClientFactory>();
        var dummyHttpClient = new HttpClient();

        configurationMock.Setup(config => config["SomeKey"]).Returns("SomeValue");

        return new DAL.ServiceLayer.Utilities.ConfigHandler(
            configurationMock.Object,
            dummyHttpClient,
            httpClientFactoryMock.Object
        );
    }
}