using Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ngMattAlgorithms
{
    /// <summary>
    /// Used to take a list of sleep sessions and determine which sessions may belong together and which cannot be used for further sleep analysis.
    /// </summary>
    internal abstract class SleepSessionPreprocessor
    {
        /// <summary>
        /// Expects a list of sleep sessions that were all recorded for one person during one or more nights (sleep sessions). 
        /// This method attempts to split and recombine the sleep sessions in a way that in the end the correct number of sleep sessions is found.
        /// </summary>
        /// <param name="sourceSessions"></param>
        /// <returns></returns>
        public static List<NgMattSleepSession> PreprocessSleepSessions(List<NgMattSleepSession> sourceSessions)
        {
            return sourceSessions; //feature is currently deactivated to make sure the received sessions are all inserted into the database 1:1

            sourceSessions = sourceSessions.OrderBy(s => s.Start).ToList();
            List<NgMattSleepSession> processedSessions = new List<NgMattSleepSession>();
            NgMattSleepSession lastSession = null;

            foreach (NgMattSleepSession source in sourceSessions)
            {
                if(source.Length > TimeSpan.FromHours(14))
                    Logger.AddLogEntry(Logger.LogEntryCategories.Warning, "A very long session was uploaded (" + source.Length.TotalHours + " hours). Start of session: " + source.Start, null, "SleepSessionPreprocessor");

                if (lastSession == null)
                {
                    lastSession = source;
                    continue;
                }

                if (lastSession.Start.AddHours(1) < lastSession.Start) //if the current and the last session are apart by more than 1 hour, they can not be joined together
                {
                    if(lastSession.Length.TotalHours > 1)
                        processedSessions.Add(lastSession); //add the last session to the output list if the sleep duration was at least 1 hour
                    else
                        Logger.AddLogEntry(Logger.LogEntryCategories.Debug, "Discarding session with length: " + (int)lastSession.Length.TotalMinutes + "min", null, "SleepSessionPreprocessor");

                    lastSession = source;
                    continue;
                }

                //join the 2 sessions together and continue one with the resulting session
                Logger.AddLogEntry(Logger.LogEntryCategories.Debug, "Joining the following 2 sessions: " + lastSession.Start + " / " + lastSession.End + " and " + source.Start + " / " + source.End + ". Total number of movements: " + (lastSession.Movements.Count + source.Movements.Count), null, "SleepSessionPreprocessor");
                NgMattSleepSession joinedSession = JoinSessions(lastSession, source);
                lastSession = joinedSession;
                processedSessions.Add(lastSession);
            }

            Logger.AddLogEntry(Logger.LogEntryCategories.Debug, "Returning processed sessions. Source session count: " + sourceSessions.Count + ". Resulting session count: " + processedSessions, null, "SleepSessionPreprocessor");
            return processedSessions;
        }

        private static NgMattSleepSession JoinSessions(NgMattSleepSession priorSession, NgMattSleepSession subsequentSession)
        {
            NgMattSleepSession joined = new NgMattSleepSession() { SessionId = priorSession.SessionId, Start = priorSession.Start, End = subsequentSession.End };
            List<Movement> movements = new List<Movement>(priorSession.Movements);
            movements.AddRange(subsequentSession.Movements);
            joined.Movements = movements.AsReadOnly();
            return joined;
        }
    }
}
