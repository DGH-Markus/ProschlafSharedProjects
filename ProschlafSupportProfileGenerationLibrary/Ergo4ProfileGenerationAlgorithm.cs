using System;
using System.Collections.Generic;
using System.Text;
using static ProschlafSupportProfileGenerationLibrary.GenerationConstants;

namespace ProschlafSupportProfileGenerationLibrary
{
    public abstract class Ergo4ProfileGenerationAlgorithm
    {
        /// <summary>
        /// Generates a profile based in the input person data.
        /// An Ergo 4 profile consists of 4 letters. 3 of these letters are the same, the other one is individual.
        /// The position of the individual profile role is based on the position of the person's lordosis area and the firmness of the role is based on the body mass index of the person.
        /// This algorithm attempts to find the position and firmness of the individual role as well as the firmness of the 3 remaining roles and returns a support profile.
        /// </summary>
        /// <param name="gender"></param>
        /// <param name="height"></param>
        /// <param name="weight"></param>
        /// <param name="pressureMeasurementValues"></param>
        /// <returns></returns>
        public static Exception GenerateProfileBasedOnPressureMapping(Genders gender, int height, int weight, int[] pressureMeasurementValuesComplete, out Ergo4ProfileGenerationResult result)
        {
            try
            {
                result = null;

                if (pressureMeasurementValuesComplete == null || pressureMeasurementValuesComplete.Length != 12)
                    return new ArgumentException("pressureMeasurementValuesComplete is invalid: required length is 12.");


                int[] pressureMeasurementValues = new int[4]; //Ergo 4 only has individual roles in the lordosis area
                Array.Copy(pressureMeasurementValuesComplete, 4, pressureMeasurementValues, 0, 4);

                result = new Ergo4ProfileGenerationResult();

                //calculate BMI
                double bmi = weight / (Math.Pow(((double)height) / 100, 2));
                char replacementLetter = ' '; //this is used to hold the letter of the individual role
                int replacementIndex = -1; //this holds the position of the individual role in the profile

                if (gender == Genders.Female)
                {
                    //determine mattress hardness and standard profile based on BMI
                    if (bmi < 19)
                    {
                        result.Firmness = FirmnessLevels.H1;
                        result.SupportProfile = "0000";
                        replacementLetter = '1';
                    }
                    else if (bmi < 25)
                    {
                        result.Firmness = FirmnessLevels.H2;
                        result.SupportProfile = "1111";
                        replacementLetter = '2';
                    }
                    else if (bmi < 29)
                    {
                        result.Firmness = FirmnessLevels.H3;
                        result.SupportProfile = "2111";
                        replacementLetter = '3';
                    }
                    else
                    {
                        result.Firmness = FirmnessLevels.H4;
                        result.SupportProfile = "2111";
                        replacementLetter = '3';
                    }
                }
                else
                {
                    //determine mattress hardness and standard profile based on BMI
                    if (bmi < 20)
                    {
                        result.Firmness = FirmnessLevels.H1;
                        result.SupportProfile = "0000";
                        replacementLetter = '1';
                    }
                    else if (bmi < 26)
                    {
                        result.Firmness = FirmnessLevels.H2;
                        result.SupportProfile = "1111";
                        replacementLetter = '2';
                    }
                    else if (bmi < 30)
                    {
                        result.Firmness = FirmnessLevels.H3;
                        result.SupportProfile = "2111";
                        replacementLetter = '3';
                    }
                    else
                    {
                        result.Firmness = FirmnessLevels.H4;
                        result.SupportProfile = "2111";
                        replacementLetter = '3';
                    }
                }

                /*now try to find the position of the lordosis role in order to be able to modify the hardness of the role in that position */
                int maxIndex = GenerationUtils.GetIndexOfMaximum(pressureMeasurementValues, 0, pressureMeasurementValues.Length - 1);
                int maxValue = pressureMeasurementValues[maxIndex];
                bool maxValueIsDistinct = GenerationUtils.IsValueDistinct(maxValue, pressureMeasurementValues); //make sure that the max value only occurs once

                //method 1: if the highest value is on position 2, 3 or 4, take the role before that
                if (maxValueIsDistinct && maxIndex > 0)
                {
                    replacementIndex = maxIndex - 1; //role before the one with the highest pressure is individual
                    result.GenerationMethod = "1";
                }
                else if (pressureMeasurementValues[1] > pressureMeasurementValues[0] && pressureMeasurementValues[1] > pressureMeasurementValues[3] && pressureMeasurementValues[1] == pressureMeasurementValues[2])
                {
                    //if the 2 middle values are equal and both are higher than the first and last value (e.g. 7 11 11 6)
                    replacementIndex = 1; //the second role is individual
                    result.GenerationMethod = "2";
                }
                else if (pressureMeasurementValues[3] > pressureMeasurementValues[0] && pressureMeasurementValues[3] > pressureMeasurementValues[1] && pressureMeasurementValues[3] == pressureMeasurementValues[2])
                {
                    //if the 2 last values are equal and the highest ones (e.g. 6 7 11 11)
                    replacementIndex = 2; //the third role is individual
                    result.GenerationMethod = "3";
                }
                else //standard method (no pattern recognized): set individual role based on height and BMI
                {
                    if (gender == Genders.Female)
                    {
                        if (height < 166)
                            replacementIndex = 0;
                        else if (height < 180)
                            replacementIndex = 1;
                        else
                            replacementIndex = 2;
                    }
                    else
                    {
                        if (height < 166)
                            replacementIndex = 0;
                        else if (height < 180)
                            replacementIndex = 1;
                        else if (height < 190)
                            replacementIndex = 2;
                        else
                            replacementIndex = 3;
                    }

                    result.GenerationMethod = "Std";
                }

                if (replacementIndex < 0 || (replacementLetter == ' ')) //could not generate a profile, should not happen
                    return null;

                //replace one neutral role in the standard profile with the individual role that was defined by the algorithm
                string numberProfile = GenerationUtils.ReplaceAtIndex(result.SupportProfile, replacementIndex, replacementLetter);

                //convert the number indexes in roles
                result.SupportProfile = ReplaceNumbersProfileWithLetters(numberProfile);

                return null;
            }
            catch (Exception ex)
            {
                result = null;
                return ex;
            }
        }

        /// <summary>
        /// Replaces the numbers retrieved from the algorithm with their correpsonding profile letters.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        private static string ReplaceNumbersProfileWithLetters(string input)
        {
            string output = "";

            for (int i = 0; i < input.Length; i++)
            {
                switch (input[i])
                {
                    case '0':
                        output += "E";
                        break;
                    case '1':
                        output += "S";
                        break;
                    case '2':
                        output += "G";
                        break;
                    case '3':
                        output += "B";
                        break;
                    case '4':
                        output += "R";
                        break;
                    default:
                        break;
                }
            }

            return output;
        }

        public class Ergo4ProfileGenerationResult
        {
            /// <summary>
            /// The resulting 4-letter support profile. Available profile elements are: E S G B R.
            /// </summary>
            public string SupportProfile { get; set; }

            public FirmnessLevels Firmness { get; set; }

            public string GenerationMethod { get; set; }
        }
    }
}
