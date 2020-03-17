using SerialSimulatorServices;
using SimulatorController;
using SimulatorInterfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SimulatorController
{
    /// <summary>
    /// Used to give the user an overview over all assigned simulators and their connection status in multi-simulator mode. This works with serial and network simulator devices.
    /// </summary>
    public partial class ConnectedSimulatorStatusDisplay : Form
    {
        public ConnectedSimulatorStatusDisplay()
        {
            InitializeComponent();

            GenerateOverview();

            SimulatorController.MultiSimulatorMode.SimulatorControl.Instance.SimulatorConnectionChanged += Instance_ClientConnectionChanged;
        }

        /// <summary>
        /// Updates the overview when a simulator connects or disconnects.
        /// </summary>
        /// <param name="connectedClients"></param>
        void Instance_ClientConnectionChanged(List<string> simulatorIds)
        {
            if (this.IsHandleCreated && !this.IsDisposed)
                this.Invoke(new MethodInvoker(() => GenerateOverview()));
        }

        private void bClose_Click(object sender, EventArgs e)
        {
            SimulatorController.MultiSimulatorMode.SimulatorControl.Instance.SimulatorConnectionChanged -= Instance_ClientConnectionChanged;
            this.Close();
        }

        /// <summary>
        /// Generates an overview over all assigned simulators and displays it in the gridview.
        /// </summary>
        private void GenerateOverview()
        {
            //construct a table-like structure out of the assignments and bind the gridview to it
            var assignmentsTable = (from assignment in SimulatorAssignmentsManager.GetSimulatorAssignments()
                                    select new
                                    {
                                        Simulatorkennung = assignment.SimulatorId,
                                        Zuweisung = assignment.Assignment != SimulatorAssignmentsManager.SimulatorAssignments.Nicht_zugewiesen ? (assignment.Assignment.ToString() + ", ") : ""
                                        + (assignment.Position != SimulatorAssignmentsManager.SimulatorPositions.Nicht_zugewiesen ? assignment.Position.ToString() + ", " : "")
                                        + (assignment.Mattress != SimulatorAssignmentsManager.SimulationMattresses.Nicht_zugewiesen ? assignment.Mattress.ToString() : "")
                                    }).ToArray();

            dataGridViewOverview.Rows.Clear();
            dataGridViewOverview.ColumnCount = 3;

            dataGridViewOverview.Columns[0].Name = "Kennung";
            dataGridViewOverview.Columns[1].Name = "Zuweisungen";
            dataGridViewOverview.Columns[2].Name = "Status";

            dataGridViewOverview.Columns[2].DefaultCellStyle.ForeColor = Color.Red;

            double percWidth = (double)dataGridViewOverview.Width / 100d;

            dataGridViewOverview.Columns[0].Width = (int)(percWidth * 20);
            dataGridViewOverview.Columns[1].Width = (int)(percWidth * 60);
            dataGridViewOverview.Columns[2].Width = (int)(percWidth * 20);

            foreach (var item in assignmentsTable)
            {
                if (string.IsNullOrEmpty(item.Simulatorkennung)) //prevent "bad" devices from being added to the overview
                    continue;

                dataGridViewOverview.Rows.Add(item.Simulatorkennung, item.Zuweisung.TrimEnd(new char[] { ' ', ',' }).Replace('_', ' '), "Nicht verbunden");
            }

            dataGridViewOverview.ClearSelection();

            var context = TaskScheduler.FromCurrentSynchronizationContext(); //for synchronization with the GUI thread
            var token = Task.Factory.CancellationToken;

            Task t = Task.Factory.StartNew(() =>
            {
                UpdateConnectionsOverview();
            }, token, TaskCreationOptions.None, context);
        }

        /// <summary>
        /// Updates the overview when a simulator connects or disconnects.
        /// </summary>
        private void UpdateConnectionsOverview()
        {
            List<CBaseSimulator> clients = SimulatorController.MultiSimulatorMode.SimulatorControl.Instance.GetAllConnectedSimulators();

            for (int i = 0; i < dataGridViewOverview.Rows.Count; i++) //reset all connections to "not connected"
            {
                dataGridViewOverview.Rows[i].Cells[2].Value = "Nicht verbunden";
                dataGridViewOverview.Rows[i].Cells[2].Style.ForeColor = Color.Red;
            }

            List<DataGridViewRow> updatedRows = new List<DataGridViewRow>();

            foreach (CBaseSimulator someClient in clients) //now individually change to "connected"
            {
                bool clientFound = false;
                string id = "unbekannt";

                if (someClient is SerialSimulatorServices.CSerialSimulator)
                {
                    //SerialSimulatorServices.CSerialSimulator client = (someClient as SerialSimulatorServices.CSerialSimulator);
                    id = someClient.DeviceId;
                }
                else if (someClient is NgMattApiWrapper.CngMattSimulator)
                {
                    id = someClient.GetDeviceId();
                }

                //check if the simluator id is already in the list and change the status accordingly
                foreach (DataGridViewRow row in dataGridViewOverview.Rows)
                {
                    if (row.Cells[0].Value?.ToString() == id) //find the row for the current client
                    {
                        row.Cells[2].Value = "Verbunden";
                        row.Cells[2].Style.ForeColor = Color.Green;
                        clientFound = true;
                        break;
                    }
                }

                if (!clientFound) //the connected device doesn't have an assignment --> just add it to the list
                {
                    dataGridViewOverview.Rows.Add(id, "Keine Zuweisung", "Verbunden");
                }
            }

            if (SimulatorController.MultiSimulatorMode.SimulatorControl.Instance.GetIsSearchRunning() == false)
                progressBarSearchRunning.Visible = lProgressBarSearchRunning.Visible = false;
            else
                progressBarSearchRunning.Visible = lProgressBarSearchRunning.Visible = true;
        }
    }
}
