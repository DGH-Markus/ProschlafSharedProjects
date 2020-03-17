using System;
using System.Collections.Generic;
using System.Text;

namespace ProschlafUtils
{
    /// <summary>
    /// Provides methods for simple user activity tracking in WPF applications (e.g. to track button clicks or text input).
    /// </summary>
    public static class UserActivityTracker
    {
        #region Vars
        private static List<UserActivity> trackedActivities { get; set; } = new List<UserActivity>();

        private static int maximumTrackedActivitiesCount = 1000;
        #endregion

        /// <summary>
        /// Logs a new user activity by storing it in-memory.
        /// </summary>
        /// <param name="activity"></param>
        public static void TrackUserActivity(UserActivity activity)
        {
            trackedActivities.Add(activity);

            while (trackedActivities.Count > maximumTrackedActivitiesCount)
                trackedActivities.RemoveAt(0);
        }

        /// <summary>
        /// Gets all stored user activities from memory.
        /// </summary>
        /// <returns></returns>
        public static List<UserActivity> GetTrackedActivities()
        {
            return new List<UserActivity>(trackedActivities);
        }
    }

    public class UserActivity
    {
        public enum ActivityTypes { Unset = 0, ButtonClick = 1, TextInput = 2, LoadData = 3, SelectData = 4 };

        public string UserName { get; set; }
        public DateTime Time { get; set; }
        public ActivityTypes ActivityType { get; set; }
        public string ControlName { get; set; }
        public string ContainerName { get; set; }
        public string TextInput { get; set; }
        public string Data { get; set; } //can be used to store any additional data

        public override string ToString()
        {
            if (ActivityType == ActivityTypes.ButtonClick)
                return Time + " - Button click on " + ControlName + " in container " + ContainerName + (UserName.HasValue() ? " by user " + UserName : "");
            else if (ActivityType == ActivityTypes.TextInput)
                return Time + " - Text input on " + ControlName + " in container " + ContainerName + (UserName.HasValue() ? " by user " + UserName : "") + ". Value: " + TextInput;
            else if (ActivityType == ActivityTypes.LoadData)
                return Time + " - Load data via " + ControlName + " in container " + ContainerName + (UserName.HasValue() ? " by user " + UserName : "") + ". Data: " + Data;
            else if (ActivityType == ActivityTypes.SelectData)
                return Time + " - Select data via " + ControlName + " in container " + ContainerName + (UserName.HasValue() ? " by user " + UserName : "") + ". Data: " + Data;
            else
                return Time + " - unknown activity: " + ActivityType;
        }
    }
}
