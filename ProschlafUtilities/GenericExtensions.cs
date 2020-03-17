using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ProschlafUtils
{
    public static class GenericExtensions
    {
        /// <summary>
        /// Extends the Distinct() linq method so that a target property can be specified (e.g. myCustomers.DistinctBy(c => c.Id)).
        /// </summary>
        /// <typeparam name="TSource"></typeparam>
        /// <typeparam name="TKey"></typeparam>
        /// <param name="source"></param>
        /// <param name="keySelector"></param>
        /// <returns>The input collection with distinct entries by the input property.</returns>
        public static IEnumerable<TSource> DistinctBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
        {
            HashSet<TKey> seenKeys = new HashSet<TKey>();
            return source.Where(element => seenKeys.Add(keySelector(element)));
        }

        /// <summary>
        /// Joins a list of numbers using the provided separator and outputs a string.
        /// </summary>
        /// <param name="numbers"></param>
        /// <param name="separator"></param>
        /// <returns></returns>
        public static string ToJoinedString(this IEnumerable<int> numbers, string separator = ", ")
        {
            if (numbers != null)
                return string.Join(separator, numbers);

            return null;
        }

        /// <summary>
        /// Joins a list of numbers using the provided separator and outputs a string.
        /// </summary>
        /// <param name="numbers"></param>
        /// <param name="separator"></param>
        /// <returns></returns>
        public static string ToJoinedString(this IEnumerable<int?> numbers, string separator = ", ")
        {
            return string.Join(separator, numbers);
        }

        /// <summary>
        /// Joins a list of strings using the provided separator and outputs a string.
        /// </summary>
        /// <param name="numbers"></param>
        /// <param name="separator"></param>
        /// <returns></returns>
        public static string ToJoinedString(this IEnumerable<string> labels, string separator = ", ")
        {
            return string.Join(separator, labels);
        }

        /// <summary>
        /// Replaces the char at the specified index.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="index"></param>
        /// <param name="newChar"></param>
        /// <returns></returns>
        public static string ReplaceAt(this string input, int index, char newChar)
        {
            if (input == null)
                throw new ArgumentNullException("input");

            char[] chars = input.ToCharArray();
            chars[index] = newChar;
            return new string(chars);
        }

        /// <summary>
        /// Checks whether a string is not empty or not.
        /// </summary>
        /// <param name="text">String to be replaced.</param>
        /// <returns>True if the string is neither NULL nor empty.</returns>
        public static bool HasValue(this string text)
        {
            return !string.IsNullOrEmpty(text);
        }

        /// <summary>
        /// Checks whether an IEnumerable<> is null or empty.
        /// </summary>
        /// <param name="list"></param>
        /// <returns>True if the IEnumerable is NULL or empty.</returns>
        public static bool IsNullOrEmpty<T>(this IEnumerable<T> list)
        {
            return list == null || !list.Any();
        }

        /// <summary>
        /// ForEach implementation for IEnumerable<T>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="enumeration"></param>
        /// <param name="action"></param>
        public static void ForEach<T>(this IEnumerable<T> enumeration, Action<T> action)
        {
            foreach (T item in enumeration)
            {
                action(item);
            }
        }

        /// <summary>
        /// Removes a range of values from an existing source collection.
        /// This is assumes that each range value is also present in the source collection.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <param name="range"></param>
        public static void RemoveRange<T>(this ICollection<T> source, ICollection<T> range)
        {
            range.ForEach(r => source.Remove(r));
        }

        /// <summary>
        /// Modifies the list by replacing one object with another.
        /// Does nothing if the original value is not present in the list.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <param name="oldValue"></param>
        /// <param name="newValue"></param>
        /// <returns></returns>
        public static void Replace<T>(this IList<T> source, T oldValue, T newValue)
        {
            int index = source.IndexOf(oldValue);

            if (index > -1)
            {
                source.Remove(oldValue);
                source.Insert(index, newValue);
            }
        }

        /// <summary>
        /// Finds the differences between 2 objects. The objects do not have to be of the same class.
        /// Invoke example: var variances =  obj1.DetailedCompare(obj2);
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="val1"></param>
        /// <param name="val2"></param>
        /// <returns>A list of Variance objects that represent the value differences between 2 objects.</returns>
        public static List<Variance> DetailedCompare<T>(this T val1, T val2)
        {
            List<Variance> variances = new List<Variance>();
            FieldInfo[] fi = val1.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic); // | BindingFlags.NonPublic |
            FieldInfo[] fi2 = val2.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            foreach (FieldInfo field in fi)
            {
                Variance variance = new Variance();

                variance.Prop = field.Name.Replace("_", "");
                variance.ValA = field.GetValue(val1);

                FieldInfo field2 = fi2.FirstOrDefault(inf => inf.Name == field.Name);
                if (field2 == null)
                    continue;

                variance.ValB = field2.GetValue(val2);

                if (variance.ValA?.Equals(variance.ValB) != true)
                    if (variance.ValA != null || variance.ValB != null)
                        variances.Add(variance);
            }

            return variances;
        }

        /// <summary>
        /// Returns the text set via the [Description()]-attribute for a given enum value.
        /// </summary>
        /// <param name="genericEnum"></param>
        /// <returns></returns>
        public static string GetDescription(this Enum genericEnum)
        {
            Type genericEnumType = genericEnum.GetType();
            MemberInfo[] memberInfo = genericEnumType.GetMember(genericEnum.ToString());
            if ((memberInfo != null && memberInfo.Length > 0))
            {
                var _Attribs = memberInfo[0].GetCustomAttributes(typeof(System.ComponentModel.DescriptionAttribute), false);
                if ((_Attribs != null && _Attribs.Count() > 0))
                {
                    return ((System.ComponentModel.DescriptionAttribute)_Attribs.ElementAt(0)).Description;
                }
            }

            return genericEnum.ToString();
        }

        public static byte[] ConvertToByteArray(this System.IO.Stream stream)
        {
            using (var memoryStream = new System.IO.MemoryStream())
            {
                stream.CopyTo(memoryStream);
                return memoryStream.ToArray();
            }
        }

        #region TryParse with Nullables

        /// <summary>
        /// Makes it possible to directly parse a string as double? (double? is used because all numeric types except decimal can be easily converted from and to double).
        /// Usage: double? val = textbox1.Text.TryParse().
        /// </summary>
        /// <param name="value"></param>
        /// <returns>THe string converted to double?.</returns>
        public static double? TryParse(this string value)
        {
            double? parsed = null;

            try
            {
                if (string.IsNullOrEmpty(value))
                    return parsed;

                double parsedValue;
                parsed = double.TryParse(value.ToString(), out parsedValue) ? (double?)parsedValue : null;

                return parsed;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static bool TryParse(this object value, out int? parsed)
        {
            parsed = null;

            try
            {
                if (value == null)
                    return true;

                int parsedValue;
                parsed = int.TryParse(value.ToString(), out parsedValue) ? (int?)parsedValue : null;
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static bool TryParse(this object value, out short? parsed)
        {
            parsed = null;

            try
            {
                if (value == null)
                    return true;

                short parsedValue;
                parsed = short.TryParse(value.ToString(), out parsedValue) ? (short?)parsedValue : null;
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static bool TryParse(this object value, out DateTime? parsed)
        {
            parsed = null;

            try
            {
                if (value == null)
                    return true;

                DateTime parsedValue;
                parsed = DateTime.TryParse(value.ToString(), out parsedValue) ? (DateTime?)parsedValue : null;
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static bool TryParse(this object value, out double? parsed)
        {
            parsed = null;

            try
            {
                if (value == null)
                    return true;

                double parsedValue;
                parsed = double.TryParse(value.ToString(), out parsedValue) ? (double?)parsedValue : null;
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static bool TryParse(this object value, out decimal? parsed)
        {
            parsed = null;

            try
            {
                if (value == null)
                    return true;

                decimal parsedValue;
                parsed = decimal.TryParse(value.ToString(), out parsedValue) ? (decimal?)parsedValue : null;
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
        #endregion
    }

    public class Variance
    {
        public string Prop { get; set; }
        public object ValA { get; set; }
        public object ValB { get; set; }

        public override string ToString()
        {
            return Prop + ": " + ValA + " --> " + ValB;
        }
    }
}
