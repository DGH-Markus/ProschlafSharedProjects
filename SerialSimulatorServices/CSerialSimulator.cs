using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.IO.Ports;
using System.Globalization;
using ComLib;
using PressureDeviceLib;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using System.Threading;
using System.Xml;
using SimulatorInterfaces;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Linq;
using Logging;
using System.Diagnostics;
using static SimulatorInterfaces.SimulatorExceptions;
using ProschlafSupportProfileGenerationLibrary;

namespace SerialSimulatorServices
{
    /// <summary>
    /// This represents the control class for the USB/Bluetooth Simulator device and provides methods to control the it.
    /// The DLLs "ComLib" and "PressureDeviceLib" are provided by Fa. Dr. Osterer and are used to control the simulator hardware.
    /// Connection usually is made using an USB port, but there is also support for bluetooth connections.
    /// </summary>
    public class CSerialSimulator : CBaseSimulator
    {
        #region Variables
        internal PressureDevice myDevice = new PressureDevice(); //DLL used to communicate with the Simulator device

        bool bAttemptingConnection = false; //while initializing the simulator, this is true

        const int PRESSURE_DEVICE_ID = 7;       // ID of the USB / bluetooth device
        const int PRESSURE_NW_DEVICE_ID = 13;       // ID of the LAN/Wifi device

        const int BAUD_RATE = 57600; // old/alternate Baud rate: 115200 

        bool bWasConnected = false; //determines if there was a connection to the device at least once

        string myComPort = "";

        //  protected string pathToConfigDir = null; //path to the Config folder where the pressure levels are saved in the levels.txt file
        protected bool isDemo = false;
        protected bool isDebugMode = false;
        private static readonly object configLock = new object();

        bool disconnectEventReceived = false;
        #endregion

        #region Props

        /// <summary>
        /// The COM port for this device.
        /// </summary>
        public string ComPort
        {
            get { return myComPort; }
        }

        /// <summary>
        /// Accessor to the status display instance associated with this simulator class.
        /// </summary>
        [Obsolete]
        public override IStatusDisplayFunctions StatusDisplay
        {
            get
            {
                if (myStatusDisplayInstance == null || myStatusDisplayInstance.IsDisposed)
                    myStatusDisplayInstance = SimulatorInterfaces.StatusDisplay.Instance;

                return myStatusDisplayInstance;
            }
        }

        public override int[] CurrentPressureLevels
        {
            get { return myPressureLevels; }
        }

        public override bool IsConnected
        {
            get { return myDevice.Com.Connected & !bAttemptingConnection; } //only return true if the connection-attempt-procedure is not currently running
        }

        /// <summary>
        /// Updated everytime some form of communication successfully happened with the device.
        /// </summary>
        public DateTime LastUsageTime { get; internal set; } = DateTime.MinValue;

        #endregion

        /// <summary>
        /// Attempts to connect to a device using the specified COM port.
        /// </summary>
        public CSerialSimulator(string port, CBaseSimulator.SimulatorSetupTypes setupType, bool isDemoVersion, bool isDebugModeActivated)
        {
            this.myComPort = port;
            this.isDemo = isDemoVersion;
            this.isDebugMode = isDebugModeActivated;
            this.setupType = setupType;

            if (setupType == SimulatorSetupTypes.OneDevice)
                myStatusDisplayInstance = SimulatorInterfaces.StatusDisplay.Instance;

            if (!isDemo && !string.IsNullOrEmpty(port) && port.Contains("COM"))
            {
                InitializeSimulator();
            }
            else //only register error events
            {
                #region ErrorEventHandlers //Error Handlers are initialized here
                myDevice.NoConnectionError += new EventHandler(MyDevice_NoConnectionError);
                myDevice.PressureDeviceLibError += new EventHandler<DeviceEventArgs>(Simulator_PressureDeviceLibError);
                myDevice.Com.ComLibError += new EventHandler<ComLib.ErrorEventArgs>(Com_ComLibError);
                myDevice.Com.DeviceError += new EventHandler(Com_DeviceError);
                #endregion
            }
        }

        public override string ToString()
        {
            return "Liegesimulator [" + this.deviceID + "] - " + this.ComPort;
        }

