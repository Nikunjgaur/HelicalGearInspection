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
    public partial class DefectControl : UserControl
    {
        public DefectControl(DefectData defectData, int index = 0)
        {
            InitializeComponent();
            labelCameraNo.Text = $"Camera num: {defectData.CameraNum}";
            labelDefType.Text = $"Defect Type: {defectData.defType}";
            labelPos.Text = $"Degree: {defectData.Degree}";

            if ((index % 2) !=0)
            {
                this.BackColor = Color.WhiteSmoke;
            }

        }


        public DefectControl(List<DefectData> defects, int index = 0, bool reportMode = false)
        {
            InitializeComponent();
            if (reportMode)
            {
                labelDefCount.Visible = false;
                labelCameraNo.Visible = false;
                labelPos.Location = labelCameraNo.Location;
                labelDefType.Location = labelDefCount.Location;
                flowLayoutPanel1.Location = new Point(118, 32);
                this.Size = new Size(270, 109);

            }
            labelDefCount.Text += defects.Count.ToString();
            if (defects.Count > 0)
            {
                labelCameraNo.Text += defects[0].CameraNum.ToString();
                labelPos.Text += defects[0].Degree.ToString();
                for (int i = 0; i < defects.Count; i++)
                {
                    Label label = new Label();
                    label.AutoSize = true;
                    label.BackColor = System.Drawing.Color.MistyRose;
                    label.Location = new System.Drawing.Point(6, 8);
                    label.Padding = new System.Windows.Forms.Padding(0, 0, 5, 0);
                    label.Size = new System.Drawing.Size(51, 18);
                    label.Text = defects[i].defType;
                    flowLayoutPanel1.Controls.Add(label);
                }
            }
            if ((index % 2) != 0)
            {
                this.BackColor = Color.WhiteSmoke;
            }
        }

        public DefectControl()
        {
            InitializeComponent();
        }

        private void DefectControl_Load(object sender, EventArgs e)
        {

        }
    }
}
