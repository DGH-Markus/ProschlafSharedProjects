using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static ProschlafSupportProfileGenerationLibrary.GenerationConstants;

namespace ProschlafSupportProfileGenerationLibrary
{
    /// <summary>
    /// This class holds the logic for the intelligent support profile generation algorithm used to output Vitario (or similar) mattress support profiles using stamp elements. 
    /// Currently, there is a distinct differentiation between supine and lateral position: the algorithm for lateral position is more complex than the one for supine position.
    /// </summary>
    public abstract class StampProfileGenerationAlgorithm
    {
        /// <summary>
        /// Generates a support profile using an intelligent algorithm for the specified customer.
        /// </summary>
        /// <param name="position"></param>
        /// <param name="pressureMeasurementSupine">Expects 12 measurement values between 0 and 100 millibar as measured with the test person laying on the back.</param>
        /// <param name="pressureMeasurementLateral">Expects 12 measurement values between 0 and 100 millibar as measured with the test person laying on the side.</param>
        /// <param name="gender"></param>
        /// <param name="personHeightCm">The height of the test person in centimeters.</param>
        /// <param name="personWeightKg">The weight of the test person in kilogram.</param>
        /// <param name="neutralProfileLetter">The letter representing the neutral (middle) stamp firmness on which the profile is based. For example, Vitario uses 'S' and Ergomometer NL uses 'D' as neutral stamp.</param>
        /// <param name="result">Holds the resulting profile that was generated through this algorithm. NULL if something went wrong.</param>
        /// <returns>Null if everything went fine or an exception.</returns>
        public static Exception GenerateProfileBasedOnPressureMapping(MeasurementPositions position, int[] pressureMeasurementSupine, int[] pressureMeasurementLateral, Genders gender, int personHeightCm, int personWeightKg, char neutralProfileLetter, out StampProfileGenerationResult result)
        {
            try
            {
                result = default(StampProfileGenerationResult);

                if (position == MeasurementPositions.Lateral)
                {
                    int shoulderIndex, lordosisIndex, pelvisIndex = pressureMeasurementSupine.Length - 1;

                    //generate the standard profile
                    string profile = string.Join("", Enumerable.Repeat(neutralProfileLetter, 12));
                    bool personLyingPositionWrong = false;

                    #region Shoulder

                    /*Shoulder area is always stamps 1-4. If the measurement in supine position is bad (equal values), the measurement in lateral position is used. */
                    int[] shoulderAreaArray = position == MeasurementPositions.Supine ? pressureMeasurementSupine : pressureMeasurementLateral;
                    shoulderIndex = GenerationUtils.GetIndexOfMaximum(shoulderAreaArray, 0, 3);

                    if (GenerationUtils.IsLeftSideEqual(shoulderIndex, shoulderAreaArray) || shoulderIndex < 3 && GenerationUtils.IsRightSideEqual(shoulderIndex, shoulderAreaArray)) //if one of the values around the shoulder index is the same
                    {
                        shoulderAreaArray = position == MeasurementPositions.Supine ? pressureMeasurementLateral : pressureMeasurementSupine;
                        shoulderIndex = GenerationUtils.GetIndexOfMaximum(shoulderAreaArray, 0, 3);
                    }

                    if (!GenerationUtils.IsLeftSideEqual(shoulderIndex, shoulderAreaArray) && shoulderIndex < 3 && GenerationUtils.IsRightSideEqual(shoulderIndex, shoulderAreaArray)) //if the value to the right is the same, move the shoulder index one to the right (e.g. 17 19 19 15)
                        shoulderIndex++;
                    else if (shoulderIndex == 0 && GenerationUtils.IsRightSideEqual(shoulderIndex, shoulderAreaArray) && shoulderAreaArray[2] == shoulderAreaArray[0]) //e.g. 17 17 17 19
                        shoulderIndex = 1;

                    //now generate the profile for this area
                    if (shoulderIndex == 0)
                    {
                        profile = profile.ReplaceAtIndex(0, 'T');
                        profile = profile.ReplaceAtIndex(1, 'K');
                        profile = profile.ReplaceAtIndex(2, 'K');

                        if (GenerationUtils.IsInRange(shoulderAreaArray[0], shoulderAreaArray[1], 2)) //if the first 2 values are similar, the profile changes a bit
                        {
                            profile = profile.ReplaceAtIndex(1, 'T');
                            profile = profile.ReplaceAtIndex(3, 'K');
                        }
                    }
                    else if (shoulderIndex == 1)
                    {
                        profile = profile.ReplaceAtIndex(0, 'K');
                        profile = profile.ReplaceAtIndex(1, 'T');
                        profile = profile.ReplaceAtIndex(2, 'K');

                        //if the values around the shoulder index are similar, the profile changes a bit
                        if (GenerationUtils.IsInRange(shoulderAreaArray[0], shoulderAreaArray[1], 2))
                        {
                            profile = profile.ReplaceAtIndex(0, 'T');
                            profile = profile.ReplaceAtIndex(3, 'K');
                        }

                        if (GenerationUtils.IsInRange(shoulderAreaArray[1], shoulderAreaArray[2], 2))
                        {
                            profile = profile.ReplaceAtIndex(2, 'T');
                            profile = profile.ReplaceAtIndex(3, 'K');
                        }
                    }
                    else if (shoulderIndex == 2 || shoulderIndex == 3) //indices at position 2 and 3 are treated the same, but an index at 3 hints that the person is not properly positioned on the mattress
                    {
                        profile = profile.ReplaceAtIndex(0, 'K');
                        profile = profile.ReplaceAtIndex(1, 'K');
                        profile = profile.ReplaceAtIndex(2, 'T');
                        profile = profile.ReplaceAtIndex(3, 'K');

                        if (shoulderAreaArray[1] + 2 >= shoulderAreaArray[2]) //if the value before the shoulder index is no more less than 2, the profile changes
                        {
                            if (gender == Genders.Female)
                            {
                                profile = profile.ReplaceAtIndex(0, 'T');
                                profile = profile.ReplaceAtIndex(1, 'T');
                            }
                            else
                            {
                                profile = profile.ReplaceAtIndex(0, 'W');
                                profile = profile.ReplaceAtIndex(1, 'T');
                            }
                        }

                        if (shoulderIndex == 3)
                            personLyingPositionWrong = true;
                    }

                    #endregion

                    #region Pelvis area
                    /*Pelvis area is always the peak value from right to left in a specific area. */
                    int[] pelvisAreaArray = position == MeasurementPositions.Supine ? pressureMeasurementSupine : pressureMeasurementLateral;

                    if (personHeightCm >= 170)
                    {
                        //get the pelvis index (peak value from bottom to top, taking 6 values into account)
                        for (int i = pelvisAreaArray.Length - 1; i >= 6; i--)
                            if (pelvisAreaArray[i] >= pelvisAreaArray[pelvisIndex])
                                pelvisIndex = i;
                            else
                                break;
                    }
                    if (personHeightCm >= 160)
                    {
                        //get the pelvis index (peak value from bottom to top, taking 4 values into account)
                        for (int i = pelvisAreaArray.Length - 3; i >= 6; i--)
                            if (pelvisAreaArray[i] >= pelvisAreaArray[pelvisIndex])
                                pelvisIndex = i;
                            else
                                break;
                    }
                    else if (personHeightCm >= 150)
                    {
                        //get the pelvis index (peak value from bottom-3 to top, taking 3 values into account)
                        for (int i = pelvisAreaArray.Length - 4; i >= 6; i--)
                            if (pelvisAreaArray[i] >= pelvisAreaArray[pelvisIndex])
                                pelvisIndex = i;
                            else
                                break;
                    }
                    else //small persons < 150cm
                    {
                        //get the pelvis index (peak value from bottom-3 to top, taking 4 values into account)
                        for (int i = pelvisAreaArray.Length - 4; i >= 5; i--)
                            if (pelvisAreaArray[i] >= pelvisAreaArray[pelvisIndex])
                                pelvisIndex = i;
                            else
                                break;
                    }

                    if (pelvisIndex == 11 && personHeightCm < 190) //pelvis area cannot be at the last stamp for person smaller than 190cm
                        pelvisIndex = 10;

                    //modify profile
                    profile = profile.ReplaceAtIndex(pelvisIndex, 'K');

                    if (gender == Genders.Male) //males have a smaller pelvis area --> smaller range of values (+/- 3) and allow only 2 "K"-stamps
                    {
                        if (GenerationUtils.IsInRange(pelvisAreaArray[pelvisIndex], pelvisAreaArray[pelvisIndex - 1], 3)) //if the value before the pelvis index is within 3mB, change its corresponding profile stamp
                            profile = profile.ReplaceAtIndex(pelvisIndex - 1, 'K');
                    }
                    else //females have a bigger pelvis area --> higher range of values (+/- 5)
                    {
                        if (pelvisIndex < 11 && GenerationUtils.IsInRange(pelvisAreaArray[pelvisIndex], pelvisAreaArray[pelvisIndex + 1], 5)) //if the value after the pelvis index is within 4mB, change its corresponding profile stamp
                            profile = profile.ReplaceAtIndex(pelvisIndex + 1, 'K');

                        if (GenerationUtils.IsInRange(pelvisAreaArray[pelvisIndex], pelvisAreaArray[pelvisIndex - 1], 5)) //if the value before the pelvis index is within 5mB, change its corresponding profile stamp
                            profile = profile.ReplaceAtIndex(pelvisIndex - 1, 'K');
                    }
                    #endregion

                    #region Lordosis area
                    /* Lordosis area is between shoulder and pelvis area and can never be before stamp 5. The lordosis index is always the index of the lowest value between shoulder and pelvis. */

                    //calculate the BMI of the test person (determines which stamp is used in the lordosis area)
                    double heightM = personHeightCm / 100d;
                    double bmi = personWeightKg / (heightM * heightM); //body mass index

                    int[] lordosisAreaArray = position == MeasurementPositions.Supine ? pressureMeasurementSupine : pressureMeasurementLateral;

                    lordosisIndex = GenerationUtils.GetIndexOfMinimum(lordosisAreaArray, 4, pelvisIndex - 1);

                    if (bmi > 30) //special rule: if the person is overweight, the lordosis index should be calculated based on the supine measurement
                    {
                        int lordosisIdxSupine = GenerationUtils.GetIndexOfMinimum(pressureMeasurementSupine, 4, pelvisIndex - 1);
                        int lordosisIdxLateral = GenerationUtils.GetIndexOfMinimum(pressureMeasurementLateral, 4, pelvisIndex - 1);

                        if (lordosisIdxSupine > lordosisIdxLateral)
                            lordosisIndex = lordosisIdxSupine;
                    }

                    //this step is useless because we always use the lateral values here
                    if (lordosisAreaArray[lordosisIndex - 1] == lordosisAreaArray[lordosisIndex] && lordosisAreaArray[lordosisIndex + 1] == lordosisAreaArray[lordosisIndex]) //if the values around the lordosis index are equal, try to get a better result by using the lateral measurement
                    {
                        lordosisAreaArray = pressureMeasurementLateral;
                        lordosisIndex = GenerationUtils.GetIndexOfMinimum(lordosisAreaArray, 4, pelvisIndex - 1);
                    }

                    if (personHeightCm > 155 && lordosisIndex <= 4 && gender == Genders.Male) //special rule: lordosis area cannot be at stamp 5 or below for small persons
                        lordosisIndex = 5;
                    else if (personHeightCm > 160 && lordosisIndex <= 4 && gender == Genders.Female) //special rule: lordosis area cannot be at stamp 5 or below for small persons
                        lordosisIndex = 5;

                    if (pelvisIndex - lordosisIndex > 3) //special rule: if the distance between pelvis and lordosis is more than 2 stamps, something is wrong --> "move" the lordosis index closer to the pelvis index
                        lordosisIndex = pelvisIndex - 3;

                    if (pelvisIndex - lordosisIndex == 1) //special rule: if lordosis and pelvis indices were determined to be next to each other, they have to be separated by 1 stamp
                        lordosisIndex = lordosisIndex - 1;

                    //modify the profile
                    if ((gender == Genders.Male && bmi < 20) || (gender == Genders.Female && bmi < 19)) //person appears to be underweight --> neutral ("D" or "S") stamp (so the same stamp as in surrounding area)
                        profile = profile.ReplaceAtIndex(lordosisIndex, neutralProfileLetter);
                    else if (bmi > 30) //person appears to be clearly overweight --> "B" stamp
                        profile = profile.ReplaceAtIndex(lordosisIndex, 'B');
                    else
                        profile = profile.ReplaceAtIndex(lordosisIndex, 'L'); //normal weight --> "L" stamp

                    if (lordosisIndex == pelvisIndex - 1 && profile[lordosisIndex] == 'K') //if the lordosis index is directly before the pelvis index, no 'K'-stamp is allowed in the lordosis area (which can't happen with the current algorithm anyways)
                        profile = profile.ReplaceAtIndex(lordosisIndex, 'L');

                    //special rule: "K" must not come directly after "L"
                    if (profile.Contains("LK")) //"L" must not immediately followed by "K" (distance between lordosis and pelvis area is at least 1 neutral stamp)
                    {
                        profile = profile.Replace("LK", "L" + neutralProfileLetter.ToString());

                        if (profile.Contains("L" + neutralProfileLetter + neutralProfileLetter)) //e.g. "LDD"
                            profile = profile.Replace("L" + neutralProfileLetter + neutralProfileLetter, "L" + neutralProfileLetter + "K"); //add the replaced "K" at the end of the pelvis area
                    }

                    //special rule: if the lordosis index is on stamp 5, stamp 4 must be neutral and stamp 3 must not be "T"
                    if (lordosisIndex == 4)
                    {
                        profile = profile.ReplaceAtIndex(3, neutralProfileLetter);

                        if (profile[2] == 'T')
                            profile = profile.ReplaceAtIndex(2, 'K');
                    }
                    #endregion

                    result = new StampProfileGenerationResult() { SupportProfile = profile.Select(s => s.ToString()).ToArray(), IsPersonLyingPositionWrong = personLyingPositionWrong };
                    return null;
                }
                else if (position == MeasurementPositions.Supine)
                {
                    int shoulderIndex, lordosisIndex, pelvisIndex = pressureMeasurementSupine.Length - 1;

                    //generate the standard profile
                    string profile = string.Join("", Enumerable.Repeat(neutralProfileLetter, 12));
                    bool personLyingPositionWrong = false;

                    #region Shoulder

                    /*Shoulder area is always stamps 1-3.  */
                    int[] shoulderAreaArray = pressureMeasurementSupine;
                    shoulderIndex = GenerationUtils.GetIndexOfMaximum(shoulderAreaArray, 0, 2);

                    //now generate the profile for this area
                    if (shoulderIndex == 0)
                    {
                        profile = profile.ReplaceAtIndex(0, 'T');
                        profile = profile.ReplaceAtIndex(1, 'K');
                        profile = profile.ReplaceAtIndex(2, 'K');
                    }
                    else if (shoulderIndex == 1 || shoulderIndex == 2)
                    {
                        profile = profile.ReplaceAtIndex(0, 'K');
                        profile = profile.ReplaceAtIndex(1, 'T');
                        profile = profile.ReplaceAtIndex(2, 'K');
                    }

                    #endregion

                    #region Pelvis area
                    /*Pelvis area is always the peak value from right to left in a specific area. */
                    int[] pelvisAreaArray = position == MeasurementPositions.Supine ? pressureMeasurementSupine : pressureMeasurementLateral;

                    if (personHeightCm >= 170)
                    {
                        //get the pelvis index (peak value from bottom to top, taking 6 values into account)
                        for (int i = pelvisAreaArray.Length - 1; i >= 6; i--)
                            if (pelvisAreaArray[i] >= pelvisAreaArray[pelvisIndex])
                                pelvisIndex = i;
                            else
                                break;
                    }
                    if (personHeightCm >= 160)
                    {
                        //get the pelvis index (peak value from bottom to top, taking 4 values into account)
                        for (int i = pelvisAreaArray.Length - 3; i >= 6; i--)
                            if (pelvisAreaArray[i] >= pelvisAreaArray[pelvisIndex])
                                pelvisIndex = i;
                            else
                                break;
                    }
                    else if (personHeightCm >= 150)
                    {
                        //get the pelvis index (peak value from bottom-3 to top, taking 3 values into account)
                        for (int i = pelvisAreaArray.Length - 4; i >= 6; i--)
                            if (pelvisAreaArray[i] >= pelvisAreaArray[pelvisIndex])
                                pelvisIndex = i;
                            else
                                break;
                    }
                    else //small persons < 150cm
                    {
                        //get the pelvis index (peak value from bottom-3 to top, taking 4 values into account)
                        for (int i = pelvisAreaArray.Length - 4; i >= 5; i--)
                            if (pelvisAreaArray[i] >= pelvisAreaArray[pelvisIndex])
                                pelvisIndex = i;
                            else
                                break;
                    }

                    if (pelvisIndex == 11 && personHeightCm < 190) //pelvis area cannot be at the last stamp for person smaller than 190cm
                        pelvisIndex = 10;

                    //modify profile
                    profile = profile.ReplaceAtIndex(pelvisIndex, 'K');

                    #endregion

                    #region Lordosis area
                    /* Lordosis area is between shoulder and pelvis area and can never be before stamp 5. The lordosis index is always the index of the lowest value between shoulder and pelvis. */

                    //calculate the BMI of the test person (determines which stamp is used in the lordosis area)
                    double heightM = personHeightCm / 100d;
                    double bmi = personWeightKg / (heightM * heightM); //body mass index

                    int[] lordosisAreaArray = pressureMeasurementSupine;

                    lordosisIndex = GenerationUtils.GetIndexOfMinimum(lordosisAreaArray, 4, pelvisIndex - 1);

                    if (personHeightCm > 155 && lordosisIndex <= 4 && gender == Genders.Male) //special rule: lordosis area cannot be at stamp 5 or below for small persons
                        lordosisIndex = 5;
                    else if (personHeightCm > 160 && lordosisIndex <= 4 && gender == Genders.Female) //special rule: lordosis area cannot be at stamp 5 or below for small persons
                        lordosisIndex = 5;

                    if (pelvisIndex - lordosisIndex == 1) //special rule: if lordosis and pelvis indices were determined to be next to each other, they have to be separated by 1 stamp
                        lordosisIndex = lordosisIndex - 1;

                    //modify the profile
                    if ((gender == Genders.Male && bmi < 20) || (gender == Genders.Female && bmi < 19)) //person appears to be underweight --> neutral ("D" or "S") stamp (so the same stamp as in surrounding area)
                        profile = profile.ReplaceAtIndex(lordosisIndex, neutralProfileLetter);
                    else if (bmi > 30) //person appears to be clearly overweight --> "B" stamp
                        profile = profile.ReplaceAtIndex(lordosisIndex, 'B');
                    else
                        profile = profile.ReplaceAtIndex(lordosisIndex, 'L'); //normal weight --> "L" stamp
                    #endregion

                    result = new StampProfileGenerationResult() { SupportProfile = profile.Select(s => s.ToString()).ToArray(), IsPersonLyingPositionWrong = personLyingPositionWrong };
                    return null;
                }
                else

                    return null;
            }
            catch (Exception ex)
            {
                result = default(StampProfileGenerationResult);
                return ex;
            }
        }
    }

    public struct StampProfileGenerationResult
    {
        /// <summary>
        /// The resulting support profile.
        /// </summary>
        public string[] SupportProfile { get; set; }

        /// <summary>
        /// If set to true, this indicates that the test person MAY have lain in a wrong positon on the mattress during the measurement.
        /// </summary>
        public bool IsPersonLyingPositionWrong { get; set; }
    }
}