        /// <summary>
        /// Used to find the port where the Simulator is conncected to and to initialize it.
        /// Functionality: a Port Array is generated and iterated through. In every iteration step,
        /// a message is sent which asks for the ID of the Simulator. If the simulator is at that port, it will send
        /// an answer back. If an answer is received, the correct port is found.
        /// </summary>
        private void InitializeSimulator()
        {
            #region Register ErrorEventHandlers
            myDevice.NoConnectionError += MyDevice_NoConnectionError;
            myDevice.PressureDeviceLibError += new EventHandler<DeviceEventArgs>(Simulator_PressureDeviceLibError);
            myDevice.Com.ComLibError += new EventHandler<ComLib.ErrorEventArgs>(Com_ComLibError);
            myDevice.Com.DeviceError += new EventHandler(Com_DeviceError);
            myDevice.Com.DisconnectError += Com_DisconnectError;
            #endregion

            try
            {
                #region Basic init: needs to be done before the device can be used
                myDevice.Com.Checksumme = false; //Checksum
                myDevice.Com.WaitSteps = 20; //Wait-time after every command in ms
                myDevice.Com.KommandoTimeOut = 3000;  //Timeout in ms when there is no answer
                myDevice.Com.WriteLogfile = this.bWriteDeviceLogFile;

                #endregion

                bAttemptingConnection = true;

                if (StatusDisplay != null)
                    StatusDisplay.SetTextToStatus(SimulatorStati.Connecting);

                AttemptConnection();

                bAttemptingConnection = false;

                if (myDevice == null || myDevice.Com.Connected == false) //if no Simulator was found
                    Simulator_NoConnectionError(null, null);
                else
                    ConnectionEstablished();
            }
            catch (UnauthorizedAccessException UAE)
            {
                this.OnErrorOccured(new Exception("The simulator port is already in use or could not be opened!", UAE));
            }
            catch (IOException IOE)
            {
                this.OnErrorOccured(new Exception("The port you chose is not available.", IOE));
            }
            catch (ObjectDisposedException) { } //ignore
            catch (Exception ex)
            {
                this.OnErrorOccured(new Exception("An unknown error occured while initializing the simulator.", ex));
            }
            finally
            {
                bAttemptingConnection = false;
            }
        }

        /// <summary>
        /// Attempts to connect to the simulator which can be connected via USB or bluetooth (using COM ports).
        /// First, gets a list of used COM ports. If there is only one, connects to the simulator there.
        /// If there are more COM devices connected to the computer, the simulator is searched at the port it was last connected to.
        /// If that fails, a list with all used port names is iterated in order to find the simulator.
        /// </summary>
        private void AttemptConnection()
        {
            try
            {
                myDevice.Com.SetComPort(myComPort, BAUD_RATE, System.IO.Ports.Parity.None, 8, System.IO.Ports.StopBits.One); //sets the Port for the Class that controlls the Sim (old Baud: 115200)
                myDevice.Com.Connect();

                System.Threading.Thread.Sleep(20);

                string devId = myDevice.GetSerialNbr();

                if (string.IsNullOrEmpty(devId) || !myDevice.Com.Connected)
                {
                    if (myDevice.Com.Connected)
                        Logger.AddLogEntry(Logger.LogEntryCategories.Warning, "The device appears to be connected, but GetDeviceID() did not return an ID. Device will be disconnected.", null, "CSerialSimulator");

                    myDevice.Com.Disconnect();
                }
                else
                    return; //connection established
            }
            catch
            {
                //if there is no simulator available on this port
                myDevice.Com.Disconnect();
            }
        }

        /// <summary>
        /// This method is called when the connection to the simulator was established successfully.
        /// </summary>
        private void ConnectionEstablished()
        {
            bWasConnected = true;
            LastUsageTime = DateTime.Now;

            AdjustPressureMaximumValues();

            AdjustPumpingParameters();

            this.deviceID = myDevice.GetSerialNbr();

            if (setupType == SimulatorSetupTypes.MultipleDevices) //instantiate a new dummy display for this simulator object
                myStatusDisplayInstance = new SimulatorInterfaces.StatusDisplayDummy(deviceID, this);

            myStatusDisplayInstance.SetTextToStatus(SimulatorInterfaces.SimulatorStati.Ready);

            string firmwareVersion = myDevice.Com.GetFirmwareVersion();

            if (firmwareVersion != "01.06.01")
            {
                firmwareUpdateRequired = true;
                Logger.AddLogEntry(Logger.LogEntryCategories.Warning, $"Warning: Firmware version is too low ({firmwareVersion}). Simultaneous pumping operations of multiple channels is not possible. Please perform a firmware update!", null, "LiegesimulatorDevice");
            }
        }

        /// <summary>
        /// Checks if the simulator has the correct maximum pressure values set and adjusts them if needed.
        /// </summary>
        protected override void AdjustPressureMaximumValues()
        {
            int maxDruckParam = 0;
            int maxDruckAbsolut = 0;
            myDevice.GetDruckMaximum(out maxDruckParam, out maxDruckAbsolut);

            if (maxDruckParam != PRESSURE_MAXIMUM_PARAMETER)
            {
                myDevice.SetDruckMaximum(PRESSURE_MAXIMUM_PARAMETER, maxDruckAbsolut);
            }

            if (maxDruckAbsolut != PRESSURE_MAXIMUM_ABSOLUTE)
            {
                myDevice.SetDruckMaximum(maxDruckParam, PRESSURE_MAXIMUM_ABSOLUTE);
            }

            LastUsageTime = DateTime.Now;
        }

        /// <summary>
        /// Overwrites some standard device parameters with values that are closer to reality. 
        /// </summary>
        private void AdjustPumpingParameters()
        {
            myDevice.SetDruckRegelParam(60, 100, 1, 3000, 4000); //standard parameters defined by Fa. Osterer are: 40, 100, 1, 3000, 4000

            LastUsageTime = DateTime.Now;
            /*
            byte minSpeed, maxSpeed;
            int regelBereich, steigAusgangzeit, fallAusgangszeit;
            myPressureDevice.GetDruckRegelParam(out minSpeed, out maxSpeed, out regelBereich, out steigAusgangzeit, out fallAusgangszeit);
            
            if(steigAusgangzeit != 3000)
            {
                myPressureDevice.SetDruckRegelParam(40, 100, 1, 3000, 4000); //standard parameters defined by Fa. Osterer
            }
            */
        }

