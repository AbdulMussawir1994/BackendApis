using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;

namespace Cryptography.Utilities;

public class XcsEncryption
{
    private readonly IConfiguration _config;
    private readonly string _xCSKey;
    public XcsEncryption(IConfiguration config)
    {
        _config = config;
        _xCSKey = _config["EncryptionSettings:XcsEncryptionKey"];
    }
    public string DecryptXcsCardDetails(string cipherText)
    {
        string plaintext = null;
        // Create TripleDESCryptoServiceProvider  
        using (TripleDESCryptoServiceProvider tdes = new TripleDESCryptoServiceProvider())
        {
            tdes.Mode = CipherMode.ECB;
            tdes.Key = Convert.FromHexString(new AesGcmEncryption(_config).Decrypt(_xCSKey));

            // Create a decryptor  
            ICryptoTransform decryptor = tdes.CreateDecryptor();

            // Create the streams used for decryption.  
            using (MemoryStream ms = new MemoryStream(Convert.FromBase64String(cipherText)))
            {
                // Create crypto stream  
                using (CryptoStream cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                {
                    // Read crypto stream  
                    using (StreamReader reader = new StreamReader(cs))
                        plaintext = reader.ReadToEnd();
                }
            }
        }
        return plaintext;
    }
}
