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
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Button;

namespace HelicalGearInspection
{
    public partial class Image_Data_Form : Form
    {
        public int cblWidth = 0;
        public int cblHeight = 0;
        public int CheckX = 0;
        public int CheckY = 0;
        public int m_x = 0;
        public int m_y = 0;

        //-----------Picture box move------variable ----//
        private Point StartPoint;
        public int cbl_x = 0;
        public int cbl_y = 0;
        //-----------Picture box move------variable ----//


        public Image_Data_Form(Bitmap bitmap, List<DefectData> defects)
        {
            InitializeComponent();
            this.pnl_img.MouseWheel += pnl_img_MouseWheel;
           this.pb_img.Image = bitmap;
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
        }

        private void pnl_img_MouseWheel(object sender, MouseEventArgs e)
        {
            if (e.Delta >= 0)
            {

                if (pb_img.Width < (15 * this.Width) && (pb_img.Height < (15 * this.Height)))
                {
                    Console.WriteLine("Zoom in Delta Value" + e.Delta);
                    //Change pictureBox Size and Multiply Zoomfactor
                    pb_img.Width = (int)(pb_img.Width * 1.25);
                    pb_img.Height = (int)(pb_img.Height * 1.25);

                    pb_img.Top = (int)(e.Y - 1.25 * (e.Y - pb_img.Top));
                    pb_img.Left = (int)(e.X - 1.25 * (e.X - pb_img.Left));

                }
                this.Refresh();
            }
            else if ((pb_img.Width > pnl_img.Width) && (pb_img.Height > pnl_img.Height))
            {

                CheckY = (int)(e.Y - 0.80 * (e.Y - pb_img.Top));
                CheckX = (int)(e.X - 0.80 * (e.X - pb_img.Left));


                cblWidth = (int)(pb_img.Width / 1.25);
                cblHeight = (int)(pb_img.Height / 1.25);
                m_x = cblWidth - pnl_img.Width;
                m_y = cblHeight - pnl_img.Height;

                pb_img.Invalidate();

                //-------------Main Code---------------- -   //1068, 892
                if ((CheckY < 2) && (CheckY > -m_y) && (CheckX < 2) && (CheckX > -m_x) && (cblWidth > 1068) && (cblHeight > 892))
                {
                    Console.WriteLine("After zoom out mx value =" + m_x);
                    pb_img.Top = CheckY;
                    pb_img.Left = CheckX;
                    pb_img.Width = cblWidth;
                    pb_img.Height = cblHeight;
                    pb_img.Invalidate();

                }
                else
                {
                    pb_img.Top = 2;
                    pb_img.Left = 2;
                    pb_img.Width = 1068;
                    pb_img.Height = 892;
                    pb_img.Invalidate();
                }
                this.Refresh();
            }
        }

        private void Image_Data_Form_Load(object sender, EventArgs e)
        {
            
        }

        private void pb_img_MouseDown(object sender, MouseEventArgs e)
        {
            if (pb_img.Image != null)
            {

                if (e.Button == System.Windows.Forms.MouseButtons.Left)
                {
                    StartPoint = new Point(e.X, e.Y);
                }

            }
            else
            {
                MessageBox.Show("Image Not Fount");
            }
        }

        private void pb_img_MouseMove(object sender, MouseEventArgs e)
        {
            if ((pb_img.Width > pnl_img.Width) && (pb_img.Height > pnl_img.Height))
            {
                m_x = pb_img.Width - pnl_img.Width;
                m_y = pb_img.Height - pnl_img.Height;
                if (e.Button == System.Windows.Forms.MouseButtons.Left)
                {
                    cbl_x = e.X + (pb_img.Left - StartPoint.X);
                    cbl_y = e.Y + (pb_img.Top - StartPoint.Y);
                    if ((cbl_x < 1) && (cbl_x > -m_x) && (cbl_y < 1) && (cbl_y > -m_y))
                    {
                        pb_img.Left = cbl_x;
                        pb_img.Top = cbl_y;

                    }
                    pb_img.Refresh();

                }
            }
        }
    }
}
