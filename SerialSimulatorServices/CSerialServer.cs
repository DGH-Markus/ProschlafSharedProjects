using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Management;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Logging;
using SimulatorInterfaces;

namespace SerialSimulatorServices
{
    public class CSerialServer
    {
        #region Vars
        static CSerialServer myInstance = null;
        List<CSerialSimulator> connectedSimulators = new List<CSerialSimulator>();

        bool bSearchRunning = false;
        #endregion

        #region Props
        public static CSerialServer Instance
        {
            get
            {
                if (myInstance == null)
                    myInstance = new CSerialServer();

                return myInstance;
            }
        }

        /// <summary>
        /// This property can be used to check if the search for conencted devices is currently running.
        /// </summary>
        public bool SearchIsRunning
        {
            get { return bSearchRunning; }
        }
        #endregion

        /// <summary>
        /// Fired when a simulator connects or disconnects.
        /// </summary>
     //   public event Action<List<CSerialSimulator>> OnSimulatorConnectionChanged;

       // public event EventHandler<CSerialSimulator> OnSimulatorConnectionLost;

        private CSerialServer() { }

        public List<CSerialSimulator> SearchDevices(int requiredNumberOfDevices, bool useQuickConnect, string quickConnectComPorts, CBaseSimulator.SimulatorSetupTypes type, bool isDebugMode)
        {
            try
            {
                if (bSearchRunning)
                    return null;

                bSearchRunning = true;

                //first, try the last used COM port if we only need one simulator device
                if (useQuickConnect && type == SimulatorInterfaces.CBaseSimulator.SimulatorSetupTypes.OneDevice)
                {
                    #region Quick connect single simulator

                    string comPortDeluxe = "";

                    if (!quickConnectComPorts.Contains('|')) //this may contain more than one COM port, separated by '|'
                        comPortDeluxe = quickConnectComPorts;

                    if (!string.IsNullOrEmpty(comPortDeluxe))
                    {
                        try
                        {
                            CSerialSimulator newSim = new CSerialSimulator(comPortDeluxe, type, false, isDebugMode); //try to connect to a simulator device

                            if (newSim.IsConnected) //bingo!
                            {
                                Logger.AddLogEntry(Logger.LogEntryCategories.Debug, "Quick connect found a simulator at COM port: " + newSim.ComPort);
                                connectedSimulators.Add(newSim);
                                goto Finish;
                            }
                        }
                        catch { }
                    }
                    #endregion
                }
                else if (useQuickConnect && type == SimulatorInterfaces.CBaseSimulator.SimulatorSetupTypes.MultipleDevices)
                {
                    #region Quick connect multiple simulators

                    string[] comPorts = null;

                    if (quickConnectComPorts.Contains('|')) //this may contain more than one COM port, separated by '|'
                        comPorts = quickConnectComPorts.Split('|');

                    if (comPorts != null && comPorts.Length > 1)
                    {
                        try
                        {
                            foreach (string comPort in comPorts)
                            {
                                CSerialSimulator newSim = new CSerialSimulator(comPort, type, false, isDebugMode); //try to connect to a simulator device

                                if (newSim.IsConnected) //bingo!
                                {
                                    connectedSimulators.Add(newSim);
                                }
                            }

                            if (connectedSimulators.Count == requiredNumberOfDevices)
                                goto Finish;
                        }
                        catch { }
                    }
                    #endregion
                }

                List<string> portDescrList = new List<string>();

                using (var searcher = new ManagementObjectSearcher("SELECT DeviceID,Caption FROM WIN32_SerialPort")) //get descriptions for each serial port so that we can pre-filter the port list
                {
                    string[] portnames = SerialPort.GetPortNames();
                    var ports = searcher.Get().Cast<ManagementBaseObject>().ToList();
                    var tList = (from n in portnames
                                 join p in ports on n equals p["DeviceID"].ToString()
                                 select n + " - " + p["Caption"]).ToList();

                    portDescrList.AddRange(tList);
                }

                List<string> filteredComPorts = new List<string>();

                //filter the serial port list to only have usb and bluetooth ports
                foreach (string port in new List<string>(portDescrList))
                {
                    if (!port.Contains("USB") && !port.Contains("Bluetooth"))
                        portDescrList.Remove(port);
                }

                //now remove the descriptions to have a list that contains only items in the form "COM1", "COM7", ...
                Regex comPortRegex = new Regex(@"COM\d{1,3}");
                List<string> filteredPorts = new List<string>();
                foreach (string port in portDescrList)
                {
                    if (comPortRegex.Match(port).Success)
                        filteredPorts.Add(comPortRegex.Match(port).Value);
                }

                //on Windows 10, COM ports sometimes contain invalid characters due to a bug in SerialPort.GetPortNames() --> validate these ports (although they shouldn't be possible at this point anymore)
                foreach (string port in new List<string>(filteredPorts))
                {
                    string number = Regex.Replace(port, @"\D*(\d+)\D*", "$1"); //only gets the number of the COM port (e.g. "12" in "COM12")

                    if (!port.EndsWith(number))
                    {
                        Logger.AddLogEntry(Logger.LogEntryCategories.Warning, "Warning in CSerialServer.SearchDevices(): invalid COM port was found and replaced: " + port);
                        filteredPorts.Remove(port);
                        filteredPorts.Add("COM" + number);
                    }
                }

                //the variable "filteredPorts" now only contains ports that most likely are not being used by another device, now remove ports that are already used by an active simulator
                foreach (CSerialSimulator sim in connectedSimulators)
                    if (filteredPorts.Contains(sim.ComPort))
                        filteredPorts.Remove(sim.ComPort);

                //now remove any disconnected simulator
                foreach (CSerialSimulator sim in new List<CSerialSimulator>(connectedSimulators))
                {
                    if (!sim.IsConnected)
                    {
                        connectedSimulators.Remove(sim);
                    }
                }

                //now build a list of newly connected devices (this does not necessarily only count for simulator devices, but also any other serial port device)
                List<CSerialSimulator> connectedDevices = new List<CSerialSimulator>();
                List<Task> runningTasks = new List<Task>();

                Logger.AddLogEntry(Logger.LogEntryCategories.Debug, "Attempting to find simulators on the following ports: " + string.Join(", ", filteredComPorts));

                foreach (string port in filteredPorts)
                {
                    try
                    {
                        Task t = Task.Factory.StartNew(() =>
                        {
                            CSerialSimulator newSim = new CSerialSimulator(port, type, false, isDebugMode); //try to connect to a simulator device

                            lock (connectedDevices)
                            {
                                connectedDevices.Add(newSim); //doing this asynchronously causes errors
                            }
                        });

                        runningTasks.Add(t);
                    }
                    catch (Exception ex)
                    {
                        Logger.AddLogEntry(Logger.LogEntryCategories.Error, "Exception in CSerialServer.SearchDevices() while trying to find simulator devices on all ports", ex);
                    }
                }

                bool waitSuccess = Task.WaitAll(runningTasks.ToArray(), 30000);

                if (!waitSuccess)
                    Logger.AddLogEntry(Logger.LogEntryCategories.Warning, "The tasks to find the simulators was aborted after 30s!");

                //now add any connected simulator to the global list
                foreach (CSerialSimulator simulator in connectedDevices)
                    if (simulator.IsConnected) //if connected == false, this means that the device was not a simulator
                    {
                        connectedSimulators.Add(simulator);
                    }

                //now check if those that were already connected are still connected
                foreach (CSerialSimulator simulator in new List<CSerialSimulator>(connectedSimulators))
                    if (!portDescrList.Contains(simulator.ComPort) && !CheckConnectionToDeviceAsync(simulator))
                    {
                        connectedSimulators.Remove(simulator);
                    }

                Finish:
                bSearchRunning = false;

            //    if (bConnectionChanged && this.OnSimulatorConnectionChanged != null)
               //     this.OnSimulatorConnectionChanged(connectedSimulators);

                return connectedSimulators;
            }
            catch (Exception ex)
            {
                Logger.AddLogEntry(Logger.LogEntryCategories.Error, "Exception in CSerialServer.SearchDevices()", ex);
                return null;
            }
        }

