using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ProschlafSupportProfileGenerationLibrary
{
    internal static class GenerationExtensions
    {
        /// <summary>
        /// Replace a string char at index with another char.
        /// </summary>
        /// <param name="text">String to be replaced.</param>
        /// <param name="index">Position of the char to be replaced.</param>
        /// <param name="c">Replacement char.</param>
        internal static string[] ReplaceAtIndex(this string[] text, int index, char c)
        {
            //  var stringBuilder = new StringBuilder(text);
            //  stringBuilder[index] = c;
            // return stringBuilder.ToString();

            text[index] = c.ToString();
            return text;
        }

        /// <summary>
        /// Converts a string to a string array where each array field contains 1 char.
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        internal static string[] ToStringArray(this string text)
        {
            return text.Select(c => c.ToString()).ToArray();
        }
    }
}
