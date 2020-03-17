using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SapCommons
{
    public static class SapItemOperations
    {
        #region Vars
        static Regex mattressSizeRegexSmall = new Regex(@"\d{2,4}x\d{2,4}", RegexOptions.IgnoreCase); //parses the size of the actual product, which is e.g. in the following format: 90x200
        static Regex mattressSizeRegexBig = new Regex(@"\d{2,4}x\d{2,4}x\d{2,4}", RegexOptions.IgnoreCase); //parses the size of the actual product, which is e.g. in the following format: 90x200x10
        static Regex mattressSizeRegexOld = new Regex(@"\d{2,3}/\d{2,3}", RegexOptions.IgnoreCase); //parses the size of the product, which is e.g. in the following format: 90/200
        #endregion

        /// <summary>
        /// Parses a product's format based on the input name. The format may be in the form "200x100" or "200x100x20". Allowed units are centimeters and millimeters.
        /// </summary>
        /// <param name="productName">The name of the product that also contains the size of the product.</param>
        /// <returns>The size or null.</returns>
        public static string ParseProductFormat(string productName)
        {
            Match match = mattressSizeRegexBig.Match(productName);
            if (match.Success)
                return match.Value;

            match = mattressSizeRegexSmall.Match(productName);
            return match.Value;
        }

        /// <summary>
        /// Parses a product's format based on the input format. The format may be in the format "90/200".
        /// This format was used in the old Helpline databases that are not used in ISAP.
        /// </summary>
        /// <param name="productName">The size of the product.</param>
        /// <returns>The size or null.</returns>
        public static string ParseFormat_OldFormat(string format)
        {
            Match match = mattressSizeRegexOld.Match(format);
            if (match.Success)
                return match.Value.Replace(@"/", "x");

            return format;
        }

        /// <summary>
        /// Parses an input product format and outputs the width, length and (optionally) the height of the product. All units are in centimeters.
        /// </summary>
        /// <param name="productName">The full name of the product for which the format has to be parsed.</param>
        /// <param name="productFormat">The format of the product (e.g. 200x90).</param>
        /// <param name="width"></param>
        /// <param name="length"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public static void ConvertFormatToSizes(string productName, string productFormat, out string width, out string length, out string height)
        {
            try
            {
                if (string.IsNullOrEmpty(productName) || string.IsNullOrEmpty(productFormat))
                {
                    width = length = height = null;
                    return;
                }

                bool usesMillimeters = false;

                string[] splitFormat = productFormat.Split('x');

                if (splitFormat.Length < 2) //no valid format
                {
                    width = length = height = "0";
                    return;
                }

                if (splitFormat[0].Length == 4 || productName.Contains("mm")) //the format is something like "2000x1200x200mm" which means that all units are in millimeters
                    usesMillimeters = true;

                if (!usesMillimeters)
                {
                    width = splitFormat[0];
                    length = splitFormat[1];
                    height = splitFormat.Length > 2 ? splitFormat[2] : ""; //the height is usually not contained in formats that use centimeters (e.g. "90x200")
                }
                else
                {
                    width = splitFormat[0].Substring(0, splitFormat[0].Length - 1); //convert from millimeters to centimeters by cutting the last digit away
                    length = splitFormat[1].Substring(0, splitFormat[1].Length - 1);
                    height = splitFormat.Length > 2 ? splitFormat[2].Substring(0, splitFormat[2].Length - 1) : "";
                }
            }
            catch (Exception)
            {
                width = length = height = null;
            }
        }


        /// <summary>
        /// Parses the commonly used code that represents a pillow setup and converts it into the numeric ISAP format.
        /// An imput code can for example looke lihe this: "B6+P1" or "B6+P2+P1+2Kh".
        /// </summary>
        /// <param name="code">The code to be converted, always starting with "B6".</param>
        /// <returns>The 3-digit ISAP pillow code (same as used in the Frontend.PillowDisplay custom control) or NULL.</returns>
        public static string ConvertPillowCodeToNumericFormat(string code)
        {
            if (string.IsNullOrEmpty(code))
                return null;

            code = code.ToUpper();

            if (!code.StartsWith("B6"))
                return code;

            //the code is divided into base module, insert(s) and wedge (each part separated by '+'). The insert(s) and the wedge parts can be ommited of these parts are not present in the pillow.
            string[] pillowParts = code.Split(new string[] { "+" }, StringSplitOptions.RemoveEmptyEntries);

            if (pillowParts.Length < 1 || pillowParts.Length > 4)
                return code;

            string baseModule = "0"; //the base module digit ranges from 0 to 2
            if (pillowParts[0] == "B6")
                baseModule = "0";
            else if (pillowParts[0] == "B6OR") //"ohne Rolle"
                baseModule = "1";
            else if (pillowParts[0] == "B6GR") //"geteilte Rolle"
                baseModule = "2";

            if (pillowParts.Length < 2) // the input code only consists of the "B6"-part
                return baseModule + "00";

            string inserts = "0"; //there can be 0, 1 or 2 inserts and the digits range from 0 to 3
            if (pillowParts[1].StartsWith("P"))
            {
                if (pillowParts[1] == "P2")
                    inserts = "1";
                else if (pillowParts[1] == "P1")
                    inserts = "2";
            }

            if (pillowParts.Length > 2 && pillowParts[2].StartsWith("P"))
                if (pillowParts[2] == "P1" && inserts == "0") //only the 1cm insert
                    inserts = "2";
                else if (pillowParts[2] == "P1" && inserts == "1") //both inserts present
                    inserts = "3";

            string wedge = "0"; //the wedge digit ranges from 0 to 2
            string wedgePart = null;
            if (pillowParts.Length > 1 && (pillowParts[1].StartsWith("2K") || pillowParts[1].StartsWith("K2"))) //the wedge part can be between index 1 and 3 (e.g. B6+2KH or B6+P2+2KH or "B6+P2+P1+2Kh")
                wedgePart = pillowParts[1];
            else if (pillowParts.Length > 2 && (pillowParts[2].StartsWith("2K") || pillowParts[2].StartsWith("K2")))
                wedgePart = pillowParts[2];
            else if (pillowParts.Length > 3 && (pillowParts[3].StartsWith("2K") || pillowParts[3].StartsWith("K2")))
                wedgePart = pillowParts[3];

            if (wedgePart != null)
            {
                if (wedgePart == "2KH" || wedgePart == "K2")
                    wedge = "1";
                if (wedgePart == "2KV")
                    wedge = "2";
            }

            return baseModule + inserts + wedge;
        }
    }
}
