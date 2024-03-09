using HelicalGearInspection.Classes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace HelicalGearInspection.CustomControl
{
    public partial class DefectImageControl : UserControl
    {


        List<DefectData> defects;
        public DefectImageControl(Bitmap bitmap, List<DefectData> defects)
        {
            InitializeComponent();
            pictureBox1.Image = bitmap;
            this.defects = defects;
            defectControl1.labelDefCount.Text += defects.Count.ToString();
            if (defects.Count > 0)
            {
                defectControl1.labelCameraNo.Text += defects[0].CameraNum.ToString();
                defectControl1.labelPos.Text += defects[0].Degree.ToString();
                for (int i = 0; i < defects.Count; i++)
                {
                    Label label = new Label();
                    label.AutoSize = true;
                    label.BackColor = System.Drawing.Color.MistyRose;
                    label.Location = new System.Drawing.Point(6, 8);
                    label.Padding = new System.Windows.Forms.Padding(0, 0, 5, 0);
                    label.Size = new System.Drawing.Size(51, 18);
                    label.Text = defects[i].defType;
                    defectControl1.flowLayoutPanel1.Controls.Add(label);
                }
            }
            

            //defectControl.labelCameraNo.Text = defControl.labelCameraNo.Text;
            //defectControl.labelDefType.Text = defControl.labelDefType.Text;
            //defectControl.labelPos.Text = defControl.labelPos.Text;
        }

        public DefectImageControl()
        {
            InitializeComponent();
        }


        private void DefectImageControl_Load(object sender, EventArgs e)
        {

        }

        public void AddDefectControl(DefectControl defControl)
        {
            defControl.BackColor = System.Drawing.Color.SeaShell;
            defControl.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            defControl.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            defControl.Name = "defectControl";
            defControl.Size = new System.Drawing.Size(270, 142);

            //flowLayoutPanel1.Controls.Add(defControl);
        
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            PictureBox pictureBox = (PictureBox)sender;
            Bitmap bitmap = (Bitmap)pictureBox.Image.Clone();
            Image_Data_Form image_Data_Form = new Image_Data_Form(bitmap, defects);
            image_Data_Form.ShowDialog();
        }
    }
}
