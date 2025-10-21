using System.Security.Cryptography;
using System.Text;

namespace CMCS.Services
{
    public class FileEncryptionService
    {
        private static readonly byte[] Key = Encoding.UTF8.GetBytes("MySecretKey12345");
        private static readonly byte[] IV = Encoding.UTF8.GetBytes("MyInitVector16b!");

        public async Task EncryptFileAsync(Stream input, string outputPath)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            if (string.IsNullOrWhiteSpace(outputPath)) throw new ArgumentNullException(nameof(outputPath));

            using (Aes aes = Aes.Create())
            {
                aes.Key = Key;
                aes.IV = IV;

                using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
                using var fileStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write);
                using var cryptoStream = new CryptoStream(fileStream, encryptor, CryptoStreamMode.Write);
                await input.CopyToAsync(cryptoStream);
            }
        }

        public async Task<MemoryStream> DecryptFileAsync(string encryptedFilePath)
        {
            if (!File.Exists(encryptedFilePath))
                throw new FileNotFoundException("Encrypted file not found.", encryptedFilePath);

            using (Aes aes = Aes.Create())
            {
                aes.Key = Key;
                aes.IV = IV;

                using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
                using var fileStream = new FileStream(encryptedFilePath, FileMode.Open, FileAccess.Read);
                using var cryptoStream = new CryptoStream(fileStream, decryptor, CryptoStreamMode.Read);

                var decryptStream = new MemoryStream();
                await cryptoStream.CopyToAsync(decryptStream);
                decryptStream.Position = 0;
                return decryptStream;
            }
        }
    }
}
