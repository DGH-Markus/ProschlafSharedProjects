using SimulatorInterfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Linq;

namespace SimulatorInterfaces
{
    /// <summary>
    /// This Form represents a small monitor where the current status of the Simulator can be reviewed.
    /// It is only used for the single-simulator (one usb/bluetooth/lan device) mode.
    /// 
    /// Implemented as singleton.
    /// </summary>
    public partial class StatusDisplay : Form, IStatusDisplayFunctions
    {
        #region Variables
        private static StatusDisplay Display = null; //Singleton object

        private static StatusDisplay fakeDisplay = null; //can be used as display when no simulato was initialized

        delegate void TextCallback(string s); //a delegate used to set the text when called from another thread
        delegate void TextTypeCallback(SimulatorStati status); //a delegate used to set the text by type when called from another thread
        delegate void ProgressBarCallback(int value); //a delegate used to set the progessbar value when called from another thread
        delegate void HideDisplayCallback(); //Hides this form

        Point myFixedLocation;

        Timer relocationTimer = new Timer() { Interval = 1000 };
        #endregion

        #region Overrides
        /// <summary>
        /// Makes sure that cross-thread calls are handled properly.
        /// </summary>
        public new void Show()
        {
            if (this.InvokeRequired)
                this.Invoke(new MethodInvoker(this.Show));
            else
                base.Show();

        }

        public new void Hide()
        {
            if (this.InvokeRequired)
                this.Invoke(new MethodInvoker(this.Show));
            else
                base.Show();

        }
        #endregion

        #region Props

        /// <summary>
        /// Singleton accessor to this class. Instanciates a new object if necessary.
        /// </summary>
        public static StatusDisplay Instance
        {
            get
            {
                if (Display == null || Display.IsDisposed)
                    Display = new StatusDisplay();

                return Display;
            }
        }

        public static IStatusDisplayFunctions FakeDisplay
        {
            get
            {
                if (fakeDisplay == null)
                    fakeDisplay = new StatusDisplay();

                return fakeDisplay;
            }
        }
        #endregion

        private StatusDisplay()
        {
            InitializeComponent();

            Location = myFixedLocation = new Point(Screen.PrimaryScreen.WorkingArea.Width - this.Width, Screen.PrimaryScreen.WorkingArea.Height - this.Height);

            relocationTimer.Tick += relocationTimer_Tick;
        }

        /// <summary>
        /// Accessor to set the text of the status label to a enum type.
        /// </summary>
        /// <param name="status">The status to be applied.</param>
        public void SetTextToStatus(SimulatorStati status)
        {
            if (this.InvokeRequired)
            {
                TextTypeCallback cb = new TextTypeCallback(SetTextToStatus);
                this.Invoke(cb, status);
            }
            else
            {
                lStatus.BackColor = Color.White;

                switch (status)
                {
                    case SimulatorStati.Connecting:
                        lStatus.Text = "Verbinde...";
                        lStatus.BackColor = Color.Yellow;
                        break;
                    case SimulatorStati.Busy:
                        lStatus.Text = "In Betrieb...";
                        break;
                    case SimulatorStati.PreparedForMapping:
                        lStatus.Text = "Bereit";
                        progressBar1.Value = 100;
                        break;
                    case SimulatorStati.Inflate:
                        lStatus.Text = "Aufpumpen...";
                        break;
                    case SimulatorStati.Evacuate:
                        lStatus.Text = "Absaugen...";
                        break;
                    case SimulatorStati.Measuring:
                        lStatus.Text = "Messung läuft...";
                        break;
                    case SimulatorStati.Ready: //not ready (must be pumped to initial value)
                        lStatus.Text = "Nicht bereit";
                        lStatus.BackColor = Color.Yellow;
                        break;
                    case SimulatorStati.NotConnected:
                        lStatus.Text = "Nicht verbunden";
                        lStatus.BackColor = Color.Red;
                        break;
                    default:
                        lStatus.Text = "";
                        break;
                }
            }
        }

        /// <summary>
        /// Accessor to the progressbar of the form.
        /// </summary>
        /// <param name="value">The value to be applied (0-100).</param>
        public void SetProgressBarValue(int value)
        {
            if (this.InvokeRequired)
            {
                ProgressBarCallback cb = new ProgressBarCallback(SetProgressBarValue);
                this.Invoke(cb, value);
            }
            else
            {
                if (value > 100)
                    value = 100;
                else if (value < 0)
                    value = 0;

                progressBar1.Value = value;
            }
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
                this.TopMost = false;
                this.SendToBack();
                this.Hide();
            }
        }

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
                this.BringToFront();
            }
        }

        public void SetEnabledStatus(bool isEnabled)
        {
            this.Enabled = isEnabled;
        }

        public void SetSimulatorAssignment(string simulatorId, string assigment)
        {
            //not used
        }

        /// <summary>
        /// When the statusdisplay is forcibly relocated by the Windows 8 touch keyboard, this timer is fired and resets the display after some time to its original position.
        /// </summary>
        void relocationTimer_Tick(object sender, EventArgs e)
        {
            if (Location != myFixedLocation)
                Location = myFixedLocation;

            if (Location == myFixedLocation)
                relocationTimer.Stop();
        }

        private void StatusDisplay_LocationChanged(object sender, EventArgs e)
        {
            relocationTimer.Start();
        }

        public void ForceUpdateGUI()
        {
            this.Invoke((MethodInvoker)(() =>
            {
                this.Invalidate();
                this.Update();
                this.Refresh();
                Application.DoEvents();
            }));
        }
    }
}