        public override void GetPressureMaxima(out int pressureParameter, out int pressureAbsolute)
        {
            if (myDevice == null || !myDevice.Com.Connected || bSimulatorIsInUse)
            {
                pressureParameter = pressureAbsolute = -1;
                return;
            }

            myDevice.GetDruckMaximum(out pressureParameter, out pressureAbsolute);

            LastUsageTime = DateTime.Now;
        }

        public override bool SetPressureMaxima(int pressureParameter, int pressureAbsolute)
        {
            try
            {
                myDevice.SetDruckMaximum(pressureParameter, pressureAbsolute);
                LastUsageTime = DateTime.Now;
                return true;
            }
            catch (Exception) { }

            return false;
        }

        /// <summary>
        /// Applys the speficied pressure value (in mB) to the specified valve.
        /// </summary>
        /// <param name="valveNumber">The valve to be used during the pumping procedure (1 - 12).</param>
        /// <param name="pressureValue">The pressure level to be applied (usually 5 - 199mB).</param>
        /// <returns>True if the procedure succeeded; false otherwise.</returns>
        public override Exception ApplySpecificPressure(int valveNumber, int pressureValue, IProgress<GenericSimulationProgress> progress = null)
        {
            if (pressureValue < 1 || pressureValue > 199)
                return new SimulatorExceptions.InvalidPressureException() { DeviceId = this.DeviceId, PressureValue = (short)pressureValue };

            return PumpValve(valveNumber, pressureValue, progress);
        }

        /// <summary>
        /// Applies the specified pressure value (in mB) to the specified valves at once.
        /// </summary>
        /// <param name="valveNumbers">The numbers of all valves to be pumped (1-12).</param>
        /// <param name="pressureValue">The pressure level to be applied (usually 5 - 199mB).</param>
        /// <returns></returns>
        public override Exception ApplySpecificPressureToValves(List<int> valveNumbers, int pressureValue, IProgress<GenericSimulationProgress> progress = null)
        {
            if (pressureValue < 1 || pressureValue > 199)
                return new SimulatorExceptions.InvalidPressureException() { DeviceId = this.DeviceId, PressureValue = (short)pressureValue };

            if (valveNumbers == null || valveNumbers.Count < 1)
                return new ArgumentNullException("valveNumbers", "NULL or empty.");

            return PumpValves(valveNumbers, pressureValue);
        }

        /// <summary>
        /// Pumps a single valve to the received value, ranges from 5mB to pressureMaximum (199mB).
        /// </summary>
        /// <param name="valveNumber">valve number, from 1 to 12 (or 255 for all).</param>
        /// <param name="pressureValue">The desired pressure value.</param>
        /// <returns>True if success, false otherwise.</returns>
        private Exception PumpValve(int valveNumber, int pressureValue,IProgress<GenericSimulationProgress> progress = null)
        {
            if (bSimulatorIsInUse)
                return new SimulatorExceptions.DeviceBusyException() { DeviceId = this.DeviceId };

            this.bIsPreparedForExperiment = false;
            this.IsPreparedForPressureMapping = false;

            if (this.isDebugMode)
                Logger.AddLogEntry(Logger.LogEntryCategories.Debug, "Simulator " + this.DeviceId + ": DEBUG PumpValve() started with: " + valveNumber + " and " + pressureValue + "mb");

            if ((pressureValue > 199) || (pressureValue < 5))
                return new SimulatorExceptions.InvalidPressureException() { DeviceId = this.DeviceId, PressureValue = (short)pressureValue };

            bSimulatorIsInUse = true;
            int loopCnt = 0;

            try
            {
                myStatusDisplayInstance.SetTextToStatus(SimulatorStati.Inflate);

                DateTime timeout = valveNumber != 255 ? DateTime.Now.AddSeconds(SINGLE_OPERATION_TIMEOUT_SECS) : DateTime.Now.AddSeconds(SINGLE_OPERATION_TIMEOUT_SECS * 7);

                if (valveNumber != 255 && this.isDebugMode) //shorten pumping cycles in debug mode
                    myDevice.InflateEvacuateStart((byte)valveNumber, (byte)100, 2000, true);
                else
                {
                    Logger.AddLogEntry(Logger.LogEntryCategories.Debug, "Simulator " + this.DeviceId + ": Pumping valve #" + valveNumber + " to " + pressureValue + "mb");
                    myDevice.SetPressureStart((byte)valveNumber, pressureValue);
                }

                int currentPressure = 0; //pressure in millibar

                bool bStopPump = false; //boolean to check if the cycle is finished

                while (!bStopPump)
                {
                    if (!myDevice.Com.Connected)
                        return new SimulatorExceptions.DeviceNotConnectedException() { DeviceId = this.DeviceId };

                    if (valveNumber != 255 && isDebugMode) //shorten pumping cycles in debug mode
                        bStopPump = myDevice.InflateEvacuateStatus();
                    else
                    {
                        bStopPump = myDevice.SetPressureStatus(out currentPressure);

                        if (bStopPump)
                            Logger.AddLogEntry(Logger.LogEntryCategories.Debug, "Simulator " + this.DeviceId + ": Pumping finished with pressure: " + currentPressure + "mb");
                    }

                    if (progress != null && loopCnt % 5 == 0) // report progress
                    {
                        double dProgress = pressureValue == 10 ? (currentPressure * 10) : 0; //more or less fake progress
                        myStatusDisplayInstance.SetProgressBarValue((int)dProgress);
                        myStatusDisplayInstance.ForceUpdateGUI();
                    }

                    if (DateTime.Now >= timeout) //when the timeout has expired, something went wrong and the cycle is aborted
                    {
                        Logger.AddLogEntry(Logger.LogEntryCategories.Debug, "Simulator " + this.DeviceId + ": aborting PumpValve() (timeout)...");
                        myDevice.Com.AsyncAblaufCancel(); //stops the current cycle and throws error 10
                        bStopPump = true;
                        OnTimeout(true);
                    }

                    if (isDebugMode) //only pump 3secs in debug mode
                        if (DateTime.Now > timeout.AddSeconds(SINGLE_OPERATION_TIMEOUT_SECS - (SINGLE_OPERATION_TIMEOUT_SECS - 3)))
                        {
                            myDevice.Com.AsyncAblaufCancel(); //stops the current cycle and throws error 10
                            bStopPump = true;
                        }

                    loopCnt++;
                    System.Threading.Thread.Sleep(100);
                }

                LastUsageTime = DateTime.Now;

                if (Math.Abs(currentPressure - pressureValue) > 3)
                    Logger.AddLogEntry(Logger.LogEntryCategories.Warning, DeviceId + ": PumpValve() did not reach the target pressure of " + pressureValue + "mb. The current pressure is: " + currentPressure + "mb", null, "CSerialSimulator");
            }
            catch (Exception ex)
            {
                Logger.AddLogEntry(Logger.LogEntryCategories.Error, "Exception in CSerialSimulator.PumpValve() while pumping: " + valveNumber + "-->" + pressureValue + "mB", ex);
                return new SimulatorExceptions.UnhandledException() { DeviceId = this.DeviceId };
            }
            finally //whatever happens, reset the Simulator usage
            {
                bSimulatorIsInUse = false;
            }

            return null;
        }

