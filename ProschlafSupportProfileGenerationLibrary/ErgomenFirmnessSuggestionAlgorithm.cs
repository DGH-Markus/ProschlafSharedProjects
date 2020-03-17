using System;
using System.Collections.Generic;
using System.Text;
using static ProschlafSupportProfileGenerationLibrary.GenerationConstants;

namespace ProschlafSupportProfileGenerationLibrary
{
   public abstract class ErgomenFirmnessSuggestionAlgorithm
    {
        /// <summary>
        /// Gets a firmness suggestion for the "Ergo Men" / "Ergo Women" mattress model.
        /// The algorithm has been defined by Markus and as such is neither accurate nor correct.
        /// </summary>
        /// <param name="gender"></param>
        /// <param name="height"></param>
        /// <param name="weight"></param>
        /// <param name="pressureMeasurementValuesComplete"></param>
        /// <param name="result">Holds a firmness suggestion between H1 and H3.</param>
        /// <returns></returns>
        public static Exception GetFirmnessSuggestionForPerson(Genders gender, int height, int weight, int[] pressureMeasurementValuesComplete, out ErgomenFirmnessSuggestion result)
        {
            try
            {
                FirmnessLevels firmness = FirmnessLevels.None;

                double heightM = height / 100d;
                double bmi = weight / (heightM * heightM); //body mass index

                if (gender == Genders.Male)
                {
                    if (bmi < 20d)
                        firmness = FirmnessLevels.H1;
                    else if (bmi < 26d)
                        firmness = FirmnessLevels.H2;
                    else
                        firmness = FirmnessLevels.H3;
                }
                else if (gender == Genders.Female)
                {
                    if (bmi < 19d)
                        firmness = FirmnessLevels.H1;
                    else if (bmi < 25d)
                        firmness = FirmnessLevels.H2;
                    else
                        firmness = FirmnessLevels.H3;
                }
                else
                {
                    result = null;
                    return new Exception("Cannot suggest a mattress firmness without testperson's gender.");
                }

                result = new ErgomenFirmnessSuggestion() { Firmness = firmness };
                return null;
            }
            catch (Exception ex)
            {
                result = null;
                return ex;
            }
        }
    }

    public class ErgomenFirmnessSuggestion
    {
        public FirmnessLevels Firmness { get; set; }
    }
}
