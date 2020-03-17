using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace ProschlafSupportProfileGenerationLibrary
{
    /// <summary>
    /// This class holds the latest pressure values for the support profile roles as used in the "SfContact" software.
    /// </summary>
   public abstract class SfContactProfileElements
    {
        #region Constants
        readonly static string[] NEGATIVE_PRESSURE_VALUE_ROLES = new string[] { "N" }; //the roles with these letters have pressure values below 0 millibars
        readonly static string[] POSITIVE_PRESSURE_VALUE_ROLES = new string[] { "G", "R", "B" }; //all roles are above 0 millibars (G = Grau, R = Rosa, B = Blau)
        #endregion

        /// <summary>
        /// Determines whether a given element corresponds to a positive pressure value (value >= ~10mB) or not.
        /// </summary>
        /// <returns></returns>
        public static bool ElementHasPositivePressure(string letter)
        {
            if (string.IsNullOrEmpty(letter))
                throw new ArgumentNullException("letter", "Is empty: " + (letter != null));

            if (POSITIVE_PRESSURE_VALUE_ROLES.Contains(letter))
                return true;
            else if (NEGATIVE_PRESSURE_VALUE_ROLES.Contains(letter))
                return false;

            throw new UnknownProfileElementException() { Letter = letter };
        }

        /// <summary>
        /// Gets the pressure value in millibars associated with the given profile element.
        /// If the element has no positive pressure value, an exception is thrown.
        /// </summary>
        /// <param name="letter"></param>
        /// <returns></returns>
        /// <exception cref="UnknownProfileElementException">Thrown when the provided element is either empty or unknown.</exception>
        public static int GetPressureValueForElement(string letter)
        {
            switch (letter)
            {
                case "G":
                    return 15; //"Dunkelblau" in Ergonometer NL
                case "R":
                    return 20; //like "Large" in in Ergonometer NL
                case "B": //"Blau" in Ergonometer NL
                    return 30;
                default:
                    throw new UnknownProfileElementException() { Letter = letter };
            }

            throw new UnknownProfileElementException() { Letter = letter };
        }

        /// <summary>
        /// Gets the negative evacuation time in seconds associated with the given element.
        /// If the element has a positive pressure value, an exception is thrown.
        /// </summary>
        /// <param name="letter"></param>
        /// <returns>A negative time value in seconds.</returns>
        /// <exception cref="UnknownProfileElementException">Thrown when the provided element is either empty or unknown.</exception>
        public static int GetEvacuationTimeForElement(string letter)
        {
            switch (letter)
            {
                case "N":
                    return -2; //"K" in Ergonometer NL
                default:
                    throw new UnknownProfileElementException() { Letter = letter };
            }

            throw new UnknownProfileElementException() { Letter = letter };
        }
    }
}
