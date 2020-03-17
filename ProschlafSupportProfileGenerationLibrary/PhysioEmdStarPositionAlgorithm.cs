using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static ProschlafSupportProfileGenerationLibrary.GenerationConstants;

namespace ProschlafSupportProfileGenerationLibrary
{
    /// <summary>
    /// This class holds the logic for a rudimentary algorithm that determines the position of the "star"-element and the firmness of a Physio EMD / "Die Variable" mattress.
    /// </summary>
   public abstract class PhysioEmdStarPositionAlgorithm
    {
        /// <summary>
        /// Generates a mattress firmness suggestion and calculates the proper position of the "star"-element in the mattress.
        /// </summary>
        /// <param name="gender"></param>
        /// <param name="personHeightCm">The height of the test person in centimeters.</param>
        /// <param name="personWeightKg">The weight of the test person in kilogram.</param>
        /// <param name="result">Holds the resulting profile that was generated through this algorithm. NULL if something went wrong.</param>
        /// <returns>Null if everything went fine or an exception.</returns>
        public static Exception GenerateSuggestionBasedOnPersonData(Genders gender, int personHeightCm, int personWeightKg, out PhysioEmdGenerationResult result)
        {
            try
            {
                result = null;

                //determine firmness
                FirmnessLevels firmnessLevel = FirmnessLevels.None;

                if (personWeightKg <= 85)
                    firmnessLevel = FirmnessLevels.H2;
                else
                    firmnessLevel = FirmnessLevels.H3;

                //determine star position (0 - 2)
                int starPosition = -1;

                if (gender == Genders.Female)
                {
                    if (personHeightCm <= 160)
                    {
                        starPosition = 0;
                    }
                    else if (personHeightCm <= 180)
                    {
                        starPosition = 1;
                    }
                    else
                    {
                        starPosition = 2;
                    }
                }
                else
                {
                    if (personHeightCm <= 165)
                    {
                        starPosition = 0;
                    }
                    else if (personHeightCm <= 185)
                    {
                        starPosition = 1;
                    }
                    else
                    {
                        starPosition = 2;
                    }
                }

                result = new PhysioEmdGenerationResult() { FirmnessLevel = firmnessLevel, Position = starPosition };
                return null;
            }
            catch (Exception ex)
            {
                result = null;
                return ex;
            }
        }
    }

    public class PhysioEmdGenerationResult
    {
        /// <summary>
        /// The position of the star element (0 - 2).
        /// </summary>
        public int Position { get; set; }

        /// <summary>
        /// The suggested firmness level for the mattress.
        /// </summary>
        public FirmnessLevels FirmnessLevel { get; set; }
    }
}
