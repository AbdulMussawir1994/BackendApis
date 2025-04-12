using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;

namespace Cryptography.Utilities;

public class RobotackEncryption
{
    private readonly IConfiguration _config;
    private readonly string _publicKeyRobotack;

    public RobotackEncryption(IConfiguration config)
    {
        _config = config;
        _publicKeyRobotack = _config["EncryptionSettings:PublicKeyRoboTack"];
    }
    public string EncryptionRobotack(string strText)
    {
        byte[] textBytes = System.Text.Encoding.UTF8.GetBytes(strText);
        byte[] publicKeyBytes = Convert.FromBase64String(new AesGcmEncryption(_config).Decrypt(_publicKeyRobotack));

        var keyLengthBits = 4096;  // need to know length of public key in advance!
        byte[] exponent = new byte[3];
        byte[] modulus = new byte[keyLengthBits / 8];
        Array.Copy(publicKeyBytes, publicKeyBytes.Length - exponent.Length, exponent, 0, exponent.Length);
        Array.Copy(publicKeyBytes, publicKeyBytes.Length - exponent.Length - 2 - modulus.Length, modulus, 0, modulus.Length);

        RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();
        RSAParameters rsaKeyInfo = rsa.ExportParameters(false);
        rsaKeyInfo.Modulus = modulus;
        rsaKeyInfo.Exponent = exponent;
        rsa.ImportParameters(rsaKeyInfo);

        byte[] encrypted = rsa.Encrypt(textBytes, true);
        return Convert.ToBase64String(encrypted);
    }
}
