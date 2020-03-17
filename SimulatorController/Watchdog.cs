using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Win32;
using System.Runtime.InteropServices;

using System.Timers;
using System.Windows.Forms;
using Logging;
using SimulatorInterfaces;

namespace SimulatorController
{
    /// <summary>
    /// Singleton class used to listen for certain system events that can occur during the runtime of the application.
    /// Keeps the connection to the simulator devices open by pinging them every n seconds.
    /// Also watches for Windows shutdown and screen saver activation and acts accordingly.
    /// </summary>
    public class Watchdog
    {
        #region Vars
        private static Watchdog myInstance = null;

        private System.Timers.Timer ScreenSaverStateCheckTimer = new System.Timers.Timer() { AutoReset = true };
        private System.Timers.Timer SimulatorPingTimer = new System.Timers.Timer() { AutoReset = false };

        bool bWatching = false;

        static int timerTickCnt = 0;

        CBaseSimulator.SimulatorSetupTypes simulatorSetupType = CBaseSimulator.SimulatorSetupTypes.OneDevice;
        #endregion

        #region Props
        public static Watchdog Instance
        {
            get
            {
                if (myInstance == null)
                    myInstance = new Watchdog();

                return myInstance;
            }
        }
        #endregion

        #region Imports
        [DllImport("user32", CharSet = CharSet.Auto)]
        public static extern int SystemParametersInfo(int uAction, int uParam, ref int lpvParam, int fuWinIni);
        const int SPI_GETSCREENSAVERRUNNING = 114;
        int screenSaverRunning = -1;
        #endregion

        #region Events
        /// <summary>
        /// When any kind of error occurs (e.g. when the Windows screen saver was activated), this event with a user-friendly message in German is fired.
        /// </summary>
        public event EventHandler<string> OnErrorOccured;
        #endregion

        private Watchdog()
        { }

        /// <summary>
        /// Initializes the events and the timers needed for observation of the system.
        /// </summary>
        public void StartWatching(CBaseSimulator.SimulatorSetupTypes setupType)
        {
            this.simulatorSetupType = setupType;

            if (!bWatching)
            {
                Logger.AddLogEntry(Logger.LogEntryCategories.Debug, "Watchdog.StartWatching() called.");
                SystemEvents.PowerModeChanged += new PowerModeChangedEventHandler(SystemEvents_PowerModeChanged); //detect standby, hibernate, wake up, ...

                ScreenSaverStateCheckTimer.Elapsed += new ElapsedEventHandler(ScreenSaverStateCheckTimer_Tick); //periodically checks if screensaver was activated
                ScreenSaverStateCheckTimer.Interval = 2000;

                SimulatorPingTimer.Elapsed += SimulatorPingTimer_Elapsed;
                SimulatorPingTimer.Interval = 30000;
            }

            ScreenSaverStateCheckTimer.Start();
            SimulatorPingTimer.Start();
            bWatching = true;
        }

        /// <summary>
        /// Interrups the observation of the system.
        /// </summary>
        public void StopWatching()
        {
            if (!bWatching)
                return;

            Logger.AddLogEntry(Logger.LogEntryCategories.Debug, "Watchdog.StopWatching() called.");

            ScreenSaverStateCheckTimer.Stop();
            SimulatorPingTimer.Stop();

            SystemEvents.PowerModeChanged -= SystemEvents_PowerModeChanged;
            bWatching = false;
        }

        /// <summary>
        /// This event is fired when the system goes to sleep or wakes up.
        /// </summary>
        void SystemEvents_PowerModeChanged(object sender, PowerModeChangedEventArgs e)
        {
            switch (e.Mode)
            {
                case PowerModes.Suspend:
                    break;
                case PowerModes.Resume:
                    OnErrorOccured?.Invoke(this, "Der Computer wurde aus dem Standby-Modus geholt. Bitte starten Sie die Software neu, um Verbindungsprobleme mit dem Gerät zu vermeiden!");
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Checks if the ScreenSaver is active at the moment. 
        /// Reason: the ScreenSaver sometimes turns off the USB ports and after that, the connection to the simulator is lost.
        /// Also "pings" the simulator to maintain the connection.
        /// </summary>
        void ScreenSaverStateCheckTimer_Tick(object sender, EventArgs e)
        {
            int ok = SystemParametersInfo(SPI_GETSCREENSAVERRUNNING, 0, ref screenSaverRunning, 0);

            if (screenSaverRunning != 0) //screen saver is running
            {
                ScreenSaverStateCheckTimer.Stop();
                OnErrorOccured?.Invoke(this, "Der Windows Bildschirmschoner war aktiv. Bitte starten Sie die Software neu, um Verbindungsprobleme mit dem Gerät zu vermeiden!");
            }
        }

        /// <summary>
        /// Pings each available simulator device currently connected to this software.
        /// </summary>
        void SimulatorPingTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            timerTickCnt++;

            if (this.simulatorSetupType == CBaseSimulator.SimulatorSetupTypes.OneDevice)
            {
                CBaseSimulator simulator = SimulatorController.SingleSimulatorMode.SimulatorControl.Instance.GetOstererSimulator();
                string id = simulator.GetDeviceId(); //retrieve the id of the device to see whether it is still connected or not
                SimulatorPingTimer.Start();
                return;
            }

            List<CBaseSimulator> Simulators = SimulatorController.MultiSimulatorMode.SimulatorControl.Instance.GetAllConnectedSimulators();

            if (Simulators == null)
            {
                SimulatorPingTimer.Start();
                return;
            }

            foreach (CBaseSimulator device in Simulators)
            {
                string id;

                if (device is SerialSimulatorServices.CSerialSimulator)
                    if ((device as SerialSimulatorServices.CSerialSimulator).IsConnected)
                    {
                        if ((device as SerialSimulatorServices.CSerialSimulator).LastUsageTime < DateTime.Now.AddMinutes(-10d))
                            id = (device as SerialSimulatorServices.CSerialSimulator).GetDeviceId();
                    }
                    else if (device is NgMattApiWrapper.CngMattSimulator)
                        id = device.GetDeviceId(); //this does not actually communicate with the device as this is not needed for ngMatts
                    else
                        OnErrorOccured?.Invoke(this, "Fehler im Watchdog beim überprüfen der Simulatorverbindungen. Unbekannter Typ: " + device.GetType().ToString());
            }

            SimulatorPingTimer.Start();
        }
    }
}