        /// <summary>
        /// Starts a task that checks whether a simulator is still connected or not. Has a maximum runtime of 3 seconds.
        /// </summary>
        /// <param name="device"></param>
        /// <returns>True if connection is OK, false if the device is not connected anymore.</returns>
        private bool CheckConnectionToDeviceAsync(CSerialSimulator device)
        {
            if (device.bSimulatorIsInUse || !device.IsConnected)
                return true;

            var task = Task<bool>.Factory.StartNew(() => CheckClientConnection(device)); //start a new task that gets the devices ID to check the connection 

            if (task.Wait(3000)) //true if the task completes its work within the specified time (no timeout occured)
                return task.Result;
            else
                return false;
        }

        /// <summary>
        /// Checks the connection to the specified simulator.
        /// </summary>
        /// <param name="client">The simulator to be checked.</param>
        /// <returns>True if connected, false otherwise.</returns>
        private bool CheckClientConnection(CSerialSimulator sim)
        {
            string id = sim.GetDeviceId();

            if (sim.IsConnected && id != "") //usually Com.Connected is still true here even if the connection was lost
                return true;

            return false;
        }

        /// <summary>
        /// Retrieves a copy of the list of all connected serial devices.
        /// The list can be manually updated by calling SearchDevices().
        /// </summary>
        /// <returns></returns>
        public List<CSerialSimulator> GetConnectedSimulators()
        {
            return new List<CSerialSimulator>(connectedSimulators.Where(s => s.IsConnected));
        }
    }
}
