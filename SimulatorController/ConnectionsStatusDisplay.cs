
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Linq;
using SimulatorInterfaces;

namespace SimulatorController
{
    /// <summary>
    /// This Form represents a small monitor that offers the user the possibility to review the current connection / busy status of all available simulator devices by displaying an additional form.
    /// It is only used for the multiple-simulator mode.
    /// 
    /// Implemented as singleton.
    /// </summary>
    public partial class ConnectionsStatusDisplay : Form
    {
        #region Variables
        private static ConnectionsStatusDisplay Display = null; //Singleton object

        delegate void TextCallback(string s); //a delegate used to set the text when called from another thread
        delegate void TextTypeCallback(SimulatorStati status); //a delegate used to set the text by type when called from another thread
        delegate void ProgressBarCallback(int value); //a delegate used to set the progessbar value when called from another thread
        delegate void HideDisplayCallback(); //Hides this form

        OperationModes currentMode = OperationModes.MainMenue;
        #endregion

        #region Props
        /// <summary>
        /// Singleton accessor to this class. Instanciates a new object if necessary.
        /// </summary>
        public static ConnectionsStatusDisplay Instance
        {
            get
            {
                if (Display == null || Display.IsDisposed)
                    Display = new ConnectionsStatusDisplay();

                return Display;
            }
        }
        #endregion

        #region Enums
        /// <summary>
        /// Determines whether the display shows the "Show connections" or the "Show current operations" button.
        /// </summary>
        public enum OperationModes { MainMenue, StandardOperations };
        #endregion

        private ConnectionsStatusDisplay()
        {
            InitializeComponent();

            Location = new Point(Screen.PrimaryScreen.WorkingArea.Width - this.Width, Screen.PrimaryScreen.WorkingArea.Height - this.Height); //bottom right
        }

        /// <summary>
        /// Hides the Display.
        /// </summary>
        public void HideDisplay()
        {
            if (this.InvokeRequired)
            {
                HideDisplayCallback cb = new HideDisplayCallback(HideDisplay);
                this.Invoke(cb);
            }
            else
            {
                this.Hide();
            }
        }

        /// <summary>
        /// Shows the display. Should be called from the GUI thread.
        /// </summary>
        public void ShowDisplay()
        {
            if (this.InvokeRequired)
            {
                HideDisplayCallback cb = new HideDisplayCallback(ShowDisplay);
                this.Invoke(cb);
            }
            else
            {
                this.Show();
                this.TopMost = true;
            }
        }

        /// <summary>
        /// Sets the button that is displayed on the status display.
        /// </summary>
        /// <param name="mode">The mode to be applied.</param>
        public void SetOperationMode(OperationModes mode)
        {
            this.currentMode = mode;

            UpdateMode();
        }

        private void UpdateMode()
        {
            if (this.currentMode == OperationModes.MainMenue)
            {
                bShowSomething.Text = "Verbindungen anzeigen";
            }
            else if (this.currentMode == OperationModes.StandardOperations)
            {
                bShowSomething.Text = "Aktivitäten anzeigen";
            }
        }

        /// <summary>
        /// Shows either the ConnectedSimulatorStatusDisplay that lets the user review the connection status of all simulators (uses in the main menue form) or
        /// a display that lets the user review all simulator operations being currently performed.
        /// </summary>
        private void bShowSomething_Click(object sender, EventArgs e)
        {
            if (this.IsDisposed || !this.IsHandleCreated)
                return;

            if (this.InvokeRequired)
            {
                this.Invoke(new MethodInvoker(delegate() { bShowSomething_Click(sender, e); }));
                return;
            }

            if (currentMode == OperationModes.MainMenue)
            {
                ConnectedSimulatorStatusDisplay display = new ConnectedSimulatorStatusDisplay();
                display.ShowDialog();
            }
            else
            {
                ActivitiesStatusDisplay.Instance.ShowDialog();
            }
        }
    }
}