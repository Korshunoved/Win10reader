using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Security.Cryptography;
using Windows.Security.Cryptography.Core;
using Windows.Storage.Streams;

namespace LitResReadW10.Crypto
{
    public static class EncryptionProvider
    {
        /// <summary>
        /// Encrypt a string
        /// </summary>
        /// <param name="input">String to encrypt</param>
        /// <param name="password">Password to use for encryption</param>
        /// <returns>Encrypted string</returns>
        public static string Encrypt(string input, string password)
        {
            //make sure we have data to work with
            if (string.IsNullOrEmpty(input))
                throw new ArgumentException("input cannot be empty");
            if (string.IsNullOrEmpty(password))
                throw new ArgumentException("password cannot be empty");
            // get IV, key and encrypt
            var iv = CreateInitializationVector(password);
            var key = CreateKey(password);
            var encryptedBuffer = CryptographicEngine.Encrypt(
              key, CryptographicBuffer.ConvertStringToBinary(input, BinaryStringEncoding.Utf8), iv);
            return CryptographicBuffer.EncodeToBase64String(encryptedBuffer);
        }

        public static IBuffer Encrypt(IBuffer input, string password = "<tg5Xtq{>w4<7HU}#AXhlklkh213a.,2s~!2???DAAAIHATETHISOS")
        {
            // get IV, key and encrypt
            var iv = CreateInitializationVector(password);
            var key = CreateKey(password);
            var encryptedBuffer = CryptographicEngine.Encrypt(key, input, iv);
            return encryptedBuffer;
        }

        /// <summary>
        /// Decrypt a string previously ecnrypted with Encrypt method and the same password
        /// </summary>
        /// <param name="input">String to decrypt</param>
        /// <param name="password">Password to use for decryption</param>
        /// <returns>Decrypted string</returns>
        public static string Decrypt(string input, string password)
        {
            //make sure we have data to work with
            if (string.IsNullOrEmpty(input))
                throw new ArgumentException("input cannot be empty");
            if (string.IsNullOrEmpty(password))
                throw new ArgumentException("password cannot be empty");
            // get IV, key and decrypt
            var iv = CreateInitializationVector(password);
            var key = CreateKey(password);
            var decryptedBuffer = CryptographicEngine.Decrypt(
              key, CryptographicBuffer.DecodeFromBase64String(input), iv);
            return CryptographicBuffer.ConvertBinaryToString(
             BinaryStringEncoding.Utf8, decryptedBuffer);
        }

        public static IBuffer Decrypt(IBuffer input, string password = "<tg5Xtq{>w4<7HU}#AXhlklkh213a.,2s~!2???DAAAIHATETHISOS")
        {
            var iv = CreateInitializationVector(password);
            var key = CreateKey(password);
            var decryptedBuffer = CryptographicEngine.Decrypt(
              key, input, iv);
            return decryptedBuffer;
        }

        /// <summary>
        /// Create initialization vector IV
        /// </summary>
        /// <param name="password">Password is used for random vector generation</param>
        /// <returns>Vector</returns>
        private static IBuffer CreateInitializationVector(string password)
        {
            var provider = SymmetricKeyAlgorithmProvider.OpenAlgorithm("AES_CBC_PKCS7");
            var newPassword = password;
            // make sure we satify minimum length requirements
            while (newPassword.Length < provider.BlockLength)
            {
                newPassword = newPassword + password;
            }
            //create vecotr
            var iv = CryptographicBuffer.CreateFromByteArray(
              Encoding.UTF8.GetBytes(newPassword));
            return iv;
        }
        /// <summary>
        /// Create encryption key
        /// </summary>
        /// <param name="password">Password is used for random key generation</param>
        /// <returns></returns>
        private static CryptographicKey CreateKey(string password)
        {
            var provider = SymmetricKeyAlgorithmProvider.OpenAlgorithm("AES_CBC_PKCS7");
            var newPassword = password;
            // make sure we satify minimum length requirements
            while (newPassword.Length < provider.BlockLength)
            {
                newPassword = newPassword + password;
            }
            var buffer = CryptographicBuffer.ConvertStringToBinary(
              newPassword, BinaryStringEncoding.Utf8);
            buffer.Length = provider.BlockLength;
            var key = provider.CreateSymmetricKey(buffer);
            return key;
        }
    }
}
