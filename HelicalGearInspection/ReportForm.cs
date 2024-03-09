using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Npgsql;
using HelicalGearInspection.Classes;
using Newtonsoft.Json;
using HelicalGearInspection.CustomControl;
using System.Windows.Forms.DataVisualization.Charting;
using SpinnakerNET.GenApi;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;

namespace HelicalGearInspection
{
    public partial class ReportForm : Form
    {
        public ReportForm()
        {
            InitializeComponent();
            comboBoxModel.LoadDirectoryNames($"{AppData.ProjectDirectory}/Models");
            dataGridView1.ReadOnly = true;
            chart1.ChartAreas[0].AxisX.LabelStyle.Angle = -90;
            chart1.ChartAreas[0].AxisX.LabelStyle.Interval = 1;
            chart1.ChartAreas[0].AxisX.MajorGrid.Enabled = false;
            chart1.ChartAreas[0].AxisX.MinorGrid.Enabled = false;
            chart1.ChartAreas[0].AxisY.MajorGrid.Enabled = false;
            chart1.ChartAreas[0].AxisY.MinorGrid.Enabled = false;
            dataGridView1.RowTemplate.Height = 40;
            dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        }


        Color[] pallete = new Color[]
      {
            Color.FromArgb(255, 89, 94),
            Color.FromArgb(255, 146, 76),
            Color.FromArgb(255, 202, 58),
            Color.FromArgb(138, 201, 38),
            Color.FromArgb(25, 130, 196),
            Color.FromArgb(106, 76, 147),
            Color.FromArgb(17, 203, 187),
            Color.FromArgb(227, 182, 133)
      };


        private void ReportForm_Load(object sender, EventArgs e)
        {
            comboBoxDefect.SelectedIndex = 0;
        }
        public DataTable GetDataByDateAndModel(DateTime date, string modelName)
        {


            using (NpgsqlConnection conn = Database.GetConnection())
            {
                conn.Open();

                string sql = "SELECT * FROM gearreport WHERE _date = @Date AND ModelName = @ModelName order by _time desc";

                using (NpgsqlCommand cmd = new NpgsqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Date", date);
                    cmd.Parameters.AddWithValue("@ModelName", modelName);

                    using (NpgsqlDataAdapter da = new NpgsqlDataAdapter(cmd))
                    {
                        DataTable dt = new DataTable();
                        da.Fill(dt);
                        return dt;
                    }
                }
            }
        }



        public DataTable FilterDataByDefType(DateTime date, string modelName)
        {

            using (NpgsqlConnection conn = Database.GetConnection())
            {
                conn.Open();

                string sql = $"SELECT * FROM gearreport WHERE _date = @Date AND ModelName = @ModelName AND (defectdetailscam1 Like '%{comboBoxDefect.SelectedItem}%' or defectdetailscam2 LIKE '%{comboBoxDefect.SelectedItem}%')";

                using (NpgsqlCommand cmd = new NpgsqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Date", date);
                    cmd.Parameters.AddWithValue("@ModelName", modelName);

                    using (NpgsqlDataAdapter da = new NpgsqlDataAdapter(cmd))
                    {
                        DataTable dt = new DataTable();
                        da.Fill(dt);
                        return dt;
                    }
                }
            }
        }


        private void buttonShowData_Click(object sender, EventArgs e)
        {
            if (checkBoxDefFilter.Checked)
            {
                dataGridView1.DataSource = FilterDataByDefType(Convert.ToDateTime(dateTimePicker1.Value.ToString("yyyy-MM-dd")), comboBoxModel.SelectedItem.ToString());

            }
            else
            {
                dataGridView1.DataSource = GetDataByDateAndModel(Convert.ToDateTime(dateTimePicker1.Value.ToString("yyyy-MM-dd")), comboBoxModel.SelectedItem.ToString());

            }

            dataGridView1.Columns[dataGridView1.ColumnCount - 1].Visible = false;
            dataGridView1.Columns[dataGridView1.ColumnCount - 2].Visible = false;
            DisplayChart(Database.GetDataForDate(dateTimePicker1.Value));


        }



