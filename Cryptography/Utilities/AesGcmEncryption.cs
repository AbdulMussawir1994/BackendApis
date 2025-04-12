using Microsoft.Extensions.Configuration;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Parameters;
using System.Text;

namespace Cryptography.Utilities;

public class AesGcmEncryption
{
    private readonly IConfiguration _config;
    private string _SecretKey;

    public AesGcmEncryption(IConfiguration config)
    {
        _config = config;
        _SecretKey = Environment.GetEnvironmentVariable("EncryptionKey");
    }
    public string Decrypt(string encryptedText, string Key = "")
    {
        string sR = string.Empty;
        if (!string.IsNullOrEmpty(encryptedText))
        {
            _SecretKey = string.IsNullOrEmpty(Key) ? _SecretKey : Key;
            byte[] iv = new byte[16];
            byte[] encryptedBytes = Convert.FromBase64String(encryptedText);
            GcmBlockCipher cipher = new GcmBlockCipher(new AesEngine());
            AeadParameters parameters =
                      new AeadParameters(new KeyParameter(Encoding.UTF8.GetBytes(_SecretKey)), 128, iv, null);

            cipher.Init(false, parameters);
            byte[] plainBytes =
                  new byte[cipher.GetOutputSize(encryptedBytes.Length)];
            Int32 retLen = cipher.ProcessBytes
                  (encryptedBytes, 0, encryptedBytes.Length, plainBytes, 0);
            cipher.DoFinal(plainBytes, retLen);

            sR = Encoding.UTF8.GetString(plainBytes).TrimEnd
                 ("\r\n\0".ToCharArray());
        }
        return sR;
    }
    public string Encrypt(string plainText, string Key = "")
    {
        _SecretKey = string.IsNullOrEmpty(Key) ? _SecretKey : Key;
        byte[] iv = new byte[16];
        string sR = string.Empty;
        byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);

        GcmBlockCipher cipher = new GcmBlockCipher(new AesEngine());
        AeadParameters parameters =
                     new AeadParameters(new KeyParameter(Encoding.UTF8.GetBytes(_SecretKey)), 128, iv, null);
        cipher.Init(true, parameters);

        byte[] encryptedBytes =
               new byte[cipher.GetOutputSize(plainBytes.Length)];
        Int32 retLen = cipher.ProcessBytes
                       (plainBytes, 0, plainBytes.Length, encryptedBytes, 0);
        cipher.DoFinal(encryptedBytes, retLen);
        sR = Convert.ToBase64String
             (encryptedBytes, Base64FormattingOptions.None);
        return sR;
    }
}
