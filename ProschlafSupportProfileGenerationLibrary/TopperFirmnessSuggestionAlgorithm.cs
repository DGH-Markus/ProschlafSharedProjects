using System;
using System.Collections.Generic;
using System.Text;
using static ProschlafSupportProfileGenerationLibrary.GenerationConstants;

namespace ProschlafSupportProfileGenerationLibrary
{
   public abstract class TopperFirmnessSuggestionAlgorithm
    {
        /// <summary>
        /// Gets a firmness suggestion for a generic topper based on the input data.
        /// The algorithm has been defined by Markus and as such is neither accurate nor correct.
        /// </summary>
        /// <param name="gender"></param>
        /// <param name="height"></param>
        /// <param name="weight"></param>
        /// <param name="pressureMeasurementValuesComplete"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        public static Exception GetTopperSuggestionForPerson(Genders gender, int height, int weight, int[] pressureMeasurementValuesComplete, out TopperFirmnessSuggestion result)
        {
            try
            {
                FirmnessLevels firmness = FirmnessLevels.None;

                double heightM = height / 100d;
                double bmi = weight / (heightM * heightM); //body mass index

                if (gender == Genders.Male)
                {
                    if (bmi < 21d)
                        firmness = FirmnessLevels.H1;
                    else if (bmi < 27d)
                        firmness = FirmnessLevels.H2;
                    else
                        firmness = FirmnessLevels.H3;
                }
                else if (gender == Genders.Female)
                {
                    if (bmi < 19d)
                        firmness = FirmnessLevels.H1;
                    else if (bmi < 27d)
                        firmness = FirmnessLevels.H2;
                    else
                        firmness = FirmnessLevels.H3;
                }
                else
                {
                    result = null;
                    return new Exception("Cannot suggest a topper firmness without testperson's gender.");
                }

                result = new TopperFirmnessSuggestion() { Firmness = firmness };
                return null;
            }
            catch (Exception ex)
            {
                result = null;
                return ex;
            }
        }
    }

    public class TopperFirmnessSuggestion
    {
        public FirmnessLevels Firmness { get; set; }
    }
}
