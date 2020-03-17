using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static ProschlafSupportProfileGenerationLibrary.GenerationConstants;
using static ProschlafSupportProfileGenerationLibrary.StampProfileGenerationAlgorithm;

namespace ProschlafSupportProfileGenerationLibrary
{
    /// <summary>
    /// This class holds the logic for the intelligent support profile generation algorithm used to output  Liegesimulator_2._0 (or similar) mattress support profiles using role elements. 
    /// The generated profile mainly consists of 4 roles and the mattress firmness.
    /// </summary>
   public abstract class RoleProfileGenerationAlgorithm_SfContact
    {
        #region Consts
        public const string NEUTRAL_ELEMENT_LETTER = "S";
        #endregion

        /// <summary>
        /// Generates a profile based in the input person data.
        /// An  Liegesimulator_2._0 profile consists of 4 letters. 3 of these letters are the same, the other one is individual. The individual one can have 1 out of 3 available firmnesses.
        /// The position of the individual profile role is based on the position of the person's lordosis area.
        /// This algorithm attempts to find the position and hardness of the individual role and returns a support profile.
        /// Currently, the lordosis is considered to be between then 6th and the 9th role and thus one of these roles has to be the individual one. All other roles are set to the neutral letter 'N'.
        /// </summary>
        /// <param name="gender"></param>
        /// <param name="height">Height in cm.</param>
        /// <param name="weight">Weight in kg.</param>
        /// <param name="pressureMeasurementValues">4 measurement values from the simulator's pressure measurement.</param>
        /// <returns></returns>
        public static Exception GetProfileForPerson(MeasurementPositions position, Genders gender, int height, int weight, int[] pressureMeasurementSupine, int[] pressureMeasurementLateral, out SfContactProfile result)
        {
            result = default(SfContactProfile);

            if (pressureMeasurementSupine == null || pressureMeasurementSupine.Length != 12)
                return new ArgumentException("Measurement values invalid!", "pressureMeasurementSupine");

            if (pressureMeasurementLateral == null || pressureMeasurementLateral.Length != 12)
                return new ArgumentException("Measurement values invalid!", "pressureMeasurementLateral");

            if (height < 1 || weight < 1)
                return new ArgumentException("Body data invalid!", "height/weight");

            result = new  SfContactProfile() { Gender = gender, Height = height, Weight = weight };
            result.SupportProfile = "NNNN".ToStringArray();

            /*Pelvis area is always the peak value from right to left. */
            int[] measurementArray = position == MeasurementPositions.Supine ? pressureMeasurementSupine : pressureMeasurementLateral;
            int pelvisIndex = measurementArray.Length - 1;

            //get the pelvis index (peak value from bottom to top, taking 6 values into account)
            for (int i = measurementArray.Length - 1; i >= 6; i--)
                if (measurementArray[i] >= measurementArray[pelvisIndex])
                    pelvisIndex = i;
                else if (measurementArray[i] < measurementArray[pelvisIndex])
                    break;

            if (pelvisIndex == 11 && height < 190) //pelvis area cannot be at the last stamp for person smaller than 190cm
                pelvisIndex = 10;

            if (measurementArray[pelvisIndex] == measurementArray[pelvisIndex - 1])
                pelvisIndex--;

            /* now try to find the position of the lordosis role in order to be able to modify the hardness of the role in that position */
            int lordosisIndex = GenerationUtils.GetIndexOfMinimum(measurementArray, 5, pelvisIndex - 1);

            /*special rules to make sure the profile makes sense */
            if (lordosisIndex + 1 == pelvisIndex) //lordosis and pelvis index must not be after one another
                lordosisIndex--;

            if (lordosisIndex < 5)
                lordosisIndex = 5; //lordosis must always be between role 6 and role 9
            else if (lordosisIndex > 8)
                lordosisIndex = 8; //lordosis must always be between role 6 and role 9

            //calculate BMI
            double bmi = weight / (Math.Pow((double)height / 100d, 2));

            //determine the position and hardness of the individual lordosis role
            string replacementLetter = bmi < 20 ? "G" : bmi < 27 ? "R" : "B"; //< 20 means underweight person --> soft role. 20-27 = normal. >27 = overweight person --> firm role.
            int replacementIndex = lordosisIndex - 5; //-5 because the 4 roles used in  Liegesimulator_2._0 are roles 6 through 9 (and the indices represent roles 1 through 12)
            result.SupportProfile = result.SupportProfile.ReplaceAtIndex(replacementIndex, replacementLetter[0]); //replace the normal role in the standard profile with the individual role that was generated by the algorithm
            result.FirmRoleIndex = lordosisIndex;
            return null;
        }
    }

    /// <summary>
    /// Represents a profile generation result.
    /// </summary>
    public struct SfContactProfile
    {
        /// <summary>
        /// The resulting support profile.
        /// </summary>
        public string[] SupportProfile { get; set; }

        /// <summary>
        /// If set to true, this indicates that the test person MAY have lain in a wrong positon on the mattress during the measurement.
        /// </summary>
        public bool IsPersonLyingPositionWrong { get; set; }

        public int Height { get; set; }
        public int Weight { get; set; }
        public Genders Gender { get; set; }

        public int FirmRoleIndex { get; set; }
    }
}
