
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SerialSimulatorServices;

using System.Threading;
using NgMattApiWrapper;
using Logging;
using SimulatorInterfaces;

namespace SimulatorController
{

    namespace MultiSimulatorMode
    {
        /// <summary>
        /// This class manages all communication/connection-related tasks for simulator and ngMatt devices and offers methods to retrieve and manage them.
        /// </summary>
        public class SimulatorControl
        {
            #region Vars
            private static SimulatorControl myInstance = new SimulatorControl();

            private bool bInitialized = false;
            private bool bSimulatorConncted = false, bNgMattConnected = false;

            private Dictionary<string, CBaseSimulator> simulatorInstances = new Dictionary<string, CBaseSimulator>(); //a list that holds all instances of simulators during runtime, accessable through the hardware device id
            #endregion

            #region Events

            /// <summary>
            /// Occurs when a new simulator connects to this software or when an active client loses the connection.
            /// </summary>
            public event Action<List<string>> SimulatorConnectionChanged;
            #endregion

            #region Props
            public static SimulatorControl Instance
            {
                get
                {
                    return myInstance;
                }
            }
            #endregion

            private SimulatorControl()
            {
               // CSerialServer.Instance.OnSimulatorConnectionChanged += Instance_SimulatorConnectionChanged;

                //register NgMattApiWrapper events
                ApiFunctions.Instance.NgMattDeviceConnected += Instance_NgMattDeviceConnected; //this is (and should remain) the only point in the Liegesimulator application that handles this event
                ApiFunctions.Instance.NgMattConnectionLost += Instance_NgMattConnectionLost;
            }

            /// <summary>
            /// Starts search for connected usb/bluetooth devices and prepares all connected simulators for use.
            /// </summary>
            /// <returns>True if startup was successful, false otherwise.</returns>
            public bool Initialize(int numberOfRequiredDevices, bool useQuickConnect, string quickConnectComPorts, CBaseSimulator.SimulatorSetupTypes type, bool isDebugMode)
            {
                if (bInitialized)
                    return true;

                while (CSerialServer.Instance.SearchIsRunning) //if this method is called more than once before the first call has finished, the server is already searching for the devices --> wait for the search to finish
                    Thread.Sleep(10);

                var devices = CSerialServer.Instance.SearchDevices(numberOfRequiredDevices, useQuickConnect, quickConnectComPorts, type, isDebugMode);

                if(devices.Count > 0)
                {
                    bSimulatorConncted = true;
                    List<string> connectedIds = new List<string>();

                    foreach (var client in devices)
                        connectedIds.Add(client.DeviceId);

                    if (SimulatorConnectionChanged != null)
                        SimulatorConnectionChanged(connectedIds);
                }

                return devices.Count == numberOfRequiredDevices;
            }

            /// <summary>
            /// Indicates whether the search for simulator/ngMatt-devices is still running.
            /// </summary>
            /// <returns></returns>
            public bool GetIsSearchRunning()
            {
                return CSerialServer.Instance.SearchIsRunning || ApiFunctions.Instance.GetIsSearchRunning();
            }

            /// <summary>
            /// Invoked when a ngMatt connects or disconnects to this software via usb or bluetooth.
            /// This method is the central point for new connections to ngMatts and is tasked to update any other part of this program concerning the newly connected device.
            /// </summary>
            private void Instance_NgMattDeviceConnected(object sender, CngMattSimulator device)
            {
                bNgMattConnected = true;
                Exception ex = CngMattServer.Instance.AddNewlyConnectedDevice(device);

                if (ex != null)
                {
                    Logger.AddLogEntry(Logger.LogEntryCategories.Error, "SimulatorControl.Instance_NgMattDeviceConnected(): exception while adding new device to CngMattServer", ex);
                    return;
                }

                List<string> connectedIds = new List<string>();
                connectedIds.Add(device.DeviceId);

                if (SimulatorConnectionChanged != null)
                    SimulatorConnectionChanged(connectedIds);
            }

            /// <summary>
            /// Fired when a connection to an ngMatt device was lost.
            /// </summary>
            private void Instance_NgMattConnectionLost(object sender, NgMattConnectionException e)
            {
                if (e.Device?.DeviceId != null)
                {
                    Exception ex = CngMattServer.Instance.RemoveConnectedDeviceById(e.Device.DeviceId);

                    if (ex != null)
                        Logger.AddLogEntry(Logger.LogEntryCategories.Error, "SimulatorControl.Instance_NgMattConnectionLost(): exception while removing device from CngMattServer", ex);
                }
                else
                    Logger.AddLogEntry(Logger.LogEntryCategories.Error, "SimulatorControl.Instance_NgMattConnectionLost(): device in NgMattConnectionException was NULL", e);
            }

