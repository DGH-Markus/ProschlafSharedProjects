using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace ngMattAlgorithms
{
    /// <summary>
    /// This class offers methods to calculate the "Sleep Quality Index" defined by Proschlaf.
    /// The SQI is based on a sleep session recorded by an ngMatt device.
    /// </summary>
    internal abstract class SleepQualityIndex
    {
        /// <summary>
        ///
        /// </summary>
        /// <param name="session"></param>
        /// <returns></returns>
        public static SqiResult CalculateSQI(NgMattSleepSession session)
        {
            try
            {
                if (session == null || session.Movements == null || session.Movements.Count < 1)
                    return null;

                //group movements by hour
                Dictionary<int, List<Movement>> groupedMovements = new Dictionary<int, List<Movement>>();

                DateTime currentTime = session.Movements.First().Start;
                int currentHour = 1;

                foreach (Movement movement in session.Movements)
                {
                    if (!groupedMovements.ContainsKey(currentHour))
                        groupedMovements.Add(currentHour, new List<Movement>());

                    while (movement.Start >= currentTime.AddHours(1))
                    {
                        currentTime = currentTime.AddHours(1);
                        currentHour++;

                        groupedMovements.Add(currentHour, new List<Movement>());
                    }

                    groupedMovements[currentHour].Add(movement);
                }

                int points = 0;
                foreach (var group in groupedMovements)
                    points += GetPointsForMovementGroup(group);

                double sqi_raw = (double)points / groupedMovements.Count; //SQI = points / total number of hours slept
                double sqi_percent = GetSqiPercentage(sqi_raw); //gives a value between 0 and 100

                return new SqiResult() { NumberOfMovements = session.Movements.Count, Points = points, SQI_Percent = sqi_percent };
            }
            catch(Exception ex)
            {
                Logging.Logger.AddLogEntry(Logging.Logger.LogEntryCategories.Error, "Error while calculating SQI for session with id: " + session.SessionId, ex, "SleepQualityIndex");
                return null;
            }
        }

        private static int GetPointsForMovementGroup(KeyValuePair<int, List<Movement>> group)
        {
            //calculate points based on hour * weighting factor
            switch (group.Key)
            {
                case 1:
                    return group.Value.Count * 15;
                case 2:
                    return group.Value.Count * 12;
                case 3:
                    return group.Value.Count * 10;
                case 4:
                    return group.Value.Count * 8;
                case 5:
                    return group.Value.Count * 6;
                case 6:
                    return group.Value.Count * 4;
                case 7:
                    return group.Value.Count * 2;
                case 8:
                    return group.Value.Count * 1;
                default:
                    return group.Value.Count * 1;
            }
        }

        private static double GetSqiPercentage(double sqi)
        {
            if (sqi < 2)
            {
                return 100;
            }
            else if (sqi < 10)
            {
                return 95;
            }
            else if (sqi < 15)
            {
                return 90;
            }
            else if (sqi < 20)
            {
                return 85;
            }
            else if (sqi < 25)
            {
                return 80;
            }
            else if (sqi < 30)
            {
                return 75;
            }
            else if (sqi < 40)
            {
                return 70;
            }
            else if (sqi < 70)
            {
                return 65;
            }
            else if (sqi < 85)
            {
                return 60;
            }
            else if (sqi < 100)
            {
                return 55;
            }
            else if (sqi < 200)
            {
                return 50;
            }
            else if (sqi < 300)
            {
                return 45;
            }
            else if (sqi < 400)
            {
                return 40;
            }
            else if (sqi < 500)
            {
                return 35;
            }
            else if (sqi < 600)
            {
                return 30;
            }
            else if (sqi < 700)
            {
                return 25;
            }
            else if (sqi < 800)
            {
                return 20;
            }
            else if (sqi < 1000)
            {
                return 15;
            }
            else if (sqi < 1200)
            {
                return 10;
            }
            else if (sqi < 1500)
            {
                return 5;
            }
            else if (sqi < 2000)
            {
                return 2;
            }
            else
            {
                return 1;
            }
        }
    }
}
