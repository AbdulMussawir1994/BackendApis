using System.Text.Json;
using System.Text.Json.Nodes;

namespace DAL.ServiceLayer.Utilities;

public class LogsParamEncryption
{
    public string CredentialsEncryptionResponse(string req)
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
                        "password", "currentpassword", "confirmpassword","newpassword", "apploginpin", "apppin",
                        "otpcode", "otp", "ciphertext", "cardpin", "newpin", "confirmapppin",
                        "reenterpassword", "cipher", "confirmcardpin", "currentpin", "confirmpin",
                        "cardnumber", "pin", "newcardpin", "newcardpinconfrim", "mpin", "confirmmpin",
                        "cardnumbersetpin", "card_identifier_id", "fromcardnumber", "tocardnumber",
                        "encryptedcardnumber", "newpinconfirm", "cardno", "creditcardnumber",
                        "documentbase64", "imagedata", "filecontents", "accesstoken", "base64"
                    };

                    EncryptSensitiveDataResponse(jsonObj, propertiesToEncrypt);

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

    private void EncryptSensitiveDataResponse(JsonObject jsonObj, string[] propertiesToEncrypt)
    {
        foreach (var property in jsonObj.ToList())
        {
            if (propertiesToEncrypt.Contains(property.Key.ToLower()))
            {
                jsonObj[property.Key] = "*********";
            }
            else if (property.Value is JsonObject nestedObject)
            {
                EncryptSensitiveDataResponse(nestedObject, propertiesToEncrypt);
            }
            else if (property.Value is JsonArray jsonArray)
            {
                foreach (var item in jsonArray.OfType<JsonObject>())
                {
                    EncryptSensitiveDataResponse(item, propertiesToEncrypt);
                }
            }
        }
    }

    public string CredentialsEncryptionRequest(string req)
    {
        try
        {
            if (req == "PAGE GET REQUEST" || string.IsNullOrWhiteSpace(req))
                return req;

            // Auto-unescape if double-encoded JSON string
            if (req.StartsWith("\"") && req.EndsWith("\""))
            {
                req = JsonSerializer.Deserialize<string>(req);
            }

            var jsonNode = JsonNode.Parse(req);

            if (jsonNode is JsonObject jsonObj)
            {
                var sensitiveKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "password", "currentpassword", "confirmpassword", "newpassword", "apploginpin", "apppin",
                "otpcode", "otp", "ciphertext", "cardpin", "newpin", "confirmapppin",
                "reenterpassword", "cipher", "confirmcardpin", "currentpin", "confirmpin",
                "cardnumber", "pin", "newcardpin", "newcardpinconfrim", "mpin", "confirmmpin",
                "cardnumbersetpin", "card_identifier_id", "fromcardnumber", "tocardnumber",
                "encryptedcardnumber", "newpinconfirm", "cardno", "creditcardnumber",
                "documentbase64", "imagedata", "filecontents", "accesstoken", "base64"
            };

                EncryptSensitiveDataRequest(jsonObj, sensitiveKeys);

                return jsonObj.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
            }

            return req;
        }
        catch
        {
            // In case of error, return original unprocessed string
            return req;
        }
    }

    private void EncryptSensitiveDataRequest(JsonObject jsonObj, HashSet<string> propertiesToEncrypt)
    {
        foreach (var property in jsonObj.ToList()) // Snapshot to avoid mutation during iteration
        {
            string key = property.Key;
            var value = property.Value;

            if (propertiesToEncrypt.Contains(key))
            {
                jsonObj[key] = "*********"; // Replace with encrypted string if needed
            }
            else if (value is JsonObject nestedObject)
            {
                EncryptSensitiveDataRequest(nestedObject, propertiesToEncrypt);
            }
            else if (value is JsonArray jsonArray)
            {
                foreach (var item in jsonArray)
                {
                    if (item is JsonObject obj)
                    {
                        EncryptSensitiveDataRequest(obj, propertiesToEncrypt);
                    }
                }
            }
        }
    }
}
