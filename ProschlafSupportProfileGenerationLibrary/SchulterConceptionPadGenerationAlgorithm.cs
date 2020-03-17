using System;
using System.Collections.Generic;
using System.Text;
using static ProschlafSupportProfileGenerationLibrary.GenerationConstants;
using static ProschlafSupportProfileGenerationLibrary.SchulterConceptionPadGenerationAlgorithm;

namespace ProschlafSupportProfileGenerationLibrary
{
    public abstract class SchulterConceptionPadGenerationAlgorithm
    {
        #region Enums
        public enum ShoulderPadFirmnessLevels { None = 0, T = 1, V = 2, K = 3 }; //"Türkis", "Visco", "Kaffee"
        #endregion

        /// <summary>
        /// Gets a firmness suggestion for the mattress and the shoulder pads of the mattress model 'Schulterconception'.
        /// The algorithm has been defined by Markus and as such is neither accurate nor correct.
        /// </summary>
        /// <param name="gender"></param>
        /// <param name="height"></param>
        /// <param name="weight"></param>
        /// <param name="pressureMeasurementValuesComplete"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        public static Exception GetFirmnessSuggestionForPerson(Genders gender, int height, int weight, int[] pressureMeasurementValuesComplete, out ShoulderPadFirmnessSuggestion result)
        {
            try
            {
                FirmnessLevels mattressFirmness = FirmnessLevels.None;
                ShoulderPadFirmnessLevels padFirmness = ShoulderPadFirmnessLevels.None;

                double heightM = height / 100d;
                double bmi = weight / (heightM * heightM); //body mass index

                if (gender == Genders.Female)
                {
                    if (bmi < 18d)
                    {
                        mattressFirmness = FirmnessLevels.H2;
                        padFirmness = ShoulderPadFirmnessLevels.T;
                    }
                    else if (bmi < 33d)
                    {
                        mattressFirmness = FirmnessLevels.H2;
                        padFirmness = ShoulderPadFirmnessLevels.V;
                    }
                    else
                    {
                        mattressFirmness = FirmnessLevels.H3;

                        if (bmi < 35d)
                            padFirmness = ShoulderPadFirmnessLevels.V;
                        else
                            padFirmness = ShoulderPadFirmnessLevels.K;
                    }
                }
                else if (gender == Genders.Male)
                {
                    if (bmi < 19d)
                    {
                        mattressFirmness = FirmnessLevels.H2;
                        padFirmness = ShoulderPadFirmnessLevels.T;
                    }
                    else if (bmi < 27d)
                    {
                        mattressFirmness = FirmnessLevels.H2;
                        padFirmness = ShoulderPadFirmnessLevels.V;
                    }
                    else
                    {
                        mattressFirmness = FirmnessLevels.H3;

                        if (bmi < 36d)
                            padFirmness = ShoulderPadFirmnessLevels.V;
                        else
                            padFirmness = ShoulderPadFirmnessLevels.K;
                    }
                }
                else
                {
                    result = null;
                    return new Exception("Cannot suggest a shoulder pad firmness without testperson's gender.");
                }

                result = new ShoulderPadFirmnessSuggestion() { Firmness = mattressFirmness, ShoulderPadsFirmness = padFirmness };
                return null;
            }
            catch (Exception ex)
            {
                result = null;
                return ex;
            }
        }
    }

    public class ShoulderPadFirmnessSuggestion
    {
        public FirmnessLevels Firmness { get; set; }
        public ShoulderPadFirmnessLevels ShoulderPadsFirmness { get; set; }
    }
}
