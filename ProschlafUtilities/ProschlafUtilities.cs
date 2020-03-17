using Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace ProschlafUtils
{
    public static class ProschlafUtilities
    {
        #region Vars
        static DateTime lastInternetCheckTime = DateTime.MinValue;
        static bool? lastInternetCheckResult = null;
        static bool isInternetCheckRunning = false;
        #endregion

        /// <summary>
        /// Attempts to parse the input text as double in order to check if the input is numeric.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static bool IsNumeric(string input)
        {
            if (string.IsNullOrEmpty(input))
                return false;

            double tmp;
            return double.TryParse(input, out tmp);
        }

        public static string CapitalizeFirstLetter(string input)
        {
            return string.IsNullOrEmpty(input) ? input : input.Substring(0, 1).ToUpperInvariant() + input.Substring(1);
        }

        /// <summary>
        /// Makes sure a person's name is cased correctly (e.g. "Max" instead of "MAX").
        /// </summary>
        /// <param name="inputname"></param>
        /// <returns></returns>
        public static string ApplyProperNameCasing(string inputname)
        {
            if (!inputname.HasValue())
                return inputname;

            return inputname.Substring(0, 1).ToUpper() + inputname.ToLower().Substring(1);
        }

        /// <summary>
        /// Attempts to normalize the input telephone number in order to get a proper international format in the form: +43660...
        /// </summary>
        /// <param name="inputNumber"></param>
        /// <returns></returns>
        public static string NormalizeTelephoneNumber(string inputNumber)
        {
            if (!inputNumber.HasValue())
                return inputNumber;

            inputNumber = TruncateCharactersFromPhoneNumber(inputNumber);

            if (inputNumber.StartsWith("+"))
            {
                return inputNumber;
            }

            if (inputNumber.StartsWith("004")) //Austria, Germany, Switzerland
                inputNumber = inputNumber.Replace("004", "+4");
            else if (inputNumber.StartsWith("0031")) //Netherlands
                inputNumber = inputNumber.Replace("0031", "+31");
            else if (inputNumber.StartsWith("0030")) //Greece
                inputNumber = inputNumber.Replace("0030", "+30");
            else if (inputNumber.StartsWith("0662") || inputNumber.StartsWith("0664") || inputNumber.StartsWith("0676") || inputNumber.StartsWith("0677") || inputNumber.StartsWith("0660") || inputNumber.StartsWith("0650") || inputNumber.StartsWith("0680") || inputNumber.StartsWith("0699") || inputNumber.StartsWith("0676") || inputNumber.StartsWith("0678") || inputNumber.StartsWith("0681")) //common Austrian mobile providers
                inputNumber = "+43" + inputNumber.Substring(1);
            else if (inputNumber.StartsWith("43") || inputNumber.StartsWith("49") || inputNumber.StartsWith("31") || inputNumber.StartsWith("41"))
                inputNumber = "+" + inputNumber;

            return inputNumber;
        }

        private static string TruncateCharactersFromPhoneNumber(string inputNumber)
        {
            return inputNumber.Replace(" ", "").Replace("/", "").Replace("(", "").Replace(")", "").Replace("-", "");
        }

        /// <summary>
        /// Microsoft's method do determine whether an email address is valid or not.
        /// See https://docs.microsoft.com/en-us/dotnet/standard/base-types/how-to-verify-that-strings-are-in-valid-email-format for more information.
        /// </summary>
        /// <param name="email"></param>
        /// <returns></returns>
        public static bool IsValidEmail(string email)
        {
            if (String.IsNullOrEmpty(email))
                return false;

            try
            {
                email = Regex.Replace(email, @"(@)(.+)$", DomainMapper, RegexOptions.None, TimeSpan.FromMilliseconds(200)); // Use IdnMapping class to convert Unicode domain names.
            }
            catch (Exception)
            {
                return false;
            }

            try
            {
                return Regex.IsMatch(email,
                      @"^(?("")("".+?(?<!\\)""@)|(([0-9a-z]((\.(?!\.))|[-!#\$%&'\*\+/=\?\^`\{\}\|~\w])*)(?<=[0-9a-z])@))" +
                      @"(?(\[)(\[(\d{1,3}\.){3}\d{1,3}\])|(([0-9a-z][-0-9a-z]*[0-9a-z]*\.)+[a-z0-9][\-a-z0-9]{0,22}[a-z0-9]))$",
                      RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(250));
            }
            catch (RegexMatchTimeoutException)
            {
                return false;
            }
        }

        private static string DomainMapper(Match match)
        {
            // IdnMapping class with default property values.
            IdnMapping idn = new IdnMapping();

            string domainName = match.Groups[2].Value;

            try
            {
                domainName = idn.GetAscii(domainName);
            }
            catch (ArgumentException)
            {
                return null;
            }

            return match.Groups[1].Value + domainName;
        }

        /// <summary>
        /// Returns a user-friendly string representing the number of bytes specified (using the closest sufix, like "kB" or "MB").
        /// The number is based on powers of 2 (so 1 kB has 1024 bytes, and so on).
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public static string GetFriendlyNameForBytes(long bytes)
        {
            if (bytes < 1024) //we use the SI unit here, less than 1 kB
                return bytes + " bytes";
            else if (bytes < 1048576)  // less than 1MB (1024 * 1024)
                return String.Format("{0:0.00}", bytes / 1024d) + "kB";
            else if (bytes < 1073741824)  // less than 1GB (1024 * 1024 * 1024)
                return String.Format("{0:0.00}", bytes / 1048576d) + "MB";
            else // everthing above 1 GB 
                return String.Format("{0:0.00}", bytes / 1073741824d) + "GB";
        }

        /// <summary>
        /// Converts an integer to its alphabetical representation (e.g. "4" --> "D" or "12" --> "L").
        /// Numbers larger than 26 get converted into multiple letters (e.g. "27" --> "AA" or "214" --> "HF".
        /// </summary>
        /// <param name="number"></param>
        /// <returns></returns>
        public static string IntToLetters(int value)
        {
            string result = string.Empty;

            while (--value >= 0)
            {
                result = (char)('A' + value % 26) + result;
                value /= 26;
            }

            return result;
        }

        #region Check internet connection

        /// <summary>
        /// Checks for an available and working internet connection by trying to reach some sites on the internet.
        /// </summary>
        /// <param name="timeOut">The timeout for the check in milliseconds.</param>
        /// <param name="forceCheck">The check is usually only once per minute and returns a cached result except when this flag is set to 'true'.</param>
        /// <returns>True if the internet could be found.</returns>
        public static bool CheckForInternetConnection(int timeOut = 10000, bool forceCheck = false)
        {
            if (timeOut < 50)
                return false;

            if (isInternetCheckRunning)
                return lastInternetCheckResult ?? false;

            if (!forceCheck && lastInternetCheckResult.HasValue && lastInternetCheckTime > DateTime.Now.AddSeconds(timeOut / 1000)) //use the cached result if the last check was less than [timeOut]sec ago
                return lastInternetCheckResult.Value;

            isInternetCheckRunning = true;
            bool connected = false;

            //use 2 methods at the same time to determine whether an internet connection is available or not
            Task t1 = Task.Factory.StartNew(() =>
            {
                try
                {
                    //ping the ISAP root server
                    byte[] buffer = new byte[32];
                    PingReply reply = new Ping().Send("95.216.114.248", timeOut > 3000 ? 3000 : timeOut, buffer, new PingOptions()); //3sec timeout for the ping
                    if (reply.Status == IPStatus.Success)
                    {
                        connected = true;
                    }
                    else
                    {
                        //fallback: try to open google.com
                        using (var client = new WebClient())
                        {
                            using (var stream = client.OpenRead("http://www.google.com"))
                            {
                                connected = true;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.AddLogEntry(Logger.LogEntryCategories.Error, "Exception while checking internet connection to: http://www.google.com", ex, "Utils");
                }
            });

            if (!Task.WaitAll(new Task[] { t1 }, timeOut))
            {
                lastInternetCheckTime = DateTime.Now;
                lastInternetCheckResult = false;
                return false;
            }

            lastInternetCheckTime = DateTime.Now;
            lastInternetCheckResult = connected;
            return connected;
        }
        #endregion
    }
}
