using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static ProschlafSupportProfileGenerationLibrary.GenerationConstants;

namespace ProschlafSupportProfileGenerationLibrary
{
    /// <summary>
    /// This class holds the logic for the intelligent profile generation algorithm used to output "Vitario Kopfkissen" (or similar) profiles. 
    /// </summary>
   public abstract class PillowProfileGenerationAlgorithm
    {
        #region Explanation to pillow setup
        /*
         * The pillow consists of 4 levels:
         * the base module (3 variations, non-optional)
         * the 1st insert (thick, optional)
         * the 2nd insert (thin, optional)
         * the wedge (2 variations, optional)
         * 
         * A code consisting of 3 digits defines the setup of the pillow. The code looks as follows:
         * Base Module: 0 = With Role, 1 = No Role, 2 = Split Role
         * 1st and 2nd insert: 0 = no inserts, 1 = 1st insert only, 2 = 2nd insert only, 3 = both inserts
         * Wedge: 0 = no wedge, 1 = thick towards the foot end of the bed, 2 = thick at the head-end of the pillow
         * 
         * Example code for a base module with role and one thick insert, no wedge: 010
         * 
         * For the inserts, the two digits are simply added up.
         */
        #endregion

        #region Enums
        public enum PillowBaseModuleVariants { WithRole = 0, NoRole = 1, SplitRole = 2 }
        public enum PillowInsertVariants { None = 0, Thick = 1, Thin = 2, Both = 3 };
        public enum PillowWedgeVariants { None, ThickTowardsFootEnd = 1, ThickTowardsHeadEnd = 2 };
        #endregion

        /// <summary>
        /// Generates a 3-letter pillow profile using an intelligent algorithm for the specified customer.
        /// </summary>
        /// <param name="position"></param>
        /// <param name="pressureMeasurementSupine">Expects 12 measurement values between 0 and 100 millibar as measured with the test person laying on the back.</param>
        /// <param name="pressureMeasurementLateral">Expects 12 measurement values between 0 and 100 millibar as measured with the test person laying on the side.</param>
        /// <param name="gender"></param>
        /// <param name="result">Holds the resulting pillow setup that was generated through this algorithm.</param>
        /// <returns>NULL if everything went fine or an exception.</returns>
        public static Exception GenerateProfileBasedOnPressureMapping(TestpersonSleepPositions sleepPosition, int[] pressureMeasurementSupine, int[] pressureMeasurementLateral, Genders gender, out PillowProfileGenerationResult result)
        {
            try
            {
                PillowBaseModuleVariants baseModule = PillowBaseModuleVariants.NoRole;
                PillowInsertVariants inserts = PillowInsertVariants.None;
                PillowWedgeVariants wedge = PillowWedgeVariants.None;

                if (sleepPosition == TestpersonSleepPositions.Lateral) //calculation for lateral position is more complex than the other 2
                {
                    #region Lateral sleeping position
                    int shoulderIndex;

                    /* Determine shoudler index (highest pressure from roles 1-4) */
                    int[] shoulderAreaArray = pressureMeasurementLateral;
                    shoulderIndex = GenerationUtils.GetIndexOfMaximum(shoulderAreaArray, 0, 3);

                    if (!GenerationUtils.IsLeftSideEqual(shoulderIndex, shoulderAreaArray) && shoulderIndex < 3 && GenerationUtils.IsRightSideEqual(shoulderIndex, shoulderAreaArray)) //if the value to the right is the same, move the shoulder index one to the right (e.g. 17 19 19 15)
                        shoulderIndex++;
                    else if (shoulderIndex == 0 && GenerationUtils.IsRightSideEqual(shoulderIndex, shoulderAreaArray) && shoulderAreaArray[2] == shoulderAreaArray[0]) //e.g. 17 17 17 19
                        shoulderIndex = 1;

                    int shoulderIndexPressureValue = pressureMeasurementLateral[shoulderIndex]; //the absolute pressure value in millibar
                    baseModule = PillowBaseModuleVariants.WithRole; //we always have the base module

                    if (gender == Genders.Female)
                    {
                        if(shoulderIndexPressureValue < 8)
                        {
                            inserts = PillowInsertVariants.Thin;
                            wedge = PillowWedgeVariants.ThickTowardsHeadEnd;
                        }
                        else if (shoulderIndexPressureValue < 14)
                        {
                            inserts = PillowInsertVariants.Thick;
                            wedge = PillowWedgeVariants.ThickTowardsHeadEnd;
                        }
                        else
                        {
                            inserts = PillowInsertVariants.Both;
                            wedge = PillowWedgeVariants.ThickTowardsHeadEnd;
                        }
                    }
                    else
                    {
                        if (shoulderIndexPressureValue < 7)
                        {
                            inserts = PillowInsertVariants.Thin;
                            wedge = PillowWedgeVariants.ThickTowardsHeadEnd;
                        }
                        else if (shoulderIndexPressureValue < 13)
                        {
                            inserts = PillowInsertVariants.Thick;
                            wedge = PillowWedgeVariants.ThickTowardsHeadEnd;
                        }
                        else
                        {
                            inserts = PillowInsertVariants.Both;
                            wedge = PillowWedgeVariants.ThickTowardsHeadEnd;
                        }
                    }
                    #endregion
                }
                else if (sleepPosition == TestpersonSleepPositions.Supine)
                {
                    //supine position only adds a wedge for males
                    if (gender == Genders.Male)
                    {
                        baseModule = PillowBaseModuleVariants.WithRole;
                        wedge = PillowWedgeVariants.ThickTowardsHeadEnd;
                    }
                    else
                    {
                        baseModule = PillowBaseModuleVariants.WithRole;
                    }
                }
                else if (sleepPosition == TestpersonSleepPositions.Prone)
                {
                    //prone position is always only with the base module; nothing else
                    baseModule = PillowBaseModuleVariants.WithRole;
                }

                //concat the resulting pillow code
                string code = ((int)baseModule).ToString() + ((int)inserts).ToString() + ((int)wedge).ToString();
                result = new PillowProfileGenerationResult() { PillowCode = code, BaseModule = baseModule, Inserts = inserts, Wedge = wedge };

                return null;
            }
            catch (Exception ex)
            {
                result = default(PillowProfileGenerationResult);
                return ex;
            }
        }
    }

