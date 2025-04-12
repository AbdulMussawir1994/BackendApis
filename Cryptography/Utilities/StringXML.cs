using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.OpenSsl;
using System.Security.Cryptography;
using System.Xml;

namespace Cryptography.Utilities;

public static class StringXML
{
    public static RSACryptoServiceProvider PublicKeyFromPemFile(String key)
    {
        using (TextReader sr = new StringReader(key))
        {
            RsaKeyParameters publicKeyParam = (RsaKeyParameters)new PemReader(sr).ReadObject();

            RSACryptoServiceProvider cryptoServiceProvider = new RSACryptoServiceProvider();
            RSAParameters parms = new RSAParameters();

            parms.Modulus = publicKeyParam.Modulus.ToByteArrayUnsigned();
            parms.Exponent = publicKeyParam.Exponent.ToByteArrayUnsigned();

            cryptoServiceProvider.ImportParameters(parms);

            return cryptoServiceProvider;
        }
    }

    public static void FromXmlString(this RSA rsa, string xmlString)
    {
        RSAParameters parameters = new RSAParameters();

        XmlDocument xmlDoc = new XmlDocument();
        xmlDoc.LoadXml(xmlString);

        if (xmlDoc.DocumentElement.Name.Equals("RSAKeyValue"))
        {
            foreach (XmlNode node in xmlDoc.DocumentElement.ChildNodes)
            {
                switch (node.Name)
                {
                    case "Modulus": parameters.Modulus = string.IsNullOrEmpty(node.InnerText) ? null : Convert.FromBase64String(node.InnerText); break;
                    case "Exponent": parameters.Exponent = string.IsNullOrEmpty(node.InnerText) ? null : Convert.FromBase64String(node.InnerText); break;
                    case "P": parameters.P = string.IsNullOrEmpty(node.InnerText) ? null : Convert.FromBase64String(node.InnerText); break;
                    case "Q": parameters.Q = string.IsNullOrEmpty(node.InnerText) ? null : Convert.FromBase64String(node.InnerText); break;
                    case "DP": parameters.DP = string.IsNullOrEmpty(node.InnerText) ? null : Convert.FromBase64String(node.InnerText); break;
                    case "DQ": parameters.DQ = string.IsNullOrEmpty(node.InnerText) ? null : Convert.FromBase64String(node.InnerText); break;
                    case "InverseQ": parameters.InverseQ = string.IsNullOrEmpty(node.InnerText) ? null : Convert.FromBase64String(node.InnerText); break;
                    case "D": parameters.D = string.IsNullOrEmpty(node.InnerText) ? null : Convert.FromBase64String(node.InnerText); break;
                }
            }
        }
        else
        {
            throw new Exception("Invalid XML RSA key.");
        }

        rsa.ImportParameters(parameters);
    }

}
