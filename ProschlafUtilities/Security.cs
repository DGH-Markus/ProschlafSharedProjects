using Logging;
using System;
using System.IO;
using System.Security;
using System.Security.Cryptography;
using System.Text;

namespace ProschlafUtils
{
    /// <summary>
    /// Provides methods for AES-based encryption/decryption and hash functions.
    /// Note that all projects using this shared library must reference the following DLL: System.Security.
    /// Also, Encoding.UTF8 is used in most of the crypto-functions; changing this would result in the loss of all encrypted data since it cannot be decrytped using a different Encoding.
    /// </summary>
    public static class Security
    {
        #region Vars
        // This constant string is used as a "salt" value for the PasswordDeriveBytes function calls.
        // This size of the IV (in bytes) must = (keysize / 8).  Default keysize is 256, so the IV must be
        // 32 bytes long.  Using a 16 character string here gives us 32 bytes when converted to a byte array.
        private static readonly byte[] INIT_VECTOR = Encoding.ASCII.GetBytes("Zm4g1ajii1qSL93o");

        // This constant is used to determine the keysize of the encryption algorithm.
        private const int KEYSIZE = 256;

        private const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvxyz1234567890";
        private const string validPasswordChars = "abcdefghijkmnopqrstuvwxyzABCDEFGHJKLMNOPQRSTUVWXYZ1234567890"; //almost all latin letters and numbers, excluding the ones that are easily confused ('l'/'I')
        #endregion

        #region Structs
        public struct CryptographicOperationResult
        {
            public bool IsSuccessful { get; set; }
            public SecureString Password { get; set; }
            public bool IsPasswordWrong { get; set; }
            public bool IsSourceFaulty { get; set; }
            public bool IsTargetFaulty { get; set; }
            public Exception Exception { get; set; }
        }
        #endregion


        /// <summary>
        /// Gets a random string containing only letters and numbers (upper- and lower-case) with the specified length.
        /// </summary>
        /// <param name="size"></param>
        /// <returns></returns>
        public static string GetRandomString(int size)
        {
            Random random = new Random((int)(DateTime.Now.Ticks % int.MaxValue));
            char[] buffer = new char[size];

            for (int i = 0; i < size; i++)
                buffer[i] = chars[random.Next(chars.Length)];

            return new string(buffer);
        }

