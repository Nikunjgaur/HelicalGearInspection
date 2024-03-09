namespace HelicalGearInspection
{
    partial class Selection_Model_Form
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
            this.CbBoxModel = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.GrpBoxSelectModel = new System.Windows.Forms.GroupBox();
            this.GrpBoxSelectModel.SuspendLayout();
            this.SuspendLayout();
            // 
            // CbBoxModel
            // 
            this.CbBoxModel.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.CbBoxModel.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.CbBoxModel.FormattingEnabled = true;
            this.CbBoxModel.Items.AddRange(new object[] {
            "Without_Grinding",
            "Root_Grinding",
            "Step_Grinding",
            "Flank_Unclean",
            "Without_Chamfer",
            "Semitoping_NG",
            "Handling_Dent",
            "Rust",
            "Surface_Defect",
            "Default",
            "Root_Rust",
            "Handling_Dent",
            "Heat_Treatment_Dent",
            "Flank_Dent",
            "Chatering_Zig_Zag",
            "Black_Mark_On_Flank"});
            this.CbBoxModel.Location = new System.Drawing.Point(111, 41);
            this.CbBoxModel.Name = "CbBoxModel";
            this.CbBoxModel.Size = new System.Drawing.Size(241, 28);
            this.CbBoxModel.TabIndex = 0;
            this.CbBoxModel.SelectedIndexChanged += new System.EventHandler(this.CbBoxModel_SelectedIndexChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(29, 44);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(64, 20);
            this.label1.TabIndex = 1;
            this.label1.Text = " Model :";
            // 
            // GrpBoxSelectModel
            // 
            this.GrpBoxSelectModel.Controls.Add(this.label1);
            this.GrpBoxSelectModel.Controls.Add(this.CbBoxModel);
            this.GrpBoxSelectModel.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.GrpBoxSelectModel.Location = new System.Drawing.Point(12, 12);
            this.GrpBoxSelectModel.Name = "GrpBoxSelectModel";
            this.GrpBoxSelectModel.Size = new System.Drawing.Size(395, 105);
            this.GrpBoxSelectModel.TabIndex = 2;
            this.GrpBoxSelectModel.TabStop = false;
            this.GrpBoxSelectModel.Text = "SELECT MODEL";
            // 
            // Selection_Model_Form
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(428, 140);
            this.Controls.Add(this.GrpBoxSelectModel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Name = "Selection_Model_Form";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Selection_Model_Form";
            this.Load += new System.EventHandler(this.Selection_Model_Form_Load);
            this.GrpBoxSelectModel.ResumeLayout(false);
            this.GrpBoxSelectModel.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ComboBox CbBoxModel;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.GroupBox GrpBoxSelectModel;
    }
}