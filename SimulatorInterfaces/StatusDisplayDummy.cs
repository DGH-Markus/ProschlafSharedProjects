using SimulatorInterfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace SimulatorInterfaces
{
    /// <summary>
    /// A dummy status display class for simulator instances. It is never displayed.
    /// Each instance of a simulator (in multiple devices mode only!) has a corresponding StatusDisplayDummy instance assigned to it. 
    /// This dummy then reroutes any method calls to the ActivitiesStatusDisplay, which displays all ongoing processes for all connected simulators on a single form.
    /// </summary>
    public partial class StatusDisplayDummy : Form, IStatusDisplayFunctions
    {
        #region Vars
        string mySimulatorId = "";
        CBaseSimulator myDevice = null;
        #endregion

        private StatusDisplayDummy(){ } //constructor without argument is not allowed (each dummy must know its simulator object to which it is assigned to)

        /// <summary>
        /// 
        /// </summary>
        /// <param name="simulatorId">The device id of the simulator to which this display belongs to.</param>
        /// <param name="simulator">The simulator object to which this dummy is assigend to.</param>
        public StatusDisplayDummy(string simulatorId, CBaseSimulator simulator)
        {
            mySimulatorId = simulatorId;

            ActivitiesStatusDisplay.Instance.RegisterNewDummy(simulatorId);
            myDevice = simulator;
            myDevice.ErrorOccuredEvent += MyDevice_ErrorOccuredEvent;
        }

        private void MyDevice_ErrorOccuredEvent(object sender, Exception e)
        {
            if (!myDevice.IsConnected)
                this.SetTextToStatus(SimulatorStati.NotConnected);
        }

        /// <summary>
        /// Displaying this dummy is not allowed.
        /// </summary>
        public void ShowDisplay()
        {
            this.Visible = false;
        }

        public void HideDisplay()
        {
            this.Visible = false;
        }

        public void SetTextToStatus(SimulatorStati status)
        {
            ActivitiesStatusDisplay.Instance.SetTextToStatus(mySimulatorId, status);
        }

        public void SetProgressBarValue(int value)
        {
            ActivitiesStatusDisplay.Instance.SetProgressBarValue(mySimulatorId, value);
        }

        public void SetEnabledStatus(bool isEnabled)
        {
            //not used 
        }

        /// <summary>
        /// Override the base Form.Show() method because displaying this dummy is not allowed.
        /// </summary>
        public new void Show()
        {
            this.Visible = false;
        }

        public void SetSimulatorAssignment(string simulatorId, string assignment)
        {
            ActivitiesStatusDisplay.Instance.SetSimulatorAssignment(simulatorId, assignment);
        }

        public void ForceUpdateGUI()
        {
            //not used 
        }
    }
}