        /// <summary>
        /// Pumps an arbitrary number of valves to the received value, ranges from 10mB to pressureMaximum (about 180mB).
        /// It is recommended to use the PumpValve() method in order to pump all 12 valves at once.
        /// </summary>
        /// <param name="valveNumbers">The valves (1-12) to be pumped.</param>
        /// <param name="pressureValue">The target pressure value.</param>
        /// <returns>True in case of success, false otherwise.</returns>
        private Exception PumpValves(List<int> valveNumbers, int pressureValue)
        {
            if (bSimulatorIsInUse)
                return new Exception("PumpValves(): Simulator " + DeviceId + " is already in use.");

            if (valveNumbers == null || valveNumbers.Count > 12)
                return new ArgumentOutOfRangeException("valveNumbers", valveNumbers == null ? "Parameter is NULL" : "Number of valves specified is > 12 (" + valveNumbers.Count + ").");

            if (pressureValue > 190 || (pressureValue < 1))
                return new InvalidPressureException() { Channel = 254, PressureValue = (short)pressureValue };

            this.IsPreparedForPressureMapping = false;
            bSimulatorIsInUse = true;

            try
            {
                DateTime timeout = DateTime.Now.AddSeconds(SINGLE_OPERATION_TIMEOUT_SECS * 7); //pumping from red to white stamps (x12) can take really long

                Logger.AddLogEntry(Logger.LogEntryCategories.Debug, "Simulator " + this.DeviceId + ": Pumping " + valveNumbers.Count + " valves to " + pressureValue + "mb", null, "CSerialSimulator");
                List<byte> channels = valveNumbers.Select(v => (byte)v).ToList();
                myDevice.SetPressureStart(channels, pressureValue);

                if (!myDevice.Com.AsyncKommStarted)   //prüfen ob das asynchrone Kommando gestartet wurde
                    return new Exception("Device error while sending the operation command."); //wenn es nicht gestartet wurde muss das Timeout nicht abgewartet werden (Fehler werden über das Event ausgelesen)

                int currentPressure = 0; //pressure in millibar
                bool bStopPump = false; //boolean to check if the cycle has finished

                while (!bStopPump)
                {
                    if (!myDevice.Com.Connected)
                        return new DeviceNotConnectedException() { DeviceId = DeviceId };

                    bStopPump = myDevice.SetPressureStatus(out currentPressure);

                    if (bStopPump)
                        Logger.AddLogEntry(Logger.LogEntryCategories.Debug, "Simulator " + this.DeviceId + ": Pumping valves finished with pressure: " + currentPressure + "mb", null, "CSerialSimulator");

                    if (DateTime.Now >= timeout) //when the timeout has expired, something went wrong and the cycle is aborted
                    {
                        myDevice.Com.AsyncAblaufCancel(); //stops the current cycle and throws error 10
                        bStopPump = true;
                        return new TimeoutException("PumpValve(): operation ran into a timeout after " + (DateTime.Now - timeout).TotalSeconds + " seconds.");
                    }

                    Thread.Sleep(100);
                }

                LastUsageTime = DateTime.Now;

                if (Math.Abs(currentPressure - pressureValue) > 3)
                    Logger.AddLogEntry(Logger.LogEntryCategories.Warning, DeviceId + ": PumpValve() did not reach the target pressure of " + pressureValue + "mb. The current pressure is: " + currentPressure + "mb", null, "CSerialSimulator");
            }
            catch (Exception ex)
            {
                return ex;
            }
            finally //whatever happens, reset the Simulator usage
            {
                bSimulatorIsInUse = false;
            }

            return null;
        }

