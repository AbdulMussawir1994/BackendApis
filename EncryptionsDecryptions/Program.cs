using Cryptography.Utilities;

IConfiguration config = new ConfigurationBuilder()
    .AddJsonFile("Key.json")
    .Build();
bool encryptAgain = true;
while (encryptAgain)
{
    Console.WriteLine("Please enter a string for encryption/decryption Or Enter G to generate RSA key pair Or H to generate Symmetric Key");
    string input = Console.ReadLine();
    if (input == "P")
    {
        var DecryptKey = new RsaEncryption(config).Decrypt("A7C0LLzmxgLcbgLp9Ulp5eS8SV3giP08lQ+VASYDLaPcR2yrYyubB9a3m+mM+vPruMoVRAN/rGgHCIdmOu4aabU0dk54qVfTUOsmIIONKjUdEN9zzUNfT+5nTDmd53aoHfxrnTB8ifduXo2m9Hn0MlgSyVvmAEDl5hVegQ/xixXEYnha2s1Y+kdWoVHLIguYeTcdib+HmnoWhNg27UGUURmRT2hBitT2wlZDsOq93Alf0RORd0QNMjG9aJczg8XWoY61pMBEAGNMRT1bOtkhe7plpG5f/wDutS84V1aw7sftgck9uIKKwHD0DUT1e8yAChHrgbipNFNkSdM0mYgD5+y45AKrBubNygkXLzQ7SlJlomBZW5vqLkUx7IB+I7o2T7ekKCcis7JEeloQlMuuXvJgKWjnl149jDStjGqKfnsbzQpu+uVBY2EbSvTgQ55AWJneBl/asBnCQf9zggQqLPdf0WFHObJy5WM+fviBG9Y59D3bU7IMu7vwtL1Hf3seFP+JxbKo3XCruum5/TL1K8XeaY8T0T4k5kf5qWLz/21UW/0wIKCcxTwLGn9NA2fqX6zmkKivw8bXCMGPU4+67j91rOQhr/yo6Dl5CT0hxdFQB7vlIlZ9PCmEmCg6qQ8sPupjGPTmzYOp60j/UxZTzcUiKV6y9pIUo3Eo5ILsW2w=");
        Console.WriteLine($"Asymetric decrypted string = {DecryptKey}");
    }
    if (input.ToLower() == "g")
    {
        var keypair = new RsaEncryption(config).GenerateRSAKey();
        Console.WriteLine(keypair);
    }
    else if (input.ToLower() == "h")
    {
        var keypair = new GeneralEncryption(config).GenerateSymmetricKey();
        Console.WriteLine(keypair);
    }
    Console.WriteLine("Enter E for Symmetric Encrypt and D for Symmetric Decrypt, A for Asymmetric Encryption, S for Asymmetric Decryption, Z for Symmetric Encryption(random key),X for Symmetric Decryption(random key):");
    string action = Console.ReadLine();
    if (action.ToLower() == "e")
    {
        var encryptedString = new AesGcmEncryption(config).Encrypt(input);
        Console.WriteLine($"encrypted string = {encryptedString}");
    }
    else if (action.ToLower() == "d")
    {
        try
        {
            var decryptedString = new AesGcmEncryption(config).Decrypt(input);
            Console.WriteLine($"decrypted string = {decryptedString}");
        }
        catch (Exception)
        {
            Console.WriteLine("please provide the valid encrypted value");

        }

    }
    else if (action.ToLower() == "a")
    {
        var encryptedString = new RsaEncryption(config).Encrypt(input);
        Console.WriteLine($"Asymetric encrypted string = {encryptedString}");
    }
    else if (action.ToLower() == "s")
    {
        try
        {
            var decryptedString = new RsaEncryption(config).Decrypt(input);
            Console.WriteLine($"Asymetric decrypted string = {decryptedString}");
        }
        catch (Exception)
        {
            Console.WriteLine("please provide the valid encrypted value");

        }
    }
    else if (action.ToLower() == "z")
    {
        string key = Guid.NewGuid().ToString("N");
        var encryptedString = new AesGcmEncryption(config).Encrypt(input, key);
        Console.WriteLine($"Random Key = {key}");
        Console.WriteLine($"encrypted string = {encryptedString}");
    }
    else if (action.ToLower() == "x")
    {
        try
        {
            Console.WriteLine("Enter key:");
            string Key = Console.ReadLine();
            var decryptedString = new AesGcmEncryption(config).Decrypt(input, Key);
            Console.WriteLine($"decrypted string = {decryptedString}");
        }
        catch (Exception)
        {
            Console.WriteLine("please provide the valid encrypted value");

        }
    }
    else
    {
        Console.WriteLine("Invalid Value...");
    }
    Console.WriteLine("press n to exit or enter to continue");
    string again = Console.ReadLine();
    switch (again.ToLower())
    {
        case ("n"):
            encryptAgain = false;
            break;
        default:
            break;
    }

}

