using Microsoft.Extensions.Configuration;

namespace Cryptography.Utilities;

public class GeneralEncryption
{
    private readonly IConfiguration _config;
    public GeneralEncryption(IConfiguration config)
    {
        _config = config;
    }
    public string GenerateSymmetricKey()
    {
        return Guid.NewGuid().ToString("N");
    }
}
