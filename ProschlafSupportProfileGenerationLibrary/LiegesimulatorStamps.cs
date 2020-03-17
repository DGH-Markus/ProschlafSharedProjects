using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using static ProschlafSupportProfileGenerationLibrary.GenerationConstants;

namespace ProschlafSupportProfileGenerationLibrary
{
    /// <summary>
    /// This class holds the latest pressure values for the support profile stamps as used in most of the Proschlaf applications (e.g. "Liegesimulator" and "Ergonometer (Lutz)").
    /// </summary>
   public abstract class LiegesimulatorStamps
    {
        #region Constants
        readonly static string[] NEGATIVE_PRESSURE_VALUE_STAMPS = new string[] { "W", "T", "K" }; //the stamps with these letters have pressure values below 0 millibars
        readonly static string[] POSITIVE_PRESSURE_VALUE_STAMPS = new string[] { "S", "L", "B", "R", "H", }; //these stamps are above 0 millibars
        #endregion

        /// <summary>
        /// Determines whether a given stamp corresponds to a positive pressure value (value >= ~10mB) or not.
        /// </summary>
        /// <param name="stamp"></param>
        /// <returns></returns>
        public static bool StampHasPositivePressure(string stamp)
        {
            if (string.IsNullOrEmpty(stamp))
                throw new ArgumentNullException("stamp", "Is empty: " + (stamp != null));

            if (POSITIVE_PRESSURE_VALUE_STAMPS.Contains(stamp))
                return true;
            else if (NEGATIVE_PRESSURE_VALUE_STAMPS.Contains(stamp))
                return false;

            throw new UnknownProfileElementException() { Element = ProfileElements.Stamps, Letter = stamp  };
        }

        /// <summary>
        /// Gets the pressure value in millibars associated with the given stamp.
        /// If the stamp has no positive pressure value, 0 is returned.
        /// Note that these stamps do NOT correspond with the stamps in other Proschlaf applications (currently, they are all 1 level softer than in other applications).
        /// </summary>
        /// <param name="stamp"></param>
        /// <returns></returns>
        /// <exception cref="UnknownProfileElementException">Thrown when the provided stamp is either empty or unknown.</exception>
        public static int GetPressureValueForStamp(string stamp)
        {
            switch (stamp)
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
                    return 15;
                case "L":
                    return 20;
                case "B":
                    return 30;
                case "R":
                    return 50;
                case "H": //"H" is a special stamp that is not actually existing, but is used as "hardest" value in the VitarioDemo software
                        return 127;
                default:
                    throw new UnknownProfileElementException() { Element = ProfileElements.Stamps, Letter = stamp };
            }
        }

        /// <summary>
        /// Gets the negative evacuation time in seconds associated with the given stamp.
        /// If the stamp has a positive pressure value, 0 is returned.
        /// Note that these stamps do NOT correspond with the stamps in other Proschlaf applications (currently, they are all 1 level softer than in other applications).
        /// </summary>
        /// <param name="stamp"></param>
        /// <returns></returns>
        /// <exception cref="UnknownProfileElementException">Thrown when the provided stamp is either empty or unknown.</exception>
        public static int GetEvacuationTimeForStamp(string stamp)
        {
            switch (stamp)
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
                case "S":
                case "L":
                case "B":
                case "R":
                case "H":
                    return 0;
                default:
                    throw new UnknownProfileElementException() { Element = ProfileElements.Stamps, Letter = stamp };
            }
        }
    }
}