        /// <summary>
        /// Stops any running operation by sending an 'abort'-command.
        /// </summary>
        /// <returns></returns>
        public override Exception AbortCurrentPumpingOperation()
        {
            if (!bSimulatorIsInUse)
                return null;

            if (myDevice == null)
                return null;

            try
            {
                if (!myDevice.Com.Connected)
                    return new DeviceNotConnectedException() { DeviceId = DeviceId };

                myDevice.Com.AsyncAblaufCancel(); //stops the current cycle and throws error 10
                return null;
            }
            catch(Exception ex)
            {
                return ex;
            }
            finally
            {
                bSimulatorIsInUse = false;
            }
        }

        /// <summary>
        /// Used to receive the current pressure from a specific valve.
        /// </summary>
        /// <param name="int_valveNumber">The number of the valve, ranging from 1 to 12.</param>
        /// <returns>The pressure in mB.</returns>
        public override int GetCurrentPressureFromValve(int int_valveNumber)
        {
            if (bSimulatorIsInUse)
                return -1;

            if (!myDevice.Com.Connected) //throw a no connection error
            {
                Simulator_NoConnectionError(this, new Exception("GetCurrentPressureFromValve(): device not connected."));
                return -1;
            }

            bSimulatorIsInUse = true;
            bool fertig = false; //bool to determine if the measurement has been finished yet

            int druck = 0; //used to hold the pressure that is returned from time to time

            try
            {
                myDevice.GetPressureStart((byte)int_valveNumber); //begin pressure measurement

                DateTime timeout = DateTime.Now.AddSeconds(5); //define a timeout of 5 secs

                while (!fertig)
                {
                    fertig = myDevice.GetPressureStatus(out druck); //see if the measurement is done and retrieve the current pressure

                    if (DateTime.Now >= timeout) //when 3 seconds have expired and the measurement is not done yet, something went wrong and everything is stopped
                    {
                        myDevice.Com.AsyncAblaufCancel(); //stop the current command
                        OnTimeout(true);
                        break;
                    }
                }

                LastUsageTime = DateTime.Now;
            }
            finally //whatever happens, reset the Simulator usage
            {
                bSimulatorIsInUse = false;
            }

            if (fertig)
                return druck;
            else
            {
                myDevice.Com.AsyncAblaufCancel(); //throws error #10
                return -1;
            }
        }

        /// <summary>
        /// Evacuates a valve under a value of 0mB by evacuating for a specified duration.
        /// Note that if a single valve is evacuated, the device will first apply a positive base pressure in order to ensure that all negative values pumped in have the same initial base value.
        /// </summary>
        /// <param name="valveNumber">The valve that has to be evacuated.</param>
        /// <param name="time">The time how long the pump has to evacuate (in milliseconds).</param>
        /// <param name="basePressureMb">The pressure value that is applied before the evacuation starts. Must be >= 10. Ignored if all valves are pumped at once (valveNumber == 255). </param>
        /// <returns>Null if successful or an exception.</returns>
        public override Exception EvacuateValveByTime(int valveNumber, int time, int basePressureMb, IProgress<GenericSimulationProgress> progress = null)
        {
            if (bSimulatorIsInUse)
                return new SimulatorExceptions.DeviceBusyException() { DeviceId = this.DeviceId };

            if (time < 10 || (time > 10000 && valveNumber <= 12) || (valveNumber == 255 && time > 40000))
                return new SimulatorExceptions.InvalidEvacuationTimeException() { DeviceId = this.DeviceId, Time = time, Channel = (short)valveNumber };

            if (valveNumber < 255 && (basePressureMb < 10 || basePressureMb > 199))
                return new SimulatorExceptions.InvalidEvacuationBaseValueException() { DeviceId = this.DeviceId, BaseValue = basePressureMb, Channel = (short)valveNumber };

            this.bIsPreparedForExperiment = false;
            this.IsPreparedForPressureMapping = false;
            DateTime timeout = DateTime.Now.AddSeconds(SINGLE_OPERATION_TIMEOUT_SECS);
            DateTime start = DateTime.Now;

            if (valveNumber < 255) //not all valves are pumped at once
            {
                Exception exc = PumpValve(valveNumber, basePressureMb, progress); //apply 10mB before evacuating

                if (exc != null)
                    return new Exception("Exception while pumping to neutral starting value before evacuating by " + time + "ms.", exc);
            }

            try
            {
                this.bSimulatorIsInUse = true;

                myDevice.InflateEvacuateStart((byte)valveNumber, (byte)100, time, false);

                bool bStopPump = false; //boolean to check if the cycle is finished

                while (!bStopPump)
                {
                    if (!myDevice.Com.Connected)
                        return new SimulatorExceptions.DeviceNotConnectedException() { DeviceId = this.DeviceId };

                    bStopPump = myDevice.InflateEvacuateStatus();

                    if (progress != null) //report progress
                    {
                        TimeSpan duration = (DateTime.Now - start);
                        double dProgress = (100f / (time / 1000f)) * (duration.TotalSeconds + 1); //+1 to counteract the "trailing" of the progress bar

                        if (dProgress > 100)
                        {
                            Logger.AddLogEntry(Logger.LogEntryCategories.Warning, "Warning in CSerialSimulator.EvacuateValveByTime(): the calculated progress is > 100 --> " + dProgress + ". time=" + time + ", TotalSeconds=" + (DateTime.Now - start).TotalSeconds);
                            dProgress = 100;
                        }

                        myStatusDisplayInstance.SetProgressBarValue((int)dProgress);
                        myStatusDisplayInstance.ForceUpdateGUI();
                    }

                    if (DateTime.Now >= timeout) //when the timeout has expired, something went wrong and the cycle is aborted
                    {
                        myDevice.Com.AsyncAblaufCancel(); //stops the current cycle and throws error 10
                        bStopPump = true;
                        OnTimeout(true);
                        return new SimulatorExceptions.OperationTimeoutException() { DeviceId = this.DeviceId, OperationName = "EvacuateValveByTime" };
                    }

                    Thread.Sleep(100);
                }

                LastUsageTime = DateTime.Now;
            }
            catch (Exception ex)
            {
                OnErrorOccured(new Exception("Fehler beim Absaugen", ex));
                return new SimulatorExceptions.UnhandledException() { DeviceId = this.DeviceId };
            }
            finally //whatever happens, reset the Simulator usage
            {
                bSimulatorIsInUse = false;
            }

            return null;
        }

