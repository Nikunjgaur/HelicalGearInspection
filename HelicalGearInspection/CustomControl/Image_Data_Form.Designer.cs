
namespace HelicalGearInspection
{
    partial class Image_Data_Form
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
            this.pnl_img = new System.Windows.Forms.Panel();
            this.pb_img = new System.Windows.Forms.PictureBox();
            this.grpbox_img = new System.Windows.Forms.GroupBox();
            this.defectControl1 = new HelicalGearInspection.CustomControl.DefectControl();
            this.pnl_img.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pb_img)).BeginInit();
            this.grpbox_img.SuspendLayout();
            this.SuspendLayout();
            // 
            // pnl_img
            // 
            this.pnl_img.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pnl_img.Controls.Add(this.pb_img);
            this.pnl_img.Location = new System.Drawing.Point(6, 26);
            this.pnl_img.Name = "pnl_img";
            this.pnl_img.Size = new System.Drawing.Size(962, 722);
            this.pnl_img.TabIndex = 0;
            // 
            // pb_img
            // 
            this.pb_img.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pb_img.Location = new System.Drawing.Point(2, 2);
            this.pb_img.Name = "pb_img";
            this.pb_img.Size = new System.Drawing.Size(953, 712);
            this.pb_img.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.pb_img.TabIndex = 0;
            this.pb_img.TabStop = false;
            this.pb_img.MouseDown += new System.Windows.Forms.MouseEventHandler(this.pb_img_MouseDown);
            this.pb_img.MouseMove += new System.Windows.Forms.MouseEventHandler(this.pb_img_MouseMove);
            // 
            // grpbox_img
            // 
            this.grpbox_img.Controls.Add(this.defectControl1);
            this.grpbox_img.Controls.Add(this.pnl_img);
            this.grpbox_img.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.grpbox_img.Location = new System.Drawing.Point(3, 2);
            this.grpbox_img.Name = "grpbox_img";
            this.grpbox_img.Size = new System.Drawing.Size(998, 928);
            this.grpbox_img.TabIndex = 1;
            this.grpbox_img.TabStop = false;
            this.grpbox_img.Text = "Image";
            // 
            // defectControl1
            // 
            this.defectControl1.BackColor = System.Drawing.SystemColors.ActiveCaption;
            this.defectControl1.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.defectControl1.Location = new System.Drawing.Point(6, 755);
            this.defectControl1.Margin = new System.Windows.Forms.Padding(4);
            this.defectControl1.Name = "defectControl1";
            this.defectControl1.Size = new System.Drawing.Size(270, 161);
            this.defectControl1.TabIndex = 1;
            // 
            // Image_Data_Form
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1016, 938);
            this.Controls.Add(this.grpbox_img);
            this.Name = "Image_Data_Form";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Image_Data_Form";
            this.pnl_img.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.pb_img)).EndInit();
            this.grpbox_img.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel pnl_img;
        private System.Windows.Forms.PictureBox pb_img;
        private System.Windows.Forms.GroupBox grpbox_img;
        private CustomControl.DefectControl defectControl1;
    }
}