        private void dataGridView1_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            flowLayoutPanel1.Controls.Clear();
            flowLayoutPanel2.Controls.Clear();
            if (dataGridView1.SelectedCells.Count > 0)
            {
                int selectedrowindex = dataGridView1.SelectedCells[0].RowIndex;
                DataGridViewRow selectedRow = dataGridView1.Rows[selectedrowindex];
                string result = Convert.ToString(selectedRow.Cells["result"].Value);
                try
                {
                    if (result == "NG")
                    {
                        ConsoleExtension.WriteWithColor(selectedRow.Cells["defectDetailsCam1"].Value.ToString(), ConsoleColor.Yellow);
                        string s1 = selectedRow.Cells["defectDetailsCam1"].Value.ToString();
                        string s2 = selectedRow.Cells["defectDetailsCam2"].Value.ToString();

                        List<List<DefectData>> defectCam1 = JsonConvert.DeserializeObject<List<List<DefectData>>>(selectedRow.Cells["defectDetailsCam1"].Value.ToString());
                        List<List<DefectData>> defectCam2 = JsonConvert.DeserializeObject<List<List<DefectData>>>(selectedRow.Cells["defectDetailsCam2"].Value.ToString());

                        int counter = 0;

                        foreach (List<DefectData> defects in defectCam1)
                        {
                            DefectControl defectControl = new DefectControl(defects, counter, true);
                            flowLayoutPanel1.Controls.Add(defectControl);
                            counter++;

                        }
                        counter = 0;

                        foreach (List<DefectData> defects in defectCam2)
                        {

                            DefectControl defectControl = new DefectControl(defects, counter, true);
                            flowLayoutPanel2.Controls.Add(defectControl);
                            counter++;

                        }
                    }

                }
                catch (Exception ex)
                {
                    ConsoleExtension.WriteWithColor(ex.Message, ConsoleColor.Red);
                }

            }
        }

        private void DisplayChart(Dictionary<string, int> defectCounts)
        {


            // Create a new chart area
            chart1.Series.Clear();
            // Create a new series for the defect counts
            Series series = new Series();
            series.ChartType = SeriesChartType.Column;
            series.IsValueShownAsLabel = true;
            series.Font = new System.Drawing.Font("Arial Narrow", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));

            // Add data points to the series
            foreach (var defectCount in defectCounts)
            {
                series.Points.AddXY(defectCount.Key, defectCount.Value);
            }
            for (int i = 0; i < series.Points.Count; i++)
            {
                series.Points[i].Color = pallete[i];
            }
            chart1.Series.Add(series);
        }


        private void buttonSaveReport_Click(object sender, EventArgs e)
        {
            CsvExporter.ExportToCsv(dataGridView1);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Database.CreateDailyExcel();

            //    // Replace with your SMTP server details
            //    string smtpServer = "172.16.12.168";
            //    int smtpPort = 25;
            //    string smtpUsername = "notifications.msr@sonacomstar.com";
            //    //string smtpPassword = "ezhi kehi fomn axkz";
            //    string smtpPassword = "@#Alerts*5373";

            //    EmailSender emailSender = new EmailSender(smtpServer, smtpPort, smtpUsername, smtpPassword);

            //    string toEmail = "gaurnikunj116@gmail.com";
            //    string subject = "Test Email With Attachment";
            //    string body = "Hello, this mail has been sent from my program.";
            //    //emailSender.SendEmail(toEmail, subject, body);
            //    //emailSender.Send();


            //string server = "172.16.12.168";
            //int port = 25;
            //string username = "notifications.msr@sonacomstar.com";
            //string password = "@#Alerts*5373";
            //string recipientEmail = "gaurnikunj116@gmail.com";

            //EmailSender.SendEmail(server, port, username, password, recipientEmail);

        }
    }
}
