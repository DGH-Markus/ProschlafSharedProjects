using Microsoft.AspNet.SignalR.Client;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace IsapSignalRCommunication
{
    /// <summary>
    /// An implementation of the IApplicationClient interface that lets applications communicate with the ISAP SignalR application hub.
    /// Note that this class is not static, but should only be instantiated once per application!
    /// This client exposes certain actions that must be registered so that they can be called by the hub. 
    /// </summary>
    public class ApplicationClient : IApplicationClient
    {
        #region Vars
        const string BASE_URL_LOCAL = "http://localhost:61039/"; //local
        const string BASE_URL_LIVE = "https://service.proschlaf.at/"; //live-server
        //const string BASE_URL_LIVE = "http://136.243.54.8:48000/"; //test-server

        HubConnection hubConnection = null;
        IHubProxy myHub = null;
        #endregion

        #region Properties
        public bool IsConnected
        {
            get
            {
                return hubConnection != null ? hubConnection.State == ConnectionState.Connected : false;
            }
        }
        #endregion

        #region Events
        public event EventHandler OnHubConnectionClosed;

        /// <summary>
        /// Fired whenever a message from the hub has been received.
        /// Item1 = message name, Item2 = [optional] message payload.
        /// </summary>
        public event EventHandler<Tuple<string, string>> OnHubMessageReceived;


        /// <summary>
        /// Fired whenever this client sends a command result to the hub.
        /// Item1 = hub endpoint name, Item2 = SignalRCommandResult.
        /// </summary>
        public event EventHandler<Tuple<string, SignalRCommandResult>> OnResultToHubSent;
        #endregion

        #region Actions
        /// <summary>
        /// This action is executed when the 'RequestStatusReport'-method is called by the hub.
        /// It must be set to return a message string.
        /// </summary>
        public Func<string> ActionForStatusRequest { private get; set; }

        /// <summary>
        /// This action is executed when the 'RequestTeamViewerStartup'-method is called by the hub.
        /// It must be set to startup TeamViewer and either return the TeamViewer Id or an exception.
        /// </summary>
        public Func<Tuple<long?, Exception>> ActionForTeamViewerStartupRequest { private get; set; }

        /// <summary>
        /// This action is executed when the 'RequestSystemReboot'-method is called by the hub.
        /// It must be set to initiate a system reboot and return null or an exception.
        /// </summary>
        public Func<Exception> ActionForSystemRebootRequest { private get; set; }
        #endregion

        /// <summary>
        /// Main method to fire up the SignalR client and initiate a connection to the ISAP hub.
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <param name="locationIdentifier"></param>
        /// <param name="locationName"></param>
        /// <param name="applicationInstanceId"></param>
        /// <param name="application"></param>
        /// <param name="applicationVersion"></param>
        /// <param name="isLocalTestMode"></param>
        /// <returns></returns>
        public async Task<Exception> StartupClient(string username, string password, string locationIdentifier, string locationName, string applicationInstanceId, ProschlafUtils.GlobalEnums.ProschlafApplications application, string applicationVersion, bool isLocalTestMode = false)
        {
            if (hubConnection != null && hubConnection.State != ConnectionState.Disconnected)
                return new Exception("Client is already initialized and either connecting or already connected.");

            string endpoint = (isLocalTestMode ? BASE_URL_LOCAL : BASE_URL_LIVE) + "signalr/";

            hubConnection = new HubConnection(endpoint);
            hubConnection.Headers.Add("username", username); //set mandatory auth header fields
            hubConnection.Headers.Add("password", password);
            hubConnection.Closed += HubConnection_Closed;
            myHub = hubConnection.CreateHubProxy("ApplicationHub"); //name of the specific hub on the server

            #region Register and implement methods defined by the server

            //register the "ReportStatus" method which can be called by the hub

            myHub.On(nameof(IApplicationClient.RequestStatusReport), () =>
            {
                ((IApplicationClient)this).RequestStatusReport();
                OnHubMessageReceived?.Invoke(this, new Tuple<string, string>(nameof(IApplicationClient.RequestStatusReport), null));
            });

            myHub.On(nameof(IApplicationClient.RequestTeamViewerStartup), () =>
            {
                ((IApplicationClient)this).RequestTeamViewerStartup();
                OnHubMessageReceived?.Invoke(this, new Tuple<string, string>(nameof(IApplicationClient.RequestTeamViewerStartup), null));
            });

            myHub.On(nameof(IApplicationClient.RequestSystemReboot), () =>
            {
                ((IApplicationClient)this).RequestSystemReboot();
                OnHubMessageReceived?.Invoke(this, new Tuple<string, string>(nameof(IApplicationClient.RequestSystemReboot), null));
            });

            /*
            myHub.On<string>(nameof(IApplicationClient.ReportStatus), param =>
            {
                OnHubMessageReceived?.Invoke(this, nameof(IApplicationClient.ReportStatus));
            });
            */
            #endregion

            //start the connection to the hub
            Exception ex = await hubConnection.Start().ContinueWith(task =>
             {
                 if (task.IsFaulted)
                     return new Exception("Exception while starting connection to: " + endpoint, task.Exception);
                 else
                     return null;
             });

            if (ex != null)
                return ex;

            if (hubConnection.State == ConnectionState.Disconnected)
                return new Exception("Connection was disconnected while starting connection to: " + endpoint, null);

            //authenticate and register this client at the hub
            ex = await myHub.Invoke(nameof(IApplicationHub.AuthenticateAndRegisterClient), locationIdentifier, locationName, applicationInstanceId, application, applicationVersion).ContinueWith(task =>
            {
                if (task.IsFaulted)
                    return new Exception($"There was an error calling '{nameof(IApplicationHub.AuthenticateAndRegisterClient)}'", task.Exception);
                else
                    return null;
            });

            return ex;
        }

        /// <summary>
        /// Shuts down the SignalR client and closes the connection to the ISAP hub.
        /// </summary>
        /// <returns></returns>
        public Exception ShutdownClient()
        {
            if (hubConnection == null || myHub == null)
                return null;

            try
            {
                hubConnection.Stop(TimeSpan.FromSeconds(5));
                hubConnection = null;
                myHub = null;
                return null;
            }
            catch (Exception ex)
            {
                return ex;
            }
        }

        private void HubConnection_Closed()
        {
            OnHubConnectionClosed?.Invoke(this, EventArgs.Empty);
        }

        #region Interface implementation

        /// <summary>
        /// Called by the hub in order to request a status report.
        /// Executes the corresponding action set by the application using this client.
        /// </summary>
        void IApplicationClient.RequestStatusReport()
        {
            if (ActionForStatusRequest != null)
            {
                string result = ActionForStatusRequest();

                PostCommandResult(new SignalRCommandResult() { CommandName = nameof(IApplicationClient.RequestStatusReport), IsExecutedSuccessfully = true, Message = result, UserFriendlyErrorMessage = null });
            }
            else
            {
                PostCommandResult(new SignalRCommandResult() { CommandName = nameof(IApplicationClient.RequestStatusReport), IsExecutedSuccessfully = false, Message = null, UserFriendlyErrorMessage = $"Kommando '{nameof(IApplicationClient.RequestStatusReport)}' wurde in diesem Client nicht implementiert." });
            }
        }

        /// <summary>
        /// Called by the hub in order to request the start of the TeamViewer application.
        /// Executes the corresponding action set by the application using this client.
        /// </summary>
        void IApplicationClient.RequestTeamViewerStartup()
        {
            if (ActionForTeamViewerStartupRequest != null)
            {
                var result = ActionForTeamViewerStartupRequest();

                if (result.Item2 == null)
                    PostCommandResult(new SignalRCommandResult() { CommandName = nameof(IApplicationClient.RequestTeamViewerStartup), IsExecutedSuccessfully = true, Message = "TeamViewer wurde erfolgreich gestartet. Id: " + result.Item1, UserFriendlyErrorMessage = null });
                else
                    PostCommandResult(new SignalRCommandResult() { CommandName = nameof(IApplicationClient.RequestTeamViewerStartup), IsExecutedSuccessfully = false, Message = "TeamViewer konnte nicht gestartet werden.", UserFriendlyErrorMessage = result.ToString() });
            }
            else
            {
                PostCommandResult(new SignalRCommandResult() { CommandName = nameof(IApplicationClient.RequestTeamViewerStartup), IsExecutedSuccessfully = false, Message = null, UserFriendlyErrorMessage = $"Kommando '{nameof(IApplicationClient.RequestTeamViewerStartup)}' wurde in diesem Client nicht implementiert." });
            }
        }

        /// <summary>
        /// Called by the hub in order to perform a system reboot.
        /// Executes the corresponding action set by the application using this client.
        /// </summary>
        void IApplicationClient.RequestSystemReboot()
        {
            if (ActionForSystemRebootRequest != null)
            {
                Exception result = ActionForSystemRebootRequest();

                if (result == null)
                    PostCommandResult(new SignalRCommandResult() { CommandName = nameof(IApplicationClient.RequestSystemReboot), IsExecutedSuccessfully = true, Message = "Neustart wurde erfolgreich ausgelöst.", UserFriendlyErrorMessage = null });
                else
                    PostCommandResult(new SignalRCommandResult() { CommandName = nameof(IApplicationClient.RequestSystemReboot), IsExecutedSuccessfully = false, Message = "Neustart konnte nicht ausgelöst werden.", UserFriendlyErrorMessage = result.ToString() });
            }
            else
            {
                PostCommandResult(new SignalRCommandResult() { CommandName = nameof(IApplicationClient.RequestSystemReboot), IsExecutedSuccessfully = false, Message = null, UserFriendlyErrorMessage = $"Kommando '{nameof(IApplicationClient.RequestSystemReboot)}' wurde in diesem Client nicht implementiert." });
            }
        }

        /// <summary>
        /// Posts a command result message to the hub (usually done after a function was called by the hub).
        /// </summary>
        /// <param name="result"></param>
        /// <returns></returns>
        public Task<Exception> PostCommandResult(SignalRCommandResult result)
        {
            myHub.Invoke(nameof(IApplicationHub.PostCommandResult), result);
            OnResultToHubSent?.Invoke(this, new Tuple<string, SignalRCommandResult>(nameof(IApplicationHub.PostCommandResult), result));
            return null;
        }

        #endregion
    }
}
