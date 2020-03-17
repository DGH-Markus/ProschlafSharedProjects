using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using static ProschlafSupportProfileGenerationLibrary.GenerationConstants;

namespace ProschlafSupportProfileGenerationLibrary
{
    /// <summary>
    /// This class holds the latest pressure values for the support profile stamps and roles as used in the "Ergonometer NL" and other softwares.
    /// </summary>
   public abstract class OrthonometerProfileElements
    {
        #region Constants
        readonly static string[] NEGATIVE_PRESSURE_VALUE_ORTHONIC_BARS = new string[] { "W", "T", "K" }; //the bares with these letters have pressure values below 0 millibars
        readonly static string[] POSITIVE_PRESSURE_VALUE_ORTHONIC_BARS = new string[] { "A", "B", "R" }; //these bars are above 0 millibars

        public const int EVACUATE_PRESSURE_BASE_VALUE = 10; //apply a pressure of 10mb before doing any evacuation
        #endregion

        /// <summary>
        /// Determines whether a given element corresponds to a positive pressure value (value >= ~10mB) or not.
        /// </summary>
        /// <param name="element"></param>
        /// <returns></returns>
        public static bool ElementHasPositivePressure(ProfileElements element, string letter)
        {
            if (string.IsNullOrEmpty(letter))
                throw new ArgumentNullException("letter", "Is empty: " + (letter != null));

            if (element == ProfileElements.OrthonicBars)
            {
                if (POSITIVE_PRESSURE_VALUE_ORTHONIC_BARS.Contains(letter))
                    return true;
                else if (NEGATIVE_PRESSURE_VALUE_ORTHONIC_BARS.Contains(letter))
                    return false;
            }

            throw new UnknownProfileElementException() { Element = element, Letter = letter };
        }

        /// <summary>
        /// Gets the pressure value in millibars associated with the given profile element.
        /// If the element has no positive pressure value, 0 is returned.
        /// </summary>
        /// <param name="stamp"></param>
        /// <returns></returns>
        /// <exception cref="UnknownProfileElementException">Thrown when the provided element is either empty or unknown.</exception>
        public static int GetPressureValueForElement(ProfileElements element, string letter)
        {
            if (element == ProfileElements.OrthonicBars)
            {
                switch (letter)
                {
                    case null:
                    case "":
                        return 0;
                    case "W":
                    case "T":
                    case "K":
                        return 0; //negative pressure value bars have no pressure value (must be 0)
                    case "A":
                        return 20;
                    case "B":
                        return 70;
                    case "R":
                        return 127;
                    default:
                        throw new UnknownProfileElementException() { Letter = letter, Element = element };
                }
            }

            throw new UnknownProfileElementException() { Letter = letter, Element = element };
        }

        /// <summary>
        /// Gets the negative evacuation time in seconds associated with the given element.
        /// If the element has a positive pressure value, 0 is returned.
        /// </summary>
        /// <param name="letter"></param>
        /// <returns>A negative time value in seconds.</returns>
        /// <exception cref="UnknownProfileElementException">Thrown when the provided element is either empty or unknown.</exception>
        public static int GetEvacuationTimeForElement(ProfileElements element, string letter)
        {
            if (element == ProfileElements.OrthonicBars)
            {
                switch (letter)
                {
                    case null:
                    case "":
                        return 0;
                    case "W":
                        return -6;
                    case "T":
                        return -4;
                    case "K":
                        return -2;
                    case "A":  //bars with a positive pressure value don't need to be evacuated
                    case "B":
                    case "R":
                        return 0;
                    default:
                        throw new UnknownProfileElementException() { Element = element, Letter = letter };
                }
            }

            throw new UnknownProfileElementException() { Element = element, Letter = letter };
        }
    }
}

