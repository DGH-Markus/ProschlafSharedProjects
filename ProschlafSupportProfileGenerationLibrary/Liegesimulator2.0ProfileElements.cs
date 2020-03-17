using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace ProschlafSupportProfileGenerationLibrary
{
    /// <summary>
    /// This class holds the latest pressure values for the support profile stamps and roles as used in the "Liegesimulator2.0" application.
    /// </summary>
   public abstract class Liegesimulator2_0ProfileElements
    {
        #region Constants
        readonly static string[] NEGATIVE_PRESSURE_VALUE_STAMPS = new string[] { "W", "T", "K" }; //the elements with these letters have pressure values below 0 millibars
        readonly static string[] POSITIVE_PRESSURE_VALUE_STAMPS = new string[] { "A", "S", "L", "B", "R" }; //these elements are above 0 millibars

        readonly static string[] NEGATIVE_PRESSURE_VALUE_ROLES = new string[] { "W", "T", "K" }; //the elements with these letters have pressure values below 0 millibars
        readonly static string[] POSITIVE_PRESSURE_VALUE_ROLES = new string[] { "A", "B", "R" }; //these elements are above 0 millibars

        public const int EVACUATE_PRESSURE_BASE_VALUE = 10; //apply a pressure of 10mb before doing any evacuation
        #endregion

        /// <summary>
        /// Determines whether a given element corresponds to a positive pressure value (value >= ~10mB) or not.
        /// </summary>
        /// <param name="element"></param>
        /// <returns></returns>
        public static bool ElementHasPositivePressure(GenerationConstants.ProfileElements element, string letter)
        {
            if (string.IsNullOrEmpty(letter))
                throw new ArgumentNullException("letter", "Is empty: " + (letter != null));

            if (element == GenerationConstants.ProfileElements.Stamps)
            {
                if (POSITIVE_PRESSURE_VALUE_STAMPS.Contains(letter))
                    return true;
                else if (NEGATIVE_PRESSURE_VALUE_STAMPS.Contains(letter))
                    return false;
            }
            else if (element == GenerationConstants.ProfileElements.Roles)
            {
                if (POSITIVE_PRESSURE_VALUE_ROLES.Contains(letter))
                    return true;
                else if (NEGATIVE_PRESSURE_VALUE_ROLES.Contains(letter))
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
        public static int GetPressureValueForElement(GenerationConstants.ProfileElements element, string letter)
        {
            if (element == GenerationConstants.ProfileElements.Stamps)
            {
                switch (letter)
                {
                    case null:
                    case "":
                        return 0;
                    case "W":
                    case "T":
                        return 0; //negative pressure value stamps have no pressure value (must be 0)
                    case "K":
                        return 0;
                    case "S":
                    case "A":
                        return 15;
                    case "L":
                        return 20;
                    case "B":
                        return 72;
                    case "R":
                        return 127;
                    default:
                        throw new UnknownProfileElementException() { Letter = letter, Element = element };
                }
            }
            else if (element == GenerationConstants.ProfileElements.Roles)
            {
                switch (letter)
                {
                    case null:
                    case "":
                        return 0;
                    case "W":
                    case "T":
                        return 0; //negative pressure value roles have no pressure value (must be 0)
                    case "K":
                        return 0;
                    case "A":
                        return 15;
                    case "B":
                        return 72;
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
        /// Note that the simulator device has to apply a pressure of [EVACUATE_PRESSURE_BASE_VALUE] before it should start draining air.
        /// If the element has a positive pressure value, 0 is returned.
        /// </summary>
        /// <param name="letter"></param>
        /// <returns>A negative time value in seconds.</returns>
        /// <exception cref="UnknownProfileElementException">Thrown when the provided element is either empty or unknown.</exception>
        public static int GetEvacuationTimeForElement(GenerationConstants.ProfileElements element, string letter)
        {
            if (element == GenerationConstants.ProfileElements.Stamps)
            {
                switch (letter)
                {
                    case null:
                    case "":
                        return 0;
                    case "W":
                        return -7; //evacuate 7 secs
                    case "T":
                        return -5; //evacuate 5 (4800ms) secs
                    case "K":
                        return -2; //evacuate 2 (2100ms) secs 
                    case "S":  //stamps with a positive pressure value don't need to be evacuated
                    case "A":
                    case "L":
                    case "B":
                    case "R":
                    case "H":
                        return 0;
                    default:
                        throw new UnknownProfileElementException() { Element = element, Letter = letter };
                }
            }
            else if (element == GenerationConstants.ProfileElements.Roles)
            {
                switch (letter)
                {
                    case null:
                    case "":
                        return 0;
                    case "W":
                        return -7; //evacuate 7 secs
                    case "T":
                        return -5; //evacuate 5 (4800ms) secs
                    case "K":
                        return -2; //evacuate 2 (2100ms) secs 
                    case "S":  //roles with a positive pressure value don't need to be evacuated
                    case "A":
                    case "B":
                    case "R":
                    case "H":
                        return 0;
                    default:
                        throw new UnknownProfileElementException() { Element = element, Letter = letter };
                }
            }

            throw new UnknownProfileElementException() { Element = element, Letter = letter };
        }
    }
}

