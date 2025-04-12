using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;

namespace Cryptography.Utilities;

public class NiCardsEncryption
{
    private readonly IConfiguration _config;
    private readonly string _nIKey;
    public NiCardsEncryption(IConfiguration config)
    {
        _config = config;
        _nIKey = _config["EncryptionSettings:NICardEncryptionKey"];
    }
    public string GetNiCardPin(string PinBlockEncrypted, string Pan, string key = "")
    {
        string DecryptionKey = key;
        if (string.IsNullOrEmpty(key))
        {
            DecryptionKey = new AesGcmEncryption(_config).Decrypt(_nIKey);
        }
        string decryptedPan = DecryptNiPinBlockString(PinBlockEncrypted, DecryptionKey);
        string pinOutput = PinPANfromBlock(decryptedPan, Pan);
        return pinOutput;
    }
    private string DecryptNiPinBlockString(string input, string key = "")
    {
        var toEncryptArray = StringToByteArray(input);
        var tdes = new TripleDESCryptoServiceProvider();
        tdes.Key = StringToByteArray(key); //keyArray;
        tdes.Mode = CipherMode.ECB;
        tdes.Padding = PaddingMode.None;

        ICryptoTransform transformation = tdes.CreateDecryptor();
        byte[] resultArray = transformation.TransformFinalBlock(toEncryptArray, 0, toEncryptArray.Length);
        tdes.Clear();
        return BitConverter.ToString(resultArray).Replace("-", "");
    }
    private static string PinPANfromBlock(string PIN_Block, string PAN)
    {
        string str1 = Convert.ToString(PAN).Remove(0, 3);
        string str2 = str1.Remove(str1.Length - 1, 1);
        int count = 4;
        char someChar = '0';
        string AlgoA = str2.PadLeft(count + str2.Length, someChar);
        return (long.Parse(PIN_Block, System.Globalization.NumberStyles.HexNumber) ^ Convert.ToInt64(AlgoA, 16)).ToString("X").Substring(1, 4);
    }
    private static byte[] StringToByteArray(string hex)
    {
        return Enumerable.Range(0, hex.Length)
                         .Where(x => x % 2 == 0)
                         .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                         .ToArray();
    }
}