        /// <summary>
        /// Evacuates all specified valves under a value of 0mB by draining air for a specified duration.
        /// </summary>
        /// <param name="valveNumbers">The valves that have to be drained (1-12).</param>
        /// <param name="evacuationTime">The time in seconds how long the pump has to evacuate for ONE channel (must be negative, allowed range is 0 to -7).</param>
        public override Exception EvacuateValvesByTime(List<int> valveNumbers, int evacuationTime, IProgress<GenericSimulationProgress> progress = null)
        {
            if (bSimulatorIsInUse)
                return new Exception("EvacuateValvesByTime(): Simulator " + DeviceId + " is already in use.");

            if (valveNumbers == null || valveNumbers.Count > 12)
                return new ArgumentOutOfRangeException("valveNumbers", valveNumbers == null ? "Parameter is NULL" : "Number of valves specified is > 12 (" + valveNumbers.Count + ").");

            if (evacuationTime > 0 || evacuationTime < -7)
                return new InvalidEvacuationTimeException() { Channel = 254, Time = evacuationTime };

            this.IsPreparedForPressureMapping = false;
            this.bSimulatorIsInUse = true;
            DateTime start = DateTime.Now;
            DateTime timeout = DateTime.Now.AddSeconds(SINGLE_OPERATION_TIMEOUT_SECS); //since we are only evacuating for 1 to 7 seconds, this should be more than enough

            try
            {
                long time = Math.Abs(evacuationTime * 1000);
                time = (long)(time * (double)valveNumbers.Distinct().Count() * 0.9); //multiply the evac time for one channel by the number of channels and apply a factor of 0.9 since evacuating multiple channels at once is slighty faster
               
                if (time < 100) //don't pump values < 100ms
                    return null;

                List<byte> channels = valveNumbers.Distinct().Select(v => (byte)v).ToList();
                myDevice.InflateEvacuateStart(channels, (byte)100, time, false);

                Task t = Task.Factory.StartNew(() =>
                {
                    bool bStopPump = false; //boolean to check if the cycle has finished

                    while (!bStopPump)
                    {
                        if (!myDevice.Com.Connected)
                            throw new DeviceNotConnectedException() { DeviceId = DeviceId };

                        bStopPump = myDevice.InflateEvacuateStatus();

                        if (progress != null) //report progress
                        {
                            TimeSpan duration = (DateTime.Now - start);
                            double dProgress = (100f / (time / 1000f)) * (duration.TotalSeconds + 1); //+1 to counteract the "trailing" of the progress bar

                            if (dProgress > 100)
                            {
                                Logger.AddLogEntry(Logger.LogEntryCategories.Warning, "The calculated progress is > 100 --> " + dProgress + ". time=" + time + ", TotalSeconds=" + (DateTime.Now - start).TotalSeconds, null, "CSerialSimulator");
                                dProgress = 100;
                            }

                            myStatusDisplayInstance.SetProgressBarValue((int)dProgress);
                            myStatusDisplayInstance.ForceUpdateGUI();
                        }

                        if (DateTime.Now >= timeout) //when the timeout has expired, something went wrong and the cycle is aborted
                        {
                            myDevice.Com.AsyncAblaufCancel(); //stops the current cycle and throws error 10
                            bStopPump = true;
                            throw new TimeoutException("EvacuateValvesByTime(): operation ran into a timeout after " + (DateTime.Now - timeout).TotalSeconds + " seconds.");
                        }

                        Thread.Sleep(100);
                    }
                });

                bool success = t.Wait(SINGLE_OPERATION_TIMEOUT_SECS * 1000);
                LastUsageTime = DateTime.Now;

                if (!success)
                    throw new TimeoutException("EvacuateValvesByTime(): task was aborted after " + (DateTime.Now - start).TotalSeconds + " seconds. Timeout period: " + (timeout - start ).TotalSeconds + " sec.");
            }
            catch (Exception ex)
            {
                return ex;
            }
            finally //whatever happens, reset the Simulator usage
            {
                bSimulatorIsInUse = false;
            }

            return null;
        }

