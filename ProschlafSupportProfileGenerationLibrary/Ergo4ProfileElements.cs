using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Media;

namespace ProschlafSupportProfileGenerationLibrary
{
    public abstract class Ergo4ProfileElements
    {
        #region Constants
        readonly static string[] NEGATIVE_PRESSURE_VALUE_ROLES = new string[] { "E" }; //the elements with these letters have pressure values below 0 millibars
        readonly static string[] POSITIVE_PRESSURE_VALUE_ROLES = new string[] { "S", "G", "B", "R" }; //these elements are above 0 millibars

        private static readonly Color Role_Color_E = Color.FromRgb(175, 181, 177);
        private static readonly Color Role_Color_S = Color.FromRgb(149, 151, 146);
        private static readonly Color Role_Color_G = Color.FromRgb(63, 62, 67);
        private static readonly Color Role_Color_B = Color.FromRgb(22, 48, 107);
        private static readonly Color Role_Color_R = Color.FromRgb(188, 46, 26);
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
            if (element == GenerationConstants.ProfileElements.Ergo4Roles)
            {
                switch (letter)
                {
                    case null:
                    case "":
                        return 0;
                    case "E":
                        return 0; //negative pressure value elements have no pressure value (must be 0)
                    case "S":
                        return 15;
                    case "G":
                        return 20;
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
        /// If the element has a positive pressure value, 0 is returned.
        /// </summary>
        /// <param name="letter"></param>
        /// <returns>A negative time value in seconds.</returns>
        /// <exception cref="UnknownProfileElementException">Thrown when the provided element is either empty or unknown.</exception>
        public static int GetEvacuationTimeForElement(GenerationConstants.ProfileElements element, string letter)
        {
            if (element == GenerationConstants.ProfileElements.Ergo4Roles)
            {
                switch (letter)
                {
                    case null:
                    case "":
                        return 0;
                    case "E":
                        return -3; //evacuate 3 secs
                    case "S": //elements with a positive pressure value don't need to be evacuated
                    case "G":
                    case "B":
                    case "R":
                        return 0;
                    default:
                        throw new UnknownProfileElementException() { Element = element, Letter = letter };
                }
            }

            throw new UnknownProfileElementException() { Element = element, Letter = letter };
        }

        /// <summary>
        /// Gets the corresponding color for the specified Ergo 4 role element.
        /// </summary>
        /// <param name="letter"></param>
        /// <param name="tightness"></param>
        /// <returns></returns>
        public static Color GetColorForProfileLetter(string letter)
        {
            if (letter == "E")
                return Role_Color_E;
            else if (letter == "S")
                return Role_Color_S;
            else if (letter == "G")
                return Role_Color_G;
            else if (letter == "B")
                return Role_Color_B;
            else if (letter == "R")
                return Role_Color_R;

            return Colors.Red;
        }
    }
}
