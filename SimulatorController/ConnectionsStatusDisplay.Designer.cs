namespace SimulatorController
{
    partial class ConnectionsStatusDisplay
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
            this.bShowSomething = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // bShowSomething
            // 
            this.bShowSomething.Dock = System.Windows.Forms.DockStyle.Fill;
            this.bShowSomething.Font = new System.Drawing.Font("Arial Narrow", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.bShowSomething.Location = new System.Drawing.Point(0, 0);
            this.bShowSomething.Name = "bShowSomething";
            this.bShowSomething.Size = new System.Drawing.Size(120, 40);
            this.bShowSomething.TabIndex = 0;
            this.bShowSomething.Text = "Verbindungen anzeigen";
            this.bShowSomething.UseVisualStyleBackColor = true;
            this.bShowSomething.Click += new System.EventHandler(this.bShowSomething_Click);
            // 
            // ConnectionsStatusDisplay
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.BackColor = System.Drawing.Color.White;
            this.ClientSize = new System.Drawing.Size(120, 40);
            this.ControlBox = false;
            this.Controls.Add(this.bShowSomething);
            this.Cursor = System.Windows.Forms.Cursors.Default;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Fixed3D;
            this.MinimumSize = new System.Drawing.Size(130, 50);
            this.Name = "ConnectionsStatusDisplay";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.TopMost = true;
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button bShowSomething;

    }
}