        /// <summary>
        /// Pumps all valves to either "10mB - 3 seconds" or "10mb" at once. This procedure roughly matches the original procedure of "pump to 30mB then evacuate for 5 seconds".
        /// </summary>
        public override Exception PrepareSimulatorForPressureMapping(CalibrationTypes type, IProgress<GenericSimulationProgress> progress = null)
        {
            if (bSimulatorIsInUse)
                return new SimulatorExceptions.DeviceBusyException() { DeviceId = this.DeviceId };

            StatusDisplay.SetProgressBarValue(0);
            StatusDisplay.SetTextToStatus(SimulatorInterfaces.SimulatorStati.Inflate);
            Application.DoEvents();

            myPressureLevels = myPressureLevels.Select(v => v = 0).ToArray(); //invalidate the pressure levels
            Exception ex = null;
            this.bIsCurrentlyPreparingForAPressureMapping = true;

            if (type == CalibrationTypes.Liegesimulator)
            {
                //pump all valves to 10mB
                ex = PumpValve(255, 10, progress);

                StatusDisplay.SetTextToStatus(SimulatorInterfaces.SimulatorStati.Evacuate);

                if (!isDebugMode && ex == null)
                    ex = EvacuateValveByTime(255, 18000, 0, progress); //we dont need to evacuate for 12*3 seconds here, as evacuating all tubes at once is a lot faster
            }
            else if (type == CalibrationTypes.ErgonometerNL || type == CalibrationTypes.Liegesimulator2_0)
            {
                Stopwatch sw = new Stopwatch();
                sw.Start();

                //pump all valves to 10mB
                int baseValue = Liegesimulator2_0ProfileElements.EVACUATE_PRESSURE_BASE_VALUE; //the base value for evacuation is also the calibration value (10mb)
                ex = PumpValve(255, baseValue, progress);

                sw.Stop();

                if (ex == null && sw.Elapsed.TotalSeconds < 6) //start another cycle to make sure the target pressure is actually applied
                {
                    Logger.AddLogEntry(Logger.LogEntryCategories.Debug, "PrepareSimulatorForPressureMapping() took less than 6 seconds. Starting another cycle...", null, "CSerialSimulator");
                    ex = PumpValve(255, 10, progress);
                }
            }

            iCountMeasurements = 0;
            bSimulatorIsInUse = this.bIsCurrentlyPreparingForAPressureMapping = false;

            if (ex != null)
            {
                StatusDisplay.SetTextToStatus(SimulatorInterfaces.SimulatorStati.NotReady);
                StatusDisplay.SetProgressBarValue(0);
                this.IsPreparedForPressureMapping = false;
                return ex;
            }
            else
            {
                StatusDisplay.SetTextToStatus(SimulatorInterfaces.SimulatorStati.PreparedForMapping);
                StatusDisplay.SetProgressBarValue(100);
                this.IsPreparedForPressureMapping = true;
                this.OnPreparedForPressureMappingOccured(this);
            }

            return null;
        }

        /// <summary>
        /// This method is invoked whenever a timeout occurs during communication with the hardware device.
        /// This happens quite often when communicating via bluetooth because many bluetooth adapters don't work properly with the devices.
        /// </summary>
        /// <param name="isUserTimeout">If this is set to true, the timeout was detected by user code.</param>
        protected override void OnTimeout(bool isUserTimeout)
        {
            OnErrorOccured(new TimeoutException("A timeout has occured because the connection to the device was interrupted. Please make sure that everything is connected properly and restart the application."));
        }

