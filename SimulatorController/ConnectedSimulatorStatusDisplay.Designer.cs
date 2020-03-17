namespace SimulatorController
{
    partial class ConnectedSimulatorStatusDisplay
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
            this.bClose = new System.Windows.Forms.Button();
            this.dataGridViewOverview = new System.Windows.Forms.DataGridView();
            this.progressBarSearchRunning = new System.Windows.Forms.ProgressBar();
            this.lProgressBarSearchRunning = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewOverview)).BeginInit();
            this.SuspendLayout();
            // 
            // bClose
            // 
            this.bClose.Location = new System.Drawing.Point(236, 221);
            this.bClose.Name = "bClose";
            this.bClose.Size = new System.Drawing.Size(112, 32);
            this.bClose.TabIndex = 0;
            this.bClose.Text = "Schließen";
            this.bClose.UseVisualStyleBackColor = true;
            this.bClose.Click += new System.EventHandler(this.bClose_Click);
            // 
            // dataGridViewOverview
            // 
            this.dataGridViewOverview.AllowUserToAddRows = false;
            this.dataGridViewOverview.AllowUserToDeleteRows = false;
            this.dataGridViewOverview.AllowUserToOrderColumns = true;
            this.dataGridViewOverview.AutoSizeRowsMode = System.Windows.Forms.DataGridViewAutoSizeRowsMode.AllCells;
            this.dataGridViewOverview.BackgroundColor = System.Drawing.SystemColors.Control;
            this.dataGridViewOverview.CellBorderStyle = System.Windows.Forms.DataGridViewCellBorderStyle.SunkenHorizontal;
            this.dataGridViewOverview.ColumnHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.Single;
            dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle2.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle2.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle2.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle2.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle2.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle2.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.dataGridViewOverview.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle2;
            this.dataGridViewOverview.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridViewOverview.Enabled = false;
            this.dataGridViewOverview.GridColor = System.Drawing.Color.Black;
            this.dataGridViewOverview.Location = new System.Drawing.Point(12, 65);
            this.dataGridViewOverview.MultiSelect = false;
            this.dataGridViewOverview.Name = "dataGridViewOverview";
            this.dataGridViewOverview.ReadOnly = true;
            this.dataGridViewOverview.RowHeadersVisible = false;
            this.dataGridViewOverview.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.CellSelect;
            this.dataGridViewOverview.Size = new System.Drawing.Size(560, 136);
            this.dataGridViewOverview.TabIndex = 5;
            // 
            // progressBarSearchRunning
            // 
            this.progressBarSearchRunning.Location = new System.Drawing.Point(218, 10);
            this.progressBarSearchRunning.MarqueeAnimationSpeed = 50;
            this.progressBarSearchRunning.Name = "progressBarSearchRunning";
            this.progressBarSearchRunning.Size = new System.Drawing.Size(150, 25);
            this.progressBarSearchRunning.Style = System.Windows.Forms.ProgressBarStyle.Marquee;
            this.progressBarSearchRunning.TabIndex = 6;
            this.progressBarSearchRunning.Value = 100;
            // 
            // lProgressBarSearchRunning
            // 
            this.lProgressBarSearchRunning.AutoSize = true;
            this.lProgressBarSearchRunning.Location = new System.Drawing.Point(255, 38);
            this.lProgressBarSearchRunning.MinimumSize = new System.Drawing.Size(76, 12);
            this.lProgressBarSearchRunning.Name = "lProgressBarSearchRunning";
            this.lProgressBarSearchRunning.Size = new System.Drawing.Size(76, 13);
            this.lProgressBarSearchRunning.TabIndex = 7;
            this.lProgressBarSearchRunning.Text = "Suche läuft...";
            this.lProgressBarSearchRunning.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // ConnectedSimulatorStatusDisplay
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.ClientSize = new System.Drawing.Size(584, 261);
            this.ControlBox = false;
            this.Controls.Add(this.lProgressBarSearchRunning);
            this.Controls.Add(this.progressBarSearchRunning);
            this.Controls.Add(this.bClose);
            this.Controls.Add(this.dataGridViewOverview);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.MaximumSize = new System.Drawing.Size(600, 300);
            this.MinimumSize = new System.Drawing.Size(600, 300);
            this.Name = "ConnectedSimulatorStatusDisplay";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Verbindungsübersicht";
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewOverview)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button bClose;
        private System.Windows.Forms.DataGridView dataGridViewOverview;
        private System.Windows.Forms.ProgressBar progressBarSearchRunning;
        private System.Windows.Forms.Label lProgressBarSearchRunning;
    }
}