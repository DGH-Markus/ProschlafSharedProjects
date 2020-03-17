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
    /// This status display is used in multiple device mode to let the user review the current acitivties of all connected simulators.
    /// All simulator instances have a StatusDisplayDummy assigned to them, which then forwards all changes to this class.
    /// This class can only be shown by the SimulatorController.
    /// Each simulator gets one control row on this form which consists of 2 labels and a progress bar.
    /// Implements singleton pattern.
    /// </summary>
    public partial class ActivitiesStatusDisplay : Form
    {
        #region Vars
        static ActivitiesStatusDisplay myInstance = new ActivitiesStatusDisplay();

        Dictionary<string, List<Control>> registeredDisplays = new Dictionary<string, List<Control>>();

        const int X_POS_SIMULATOR_LABELS = 5;
        const int WIDTH_PROGRESSBARS = 120;
        const int WIDTH_STATUS_LABELS = 100;
        const int ROW_MARGIN_Y = 5; //5 pixels space between two control rows
        const int SPACE_BETWEEN_CONTROLS = 10;

        Font simulatorLabelsFont = new Font("Microsoft Sans Serif", 10.25f);

        static object listLock = new object();
        #endregion

        #region Props
        public static ActivitiesStatusDisplay Instance
        {
            get
            {
                return myInstance;
            }
        }
        #endregion

        private ActivitiesStatusDisplay()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Adds a new simulator to this display.
        /// </summary>
        /// <param name="simulatorId"></param>
        /// <param name="assignments"></param>
        public void RegisterNewDummy(string simulatorId)
        {
            if (this.InvokeRequired)
            {
                this.Invoke((MethodInvoker)delegate { RegisterNewDummy(simulatorId); });
                return;
            }

            if (!registeredDisplays.ContainsKey(simulatorId))
                AddDisplayControls(simulatorId);
            else
                SetTextToStatus(simulatorId, SimulatorStati.Ready);
        }

        private void AddDisplayControls(string simulatorId)
        {
            List<Control> newControls = new List<Control>();

            //create new controls
            ProgressBar newBar = new ProgressBar() { Value = 0, Width = WIDTH_PROGRESSBARS, Style = ProgressBarStyle.Continuous, Location = new Point(0, 0) };
            Label newSimulatorLabel = new Label() { Name = "lSim" + simulatorId, Font = simulatorLabelsFont, Location = new Point(X_POS_SIMULATOR_LABELS, 0), Text = "Simulator[" + simulatorId + "]:", AutoSize = true, MinimumSize = new Size(80, 23), TextAlign = ContentAlignment.MiddleLeft };
            Label newStatusLabel = new Label() { Name = "lStatus" + simulatorId, Text = "test", MinimumSize = new System.Drawing.Size(WIDTH_STATUS_LABELS, 23), TextAlign = System.Drawing.ContentAlignment.MiddleLeft, Location = new Point(0, 0), AutoSize = true };

            //register them
            newControls.Add(newBar);
            newControls.Add(newSimulatorLabel);
            newControls.Add(newStatusLabel);

            newSimulatorLabel.BringToFront();

            registeredDisplays.Add(simulatorId, newControls);

            //calculate their Y coordinate and add them to the form
            int currentYCoordinate = 10;

            foreach (List<Control> row in registeredDisplays.Values) //find the y coordinate of label that is closest to the bottom of the form
                foreach (Control c in row)
                    if (c is Label)
                        if (c.Location.Y > currentYCoordinate)
                            currentYCoordinate = c.Location.Y;

            int newYCoordinate = currentYCoordinate + newSimulatorLabel.Height + ROW_MARGIN_Y;

            foreach (Control c in newControls)
                c.Location = new Point(c.Location.X, newYCoordinate);

            this.Controls.AddRange(newControls.ToArray());

            ResizeForm(); //calculate and set the X coordinates of all controls on this form

            this.Invalidate();

            //update the height of the form
            this.Height = newYCoordinate + newSimulatorLabel.Height + ROW_MARGIN_Y * 4 + bBack.Height + ROW_MARGIN_Y * 2 + 10;
        }

        public void SetTextToStatus(string simulatorId, SimulatorStati status)
        {
            if (!registeredDisplays.ContainsKey(simulatorId))
                return;

            Label label = registeredDisplays[simulatorId].OfType<Label>().Where(l => l.Name.Contains("lStatus")).FirstOrDefault();

            if (label != null)
                if (this.InvokeRequired)
                    this.Invoke((MethodInvoker)delegate { SetTextToStatus(simulatorId, status); });
                else
                {
                    switch (status)
                    {
                        case SimulatorStati.Connecting:
                            label.Text = "Verbinde...";
                            break;
                        case SimulatorStati.Busy:
                            label.Text = "In Betrieb...";
                            break;
                        case SimulatorStati.PreparedForMapping:
                            label.Text = "Bereit";
                            break;
                        case SimulatorStati.Inflate:
                            label.Text = "Aufpumpen...";
                            break;
                        case SimulatorStati.Evacuate:
                            label.Text = "Absaugen...";
                            break;
                        case SimulatorStati.Measuring:
                            label.Text = "Messung läuft...";
                            break;
                        case SimulatorStati.Ready: //not ready (must be pumped to initial value)
                            label.Text = "Nicht bereit";
                            break;
                        case SimulatorStati.NotConnected:
                            label.Text = "Nicht verbunden";
                            break;

                        default:
                            label.Text = "";
                            break;
                    }
                }
        }

        public void SetProgressBarValue(string simulatorId, int value)
        {
            if (!registeredDisplays.ContainsKey(simulatorId))
                return;

            if (!this.IsHandleCreated) //some exceptions happen frequently here without this (complaing about the window handle not being created yet)
                return;

            value = value > 100 ? 100 : value;
            value = value < 0 ? 0 : value;

            ProgressBar bar = registeredDisplays[simulatorId].OfType<ProgressBar>().FirstOrDefault();

            if (bar != null)
                this.Invoke((MethodInvoker)delegate { bar.Value = value; });
        }

        /// <summary>
        /// Updates the assignment of an already registered simulator. Also re-sizes the form to match the new size of the assigment labels. 
        /// </summary>
        /// <param name="simulatorId"></param>
        /// <param name="assignments"></param>
        public void SetSimulatorAssignment(string simulatorId, string assignment)
        {
            if (!registeredDisplays.ContainsKey(simulatorId))
                return;

            if (this.InvokeRequired)
            {
                this.Invoke((MethodInvoker)delegate { SetSimulatorAssignment(simulatorId, assignment); });
                return;
            }

            Label label = registeredDisplays[simulatorId].OfType<Label>().Where(l => l.Name.Contains("lSim")).FirstOrDefault();
            label.Text = "Simulator[" + simulatorId + "]: " + assignment;

            ResizeForm();
        }

        /// <summary>
        /// Adjusts the form's size depending to the size of the largest simulator label and re-calculates all X-positions of all controls.
        /// </summary>
        private void ResizeForm()
        {
            int maxWidth = 0;

            lock (listLock)
            {
                foreach (List<Control> controls in registeredDisplays.Values)
                {
                    Label label = controls.OfType<Label>().Where(l => l.Name.Contains("lSim")).FirstOrDefault();

                    if (label.Width > maxWidth)
                        maxWidth = label.Width;
                }
            }

            int formWidth = SPACE_BETWEEN_CONTROLS + maxWidth + SPACE_BETWEEN_CONTROLS + WIDTH_PROGRESSBARS + SPACE_BETWEEN_CONTROLS + WIDTH_STATUS_LABELS + SPACE_BETWEEN_CONTROLS;
            this.Size = new System.Drawing.Size(formWidth, this.Height);

            int progressbarX = X_POS_SIMULATOR_LABELS + maxWidth + SPACE_BETWEEN_CONTROLS;
            int statusLabelX = progressbarX + WIDTH_PROGRESSBARS + SPACE_BETWEEN_CONTROLS;

            lock (listLock)
            {
                foreach (List<Control> controls in registeredDisplays.Values)
                {
                    Label label = controls.OfType<Label>().Where(l => l.Name.Contains("lStatus")).FirstOrDefault();
                    label.Location = new Point(statusLabelX, label.Location.Y);

                    ProgressBar bar = controls.OfType<ProgressBar>().FirstOrDefault();
                    bar.Location = new Point(progressbarX, bar.Location.Y);

                }
            }
        }

        private void bBack_Click(object sender, EventArgs e)
        {
            this.Hide();
        }
    }
}
