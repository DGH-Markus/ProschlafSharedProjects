using System;
using System.Threading.Tasks;

namespace IsapSignalRCommunication
{
    /// <summary>
    /// This interface defines all public methods available at ISAP's application hub.
    /// </summary>
    public interface IApplicationHub
    {
        /* These methods can be called by the client on the hub and are used to report something */
        bool AuthenticateAndRegisterClient(string locationIdentifier, string locationName, string applicationInstanceId, ProschlafUtils.GlobalEnums.ProschlafApplications application, string applicationVersion);
        void PostCommandResult(SignalRCommandResult result);
    }

    /// <summary>
    /// This interface defines all public methods any client of ISAP's application hub must support.
    /// </summary>
    public interface IApplicationClient
    {
        /* These methods can be called by the Hub on the client and are used to request something from the client */
        void RequestStatusReport();
        void RequestTeamViewerStartup();
        void RequestSystemReboot();

        /* These methods can be called on client-side and are used to post something to the hub */
        Task<Exception> PostCommandResult(SignalRCommandResult result);
    }

    public class SignalRCommandResult
    {
        public string CommandName { get; set; }
        public bool IsExecutedSuccessfully { get; set; }
        public string Message { get; set; }
        public string UserFriendlyErrorMessage { get; set; }
    }
}
