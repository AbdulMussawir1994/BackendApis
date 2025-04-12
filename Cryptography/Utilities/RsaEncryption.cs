using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;
using System.Text;

namespace Cryptography.Utilities;

public class RsaEncryption
{
    private readonly IConfiguration _config;
    private readonly string _privateKey;
    private readonly string _publicKey;
    private string _SecretKey;
    public RsaEncryption(IConfiguration config)
    {
        _config = config;
        _privateKey = _config["EncryptionSettings:PrivateKey"];
        _publicKey = _config["EncryptionSettings:PublicKey"];
    }
    public string GenerateRSAKey()
    {
        string phead = "-----BEGIN PUBLIC KEY-----";
        string pfoot = "-----End PUBLIC KEY-----";
        string pEhead = "-----BEGIN Encrypted PUBLIC KEY-----";
        string pEfoot = "-----End Encrypted PUBLIC KEY-----";
        string pEMhead = "-----BEGIN PEM PUBLIC KEY-----";
        string pEMfoot = "-----End PEM PUBLIC KEY-----";
        string prhead = "-----BEGIN PRIVATE KEY-----";
        string prfoot = "-----End PRIVATE KEY-----";
        var rsa = new RSACryptoServiceProvider(4096);
        string publicPrivateKeyXML = rsa.ToXmlString(true);
        string publicOnlyKeyXML = rsa.ToXmlString(false);

        string pemPublicKey = Convert.ToBase64String(rsa.ExportRSAPublicKey());
        return $"{prhead}{Environment.NewLine}{publicPrivateKeyXML}{Environment.NewLine}{prfoot}{Environment.NewLine}{phead}{Environment.NewLine}{publicOnlyKeyXML}{Environment.NewLine}{pfoot}{Environment.NewLine}{pEMhead}{Environment.NewLine}{pemPublicKey}{Environment.NewLine}{pEMfoot}{Environment.NewLine}{pEhead}{Environment.NewLine}{new AesGcmEncryption(_config).Encrypt(pemPublicKey)}{Environment.NewLine}{pEfoot}";
    }
    public string Encrypt(string plainText, string Key = "")
    {
        if (string.IsNullOrEmpty(Key))
            Key = _publicKey;

        var Data = Encoding.UTF8.GetBytes(plainText);
        using (var rsa = new RSACryptoServiceProvider(4096))
        {
            try
            {
                StringXML.FromXmlString(rsa, Key.ToString());
                var encryptedData = rsa.Encrypt(Data, true);
                var base64Encrypted = Convert.ToBase64String(encryptedData);
                return base64Encrypted;
            }
            catch (Exception)
            {
                rsa.PersistKeyInCsp = false;
                return "";
            }
        }
    }

    public string Decrypt(string encryptedText)
    {
        using (var rsa = new RSACryptoServiceProvider(4096))
        {
            try
            {
                var base64Encrypted = encryptedText;
                StringXML.FromXmlString(rsa, _privateKey.ToString());
                var resultBytes = Convert.FromBase64String(base64Encrypted.Trim());
                var decryptedBytes = rsa.Decrypt(resultBytes, true);
                var decryptedData = Encoding.UTF8.GetString(decryptedBytes);
                return decryptedData.ToString();
            }
            catch (Exception)
            {
                rsa.PersistKeyInCsp = false;
                return "";
            }
        }
    }
}
