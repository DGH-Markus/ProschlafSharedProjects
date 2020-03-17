using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ngMattAlgorithms
{
    /// <summary>
    /// This class holds methods to recognize movements based on sleep recording data retrieved from LS 2.0 devices. 
    /// </summary>
    internal abstract class MovementRecognition
    {
        #region Consts
        private const int NUMBER_OF_ACTIVE_CHANNELS_FOR_MOVEMENT = 3;
        private const int THRESHOLD_DIFFERENCE_REQUIRED = 2;
        private const int THRESHOLD_DELTAS_SUM = 6; //the difference of the delta value between two rows in order to consider it as a movement (ignoring number of channels active)
        private const int LOOK_BACK_ROWS = 5; //basically the number of seconds to look back in order to check if there was a movement too
        #endregion

        /// <summary>
        /// Recognizes movements in the input list based on the specified parameters.
        /// </summary>
        /// <param name="data">The raw movement data. Sampling rate is expected to be 1 second.</param>
        /// <param name="numberOfActiveChannelsForMovement">The number of channels were a change is detected in order to count as "movement".</param>
        /// <param name="threshold">The difference in millibar between the previous and the current pressure value in order to count as a change.</param>
        /// <param name="deltaSumThreshold">The difference in millibar between two rows in order to count as "movement" (overrides the other parameters).</param>
        /// <param name="lookBack">The number of rows (seconds) to look back for another movement. If a movement is found, it is joined together with the current one and they are regarded as one movement.</param>
        /// <returns></returns>
        public static List<Movement> RecognizeMovements(IReadOnlyList<MovementRawData> data, int numberOfActiveChannelsForMovement = NUMBER_OF_ACTIVE_CHANNELS_FOR_MOVEMENT, int threshold = THRESHOLD_DIFFERENCE_REQUIRED, int deltaSumThreshold = THRESHOLD_DELTAS_SUM, int lookBack = LOOK_BACK_ROWS)
        {
            List<Movement> movements = new List<Movement>();
            MovementRawData[] dataArray = data.OrderBy(d => d.Time).ToArray();
            int lastMovementIndex = -1000;

            for (int i = 1; i < dataArray.Length; i++) //start at index 1 since we have no reference value at [0]
            {
                //calculate changes on all 12 channels compared to the last data point
                int[] deltas = new int[12];
                for (int j = 0; j < 12; j++)
                    deltas[j] = Math.Abs(dataArray[i].PressureValues[j] - dataArray[i - 1].PressureValues[j]);

                if (deltas.Count(d => d >= threshold) >= numberOfActiveChannelsForMovement || deltas.Sum() >= deltaSumThreshold) //bingo! we believe to have a movement
                {
                    if (lastMovementIndex < (i - 1 - lookBack)) //a new movement has started
                    {
                        movements.Add(new Movement() { Start = dataArray[i - 1].Time, End = dataArray[i].Time, PressureValues_Start = dataArray[i - 1].PressureValues, PressureValues_End = dataArray[i].PressureValues });

                        if (deltas.Sum() >= deltaSumThreshold && deltas.Count(d => d >= threshold) < numberOfActiveChannelsForMovement)
                            deltaSumThreshold.ToString();
                    }
                    else //a movement is still in progress
                    {
                        Movement currentMovement = movements.Last();
                        currentMovement.PressureValues_End = dataArray[i].PressureValues;
                        currentMovement.End = dataArray[i].Time;
                    }

                    lastMovementIndex = i;
                }
            }

            return movements;
        }

        /// <summary>
        /// Checks if the raw data contains fields added by the firmware that signal a movement and returns those movements.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static List<Movement> RecognizeFirmwareDetectedMovements(IReadOnlyList<MovementRawData> data)
        {
            List<Movement> movements = new List<Movement>();
            MovementRawData[] dataArray = data.OrderBy(d => d.Time).ToArray();
            int startMovementIndex = -1000;
            int lastMovementIndex = -1000;
            bool isMovementOngoing = false;

            for (int i = 1; i < dataArray.Length; i++) //start at index 1 since we have no reference value at [0]
            {
                if (dataArray[i].IsMovementDetectedByFirmware == true)
                {
                    if (!isMovementOngoing) //a new movement has begun
                    {
                        startMovementIndex = i;
                        isMovementOngoing = true;
                    }

                    lastMovementIndex = i;
                }
                else
                {
                    if (lastMovementIndex == i - 1) //a movement just finished
                    {
                        movements.Add(new Movement() { Start = dataArray[startMovementIndex].Time, End = dataArray[lastMovementIndex].Time, PressureValues_Start = dataArray[startMovementIndex].PressureValues, PressureValues_End = dataArray[lastMovementIndex].PressureValues });
                        isMovementOngoing = false;
                    }
                    else
                    {
                        continue; //no previous or current movement according to the firmware
                    }
                }
            }

            return movements;
        }

        /// <summary>
        /// Filters the input raw data and returns a new list without any movements in it.
        /// </summary>
        /// <returns></returns>
        public static List<MovementRawData> FilterOutMovements(IReadOnlyList<MovementRawData> data, List<Movement> movements)
        {
            List<MovementRawData> filterData = new List<MovementRawData>();
            MovementRawData[] dataArray = data.OrderBy(d => d.Time).ToArray();

            for (int i = 0; i < dataArray.Length; i++) //start at index 1 since we have no reference value at [0]
            {
                if (movements.Any(m => m.Start >= data[i].Time && m.End <= data[i].Time)) //it's a movement
                    continue;

                filterData.Add(dataArray[i]);
            }

            return filterData;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static Dictionary<int, short> RecognizeSleepPositionTest(IReadOnlyList<MovementRawData> data)
        {
            List<MovementRawData> filterData = new List<MovementRawData>();
            MovementRawData[] dataArray = data.OrderBy(d => d.Time).ToArray();

            int shoulderEndIndex = 3;
            Dictionary<int, short> movementSums = new Dictionary<int, short>(); //sum of shoulder values, number of times this sum occured

            for (int i = 0; i < dataArray.Length; i++) //start at index 1 since we have no reference value at [0]
            {
                int sum = 0;

                for (int j = 0; j <= shoulderEndIndex; j++)
                    sum += dataArray[i].PressureValues[j];

                if (!movementSums.ContainsKey(sum))
                    movementSums.Add(sum, 0);

                movementSums[sum]++;
            }

            return movementSums;
        }
    }
}