            /// <summary>
            /// Retrieves a list of all simulators that are currently connected to this software.
            /// This includes all clients connected all usb/bluetooth clients, be they simulators or ngMatts.
            /// </summary>
            /// <returns>A list of CBaseSimulator objects.</returns>
            public List<CBaseSimulator> GetAllConnectedSimulators()
            {
                List<CBaseSimulator> allClients = new List<CBaseSimulator>();

                //simulators
                List<CSerialSimulator> serialClients = CSerialServer.Instance.GetConnectedSimulators();
                allClients.AddRange(serialClients);

                //ngMatt(s)
                List<CngMattSimulator> ngMatts = CngMattServer.Instance.GetConnectedDevices();
                allClients.AddRange(ngMatts);

                return allClients;
            }

            /// <summary>
            /// Returns a list of hardware ids of all currently connected devices.
            /// </summary>
            /// <returns></returns>
            public List<string> GetIdsOfConnectedSimulators()
            {
                List<string> ids = new List<string>();

                List<CSerialSimulator> serialClients = CSerialServer.Instance.GetConnectedSimulators(); //...and all connected serial devices
                serialClients.ForEach(c => ids.Add(c.DeviceId));

                //ngMatt(s)
                List<CngMattSimulator> ngMatts = CngMattServer.Instance.GetConnectedDevices();
                ngMatts.ForEach(c => ids.Add(c.DeviceId));

                return ids.Distinct().OrderBy(s => s.Length).ThenBy(s => s).ToList();
            }

            /// <summary>
            /// Searches for the specified simulator using its id in the list of the connected clients and returns it.
            /// </summary>
            /// <param name="id">The hardware device id of the simulator.</param>
            /// <returns>The CBaseSimulator object (either a CNetworkSimulator or a CSerialSimulator) or null.</returns>
            public SimulatorInterfaces.CBaseSimulator GetSimulatorById(string id)
            {
                if (string.IsNullOrEmpty(id))
                    return null;

                if (simulatorInstances.ContainsKey(id)) //check if the CNetworkSimulator for this id is already existing
                    return simulatorInstances[id];

                if (bSimulatorConncted)
                {
                    //try to return a CSerialSimulator 
                    var serialDevice = from device in CSerialServer.Instance.GetConnectedSimulators() where device.DeviceId == id select device;

                    if (serialDevice.FirstOrDefault() != null)
                    {
                        simulatorInstances.Add(id, serialDevice.FirstOrDefault() as CSerialSimulator);
                        return serialDevice.FirstOrDefault();
                    }
                }
                else if (bNgMattConnected) //ngMatt
                {
                    List<CngMattSimulator> ngMattDevices = ApiFunctions.Instance.GetConnectedDevices();

                    CngMattSimulator device = ngMattDevices.FirstOrDefault(d => d.DeviceId == id);

                    if (device == null)
                        return null;

                    //map the ngMattDevice to CngMattSimulator (which derives from CBaseSimulator) 
                    simulatorInstances.Add(id, device); //add it to the dictionary to make sure that always the same object is returned for the current id
                    return device;
                }

                return null;
            }
        }
    }

    namespace SingleSimulatorMode
    {
        /// <summary>
        /// This class manages all single-serial-simulator related tasks and offers methods to retrieve and manage the connected simulator device.
        /// </summary>
        public class SimulatorControl
        {
            #region Vars
            private static SimulatorControl myInstance = null;

            private CBaseSimulator myDevice = null;
            #endregion

            #region Props


            public static SimulatorControl Instance
            {
                get
                {
                    if (myInstance == null)
                        myInstance = new SimulatorControl();

                    return myInstance;
                }
            }
            #endregion

            /// <summary>
            /// Instanciates a new simulator object and attemps to establish a connection.
            /// </summary>
            private SimulatorControl() { }

            /// <summary>
            /// Starts search for connected usb/bluetooth devices and prepares all connected simulators for use.
            /// </summary>
            /// <returns>True if startup was successful, false otherwise.</returns>
            public bool Initialize(bool useQuickConnect, string quickConnectComPorts, CBaseSimulator.SimulatorSetupTypes type, bool isDebugMode)
            {
                var devices = CSerialServer.Instance.SearchDevices(1, useQuickConnect, quickConnectComPorts, type, isDebugMode);

                return devices?.Count > 0;
            }

            /// <summary>
            /// Retrieves the USB/Bluetooth device connected to this software.
            /// </summary>
            /// <returns>The simulator object or null.</returns>
            public CBaseSimulator GetOstererSimulator()
            {
                if (myDevice == null)
                    myDevice = CSerialServer.Instance.GetConnectedSimulators().FirstOrDefault();

                return myDevice;
            }

            /// <summary>
            /// Retrieves the ngMatt device connected to this software.
            /// </summary>
            /// <returns>The simulator object or null.</returns>
            public CBaseSimulator GetNgMatt()
            {
                List<CngMattSimulator> ngMatts = CngMattServer.Instance.GetConnectedDevices();

                if (ngMatts?.Count > 0)
                    return ngMatts.First();

                return null;
            }

            /// <summary>
            /// Returns the demo simulator object that has no connection to a device.
            /// </summary>
            /// <returns></returns>
            public CBaseSimulator GetDemoObject()
            {
                return new CDemoSimulator();
            }
        }
    }
}
