namespace SimulatorInterfaces
{
    partial class StatusDisplay
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
            this.lStatus = new System.Windows.Forms.Label();
            this.progressBar1 = new System.Windows.Forms.ProgressBar();
            this.SuspendLayout();
            // 
            // lStatus
            // 
            this.lStatus.Dock = System.Windows.Forms.DockStyle.Top;
            this.lStatus.Font = new System.Drawing.Font("Arial", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lStatus.Location = new System.Drawing.Point(0, 0);
            this.lStatus.Name = "lStatus";
            this.lStatus.Size = new System.Drawing.Size(120, 17);
            this.lStatus.TabIndex = 0;
            this.lStatus.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // progressBar1
            // 
            this.progressBar1.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.progressBar1.ForeColor = System.Drawing.Color.Blue;
            this.progressBar1.Location = new System.Drawing.Point(0, 21);
            this.progressBar1.MarqueeAnimationSpeed = 50;
            this.progressBar1.Name = "progressBar1";
            this.progressBar1.Size = new System.Drawing.Size(120, 19);
            this.progressBar1.Style = System.Windows.Forms.ProgressBarStyle.Continuous;
            this.progressBar1.TabIndex = 1;
            // 
            // StatusDisplay
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.White;
            this.ClientSize = new System.Drawing.Size(120, 40);
            this.ControlBox = false;
            this.Controls.Add(this.lStatus);
            this.Controls.Add(this.progressBar1);
            this.Cursor = System.Windows.Forms.Cursors.Default;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Fixed3D;
            this.MinimumSize = new System.Drawing.Size(130, 50);
            this.Name = "StatusDisplay";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.TopMost = true;
            this.LocationChanged += new System.EventHandler(this.StatusDisplay_LocationChanged);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Label lStatus;
        private System.Windows.Forms.ProgressBar progressBar1;
    }
}