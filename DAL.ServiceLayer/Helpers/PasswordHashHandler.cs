using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using System.Security.Cryptography;

namespace DAL.ServiceLayer.Helpers
{
    public static class PasswordHashHandler
    {
        private const int IterationCount = 100000;
        private const int SaltSize = 16; // 128 bits
        private const int KeySize = 32;  // 256 bits

        public static string HashPassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
                throw new ArgumentException("Password cannot be null or empty.");

            byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);

            byte[] subkey = KeyDerivation.Pbkdf2(
                password: password,
                salt: salt,
                prf: KeyDerivationPrf.HMACSHA512,
                iterationCount: IterationCount,
                numBytesRequested: KeySize);

            byte[] outputBytes = new byte[1 + 4 + 4 + SaltSize + KeySize];
            outputBytes[0] = 0x01; // Version marker
            Buffer.BlockCopy(BitConverter.GetBytes((int)KeyDerivationPrf.HMACSHA512), 0, outputBytes, 1, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(IterationCount), 0, outputBytes, 5, 4);
            Buffer.BlockCopy(salt, 0, outputBytes, 9, SaltSize);
            Buffer.BlockCopy(subkey, 0, outputBytes, 9 + SaltSize, KeySize);

            return Convert.ToBase64String(outputBytes);
        }

        public static bool VerifyPassword(string password, string hashedPassword)
        {
            if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(hashedPassword))
                return false;

            try
            {
                byte[] decodedHashedPassword = Convert.FromBase64String(hashedPassword);

                if (decodedHashedPassword[0] != 0x01)
                    return false;

                var prf = (KeyDerivationPrf)BitConverter.ToInt32(decodedHashedPassword, 1);
                var iterations = BitConverter.ToInt32(decodedHashedPassword, 5);

                byte[] salt = new byte[SaltSize];
                Buffer.BlockCopy(decodedHashedPassword, 9, salt, 0, SaltSize);

                byte[] expectedSubkey = new byte[KeySize];
                Buffer.BlockCopy(decodedHashedPassword, 9 + SaltSize, expectedSubkey, 0, KeySize);

                byte[] actualSubkey = KeyDerivation.Pbkdf2(
                    password,
                    salt,
                    prf,
                    iterations,
                    KeySize);

                return CryptographicOperations.FixedTimeEquals(actualSubkey, expectedSubkey);
            }
            catch
            {
                return false;
            }
        }
    }
}
