using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace SimulatorController
{
    public partial class SetupMultipleSimulatorsWizard : Form
    {
        #region Vars
        const string COMBOBOX_ITEM_NOT_USED = "Nicht verwendet";
        #endregion

        public SetupMultipleSimulatorsWizard()
        {
            InitializeComponent();

            //populate the comboboxes with the serial numbers
            var ids = SimulatorController.MultiSimulatorMode.SimulatorControl.Instance.GetIdsOfConnectedSimulators();
            ids.Insert(0, COMBOBOX_ITEM_NOT_USED);

            foreach (string s in ids)
            {
                cbSerialNbr1.Items.Add(s);
                cbSerialNbr2.Items.Add(s);
                cbSerialNbr3.Items.Add(s);
                cbSerialNbr4.Items.Add(s);
            }

            //populate the comboboxes with the available simulator assignments
            foreach (string s in Enum.GetNames(typeof(SimulatorAssignmentsManager.SimulatorAssignments)))
            {
                cbDeviceAssignment1.Items.Add(s);
                cbDeviceAssignment2.Items.Add(s);
                cbDeviceAssignment3.Items.Add(s);
                cbDeviceAssignment4.Items.Add(s);
            }

            //populate the comboboxes with the available simulator positions
            foreach (string s in Enum.GetNames(typeof(SimulatorAssignmentsManager.SimulatorPositions)))
            {
                cbPosition1.Items.Add(s);
                cbPosition2.Items.Add(s);
                cbPosition3.Items.Add(s);
                cbPosition4.Items.Add(s);
            }

            //simulator mattresses
            foreach (string s in Enum.GetNames(typeof(SimulatorAssignmentsManager.SimulationMattresses)))
            {
                cbMattress1.Items.Add(s);
                cbMattress2.Items.Add(s);
                cbMattress3.Items.Add(s);
                cbMattress4.Items.Add(s);
            }

            PrefillForm();
        }


        /// <summary>
        /// Loads any existing simulator assignments and prefills the corresponding controls.
        /// </summary>
        private void PrefillForm()
        {
            #region Pre-select combobox items

            cbSerialNbr1.SelectedIndex = cbSerialNbr2.SelectedIndex = cbSerialNbr3.SelectedIndex = cbSerialNbr4.SelectedIndex = 0;

            if (cbDeviceAssignment1.Items.Count > 0)
                cbDeviceAssignment1.SelectedIndex = cbDeviceAssignment2.SelectedIndex = cbDeviceAssignment3.SelectedIndex = cbDeviceAssignment4.SelectedIndex = 0;

            if (cbMattress1.Items.Count > 0)
                cbMattress1.SelectedIndex = cbMattress2.SelectedIndex = cbMattress3.SelectedIndex = cbMattress4.SelectedIndex = 0;

            if (cbPosition1.Items.Count > 4)
            {
                cbPosition1.SelectedIndex = 1;
                cbPosition2.SelectedIndex = 2;
                cbPosition3.SelectedIndex = 3;
                cbPosition4.SelectedIndex = 4;
            }

            #endregion

            List<SimulatorAssignment> assignments = SimulatorAssignmentsManager.GetSimulatorAssignments();

            assignments = assignments.OrderBy(a => a.SimulatorId).ToList();

            if (assignments.Count >= 1)
            {
                if (cbSerialNbr1.Items.Contains(assignments[0].SimulatorId))
                    cbSerialNbr1.SelectedItem = assignments[0].SimulatorId;
                else
                {
                    cbSerialNbr1.Items.Add(assignments[0].SimulatorId);
                    cbSerialNbr1.ForeColor = Color.Red;
                }

                cbDeviceAssignment1.SelectedItem = assignments[0].Assignment.ToString();
                cbPosition1.SelectedItem = assignments[0].Position.ToString();
                cbMattress1.SelectedItem = assignments[0].Mattress.ToString();
                tbCustomName1.Text = assignments[0].CustomAssignmentName;
            }

            if (assignments.Count >= 2)
            {
                if (cbSerialNbr2.Items.Contains(assignments[1].SimulatorId))
                    cbSerialNbr2.SelectedItem = assignments[1].SimulatorId;
                else
                {
                    cbSerialNbr2.Items.Add(assignments[1].SimulatorId);
                    cbSerialNbr2.ForeColor = Color.Red;
                }

                cbDeviceAssignment2.SelectedItem = assignments[1].Assignment.ToString();
                cbPosition2.SelectedItem = assignments[1].Position.ToString();
                cbMattress2.SelectedItem = assignments[1].Mattress.ToString();
                tbCustomName2.Text = assignments[1].CustomAssignmentName;
            }

            if (assignments.Count >= 3)
            {
                if (cbSerialNbr3.Items.Contains(assignments[2].SimulatorId))
                    cbSerialNbr3.SelectedItem = assignments[2].SimulatorId;
                else
                {
                    cbSerialNbr3.Items.Add(assignments[2].SimulatorId);
                    cbSerialNbr3.ForeColor = Color.Red;
                }

                cbDeviceAssignment3.SelectedItem = assignments[2].Assignment.ToString();
                cbPosition3.SelectedItem = assignments[2].Position.ToString();
                cbMattress3.SelectedItem = assignments[2].Mattress.ToString();
                tbCustomName3.Text = assignments[2].CustomAssignmentName;
            }

            if (assignments.Count >= 4)
            {
                if (cbSerialNbr4.Items.Contains(assignments[3].SimulatorId))
                    cbSerialNbr4.SelectedItem = assignments[3].SimulatorId;
                else
                {
                    cbSerialNbr4.Items.Add(assignments[3].SimulatorId);
                    cbSerialNbr4.ForeColor = Color.Red;
                }

                cbDeviceAssignment4.SelectedItem = assignments[3].Assignment.ToString();
                cbPosition4.SelectedItem = assignments[3].Position.ToString();
                cbMattress4.SelectedItem = assignments[3].Mattress.ToString();
                tbCustomName4.Text = assignments[3].CustomAssignmentName;
            }

            if (assignments.Count > 4)
                MessageBox.Show("Achtung! Es sind Zuweisungen für mehr als 4 Simulatoren vorhanden. Es können jedoch nur 4 angezeigt werden. Um eine Zuweisung zu löschen, klicken Sie auf den entsprechenden Button!", "Zu viele Zuweisungen", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }



        private void bCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }


        /// <summary>
        /// Validates and saves the assignments.
        /// </summary>
        private void bSave_Click(object sender, EventArgs e)
        {
            #region Validation
            //only 1 simulator may have the pressure mapping assignment
            int pressureMappingCnt = 0;
            if ((string)cbDeviceAssignment1.Text == SimulatorAssignmentsManager.SimulatorAssignments.Druckmessung.ToString() || (string)cbDeviceAssignment1.Text == SimulatorAssignmentsManager.SimulatorAssignments.Druckmessung_und_Simulation_Kaltschaum.ToString() || (string)cbDeviceAssignment1.Text == SimulatorAssignmentsManager.SimulatorAssignments.Druckmessung_und_Simulation_NaturLatex.ToString())
                pressureMappingCnt++;
            if ((string)cbDeviceAssignment2.Text == SimulatorAssignmentsManager.SimulatorAssignments.Druckmessung.ToString() || (string)cbDeviceAssignment2.Text == SimulatorAssignmentsManager.SimulatorAssignments.Druckmessung_und_Simulation_Kaltschaum.ToString() || (string)cbDeviceAssignment2.Text == SimulatorAssignmentsManager.SimulatorAssignments.Druckmessung_und_Simulation_NaturLatex.ToString())
                pressureMappingCnt++;
            if ((string)cbDeviceAssignment3.Text == SimulatorAssignmentsManager.SimulatorAssignments.Druckmessung.ToString() || (string)cbDeviceAssignment3.Text == SimulatorAssignmentsManager.SimulatorAssignments.Druckmessung_und_Simulation_Kaltschaum.ToString() || (string)cbDeviceAssignment3.Text == SimulatorAssignmentsManager.SimulatorAssignments.Druckmessung_und_Simulation_NaturLatex.ToString())
                pressureMappingCnt++;
            if ((string)cbDeviceAssignment4.Text == SimulatorAssignmentsManager.SimulatorAssignments.Druckmessung.ToString() || (string)cbDeviceAssignment4.Text == SimulatorAssignmentsManager.SimulatorAssignments.Druckmessung_und_Simulation_Kaltschaum.ToString() || (string)cbDeviceAssignment4.Text == SimulatorAssignmentsManager.SimulatorAssignments.Druckmessung_und_Simulation_NaturLatex.ToString())
                pressureMappingCnt++;

            if (pressureMappingCnt != 1 && pressureMappingCnt != 2)
            {
                MessageBox.Show("Bitte weisen Sie maximal zwei Simulatoren die Funktion \"Druckmessung\" bzw. \"Druckmessung_und_Simulation_Kaltschaum\" und \"Druckmessung_und_Simulation_NaturLatex\" zu!", "Zuweisungen nicht korrekt", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                return;
            }

            //check if a simulator has been assigned multiple times
            List<string> simulatorSerialNbrs = new List<string>() { (string)cbSerialNbr1.Text, (string)cbSerialNbr2.Text, (string)cbSerialNbr3.Text, (string)cbSerialNbr4.Text };

            //delete any "Nicht verwendet" selections
            simulatorSerialNbrs = simulatorSerialNbrs.Where(s => s != COMBOBOX_ITEM_NOT_USED).ToList();

            if (simulatorSerialNbrs.Count != simulatorSerialNbrs.Distinct().Count())
            {
                MessageBox.Show("Es wurde mindestens ein Simulator doppelt eingerichtet. Bitte stellen Sie sicher, dass jede Seriennummer nur einmal verwendet wird!", "Simulator doppelt", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                return;
            }

            //if a serial number was selected, all other settings have to be made as well
            #endregion

            #region Save assignments
            bool success = true;

            List<SimulatorAssignment> unchangedAssignments = SimulatorAssignmentsManager.GetSimulatorAssignments();

            if (cbSerialNbr1.SelectedIndex > 0)
            {
                string simId = cbSerialNbr1.Text;
                SimulatorAssignmentsManager.SimulatorAssignments assignment = (SimulatorAssignmentsManager.SimulatorAssignments)Enum.Parse(typeof(SimulatorAssignmentsManager.SimulatorAssignments), cbDeviceAssignment1.Text);
                SimulatorAssignmentsManager.SimulatorPositions position = (SimulatorAssignmentsManager.SimulatorPositions)Enum.Parse(typeof(SimulatorAssignmentsManager.SimulatorPositions), cbPosition1.Text);
                SimulatorAssignmentsManager.SimulationMattresses mattress = (SimulatorAssignmentsManager.SimulationMattresses)Enum.Parse(typeof(SimulatorAssignmentsManager.SimulationMattresses), cbMattress1.Text);
                string customName = tbCustomName1.Text;

                SimulatorAssignment newAssignment = new SimulatorAssignment(simId, assignment, position, mattress, customName);

                success &= SimulatorAssignmentsManager.AddOrUpdateAssignment(newAssignment);

                if (unchangedAssignments.Where(a => a.SimulatorId == cbSerialNbr1.Text).Count() == 1) //remove the assignment from the list if it was existing before
                    unchangedAssignments.Remove(unchangedAssignments.Where(a => a.SimulatorId == cbSerialNbr1.Text).FirstOrDefault());
            }

            if (cbSerialNbr2.SelectedIndex > 0)
            {
                string simId = cbSerialNbr2.Text;
                SimulatorAssignmentsManager.SimulatorAssignments assignment = (SimulatorAssignmentsManager.SimulatorAssignments)Enum.Parse(typeof(SimulatorAssignmentsManager.SimulatorAssignments), cbDeviceAssignment2.Text);
                SimulatorAssignmentsManager.SimulatorPositions position = (SimulatorAssignmentsManager.SimulatorPositions)Enum.Parse(typeof(SimulatorAssignmentsManager.SimulatorPositions), cbPosition2.Text);
                SimulatorAssignmentsManager.SimulationMattresses mattress = (SimulatorAssignmentsManager.SimulationMattresses)Enum.Parse(typeof(SimulatorAssignmentsManager.SimulationMattresses), cbMattress2.Text);
                string customName = tbCustomName2.Text;

                SimulatorAssignment newAssignment = new SimulatorAssignment(simId, assignment, position, mattress, customName);

                success &= SimulatorAssignmentsManager.AddOrUpdateAssignment(newAssignment);

                if (unchangedAssignments.Where(a => a.SimulatorId == cbSerialNbr2.Text).Count() == 1) //remove the assignment from the list if it was existing before
                    unchangedAssignments.Remove(unchangedAssignments.Where(a => a.SimulatorId == cbSerialNbr2.Text).FirstOrDefault());
            }

            if (cbSerialNbr3.SelectedIndex > 0)
            {
                string simId = cbSerialNbr3.Text;
                SimulatorAssignmentsManager.SimulatorAssignments assignment = (SimulatorAssignmentsManager.SimulatorAssignments)Enum.Parse(typeof(SimulatorAssignmentsManager.SimulatorAssignments), cbDeviceAssignment3.Text);
                SimulatorAssignmentsManager.SimulatorPositions position = (SimulatorAssignmentsManager.SimulatorPositions)Enum.Parse(typeof(SimulatorAssignmentsManager.SimulatorPositions), cbPosition3.Text);
                SimulatorAssignmentsManager.SimulationMattresses mattress = (SimulatorAssignmentsManager.SimulationMattresses)Enum.Parse(typeof(SimulatorAssignmentsManager.SimulationMattresses), cbMattress3.Text);
                string customName = tbCustomName3.Text;

                SimulatorAssignment newAssignment = new SimulatorAssignment(simId, assignment, position, mattress, customName);

                success &= SimulatorAssignmentsManager.AddOrUpdateAssignment(newAssignment);

                if (unchangedAssignments.Where(a => a.SimulatorId == cbSerialNbr3.Text).Count() == 1) //remove the assignment from the list if it was existing before
                    unchangedAssignments.Remove(unchangedAssignments.Where(a => a.SimulatorId == cbSerialNbr3.Text).FirstOrDefault());
            }

            if (cbSerialNbr4.SelectedIndex > 0)
            {
                string simId = cbSerialNbr4.Text;
                SimulatorAssignmentsManager.SimulatorAssignments assignment = (SimulatorAssignmentsManager.SimulatorAssignments)Enum.Parse(typeof(SimulatorAssignmentsManager.SimulatorAssignments), cbDeviceAssignment4.Text);
                SimulatorAssignmentsManager.SimulatorPositions position = (SimulatorAssignmentsManager.SimulatorPositions)Enum.Parse(typeof(SimulatorAssignmentsManager.SimulatorPositions), cbPosition4.Text);
                SimulatorAssignmentsManager.SimulationMattresses mattress = (SimulatorAssignmentsManager.SimulationMattresses)Enum.Parse(typeof(SimulatorAssignmentsManager.SimulationMattresses), cbMattress4.Text);
                string customName = tbCustomName4.Text;

                SimulatorAssignment newAssignment = new SimulatorAssignment(simId, assignment, position, mattress, customName);

                success &= SimulatorAssignmentsManager.AddOrUpdateAssignment(newAssignment);

                if (unchangedAssignments.Where(a => a.SimulatorId == cbSerialNbr4.Text).Count() == 1) //remove the assignment from the list if it was existing before
                    unchangedAssignments.Remove(unchangedAssignments.Where(a => a.SimulatorId == cbSerialNbr4.Text).FirstOrDefault());
            }

            if (success)
            {
                //check if an assignment has been deleted
                if (unchangedAssignments.Count > 0) //if there are assignments left in this list it means that the user selected "Nicht verwendet" combobox item --> delete these assignments
                {
                    foreach (var assignment in unchangedAssignments)
                        SimulatorAssignmentsManager.DeleteAssignment(assignment);
                }

                MessageBox.Show("Die Simulatorzuweisungen wurden erfolgreich übernommen.", "Erfolg", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show("Fehler beim Speichern der Simulatorzuweisungen!", "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            #endregion
        }


        private void bDeleteAssignment1_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(cbSerialNbr1.Text))
            {
                MessageBox.Show("Es wurde kein Simulator ausgewählt.", "Löschen nicht möglich", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                return;
            }

            string simId = cbSerialNbr1.Text;
            var assignment = SimulatorAssignmentsManager.GetAssignmentForSimulatorById(simId);

            if (assignment != null)
            {
                if (SimulatorAssignmentsManager.DeleteAssignment(assignment))
                {
                    cbSerialNbr1.SelectedIndex = cbPosition1.SelectedIndex = cbMattress1.SelectedIndex = cbDeviceAssignment1.SelectedIndex = 0;
                    MessageBox.Show("Die Zuweisung von Simulator " + simId + " wurde erfolgreich gelöscht.", "Löschen erfolgreich", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        private void bDeleteAssignment2_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(cbSerialNbr2.Text))
            {
                MessageBox.Show("Es wurde kein Simulator ausgewählt.", "Löschen nicht möglich", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                return;
            }

            string simId = cbSerialNbr2.Text;
            var assignment = SimulatorAssignmentsManager.GetAssignmentForSimulatorById(simId);

            if (assignment != null)
            {
                if (SimulatorAssignmentsManager.DeleteAssignment(assignment))
                {
                    cbSerialNbr2.SelectedIndex = cbPosition2.SelectedIndex = cbMattress2.SelectedIndex = cbDeviceAssignment2.SelectedIndex = 0;
                    MessageBox.Show("Die Zuweisung von Simulator " + simId + " wurde erfolgreich gelöscht.", "Löschen erfolgreich", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        private void bDeleteAssignment3_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(cbSerialNbr3.Text))
            {
                MessageBox.Show("Es wurde kein Simulator ausgewählt.", "Löschen nicht möglich", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                return;
            }

            string simId = cbSerialNbr3.Text;
            var assignment = SimulatorAssignmentsManager.GetAssignmentForSimulatorById(simId);

            if (assignment != null)
            {
                if (SimulatorAssignmentsManager.DeleteAssignment(assignment))
                {
                    cbSerialNbr3.SelectedIndex = cbPosition3.SelectedIndex = cbMattress3.SelectedIndex = cbDeviceAssignment3.SelectedIndex = 0;
                    MessageBox.Show("Die Zuweisung von Simulator " + simId + " wurde erfolgreich gelöscht.", "Löschen erfolgreich", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        private void bDeleteAssignment4_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(cbSerialNbr4.Text))
            {
                MessageBox.Show("Es wurde kein Simulator ausgewählt.", "Löschen nicht möglich", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                return;
            }

            string simId = cbSerialNbr4.Text;
            var assignment = SimulatorAssignmentsManager.GetAssignmentForSimulatorById(simId);

            if (assignment != null)
            {
                if (SimulatorAssignmentsManager.DeleteAssignment(assignment))
                {
                    cbSerialNbr4.SelectedIndex = cbPosition4.SelectedIndex = cbMattress4.SelectedIndex = cbDeviceAssignment4.SelectedIndex = 0;
                    MessageBox.Show("Die Zuweisung von Simulator " + simId + " wurde erfolgreich gelöscht.", "Löschen erfolgreich", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

    }
}
