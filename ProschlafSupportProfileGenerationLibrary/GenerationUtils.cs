using System;
using System.Collections.Generic;
using System.Text;

namespace ProschlafSupportProfileGenerationLibrary
{
    /// <summary>
    /// Utility class that provides certain required array operations and extensions.
    /// </summary>
    public static class GenerationUtils
    {
        /// <summary>
        /// Searches the maximum value in an integer array and returns the index of it. If there are multiple occurences of the highest value, the index of the first occurence is returned.
        /// </summary>
        /// <param name="array">The array to be searched.</param>
        /// <param name="startIndex">The array index to start the search from.</param>
        /// <param name="endIndex">The array index (inclusive) where the search ends.</param>
        /// <returns>The index of the highest value in the array.</returns>
        public static int GetIndexOfMaximum(int[] array, int startIndex, int endIndex)
        {
            if (startIndex >= array.Length || endIndex >= array.Length || startIndex < 0 || endIndex < 0)
                return -1;

            int maxVal = array[startIndex];
            int index = startIndex;

            for (int i = startIndex + 1; i <= endIndex; i++)
                if (array[i] > maxVal)
                {
                    maxVal = array[i];
                    index = i;
                }

            return index;
        }

        /// <summary>
        /// Searches the minimum value in an integer array and returns the index of it.
        /// </summary>
        /// <param name="array">The array to be searched.</param>
        /// <param name="startIndex">The array index to start the search from.</param>
        /// <param name="endIndex">The array index (inclusive) where the search ends.</param>
        /// <returns>The index of the lowest value in the array.</returns>
        public static int GetIndexOfMinimum(int[] array, int startIndex, int endIndex)
        {
            if (startIndex >= array.Length || endIndex >= array.Length || startIndex < 0 || endIndex < 0)
                return -1;

            int minVal = array[startIndex];
            int index = startIndex;

            for (int i = startIndex + 1; i <= endIndex; i++)
                if (array[i] < minVal)
                {
                    minVal = array[i];
                    index = i;
                }

            return index;
        }

        /// <summary>
        /// Searches for the specified value in the array and returns the index of the first occurance of it.
        /// </summary>
        /// <param name="array">The array to be searched.</param>
        /// <param name="value"></param>
        /// <returns>The index of the value in the array or -1.</returns>
        public static int GetIndexOfValue(int[] array, int value)
        {
            if (array == null || array.Length < 1)
                return -1;

            for (int i = 0; i < array.Length; i++)
                if (array[i] == value)
                {
                    return i;
                }

            return -1;
        }

        /// <summary>
        /// Checks whether 2 values are within a given range. 
        /// </summary>
        /// <param name="value1"></param>
        /// <param name="value2"></param>
        /// <param name="range">The inclusive range (e.g. 5 is in range of 7 when range is "2").</param>
        /// <returns></returns>
        public static bool IsInRange(int value1, int value2, int range)
        {
            return Math.Abs((value1 - value2)) <= range;
        }

        /// <summary>
        /// Checks whether the specified value is the only occurence in the specified array.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="array"></param>
        /// <returns>False if the value occurs multiple times, true otherwise.</returns>
        public static bool IsValueDistinct(int value, int[] array)
        {
            bool first = true;
            for (int i = 0; i < array.Length; i++)
            {
                if (array[i] == value)
                {
                    if (first)
                        first = false;
                    else
                        return false;
                }
            }

            return true;
        }

        internal static bool IsLeftSideEqual(int arrayIndex, int[] array)
        {
            return arrayIndex == 0 ? false : array[arrayIndex - 1] == array[arrayIndex];
        }

        internal static bool IsRightSideEqual(int arrayIndex, int[] array)
        {
            return arrayIndex == array.Length - 1 ? false : array[arrayIndex + 1] == array[arrayIndex];
        }

        /// <summary>
        /// Shortens a profile for better readability and less chance for errors during manual processing.
        /// Example: "SSSKKSTSTTS" becomes "3S2KSTS2TS".
        /// </summary>
        /// <param name="profile"></param>
        /// <returns></returns>
        public static string ShortenProfile(string profile)
        {
            if (profile == null || profile.Length < 1)
                return null;

            string profileShort = "";

            int currentLetterCnt = 1;
            char currentLetter = '\0';

            for (int i = 0; i < profile.Length; i++)
            {
                if (currentLetter == profile[i])
                {
                    currentLetterCnt++;
                }
                else
                {
                    if (currentLetterCnt > 1)
                        profileShort += currentLetterCnt.ToString() + currentLetter;
                    else
                        profileShort += currentLetter;

                    currentLetterCnt = 1;
                }

                currentLetter = profile[i];
            }

            if (currentLetterCnt > 1)
                profileShort += currentLetterCnt.ToString() + currentLetter;
            else
                profileShort += currentLetter;

            return profileShort;
        }

        #region Extensions
        /// <summary>
        /// Replaces a specific char  in a string (via index) with another char.
        /// </summary>
        /// <param name="text">String to be replaced.</param>
        /// <param name="index">Position of the char to be replaced.</param>
        /// <param name="c">Replacement char.</param>
        internal static string ReplaceAtIndex(this string text, int index, char c)
        {
            var stringBuilder = new StringBuilder(text);
            stringBuilder[index] = c;
            return stringBuilder.ToString();
        }

        #endregion
    }
}
