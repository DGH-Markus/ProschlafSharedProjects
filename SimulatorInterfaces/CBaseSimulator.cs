using SimulatorInterfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace SimulatorInterfaces
{
    /// <summary>
    /// The base class for all simulator classes. Defines all necessary methods a simulation device has to implement.
    /// 
    /// Currently, the following classes derive from this base class:
    /// - CSerialSimulator (controls USB and bluettooth devices)
    /// - CNetworkSimulator (controls LAN/wifi devices)
    /// - CngMattSimulator (controls the spanish ngMatt devices)
    /// </summary>
    public abstract class CBaseSimulator
    {
        #region Variables
        public bool bSimulatorIsInUse = false;

        public bool bWriteDeviceLogFile = false;

        protected bool bIsPreparedForExperiment = false; //used to determine if the Simulator was just pumped to zero pressure

        protected bool bIsPreparedForMeasurement = false; //used to determine if the simulator is calibrated for a pressure mapping

        protected bool bIsCurrentlyPreparingForAPressureMapping = false; //used to determine if the device is currently being calibrated

        protected bool bSuppressMaxPressureError = true; //for many reasons, the hardware devices exceed the pressure limit and throw an error. This boolean can be set to true in order to suppress that error.

        protected int iCountMeasurements = 9; //8 measurements can be performed in a row without the need to re-initialize the simulator (provided that no other pumping actions were performed)

        protected IStatusDisplayFunctions myStatusDisplayInstance = null;

        protected string deviceID = ""; //the device id for the current device (usually the serial number)

        protected string[] myAssignments = null;

        protected const int SINGLE_OPERATION_TIMEOUT_SECS = 70; //the timeout for a pumping procedure

        protected int[] myPressureLevels = new int[12];

        protected CBaseSimulator.SimulatorSetupTypes setupType = SimulatorSetupTypes.OneDevice;

        protected bool firmwareUpdateRequired = false;
        #endregion

        #region Props

        /// <summary>
        /// Determines whether the simulator is ready for a pressure mapping (pumped to initial value) or not.
        /// </summary>
        public bool IsPreparedForPressureMapping
        {
            get
            {
                return bIsPreparedForMeasurement;
            }
            set
            {
                bIsPreparedForMeasurement = value;

                if (value == false)
                {
                    iCountMeasurements = iMeasurementBorder + 1; //reset the counter
                }
            }
        }
        /// <summary>
        /// Determines whether the device is currently preparing for a pressure mapping or not.
        /// </summary>
        public bool IsCurrentlyPreparingForAPressureMapping
        {
            get
            {
                return bIsCurrentlyPreparingForAPressureMapping;
            }
            internal set
            {
                bIsCurrentlyPreparingForAPressureMapping = value;
            }
        }

        /// <summary>
        /// Checks whether the simulator is prepared for a pressure experiment or not.
        /// </summary>
        /// <returns>True if prepared, false otherwise.</returns>
        public bool IsPreparedForExperiment
        {
            get
            {
                return bIsPreparedForExperiment;
            }
            internal set
            {
                bIsPreparedForExperiment = value;
            }
        }

        /// <summary>
        /// Accessor to the pressure mapping counter.
        /// </summary>
        public int MeasurementCounter
        {
            get
            {
                return iCountMeasurements;
            }
            set
            {
                iCountMeasurements = value;
            }
        }

        /// <summary>
        /// Checks whether the simulator is being used at the moment or not.
        /// </summary>
        /// <returns>True if the simulator is in use, false if the simulator is in idle mode.</returns>
        public bool IsSimulatorInUse
        {
            get
            {
                return bSimulatorIsInUse;
            }
        }

        /// <summary>
        /// The serial number of the hardware device.
        /// </summary>
        public string DeviceId
        {
            get { return deviceID; }
        }

        /// <summary>
        /// If the device is running on an old firmware version, this flag is set to true.
        /// It is important to have at least firmware version "01.06.01" installed because otherwise the pumping of multiple channels at once is not possible (e.g. PumpValves() ).
        /// </summary>
        public bool IsFirmwareUpdateRequired
        {
            get { return firmwareUpdateRequired; }
        }

        #endregion

        #region Consts

        /// <summary>
        /// The threshold for consecutive pressure mappings before a re-calibration has to be initiated.
        /// </summary>
        public const int iMeasurementBorder = 8;

        /// <summary>
        /// The maximum pressure a device may reach on a valve before shutting down.
        /// </summary>
        protected const int PRESSURE_MAXIMUM_ABSOLUTE = 199;

        /// <summary>
        /// The maximum pressure a device can be issued to apply.
        /// </summary>
        protected const int PRESSURE_MAXIMUM_PARAMETER = 189;
        #endregion

        #region Enums
        public enum SimulatorSetupTypes { OneDevice, MultipleDevices }

        /// <summary>
        /// Defines the types of pressure mapping calibrations that are possible.
        /// This enum is the same as in the NgMattApiWrapper.ApiFunctions class and should be kept synchronous to avoid confusion.
        /// <para>Liegesimulator: The pressure mapping is based on a calibration with negative values (10mB - 3 seconds per tube).</para>
        /// <para>ErgonometerNL: The pressure mapping is based on a calibration with positive pressure values (10mB per tube).</para>
        /// <para>Liegesimulator2_0: The pressure mapping is based on a calibration with positive pressure values (10mB per tube).</para>
        /// </summary>
        public enum CalibrationTypes { Liegesimulator, ErgonometerNL, Liegesimulator2_0 }
        #endregion

        #region Events

        /// <summary>
        /// Fired when a simulation device is finished preparing for a pressure mapping.
        /// </summary>
        public event EventHandler<CBaseSimulator> PreparedForPressureMappingEvent;

        protected virtual void OnPreparedForPressureMappingOccured(CBaseSimulator s)
        {
            EventHandler<CBaseSimulator> handler = PreparedForPressureMappingEvent;
            handler?.Invoke(this, s);
        }

        /// <summary>
        /// This event is raised whenever a simulator-related exception occurs.
        /// </summary>
        public event EventHandler<Exception> ErrorOccuredEvent;

        protected virtual void OnErrorOccured(Exception e)
        {
            EventHandler<Exception> handler = ErrorOccuredEvent;
            handler?.Invoke(this, e);
        }

        /// <summary>
        /// This event is raised once when a a connection to the device is lost.
        /// </summary>
        public event EventHandler<CBaseSimulator> DisconnectOccuredEvent;

        protected virtual void OnDisconnectOccured(CBaseSimulator device)
        {
            DisconnectOccuredEvent?.Invoke(this, device ?? this);
        }

        public event EventHandler<LogEventClass> LogEvent;

        protected virtual void OnLogOccured(LogEventClass l)
        {
            EventHandler<LogEventClass> handler = LogEvent;
            handler?.Invoke(this, l);
        }
        #endregion

        protected CBaseSimulator() { }

        /// <summary>
        /// Checks if the simulator has the correct maximum pressure values set and adjusts them if needed.
        /// </summary>
        protected abstract void AdjustPressureMaximumValues();

        /// <summary>
        /// Depending on application and device type, this method calibrates the device for a pressure mapping.
        /// </summary>
        /// <returns>True if successful, false otherwise.</returns>
        public abstract Exception PrepareSimulatorForPressureMapping(CalibrationTypes type, IProgress<GenericSimulationProgress> progress = null); 

        /// <summary>
        /// Applies the specified pressure value (in mB) to the specified valve.
        /// </summary>
        /// <param name="valveNumber">The valve to be used during the pumping procedure (1 - 12).</param>
        /// <param name="pressureValue">The pressure level to be applied (usually 5 - 199mB).</param>
        /// <returns>True if the procedure succeeded; false otherwise.</returns>
        public abstract Exception ApplySpecificPressure(int valveNumber, int pressureValue, IProgress<GenericSimulationProgress> progress = null);

        /// <summary>
        /// Applies the specified pressure value (in mB) to the specified valves at once.
        /// </summary>
        /// <param name="valveNumbers">The numbers of all valves to be pumped (1-12).</param>
        /// <param name="pressureValue">The pressure level to be applied (usually 5 - 199mB).</param>
        /// <returns></returns>
        public abstract Exception ApplySpecificPressureToValves(List<int> valveNumbers, int pressureValue, IProgress<GenericSimulationProgress> progress = null);

        /// <summary>
        /// Pumps to a base pressure value and then drains air from the specified tube for the specified amount of time.
        /// </summary>
        /// <param name="valveNumber">The valve to be used during the pumping procedure (1 - 12).</param>
        /// <param name="time">The evacuation time in ms (min = 10, max = 10000 or 40000 when all valves are evacuated at once.).</param>
        /// <param name="basePressureMb">This pressure value is applied before starting the evacuation.</param>
        /// <param name="reportProgress">If set to true, the method will update the corresponding StatusDisplay instance with the current progress.</param>
        /// <returns>True if the procedure succeeded; false otherwise.</returns>
        public abstract Exception EvacuateValveByTime(int valveNumber, int time, int basePressureMb, IProgress<GenericSimulationProgress> progress = null);

        /// <summary>
        /// Evacuates all specified valves under a value of 0mB by draining air for a specified duration.
        /// </summary>
        /// <param name="valveNumbers">The valves that have to be drained (1-12).</param>
        /// <param name="evacuationTime">The time in seconds how long the pump has to evacuate for ONE channel (must be negative, allowed range is 0 to -7).</param>
        public abstract Exception EvacuateValvesByTime(List<int> valveNumbers, int evacuationTime, IProgress<GenericSimulationProgress> progress = null);

        /// <summary>
        /// Used to receive the current pressure from a specific valve.
        /// </summary>
        /// <param name="valveNumber">The number of the valve, ranging from 1 to 12.</param>
        /// <returns>The pressure in mB.</returns>
        public abstract int GetCurrentPressureFromValve(int valveNumber);

        /// <summary>
        /// Attempts to cancel the current pumping procedure.
        /// </summary>
        /// <returns></returns>
        public abstract Exception AbortCurrentPumpingOperation();

        /// <summary>
        /// Checks the connection status to the hardware device.
        /// </summary>
        /// <returns>True if connected, false otherwise.</returns>
        public abstract bool IsConnected { get; }

        /// <summary>
        /// Queries the hardware device id (serial number) of the connected simulator. 
        /// Can also be used to "ping" the device to see if its still connected.
        /// </summary>
        /// <returns>The device id or null.</returns>
        public abstract string GetDeviceId();

        /// <summary>
        /// Gets the settings for the maximum pressure thresholds for this device.
        /// </summary>
        /// <param name="pressureParameter">The variable that will hold the maximum pressure value that can be sent to the device.</param>
        /// <param name="pressureAbsolute">The variable that will hold the maximum pressure that must not be exceeded during any pumping procedure.</param>
        public abstract void GetPressureMaxima(out int pressureParameter, out int pressureAbsolute);

        /// <summary>
        /// Updates the settings for the maximum pressure thresholds for this device.
        /// </summary>
        /// <param name="pressureParameter">The new maximum pressure value that can be sent to the device.</param>
        /// <param name="pressureAbsolute">The new maximum pressure that must not be exceeded during any pumping procedure.</param>
        /// <returns>True if the values were sent successfully, false otherwise.</returns>
        public abstract bool SetPressureMaxima(int pressureParameter, int pressureAbsolute);

        /// <summary>
        /// Returns the serial port number for this device.
        /// </summary>
        /// <returns>The COM port for serial devices.</returns>
        public abstract string GetPortInformation();

        /// <summary>
        /// Gets the curren firmware version of the hardware.
        /// </summary>
        /// <returns></returns>
        public abstract string GetFirmwareVersion();

        /// <summary>
        /// Disconnects the device.
        /// </summary>
        public abstract Exception Disconnect();

        /// <summary>
        /// Accessor to the array that holds the current pressure profile. This is a legacy property that is only used in the Liegesimulator-software.
        /// Array range is from 0 to 11.
        /// </summary>
        public abstract int[] CurrentPressureLevels { get; }

        /// <summary>
        /// Accessor to the individual status display instance. Has to be overridden by every implementation class.
        /// </summary>
        [ObsoleteAttribute("Legacy property that is not supported for the new LS 2.0 devices. Should not be used in applications starting with LS 2.0.")]
        public abstract IStatusDisplayFunctions StatusDisplay { get; }

        /// <summary>
        /// Checks if a simulator is calibrated and ready for a pressure mapping.
        /// </summary>
        /// <returns>True if a pressure mapping is (still) possible; false if a calibration is required.</returns>
        public bool GetMeasurementCountStatus()
        {
            return iCountMeasurements < iMeasurementBorder;
        }

        #region Error handlers

        /// <summary>
        /// A generic timeout event that handles every possible timeout.
        /// </summary>
        protected abstract void OnTimeout(bool isUserTimeout);

        /// <summary>
        /// A generic error handler for any device-related errors.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected abstract void DeviceError(CBaseSimulator sender, Exception exception);

        /// <summary>
        /// If the connection to the hardware device cannot be established or if the connection is interrupted during communication, this method is invoked.
        /// </summary>
        protected abstract void Simulator_NoConnectionError(CBaseSimulator sender, Exception exception);

        #endregion
    }

    /// <summary>
    /// A helper class that represents any kind of percentual progress.
    /// </summary>
    public class PercentualProgress
    {
        /// <summary>
        /// Guaranteed to be in the range of 0 - 100.
        /// </summary>
        public int ProgressPercent { get; }
      
        private PercentualProgress() { }

        public PercentualProgress(int progressPercent)
        {
            ProgressPercent = progressPercent < 0 ? 0 : progressPercent > 100 ? 100 : progressPercent;
        }
    }

    /// <summary>
    /// A helper class that represents any kind of simulation progress; either percentual or absolute in [mb].
    /// </summary>
    public class GenericSimulationProgress
    {
        /// <summary>
        /// If set, guaranteed to be in the range of 0 - 100.
        /// </summary>
        public int? ProgressPercent { get; }

        /// <summary>
        /// An absolute progress that represents the current pressure value in millibar between 0 and 1000;
        /// </summary>
        public int? ProgressMillibar { get; }

        private GenericSimulationProgress() { }

        public GenericSimulationProgress(int? progressPercent, int? progressMillibar)
        {
            ProgressPercent = progressPercent < 0 ? 0 : progressPercent > 100 ? 100 : progressPercent;
            ProgressMillibar = progressMillibar < 0 ? 0 : progressMillibar > 1000 ? 1000 : progressMillibar;
        }
    }
}