        /// <summary>
        /// Generates a random password consisting of digits and letters in upper- and lower case.
        /// Note that not all latin characters are in use in order to avoid certain unreadable combinations (e.g. "Il").
        /// </summary>
        /// <param name="length">The desired length of the password.</param>
        /// <returns></returns>
        public static string GenerateRandomAlphaNumericPassword(int length)
        {
            string res = "";
            Random rnd = new Random();

            while (0 < length--)
                res += validPasswordChars[rnd.Next(validPasswordChars.Length)];

            //replace similar characters that follow directly on each other
            while (res.Contains("o0") || res.Contains("O0"))
            {
                string replace = validPasswordChars[rnd.Next(validPasswordChars.Length)] + "" + validPasswordChars[rnd.Next(validPasswordChars.Length)];
                res = res.Replace("o0", replace).Replace("O0", replace);
            }

            while (res.Contains("0o") || res.Contains("0O"))
            {
                string replace = validPasswordChars[rnd.Next(validPasswordChars.Length)] + "" + validPasswordChars[rnd.Next(validPasswordChars.Length)];
                res = res.Replace("0o", replace).Replace("0O", replace);
            }

            return res;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="plainText"></param>
        /// <param name="passPhrase"></param>
        /// <param name="salt">The salt is appended to the password as a measure to make dictionary attacks harder. The salt must remain the same for encryption and decryption and be ASCII-encoded.</param>
        /// <param name="initVector">This size of the IV (in bytes) must be (keysize / 8). Default keysize is 256, so the IV must be 32 bytes long. This parameter can be left null in order to use a default IV.</param>
        /// <returns></returns>
        public static string Encrypt(string plainText, string passPhrase, string salt = null, byte[] initVector = null)
        {
            if (string.IsNullOrEmpty(plainText))
                return "";

            if (initVector == null)
                initVector = INIT_VECTOR;

            byte[] plainTextBytes = Encoding.UTF8.GetBytes(plainText);
            byte[] saltValueBytes = string.IsNullOrEmpty(salt) ? null : Encoding.ASCII.GetBytes(salt);
            using (PasswordDeriveBytes password = new PasswordDeriveBytes(passPhrase, saltValueBytes))
            {
                byte[] keyBytes = password.GetBytes(KEYSIZE / 8);
                using (RijndaelManaged symmetricKey = new RijndaelManaged())
                {
                    symmetricKey.Mode = CipherMode.CBC;
                    using (ICryptoTransform encryptor = symmetricKey.CreateEncryptor(keyBytes, initVector))
                    {
                        using (MemoryStream memoryStream = new MemoryStream())
                        {
                            using (CryptoStream cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
                            {
                                cryptoStream.Write(plainTextBytes, 0, plainTextBytes.Length);
                                cryptoStream.FlushFinalBlock();
                                byte[] cipherTextBytes = memoryStream.ToArray();
                                return Convert.ToBase64String(cipherTextBytes);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cipherText"></param>
        /// <param name="passPhrase"></param>
        /// <param name="salt">Optional salt for the pass phrase. Must be ASCII-encoded if specified.</param>
        /// <param name="initVector">Optional, but enforced: If no init vector is provided, a default vector will be used.</param>
        /// <returns></returns>
        public static string Decrypt(string cipherText, string passPhrase, string salt = null, byte[] initVector = null)
        {
            if (string.IsNullOrEmpty(cipherText))
                return "";

            if (initVector == null)
                initVector = INIT_VECTOR;

            byte[] cipherTextBytes = Convert.FromBase64String(cipherText);
            byte[] saltValueBytes = string.IsNullOrEmpty(salt) ? null : Encoding.ASCII.GetBytes(salt);
            using (PasswordDeriveBytes password = new PasswordDeriveBytes(passPhrase, saltValueBytes))
            {
                byte[] keyBytes = password.GetBytes(KEYSIZE / 8);
                using (RijndaelManaged symmetricKey = new RijndaelManaged())
                {
                    symmetricKey.Mode = CipherMode.CBC;
                    using (ICryptoTransform decryptor = symmetricKey.CreateDecryptor(keyBytes, initVector))
                    {
                        using (MemoryStream memoryStream = new MemoryStream(cipherTextBytes))
                        {
                            using (CryptoStream cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read))
                            {
                                byte[] plainTextBytes = new byte[cipherTextBytes.Length];
                                int decryptedByteCount = cryptoStream.Read(plainTextBytes, 0, plainTextBytes.Length);
                                return Encoding.UTF8.GetString(plainTextBytes, 0, decryptedByteCount);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Hashes the input string using the MD5 algorithm. 
        /// Returns a base-64 string representation of the hash.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static string GetMD5Hash(string input)
        {
            using (MD5 md5Hash = MD5.Create())
            {
                byte[] data = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(input));
                return Convert.ToBase64String(data);
            }
        }

        /// <summary>
        /// Encrypts a file using AES 256 and saves it.
        /// </summary>
        /// <param name="originalFilePath">The file to be encrypted.</param>
        /// <param name="newFilePath">The location where the resulting file has to be saved.</param>
        /// <param name="password">The password to use for the encryption.</param>
        /// <param name="salt"></param>
        public static CryptographicOperationResult EncryptFileToFile(string originalFilePath, string newFilePath, string password, Encoding encoding, string salt = "asdk23904uasdfji", byte[] initVector = null) //do not remove the preset salt string or some functions used in other applications might stop working
        {
            if (string.IsNullOrEmpty(password))
                return new CryptographicOperationResult() { IsSuccessful = false, IsPasswordWrong = true };

            if (initVector == null)
                initVector = INIT_VECTOR;

            SecureString securePwd = new SecureString();
            foreach (char c in password)
                securePwd.AppendChar(c);

            securePwd.MakeReadOnly();

            byte[] saltValueBytes = Encoding.ASCII.GetBytes(salt);
            byte[] plainTextBytes = null;

            //read file
            using (StreamReader sr = new StreamReader(originalFilePath))
            {
                try
                {
                    plainTextBytes = Encoding.Default.GetBytes(sr.ReadToEnd());
                }
                catch (Exception e)
                {
                    Logger.AddLogEntry(Logger.LogEntryCategories.Error, "Exception while encrypting file '" + originalFilePath + "' to file '" + newFilePath + "'.", e, "Security");
                    return new CryptographicOperationResult() { IsSuccessful = false, IsSourceFaulty = true, Exception = e, Password = securePwd };
                }
            }

            if (plainTextBytes == null)
                return new CryptographicOperationResult() { IsSuccessful = false, IsPasswordWrong = false, Password = securePwd };

            Rfc2898DeriveBytes passwordBytes = new Rfc2898DeriveBytes(password, saltValueBytes, 1000);

            byte[] keyBytes = passwordBytes.GetBytes(256 / 8);

            RijndaelManaged symmetricKey = new RijndaelManaged();
            symmetricKey.Mode = CipherMode.CBC;

            ICryptoTransform encryptor = symmetricKey.CreateEncryptor(keyBytes, initVector);
            byte[] cipherTextBytes;

            try
            {
                //define memory stream which will be used to hold the encrypted data
                using (MemoryStream memoryStream = new MemoryStream())
                using (CryptoStream cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
                {
                    cryptoStream.Write(plainTextBytes, 0, plainTextBytes.Length);
                    cryptoStream.FlushFinalBlock();
                    cipherTextBytes = memoryStream.ToArray();
                }
            }
            catch (Exception ex)
            {
                return new CryptographicOperationResult() { IsSuccessful = false, Exception = ex, Password = securePwd };
            }

            try
            {
                //convert the encrypted data into a base64-encoded string
                string cipherText = Convert.ToBase64String(cipherTextBytes);

                using (StreamWriter sw = new StreamWriter(newFilePath, false, encoding))
                {
                    sw.Write(cipherText);
                }

                return new CryptographicOperationResult() { IsSuccessful = true, Password = securePwd };
            }
            catch (Exception ex)
            {
                return new CryptographicOperationResult() { IsSuccessful = false, IsTargetFaulty = true, Exception = ex, Password = securePwd };
            }
        }

        /// <summary>
        /// Decrypts the specified file and saves it to the given Filepath.
        /// </summary>
        /// <param name="cryptedFilePath">The file to be decrypted.</param>
        /// <param name="password">The password to use for the decryption.</param>
        /// <returns>The decrypted file as string.</returns>
        public static CryptographicOperationResult DecryptFileToFile(string cryptedFilePath, string newFilePath, string password, Encoding encoding, string salt = "asdk23904uasdfji", byte[] initVector = null)
        {
            if (initVector == null)
                initVector = INIT_VECTOR;

            if (string.IsNullOrEmpty(password))
                return new CryptographicOperationResult() { IsSuccessful = false, IsPasswordWrong = true };

            SecureString securePwd = new SecureString();
            foreach (char c in password)
                securePwd.AppendChar(c);

            securePwd.MakeReadOnly();

            byte[] saltValueBytes = Encoding.ASCII.GetBytes(salt);

            //read file                
            byte[] cipherTextBytes = null;
            using (StreamReader sr = new StreamReader(cryptedFilePath))
            {
                try
                {
                    cipherTextBytes = Convert.FromBase64String(sr.ReadToEnd());
                }
                catch (Exception e)
                {
                    Logger.AddLogEntry(Logger.LogEntryCategories.Error, "Exception while reading file '" + cryptedFilePath, e, "Security");
                    return new CryptographicOperationResult() { IsSourceFaulty = true, IsSuccessful = false, Password = securePwd, Exception = e };
                }
            }

            if (cipherTextBytes == null)
                return new CryptographicOperationResult() { IsSuccessful = false, Password = securePwd };

            Rfc2898DeriveBytes passwordBytes = new Rfc2898DeriveBytes(password, saltValueBytes, 1000);

            byte[] keyBytes = passwordBytes.GetBytes(256 / 8);

            RijndaelManaged symmetricKey = new RijndaelManaged();
            symmetricKey.Mode = CipherMode.CBC;

            ICryptoTransform decryptor = symmetricKey.CreateDecryptor(keyBytes, initVector);
            byte[] plainTextBytes;
            int decryptedByteCount;

            try
            {
                using (MemoryStream memoryStream = new MemoryStream(cipherTextBytes))
                using (CryptoStream cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read))
                {
                    // Since at this point we don't know what the size of decrypted data will be, allocate the buffer long enough to hold ciphertext; plaintext is never longer than ciphertext.
                    plainTextBytes = new byte[cipherTextBytes.Length];

                    // Start decrypting.
                    decryptedByteCount = cryptoStream.Read(plainTextBytes, 0, plainTextBytes.Length);
                }

                // Convert decrypted data into a string. Let us assume that the original plaintext string was UTF8-encoded.
                string plainText = Encoding.Default.GetString(plainTextBytes, 0, decryptedByteCount);

                using (StreamWriter sw = new StreamWriter(newFilePath, false, encoding))
                {
                    sw.Write(plainText);
                }
            }
            catch (CryptographicException ce) //usually means a wrong password
            {
                return new CryptographicOperationResult() { IsPasswordWrong = true, IsSuccessful = false, Password = securePwd, Exception = ce };
            }
            catch (Exception ex)
            {
                Logger.AddLogEntry(Logger.LogEntryCategories.Error, "Unhandled exception while decrypting file '" + cryptedFilePath + "' to file '" + newFilePath + "'.", ex, "Security");
                return new CryptographicOperationResult() { IsTargetFaulty = true, IsSuccessful = false, Password = securePwd, Exception = ex };
            }

            return new CryptographicOperationResult() { IsSuccessful = true, Password = securePwd };
        }

        /// <summary>
        /// Encrypts a string using AES 256 and saves it to a file.
        /// </summary>
        /// <param name="destinationFilePath">The location where the resulting file has to be saved.</param>
        /// <param name="plainText">String wich has to be encrypted and stored to a file.</param>
        /// <param name="passPhrase">The password to use for the encryption.</param>
        /// <param name="salt">If no specific salt value is provided, a default one is used.</param>
        public static void EncryptStringToFile(string destinationFilePath, string plainText, string passPhrase, Encoding encoding, string salt = "asdk23904uasdfji", byte[] initVector = null)
        {
            if (string.IsNullOrEmpty(destinationFilePath) || string.IsNullOrEmpty(passPhrase))
                return;

            if (initVector == null)
                initVector = INIT_VECTOR;

            byte[] saltValueBytes = Encoding.ASCII.GetBytes(salt);
            byte[] plainTextBytes = Encoding.Default.GetBytes(plainText.ToCharArray());

            Rfc2898DeriveBytes passwordBytes = new Rfc2898DeriveBytes(passPhrase, saltValueBytes, 1000);

            byte[] keyBytes = passwordBytes.GetBytes(256 / 8);

            RijndaelManaged symmetricKey = new RijndaelManaged();
            symmetricKey.Mode = CipherMode.CBC;

            ICryptoTransform encryptor = symmetricKey.CreateEncryptor(keyBytes, initVector);
            byte[] cipherTextBytes;

            // Define memory stream which will be used to hold encrypted data and a cryptographic stream (always use Write mode for encryption).
            using (MemoryStream memoryStream = new MemoryStream())
            using (CryptoStream cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
            {
                cryptoStream.Write(plainTextBytes, 0, plainTextBytes.Length);
                cryptoStream.FlushFinalBlock();
                cipherTextBytes = memoryStream.ToArray();
            }

            // Convert encrypted data into a base64-encoded string.
            string cipherText = Convert.ToBase64String(cipherTextBytes);

            using (StreamWriter sw = new StreamWriter(destinationFilePath, false, encoding))
                sw.Write(cipherText);
        }

        /// <summary>
        /// Decrypts the specified file and returns its content as string.
        /// </summary>
        /// <param name="cryptedFilePath">The file to be decrypted.</param>
        /// <param name="passPhrase">The password to use for the decryption.</param>
        /// <param name="salt">If no specific salt value is provided, a default one is used.</param>
        /// <returns>The decrypted file as string.</returns>
        public static string DecryptFileToString(string cryptedFilePath, string passPhrase, string salt = "asdk23904uasdfji", byte[] initVector = null)
        {
            if (string.IsNullOrEmpty(cryptedFilePath) || !File.Exists(cryptedFilePath))
                return null;

            if (initVector == null)
                initVector = INIT_VECTOR;

            byte[] saltValueBytes = Encoding.ASCII.GetBytes(salt);
            byte[] cipherTextBytes;

            //read file
            using (StreamReader sr = new StreamReader(cryptedFilePath))
            {
                cipherTextBytes = Convert.FromBase64String(sr.ReadToEnd());
            }

            Rfc2898DeriveBytes passwordBytes = new Rfc2898DeriveBytes(passPhrase, saltValueBytes, 1000);

            byte[] keyBytes = passwordBytes.GetBytes(256 / 8);

            RijndaelManaged symmetricKey = new RijndaelManaged();
            symmetricKey.Mode = CipherMode.CBC;

            ICryptoTransform decryptor = symmetricKey.CreateDecryptor(keyBytes, initVector);

            // Define memory stream which will be used to hold encrypted data and a cryptographic stream (always use Read mode for encryption).
            byte[] plainTextBytes;
            int decryptedByteCount;
            using (MemoryStream memoryStream = new MemoryStream(cipherTextBytes))
            using (CryptoStream cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read))
            {
                // Since at this point we don't know what the size of decrypted data will be, allocate the buffer long enough to hold ciphertext; plaintext is never longer than ciphertext.
                plainTextBytes = new byte[cipherTextBytes.Length];
                decryptedByteCount = cryptoStream.Read(plainTextBytes, 0, plainTextBytes.Length);
            }

            //Convert decrypted data into a string
            string plainText = Encoding.Default.GetString(plainTextBytes, 0, decryptedByteCount);

            return plainText;
        }

        /// <summary>
        /// Takes an input password (or any text) and encrypts it using the built-in Windows DPAPI.
        /// </summary>
        /// <param name="secret"></param>
        /// <param name="encrypted">Will hold the base-64 string representation of the encrypted input text.</param>
        /// <param name="optionalEntropy">Optional additional entropy, not really needed as .Net generates its own entropy. 
        /// If used, the same entropy must be used for protect/unprotect.</param>
        /// <returns></returns>
        public static Exception ProtectPassword(string secret, out string encrypted, byte[] optionalEntropy = null)
        {
            encrypted = null;

            if (string.IsNullOrEmpty(secret))
                return new ArgumentNullException("password");

            try
            {
                byte[] plainTextBytes = Encoding.UTF8.GetBytes(secret); //convert string to bytes
                byte[] ciphertext = ProtectedData.Protect(plainTextBytes, optionalEntropy, DataProtectionScope.LocalMachine); //encrypt the input (we always want to store the password for the current machine)
                encrypted = Convert.ToBase64String(ciphertext); //convert the encrypted input to a base-64 string
                return null;
            }
            catch (Exception ex)
            {
                encrypted = null;
                return ex;
            }
        }

        /// <summary>
        /// Takes an encrypted password (or any text) and decrypts it using the built-in DPAPI.
        /// </summary>
        /// <param name="encrypted">The base-64 string representation of the encrypted text.</param>
        /// <param name="plainText">Will hold the decrypted text.</param>
        /// <param name="optionalEntropy">Optional additional entropy, not really needed as .Net generates its own entropy. If used, the same entropy must be used for protect/unprotect.</param>
        /// <returns></returns>
        public static Exception UnprotectPassword(string encrypted, out string plainText, byte[] optionalEntropy = null)
        {
            plainText = null;

            if (string.IsNullOrEmpty(encrypted))
                return new ArgumentNullException("encrypted");

            try
            {
                byte[] encryptedBytes = Convert.FromBase64String(encrypted);
                byte[] plainTextBytes = ProtectedData.Unprotect(encryptedBytes, optionalEntropy, DataProtectionScope.LocalMachine); //we always want to recover the password for the current machine
                plainText = Encoding.UTF8.GetString(plainTextBytes);
                return null;
            }
            catch (Exception ex)
            {
                plainText = null;
                return ex;
            }
        }

        /// <summary>
        /// A quick way to hash an integer so that it can be used as a control's unique ID on a public page.
        /// </summary>
        /// <param name="id">The id to be hashed.</param>
        /// <returns>An almost-unique hash value.</returns>
        public static string HashClientId(int id)
        {
            return HashClientId(id.ToString());
        }

        /// <summary>
        /// A quick way to hash a string so that it can be used as a control's unique ID on a public page.
        /// </summary>
        /// <param name="id">The id to be hashed.</param>
        /// <returns>An almost-unique hash value.</returns>
        public static string HashClientId(string id)
        {
            byte[] encodedBytes;

            using (var sha256 = new SHA256Managed())
            {
                var originalBytes = Encoding.Default.GetBytes(id);
                encodedBytes = sha256.ComputeHash(originalBytes);
            }

            return Convert.ToBase64String(encodedBytes);
        }
    }
}
