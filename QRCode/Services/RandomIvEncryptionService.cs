using System.Security.Cryptography;

namespace QRCode.Services;

public class RandomIvEncryptionService
{
    private readonly byte[] _encryptionKey;

    public RandomIvEncryptionService(byte[] encryptionKey)
    {
        _encryptionKey = encryptionKey;
    }

    public string Encrypt(string plaintext)
    {
        byte[] cyphertextBytes;
        using var aes = Aes.Create();
        var encryptor = aes.CreateEncryptor(_encryptionKey, aes.IV);
        using (var memoryStream = new MemoryStream())
        {
            using (var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
            {
                using (var streamWriter = new StreamWriter(cryptoStream))
                {
                    streamWriter.Write(plaintext);
                }
            }
            cyphertextBytes = memoryStream.ToArray();

            return new AesCbcCiphertext(aes.IV, cyphertextBytes).ToString();
        }
    }

    public string Decrypt(string ciphertext)
    {
        var cbcCiphertext = AesCbcCiphertext.FromBase64String(ciphertext);
        using var aes = Aes.Create();
        var decryptor = aes.CreateDecryptor(_encryptionKey, cbcCiphertext.Iv);
        using (var memoryStream = new MemoryStream(cbcCiphertext.CiphertextBytes))
        {
            using (var cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read))
            {
                using (var streamReader = new StreamReader(cryptoStream))
                {
                    return streamReader.ReadToEnd();
                }
            }
        }
    }

    internal class AesCbcCiphertext
    {
        public byte[] Iv { get; }
        public byte[] CiphertextBytes { get; }

        public static AesCbcCiphertext FromBase64String(string data)
        {
            var dataBytes = Convert.FromBase64String(data);
            return new AesCbcCiphertext(
                dataBytes.Take(16).ToArray(),
                dataBytes.Skip(16).ToArray()
            );
        }

        public AesCbcCiphertext(byte[] iv, byte[] ciphertextBytes)
        {
            Iv = iv;
            CiphertextBytes = ciphertextBytes;
        }

        public override string ToString()
        {
            return Convert.ToBase64String(Iv.Concat(CiphertextBytes).ToArray());
        }
    }
}