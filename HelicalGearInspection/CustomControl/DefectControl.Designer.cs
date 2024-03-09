namespace HelicalGearInspection.CustomControl
{
    partial class DefectControl
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.labelCameraNo = new System.Windows.Forms.Label();
            this.labelPos = new System.Windows.Forms.Label();
            this.labelDefType = new System.Windows.Forms.Label();
            this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
            this.labelDefCount = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // labelCameraNo
            // 
            this.labelCameraNo.AutoSize = true;
            this.labelCameraNo.Location = new System.Drawing.Point(18, 12);
            this.labelCameraNo.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.labelCameraNo.Name = "labelCameraNo";
            this.labelCameraNo.Size = new System.Drawing.Size(93, 18);
            this.labelCameraNo.TabIndex = 0;
            this.labelCameraNo.Text = "Camera No. ";
            // 
            // labelPos
            // 
            this.labelPos.AutoSize = true;
            this.labelPos.Location = new System.Drawing.Point(18, 72);
            this.labelPos.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.labelPos.Name = "labelPos";
            this.labelPos.Size = new System.Drawing.Size(134, 18);
            this.labelPos.TabIndex = 1;
            this.labelPos.Text = "Position in degree: ";
            // 
            // labelDefType
            // 
            this.labelDefType.AutoSize = true;
            this.labelDefType.Location = new System.Drawing.Point(18, 112);
            this.labelDefType.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.labelDefType.Name = "labelDefType";
            this.labelDefType.Size = new System.Drawing.Size(90, 18);
            this.labelDefType.TabIndex = 1;
            this.labelDefType.Text = "Defect type :";
            // 
            // flowLayoutPanel1
            // 
            this.flowLayoutPanel1.AutoScroll = true;
            this.flowLayoutPanel1.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.flowLayoutPanel1.BackColor = System.Drawing.Color.White;
            this.flowLayoutPanel1.Location = new System.Drawing.Point(115, 102);
            this.flowLayoutPanel1.Name = "flowLayoutPanel1";
            this.flowLayoutPanel1.Padding = new System.Windows.Forms.Padding(3, 8, 0, 0);
            this.flowLayoutPanel1.Size = new System.Drawing.Size(152, 46);
            this.flowLayoutPanel1.TabIndex = 2;
            this.flowLayoutPanel1.WrapContents = false;
            // 
            // labelDefCount
            // 
            this.labelDefCount.AutoSize = true;
            this.labelDefCount.Location = new System.Drawing.Point(18, 42);
            this.labelDefCount.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.labelDefCount.Name = "labelDefCount";
            this.labelDefCount.Size = new System.Drawing.Size(100, 18);
            this.labelDefCount.TabIndex = 1;
            this.labelDefCount.Text = "Defect count: ";
            // 
            // DefectControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 18F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.flowLayoutPanel1);
            this.Controls.Add(this.labelDefType);
            this.Controls.Add(this.labelDefCount);
            this.Controls.Add(this.labelPos);
            this.Controls.Add(this.labelCameraNo);
            this.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Margin = new System.Windows.Forms.Padding(4);
            this.Name = "DefectControl";
            this.Size = new System.Drawing.Size(270, 161);
            this.Load += new System.EventHandler(this.DefectControl_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        public System.Windows.Forms.Label labelCameraNo;
        public System.Windows.Forms.Label labelPos;
        public System.Windows.Forms.Label labelDefType;
        public System.Windows.Forms.FlowLayoutPanel flowLayoutPanel1;
        public System.Windows.Forms.Label labelDefCount;
    }
}
