using System;
using System.Collections.Generic;
using System.Text;

namespace ngMattAlgorithms
{
    internal class NgMattSleepSession
    {
        public DateTime Start { get; set; }
        public DateTime End { get; set; }

        public string SessionId { get; set; }

        public IReadOnlyList<Movement> Movements { get; set; }

        public TimeSpan Length
        {
            get
            {
                return (End - Start);
            }
        }
    }

    internal class Movement
    {
        public DateTime Start { get; set; }
        public DateTime End { get; set; }

        public int? Pair { get; set; }

        public byte[] PressureValues_Start { get; set; }
        public byte[] PressureValues_End { get; set; }
    }

    internal class SqiResult
    {
        public double SQI_Percent { get; set; }
        public int NumberOfMovements { get; set; }
        public int Points { get; set; }
    }

    /// <summary>
    /// Raw data as retrieved from LS 2.0.
    /// </summary>
    internal class MovementRawData
    {
        public DateTime Time { get; set; }

        /// <summary>
        /// 12 pressure values ranging from 0 to 255 (typically 1 to ~50).
        /// </summary>
        public byte[] PressureValues { get; set; } = new byte[12];

        public bool? IsPresenceDetectedByFirmware { get; set; }

        public bool? IsMovementDetectedByFirmware { get; set; }
    }
}