    public struct PillowProfileGenerationResult
    {
        /// <summary>
        /// The resulting 3-letter code representing the pillow's profile.
        /// </summary>
        public string PillowCode { get; set; }

        public PillowProfileGenerationAlgorithm.PillowBaseModuleVariants BaseModule { get; set; }
        public PillowProfileGenerationAlgorithm.PillowInsertVariants Inserts { get; set; }
        public PillowProfileGenerationAlgorithm.PillowWedgeVariants Wedge { get; set; }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder("Basismodul: ");

            switch(BaseModule)
            {
                case PillowProfileGenerationAlgorithm.PillowBaseModuleVariants.NoRole:
                    sb.Append("keine Rolle");
                    break;
                case PillowProfileGenerationAlgorithm.PillowBaseModuleVariants.WithRole:
                    sb.Append("mit Rolle");
                    break;
                case PillowProfileGenerationAlgorithm.PillowBaseModuleVariants.SplitRole:
                    sb.Append("geteilte Rolle");
                    break;
                default:
                    break;
            }

            sb.Append(Environment.NewLine + "Platte(n): ");

            switch (Inserts)
            {
                case PillowProfileGenerationAlgorithm.PillowInsertVariants.None:
                    sb.Append("ohne");
                    break;
                case PillowProfileGenerationAlgorithm.PillowInsertVariants.Thin:
                    sb.Append("1cm");
                    break;
                case PillowProfileGenerationAlgorithm.PillowInsertVariants.Thick:
                    sb.Append("2cm");
                    break;
                case PillowProfileGenerationAlgorithm.PillowInsertVariants.Both:
                    sb.Append("2cm + 1cm");
                    break;
                default:
                    break;
            }

            sb.Append(Environment.NewLine + "Keil: ");

            switch (Wedge)
            {
                case PillowProfileGenerationAlgorithm.PillowWedgeVariants.None:
                    sb.Append("ohne");
                    break;
                case PillowProfileGenerationAlgorithm.PillowWedgeVariants.ThickTowardsFootEnd:
                    sb.Append("dickes Ende Richtung Fußende");
                    break;
                case PillowProfileGenerationAlgorithm.PillowWedgeVariants.ThickTowardsHeadEnd:
                    sb.Append("dickes Ende Richtung Kopfende");
                    break;
                default:
                    break;
            }

            return sb.ToString();
        }
    }
}
