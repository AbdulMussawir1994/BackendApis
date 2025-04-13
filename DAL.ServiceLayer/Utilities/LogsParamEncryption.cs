using System.Text.Json;
using System.Text.Json.Nodes;

namespace DAL.ServiceLayer.Utilities;

public class LogsParamEncryption
{
    public string CredentialsEncryption(string req)
    {
        try
        {
            if (req != "PAGE GET REQUEST" && !string.IsNullOrEmpty(req))
            {
                var obj = JsonNode.Parse(req);

                if (obj is JsonObject jsonObj)
                {
                    var propertiesToEncrypt = new string[]
                    {
                        "password", "currentpassword", "newpassword", "apploginpin", "apppin",
                        "otpcode", "otp", "ciphertext", "cardpin", "newpin", "confirmapppin",
                        "reenterpassword", "cipher", "confirmcardpin", "currentpin", "confirmpin",
                        "cardnumber", "pin", "newcardpin", "newcardpinconfrim", "mpin", "confirmmpin",
                        "cardnumbersetpin", "card_identifier_id", "fromcardnumber", "tocardnumber",
                        "encryptedcardnumber", "newpinconfirm", "cardno", "creditcardnumber",
                        "documentbase64", "imagedata", "filecontents", "accesstoken", "base64"
                    };

                    EncryptSensitiveData(jsonObj, propertiesToEncrypt);

                    req = jsonObj.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
                }
            }
            return req;
        }
        catch (Exception)
        {
            return req;
        }
    }

    private void EncryptSensitiveData(JsonObject jsonObj, string[] propertiesToEncrypt)
    {
        foreach (var property in jsonObj.ToList())
        {
            if (propertiesToEncrypt.Contains(property.Key.ToLower()))
            {
                jsonObj[property.Key] = "*********";
            }
            else if (property.Value is JsonObject nestedObject)
            {
                EncryptSensitiveData(nestedObject, propertiesToEncrypt);
            }
            else if (property.Value is JsonArray jsonArray)
            {
                foreach (var item in jsonArray.OfType<JsonObject>())
                {
                    EncryptSensitiveData(item, propertiesToEncrypt);
                }
            }
        }
    }
}
