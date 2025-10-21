using System.Security.Cryptography;
using System.Text;

namespace CMCS.Services
{
    public class FileEncryptionService
    {
        //AES encryption key 
        private static readonly byte[] Key = Encoding.UTF8.GetBytes("MySecretKey12345");
        private static readonly byte[] IV = Encoding.UTF8.GetBytes("MyInitVector16b!");

        // This encrypts an input stream and writes the encrypted content to the output file
        public async Task EncryptFileAsync(Stream input, string outputPath)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            if (string.IsNullOrWhiteSpace(outputPath)) throw new ArgumentNullException(nameof(outputPath));

            using (Aes aes = Aes.Create())
            {
                aes.Key = Key;
                aes.IV = IV;

                // Creates an encryptor and writes the data to a new file
                using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
                using var fileStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write);
                using var cryptoStream = new CryptoStream(fileStream, encryptor, CryptoStreamMode.Write);
                await input.CopyToAsync(cryptoStream);
            }
        }

        // Decrypts an encrypted file and returns the content as a memory stream
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
            // This stores the decrypted content in a memory stream
                var decryptStream = new MemoryStream();
                await cryptoStream.CopyToAsync(decryptStream);
                decryptStream.Position = 0;
                return decryptStream;
            }
        }
    }
}
