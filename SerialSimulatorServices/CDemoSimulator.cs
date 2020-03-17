using SimulatorInterfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace SerialSimulatorServices
{
    /// <summary>
    /// A simulator class used for demo purposes only.
    /// Does not control any simulator device. Just there to prevent hte software from crashing when using a simulator object. Mainly created for convenience reasons concerning the status display.
    /// </summary>
    public class CDemoSimulator : CBaseSimulator
    {
        #region Vars
        const int DEMO_ACTION_WAIT_TIME = 200;

        private static int deviceId = 123;

        #endregion

        #region Overrides
        protected override void AdjustPressureMaximumValues()
        {

        }

        protected override void OnTimeout(bool isUserTimeout)
        {

        }

        protected override void DeviceError(CBaseSimulator sender, Exception exception)
        {

        }

        protected override void Simulator_NoConnectionError(CBaseSimulator sender, Exception exception)
        {

        }

        public override int[] CurrentPressureLevels
        {
            get { return myPressureLevels; }
        }

        public override bool IsConnected
        {
            get { return true; } //demo simulator is always connected
        }

        public override Exception ApplySpecificPressure(int valveNumber, int pressureValue, IProgress<GenericSimulationProgress> progress = null)
        {
            Thread.Sleep(DEMO_ACTION_WAIT_TIME);
            return null;
        }

        public override Exception EvacuateValveByTime(int valveNumber, int time, int basePressureMb, IProgress<GenericSimulationProgress> progress = null)
        {
            Thread.Sleep(DEMO_ACTION_WAIT_TIME);
            return null;
        }

        public override int GetCurrentPressureFromValve(int valveNumber)
        {
            Thread.Sleep(DEMO_ACTION_WAIT_TIME);
            return new Random().Next(1, 30);
        }

        public override string GetDeviceId()
        {
            return this.deviceID;
        }

        public override void GetPressureMaxima(out int pressureParameter, out int pressureAbsolute)
        {
            pressureAbsolute = 199;
            pressureParameter = 180;
        }

        public override bool SetPressureMaxima(int pressureParameter, int pressureAbsolute)
        {
            return true;
        }

        public override string GetPortInformation()
        {
            return "Demo";
        }

        public override Exception PrepareSimulatorForPressureMapping(CalibrationTypes type, IProgress<GenericSimulationProgress> progress = null)
        {
            throw new NotImplementedException();
        }

        public override Exception Disconnect()
        {
            return null;
        }

        public override Exception ApplySpecificPressureToValves(List<int> valveNumbers, int pressureValue, IProgress<GenericSimulationProgress> progress = null)
        {
            throw new NotImplementedException();
        }

        public override Exception EvacuateValvesByTime(List<int> valveNumbers, int evacuationTime, IProgress<GenericSimulationProgress> progress = null)
        {
            throw new NotImplementedException();
        }

        [Obsolete]
        public override IStatusDisplayFunctions StatusDisplay
        {
            get
            {
                if (myStatusDisplayInstance == null)
                    myStatusDisplayInstance = SimulatorInterfaces.StatusDisplay.Instance;

                return myStatusDisplayInstance;
            }
        }

        public override string GetFirmwareVersion()
        {
            return "DEMO";
        }

        public override Exception AbortCurrentPumpingOperation()
        {
            return null;
        }
        #endregion

        public CDemoSimulator()
        {
            this.deviceID = deviceId++ + "";
        }
    }
}
