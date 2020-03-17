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
    public abstract class ErgonometerNLProfileElements
    {
        #region Constants
        readonly static string[] NEGATIVE_PRESSURE_VALUE_STAMPS = new string[] { "W", "T", "K" }; //the stamps with these letters have pressure values below 0 millibars
        readonly static string[] POSITIVE_PRESSURE_VALUE_STAMPS = new string[] { "D", "L", "B", "R", "H", "S" }; //these stamps are above 0 millibars

        readonly static string[] NEGATIVE_PRESSURE_VALUE_ROLES = new string[] { "W", "T", "K" }; //the roles with these letters have pressure values below 0 millibars
        readonly static string[] POSITIVE_PRESSURE_VALUE_ROLES = new string[] { "D", "B", "R", "H" }; //these roles are above 0 millibars

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

            if (element == ProfileElements.Stamps)
            {
                if (POSITIVE_PRESSURE_VALUE_STAMPS.Contains(letter))
                    return true;
                else if (NEGATIVE_PRESSURE_VALUE_STAMPS.Contains(letter))
                    return false;
            }
            else if (element == ProfileElements.Roles)
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
        public static int GetPressureValueForElement(ProfileElements element, string letter)
        {
            if (element == ProfileElements.Stamps)
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
                    case "D": //silver in other applications
                        return 15;
                    case "L":
                        return 20;
                    case "B":
                        return 30;
                    case "R":
                        return 50;
                    case "H": //"H" is a special stamp that is not actually existing, but is used as "hardest" value in the Ergonometer NL software
                        return 127;
                    default:
                        throw new UnknownProfileElementException() { Letter = letter, Element = element };
                }
            }
            else if (element == ProfileElements.Roles) //these values differ sligthly from the Stamps
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
                    case "D":
                        return 20;
                    case "B":
                        return 70;
                    case "R":
                        return 127;
                    case "H": //"H" is a special stamp that is not actually existing, but is used as "hardest" value in the Ergonometer NL software
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
            if (element == ProfileElements.Stamps)
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
                    case "D":  //stamps with a positive pressure value don't need to be evacuated
                    case "S":
                    case "L":
                    case "B":
                    case "R":
                    case "H":
                        return 0;
                    default:
                        throw new UnknownProfileElementException() { Element = element, Letter = letter };
                }
            }
            else if (element == ProfileElements.Roles) //these values differ sligthly from the Stamps version and have another base value (20mb) from which the evacuation starts
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
                        return -3; //evacuate 2 (2100ms) secs
                    case "D":  //roles with a positive pressure value don't need to be evacuated
                    case "S":
                    case "L":
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

