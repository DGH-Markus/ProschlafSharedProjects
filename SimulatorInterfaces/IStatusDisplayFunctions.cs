using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace SimulatorInterfaces
{
    /// <summary>
    /// This interface defines the basic functions which every derived status display class has to implement.
    /// A status display is used to inform the user about connectivity status, current progress, idle mode, etc.
    /// 
    /// The deriving classes should implement the singleton pattern because there should be only one active display.
    /// </summary>
    public interface IStatusDisplayFunctions
    {

        /// <summary>
        /// Shows the display. This should always be called from the GUI thread.
        /// </summary>
        void ShowDisplay();

        void HideDisplay();

        /// <summary>
        /// Sets the main info text of the display to the specified predefined status.
        /// </summary>
        /// <param name="status">The status to be displayed.</param>
        void SetTextToStatus(SimulatorStati status);

        /// <summary>
        /// Accessor to the progressbar of the display.
        /// </summary>
        /// <param name="value">The value to be displayed (0-100).</param>
        void SetProgressBarValue(int value);

        /// <summary>
        /// En- or disables the whole display to prevent any user interaction.
        /// </summary>
        /// <param name="isEnabled">True to enable the display, false to disable it.</param>
        void SetEnabledStatus(bool isEnabled);

        /// <summary>
        /// Updates the status display in order to display the assignments for a specific simulator.
        /// </summary>
        /// <param name="simulatorId"></param>
        /// <param name="assigment"></param>
        void SetSimulatorAssignment(string simulatorId, string assigment);

        /// <summary>
        /// Determines whether the status display was disposed or not.
        /// </summary>
        bool IsDisposed { get; }

        /// <summary>
        /// A thread-safe method to force an update of the display's window.
        /// </summary>
        void ForceUpdateGUI();
    }

    /// <summary>
    /// The basic states in which a simulator device can be.
    /// </summary>
    public enum SimulatorStati { Connecting, Busy, PreparedForMapping, Inflate, Evacuate, Measuring, Ready, NotConnected, NotReady }; 
}