        private void Com_DeviceError(object sender, EventArgs e)
        {
            string text = "Folgende Fehler sind für Simulator " + this.deviceID + " aufgetreten: ";

            int error = myDevice.Com.GetErrorDevice(); //get the latest error from the queue

            while (error != 0) //while there are errors available in the queue
            {
                if (error == 10 && isDebugMode)
                    return;

                text += Environment.NewLine;

                switch (error)
                {
                    case 1:
                        text += "1: Ein unbekanntes Kommando wurde an den Simulator gesendet!";
                        break;
                    case 2:
                        text += "2: Bei der Kommunikation trat ein Fehler auf: falsche Parameteranzahl";
                        break;
                    case 3:
                        text += "3: Bei der Kommunikation trat ein Fehler auf: Fehler bei Parameter 1";
                        break;
                    case 4:
                        text += "4: Bei der Kommunikation trat ein Fehler auf: Fehler bei Parameter 2";
                        break;
                    case 5:
                        text += "5: Fehler im Flashspeicher des Simulators!";
                        break;
                    case 6:
                        text += "6: Watchdog Reset ist aufgetreten!";
                        break;
                    case 7:
                        text += "7: I²C Ack.";
                        break;
                    case 8:
                        text += "8: Angegebener Kanal ist außerhalb des Bereichs!";
                        break;
                    case 9:
                        if (!bSuppressMaxPressureError)
                            text += "9: " + "Maximaldruck überschritten!";
                        else
                            continue; //suppress this error

                        break;
                    case 10: //AsyncAblaufCancel()
                        text += "10: Ein Fehler ist aufgetreten und der Vorgang wurde abgebrochen!";
                        break;
                    default:
                        text += "Fehlernummer: " + error.ToString();
                        break;
                }

                error = myDevice.Com.GetErrorDevice(); //get the next error on the queue
            }

            if (!string.IsNullOrEmpty(text))
                this.DeviceError(this, new Exception(text));
        }

        private void Com_DisconnectError(object sender, EventArgs e)
        {
            if (!disconnectEventReceived)
            {
                disconnectEventReceived = true;
                this?.OnDisconnectOccured(this);
            }
        }

        private void Com_ComLibError(object sender, EventArgs ea)
        {
            ComLib.ErrorEventArgs e = ea as ComLib.ErrorEventArgs;

            if (e.ErrorCode > 8)
                Simulator_NoConnectionError(this, new Exception("Com_ComLibError: " + e.ErrorCode + "-->" + e.Kommando));

            //suppress any other errors
            Logger.AddLogEntry(Logger.LogEntryCategories.Error, "Suppressed ComLibError: " + e.ErrorCode + " [" + e.Kommando + "]", null, "CSerialSimulator");
        }

        private void Simulator_PressureDeviceLibError(object sender, EventArgs e)
        {
            string text = "Simulator_PressureDeviceLibError";
            if (e is DeviceEventArgs)
                text += ": " + (e as DeviceEventArgs).ErrorCode + "-->" + (e as DeviceEventArgs).Functionname;

            Simulator_NoConnectionError(this, new Exception(text));
        }

        private void MyDevice_NoConnectionError(object sender, EventArgs e)
        {
            Simulator_NoConnectionError(this, new Exception("NoConnectionError(): " + e.ToString()));
        }

        protected override void DeviceError(CBaseSimulator sender, Exception exception)
        {
            //  if (CAppConfig.ShowAllErrorMessages)
            StatusDisplay.SetTextToStatus(SimulatorInterfaces.SimulatorStati.Ready);
            OnErrorOccured(new Exception("Error for device with id: " + this.DeviceId, exception));
        }

        /// <summary>
        /// If the connection to the hardware device cannot be established or if the connection is interrupted during communication, this method is invoked.
        /// </summary>
        protected override void Simulator_NoConnectionError(CBaseSimulator sender, Exception exception)
        {
            StatusDisplay.SetTextToStatus(SimulatorInterfaces.SimulatorStati.NotConnected);

            if (!this.myDevice.Com.Connected && bWasConnected)
            {
                base.OnErrorOccured(exception);
                bSimulatorIsInUse = false;
            }
        }

        /// <summary>
        /// Queries the device's serial number.
        /// </summary>
        /// <returns></returns>
        public override string GetDeviceId()
        {
            if (bSimulatorIsInUse) //sending multiple commands to the device at once must be avoided
                return this.DeviceId;

            try
            {
                var tokenSource = new CancellationTokenSource(); //used to abort tasks
                var token = tokenSource.Token;

                var task = Task<string>.Factory.StartNew(() => //start a new task that gets the device ID to check the connection 
                {
                    if (!this.bSimulatorIsInUse)
                        return myDevice.GetSerialNbr();

                    return null;
                }, token);

                if (task.Wait(3000)) //true if task finishes within the specified time
                {
                    string id = "";

                    if (!task.IsCompleted)
                    {
                        tokenSource.Cancel();
                        Thread.Sleep(10);
                    }
                    else
                    {
                        id = task.Result;
                        LastUsageTime = DateTime.Now;
                    }

                    if (!task.IsCanceled && this.myDevice.Com.Connected && id != "") //usually, task.IsCanceled is false and Com.Connected is still true here when the connection has been lost
                        return id;
                }

                return null;
            }
            catch (Exception ex)
            {
                Logger.AddLogEntry(Logger.LogEntryCategories.Error, "Exception while querying device id for device: " + this.DeviceId, ex, "CSerialSimulator");
                return null;
            }
        }

        public override string GetPortInformation()
        {
            return myDevice.Com.SerialPort.PortName;
        }

        public override Exception Disconnect()
        {
            try
            {
                myDevice.Com.Disconnect(true);
                return null;
            }
            catch (Exception ex)
            {
                return ex;
            }
        }

        public override string GetFirmwareVersion()
        {
            try
            {
                return myDevice.Com.GetFirmwareVersion();
            }
            catch (Exception) { }

            return null;
        }
    }
}
