
using SimulatorInterfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace SimulatorController
{

    /// <summary>
    /// Represents an assignment for a simulator. A simulator can only have one assignment.
    /// </summary>
    public class SimulatorAssignment
    {
        /// <summary>
        /// The serial number of the simulator device.
        /// </summary>
        public string SimulatorId { get; set; }

        public SimulatorAssignmentsManager.SimulatorAssignments Assignment { get; set; }

        public SimulatorAssignmentsManager.SimulatorPositions Position { get; set; }

        public SimulatorAssignmentsManager.SimulationMattresses Mattress { get; set; }

        public string CustomAssignmentName { get; set; }

        private SimulatorAssignment() { } //prevent any incomplete objects

        public SimulatorAssignment(string simulatorId, SimulatorAssignmentsManager.SimulatorAssignments assignment, SimulatorAssignmentsManager.SimulatorPositions position, SimulatorAssignmentsManager.SimulationMattresses mattress, string customName)
        {
            this.SimulatorId = simulatorId;
            this.Assignment = assignment;
            this.Position = position;
            this.Mattress = mattress;
            this.CustomAssignmentName = customName;
        }
    }

    /// <summary>
    /// Manages all settings and data concerning the multiple simulators program mode.
    /// The assignments are saved in the "simulatorAssignments.csv" file.
    /// </summary>
    public static class SimulatorAssignmentsManager
    {
        #region Vars
        private static string myPathToSimulatorAssignments = null;

        private static List<SimulatorAssignment> simulatorAssignments = null; //a list that holds all known simulator assignments

        const string assignmentsFileHeader = "simulatorID;simulatorAssignments;simulatorPositions;simulationMattresses;customName";
        #endregion

        #region Enums
        /// <summary>
        /// All available assignments to which a simulator device can be assigned to. In single-simulator-mode, "Nicht zugewiesen" can be used to get the USB-simulator object.
        /// </summary>
        public enum SimulatorAssignments { Nicht_zugewiesen, Druckmessung, Simulation, Druckmessung_und_Simulation_Kaltschaum, Druckmessung_und_Simulation_NaturLatex };

        public enum SimulatorPositions { Nicht_zugewiesen, Simulator1, Simulator2, Simulator3, Simulator4 };

        public enum SimulationMattresses { Nicht_zugewiesen, Vitario_Comfort, Vitario_Premium, Vitario_Serie_N };
        #endregion

        /// <summary>
        /// Must be called before the data provided by this class can be used.
        /// </summary>
        /// <param name="pathToSimulatorAssignments"></param>
        public static Exception Initialize(string pathToSimulatorAssignments)
        {
            myPathToSimulatorAssignments = pathToSimulatorAssignments;
            return LoadAssignments();
        }

        /// <summary>
        /// Returns a list of all stored simulator assignments.
        /// </summary>
        /// <returns></returns>
        public static List<SimulatorAssignment> GetSimulatorAssignments()
        {
            return new List<SimulatorAssignment>(simulatorAssignments);
        }

        /// <summary>
        /// Loads all known assignments from the *csv file and adds them to the assignedSimulators-dictionary.
        /// </summary>
        private static Exception LoadAssignments()
        {
            if (simulatorAssignments == null)
            {
                #region Load assignments
                simulatorAssignments = new List<SimulatorAssignment>();

                Exception ex;
                DataTable dtAssignments = Logging.CsvHandler.ReadCSVFile(myPathToSimulatorAssignments, out ex);

                if (dtAssignments == null || ex != null)
                    return new Exception("Interner Fehler beim Laden der Simulatorzuweisungen.");

                if ((string)dtAssignments.Columns[1].ColumnName != "simulatorAssignments" || dtAssignments.Columns.Count != assignmentsFileHeader.Split(';').Count()) //the file is outdated --> delete all assignments and notify the user that they have to be made again
                {
                    ResetAssignments();
                    simulatorAssignments.Clear();
                }

                try
                {
                    foreach (DataRow row in dtAssignments.Rows) //parse all assignments (there can be only one assignment per simulator id)
                    {
                        string simId = row[0].ToString();
                        SimulatorAssignmentsManager.SimulatorAssignments assignment = (SimulatorAssignmentsManager.SimulatorAssignments)Enum.Parse(typeof(SimulatorAssignmentsManager.SimulatorAssignments), row[1].ToString());
                        SimulatorAssignmentsManager.SimulatorPositions position = (SimulatorAssignmentsManager.SimulatorPositions)Enum.Parse(typeof(SimulatorAssignmentsManager.SimulatorPositions), row[2].ToString());
                        SimulatorAssignmentsManager.SimulationMattresses mattress = (SimulatorAssignmentsManager.SimulationMattresses)Enum.Parse(typeof(SimulatorAssignmentsManager.SimulationMattresses), row[3].ToString());
                        string customName = row[4].ToString();

                        if (simulatorAssignments.Where(a => a.SimulatorId == simId).Count() > 0)
                            return new Exception("Fehler beim Lesen der Simulatorzuweisungen: der Simulator mit der Seriennummer " + simId + " kommt mehrfach vor!");

                        SimulatorAssignment newAssignment = new SimulatorAssignment(simId, assignment, position, mattress, customName);
                        simulatorAssignments.Add(newAssignment);
                    }
                }
                catch (Exception exc)
                {
                    simulatorAssignments.Clear();
                    ResetAssignments();
                    return exc;
                }
                #endregion
            }

            return null;
        }

        private static void ResetAssignments()
        {
            try
            {
                File.Delete(myPathToSimulatorAssignments);

                //create a file and add the current header to it
                using (StreamWriter sw = File.CreateText(myPathToSimulatorAssignments))
                {
                    sw.WriteLine(assignmentsFileHeader);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Fehler beim Updaten der Simulatorzuweisungen:" + Environment.NewLine + ex.ToString(), "Interner Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                MessageBox.Show("Die Simulatorzuweisungen wurden zurückgesetzt, da diese Version nicht mit älteren Versionen kompatibel ist. Bitte führen Sie die Zuweisung erneut durch!", "Simulatorzuweisungen zurückgesetzt", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        /// <summary>
        /// Returns the corresponding simulator object for the specified assignment, position and mattress type. The last two parameters can be ommited.
        /// Special case: if the required assignment is "Simulation" and no object can be found, the assignment "Druckmessung_und_Simulation" is also checked.
        /// </summary>
        /// <returns>The correct simulator object or null if no simulator was specified for the assignment.</returns>
        public static CBaseSimulator GetSimulatorObject(SimulatorAssignments assignment, SimulatorPositions position = (SimulatorPositions)(-1), SimulationMattresses mattress = (SimulationMattresses)(-1)) //optional parameters
        {
            //get the id of the simulator that is assigned to the specified assignment
            string id = "";
            SimulatorAssignment existingAssignment = null;

            QueryStart:
            var result = simulatorAssignments.Where(a => a.Assignment == assignment);

            if ((int)position > -1)
                result = result.Where(r => r.Position == position);

            if ((int)mattress > -1)
                result = result.Where(r => r.Mattress == mattress);

            existingAssignment = result.FirstOrDefault();

            if (existingAssignment != null)
                id = existingAssignment.SimulatorId;
            else if (assignment == SimulatorAssignments.Simulation)
            {
                assignment = SimulatorAssignments.Druckmessung_und_Simulation_Kaltschaum; //re-attempt the query with a slightly different assignment that includes the "Simulation" assignment
                goto QueryStart;
            }
            else if (assignment == SimulatorAssignments.Druckmessung_und_Simulation_Kaltschaum)
            {
                assignment = SimulatorAssignments.Druckmessung_und_Simulation_NaturLatex; //re-attempt the query with a slightly different assignment that also includes the "Simulation" assignment
                goto QueryStart;
            }

            if (string.IsNullOrEmpty(id)) //no simulator available
                return null;

            return SimulatorController.MultiSimulatorMode.SimulatorControl.Instance.GetSimulatorById(id);
        }

        /// <summary>
        /// Returns the assignment object for the specified simulator or null.
        /// </summary>
        /// <param name="simulatorId"></param>
        /// <returns></returns>
        public static SimulatorAssignment GetAssignmentForSimulatorById(string simulatorId)
        {
            return simulatorAssignments.Where(a => a.SimulatorId == simulatorId).FirstOrDefault();
        }

        public static string GetCustomNameForSimulatorPosition(SimulatorPositions position)
        {
            var result = simulatorAssignments.Where(a => a.Position == position);

            if (result.FirstOrDefault() != null)
                return result.FirstOrDefault().CustomAssignmentName.ToString();

            return "Kein Simulator zugewiesen";
        }

        /// <summary>
        /// Adds a simulator assignment and saves it in the simulatorAssignments.csv file.
        /// There can be multiple assignments with the same simulator, but an assignment cannot have multiple simulators assigned to it.
        /// </summary>
        /// <param name="assignmentName">The name of the form to where the simulator is assigned to.</param>
        /// <param name="simulatorID">The hardware ID of the simulator.</param>
        /// <returns>True if the adding was successful (or if the assignment was already existing); false otherwise.</returns>
        public static bool AddOrUpdateAssignment(SimulatorAssignment assignment)
        {
            if (simulatorAssignments.Count(a => a.SimulatorId == assignment.SimulatorId) != 0) //update existing assigment (remove the old one and add the new one)
                simulatorAssignments.Remove(simulatorAssignments.Where(a => a.SimulatorId == assignment.SimulatorId).FirstOrDefault());

            simulatorAssignments.Add(assignment);

            //load and update the csv file
            Exception ex;
            DataTable dtAssignments = Logging.CsvHandler.ReadCSVFile(myPathToSimulatorAssignments, out ex);

            if (dtAssignments == null || ex != null)
            {
                MessageBox.Show("Interner Fehler beim Speichern der Simulatorzuweisungen. Die Zuweisungen werden nicht angewendet.", "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            dtAssignments.PrimaryKey = new DataColumn[] { dtAssignments.Columns[0] }; //set a primary key on the "simulatorId" column for easier searching

            //remove existing row if possible
            DataRow existingRow = dtAssignments.Rows.Find(assignment.SimulatorId);

            if (existingRow != null)
                dtAssignments.Rows.Remove(existingRow);

            //add a new row
            DataRow newRow = dtAssignments.NewRow();
            newRow[0] = assignment.SimulatorId;
            newRow[1] = assignment.Assignment;
            newRow[2] = assignment.Position;
            newRow[3] = assignment.Mattress;
            newRow[4] = assignment.CustomAssignmentName;
            dtAssignments.Rows.Add(newRow);

            return Logging.CsvHandler.WriteDataTable(dtAssignments, ';', myPathToSimulatorAssignments) == null;
        }

        /// <summary>
        /// Deletes an existing simulator assignment and saves the changes to the simulatorAssignments.csv file.
        /// </summary>
        /// <returns>True if deletion was successful (or if the specified assignment wasn't found), false otherwise.</returns>
        public static bool DeleteAssignment(SimulatorAssignment assignment)
        {
            SimulatorAssignment existingAssignment = simulatorAssignments.Where(a => a.SimulatorId == assignment.SimulatorId).FirstOrDefault();

            if (existingAssignment == null)
                return true;

            simulatorAssignments.Remove(existingAssignment);

            //load and update the csv file
            Exception ex;
            DataTable dtAssignments = Logging.CsvHandler.ReadCSVFile(myPathToSimulatorAssignments, out ex);

            if (dtAssignments == null || ex != null)
            {
                MessageBox.Show("Interner Fehler beim Löschen der Simulatorzuweisung. Die Zuweisungen werden nicht angewendet.", "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            dtAssignments.PrimaryKey = new DataColumn[] { dtAssignments.Columns[0] }; //set a primary key on the "simulatorId" column for easier searching

            //remove existing row if possible
            DataRow existingRow = dtAssignments.Rows.Find(assignment.SimulatorId);

            if (existingRow != null)
            {
                dtAssignments.Rows.Remove(existingRow);
                return Logging.CsvHandler.WriteDataTable(dtAssignments, ';', myPathToSimulatorAssignments) == null;
            }

            return true;
        }
    }
}
