namespace SimulatorInterfaces
{
    partial class ActivitiesStatusDisplay
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
            this.lHeading = new System.Windows.Forms.Label();
            this.bBack = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // lHeading
            // 
            this.lHeading.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.lHeading.AutoSize = true;
            this.lHeading.Font = new System.Drawing.Font("Microsoft Sans Serif", 15.75F, System.Drawing.FontStyle.Underline, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lHeading.Location = new System.Drawing.Point(171, 8);
            this.lHeading.Name = "lHeading";
            this.lHeading.Size = new System.Drawing.Size(208, 25);
            this.lHeading.TabIndex = 0;
            this.lHeading.Text = "Derzeitige Vorgänge";
            // 
            // bBack
            // 
            this.bBack.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.bBack.Location = new System.Drawing.Point(232, 210);
            this.bBack.Name = "bBack";
            this.bBack.Size = new System.Drawing.Size(85, 30);
            this.bBack.TabIndex = 1;
            this.bBack.Text = "Zurück";
            this.bBack.UseVisualStyleBackColor = true;
            this.bBack.Click += new System.EventHandler(this.bBack_Click);
            // 
            // ActivitiesStatusDisplay
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.ClientSize = new System.Drawing.Size(548, 248);
            this.ControlBox = false;
            this.Controls.Add(this.bBack);
            this.Controls.Add(this.lHeading);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Name = "ActivitiesStatusDisplay";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lHeading;
        private System.Windows.Forms.Button bBack;

    }
}