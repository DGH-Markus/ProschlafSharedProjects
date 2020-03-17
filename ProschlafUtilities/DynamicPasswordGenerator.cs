using System;
using System.Collections.Generic;
using System.Text;

namespace ProschlafUtils
{
    /// <summary>
    /// Provides dynamic passwords which are based on some kind of calculation instead of a fixed string. 
    /// Can be used in our applications to secure the access to the public settings.
    /// </summary>
    public class DynamicPasswordGenerator
    {
        /// <summary>
        /// Gets a dynamic password based on today's date. 
        /// Takes the first 4 letters of the week day (in German) and the day-portion of the date.
        /// </summary>
        /// <returns>A 5- or 6-letter password, all lower-case.</returns>
        public static string GetTodaysPassword()
        {
            switch (DateTime.Now.DayOfWeek)
            {
                case DayOfWeek.Monday:
                    return "mont" + DateTime.Now.Day;
                case DayOfWeek.Tuesday:
                    return "dien" + DateTime.Now.Day;
                case DayOfWeek.Wednesday:
                    return "mitt" + DateTime.Now.Day;
                case DayOfWeek.Thursday:
                    return "donn" + DateTime.Now.Day;
                case DayOfWeek.Friday:
                    return "frei" + DateTime.Now.Day;
                case DayOfWeek.Saturday:
                    return "sams" + DateTime.Now.Day;
                case DayOfWeek.Sunday:
                    return "sonn" + DateTime.Now.Day;
                default:
                    return null;
            }
        }

        /// <summary>
        /// Gets the encrypted form of a dynamic password based on today's date. 
        /// </summary>
        /// <returns>An encrypted password.</returns>
        public static string GetTodaysPasswordEncrypted(string password, string salt, byte[] initVector)
        {
            return Security.Encrypt(GetTodaysPassword(), password, salt, initVector);
        }
    }
}
