using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimulatorInterfaces
{
    public abstract class SimulatorExceptions
    {
        public class NoBleDongleFoundException : Exception
        {
            public NoBleDongleFoundException(string message) : base(message)
            {

            }
        }

        public class DeviceNotConnectedException : Exception
        {
            public string DeviceId { get; set; }
        }

        public class DeviceBusyException : Exception
        {
            public string DeviceId { get; set; }
        }

        public class UnhandledException : Exception
        {
            public string DeviceId { get; set; }
        }

        public class PressureValuesNullException : Exception
        {
            public string DeviceId { get; set; }
        }

        public class InvalidPressureException : Exception
        {
            public string DeviceId { get; set; }
            public short Channel { get; set; }
            public short PressureValue { get; set; }

            public override string ToString()
            {
                return DeviceId + " invalid pressure for channel: " + Channel + ": " + PressureValue + Environment.NewLine + base.ToString();
            }
        }

        public class InvalidEvacuationTimeException : Exception
        {
            public string DeviceId { get; set; }
            public short Channel { get; set; }
            public int Time { get; set; }

            public override string ToString()
            {
                return DeviceId + " invalid evac time for channel: " + Channel + ": " + Time + Environment.NewLine + base.ToString();
            }
        }

        public class InvalidEvacuationBaseValueException : Exception
        {
            public string DeviceId { get; set; }
            public short Channel { get; set; }
            public int BaseValue { get; set; }

            public override string ToString()
            {
                return DeviceId + " invalid evac base value for channel: " + Channel + ": " + BaseValue + "mb" + Environment.NewLine + base.ToString();
            }
        }

        public class InvalidChannelException : Exception
        {
            public string DeviceId { get; set; }
            public int Channel { get; set; }
        }

        public class InvalidProfileException : Exception
        {
            public string DeviceId { get; set; }
        }

        public class SendProfileException : Exception
        {
            public string DeviceId { get; set; }
            public int ErrorFlag { get; set; }
            public string[] Error { get; set; }
        }

        public class OperationTimeoutException : Exception
        {
            public string DeviceId { get; set; }
            public string OperationName { get; set; }
        }

        /// <summary>
        /// Returned from a pumping process when a channel could not be inflated. This usually means that the corresponding tube is not properly connected.
        /// </summary>
        public class InflateException : Exception
        {
            public InflateException(string message) : base(message)
            {

            }

            public string DeviceId { get; set; }
            public int Channel { get; set; }
            public int PressureValue { get; set; }

            public override string ToString()
            {
                return DeviceId + ": inflate failed. Channel: " + Channel + ", pressure value: " + PressureValue + Environment.NewLine + base.ToString();
            }
        }

        /// <summary>
        /// Returned from a pumping process when a channel could not be evacuated. This usually means that the corresponding tube is not properly connected.
        /// </summary>
        public class EvacuateException : Exception
        {
            public EvacuateException(string message) : base(message)
            {

            }

            public string DeviceId { get; set; }
            public int Channel { get; set; }
            public int Time { get; set; }
        }
    }
}
