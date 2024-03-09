using HelicalGearInspection.Classes;
using HelicalGearInspection.CustomControl;
using HelicalGearInspection.Properties;
using Modbus;
using Newtonsoft.Json;
using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using static HelicalGearInspection.AppData;
using static System.Windows.Forms.AxHost;
using Timer = System.Windows.Forms.Timer;

namespace HelicalGearInspection
{
    public partial class InspectionForm : Form
    {

        int originalExStyle = -1;
        bool enableFormLevelDoubleBuffering = true;

        protected override CreateParams CreateParams
        {
            get
            {
                if (originalExStyle == -1)
                    originalExStyle = base.CreateParams.ExStyle;

                CreateParams cp = base.CreateParams;
                if (enableFormLevelDoubleBuffering)
                    cp.ExStyle |= 0x02000000;   // WS_EX_COMPOSITED
                else
                    cp.ExStyle = originalExStyle;

                return cp;
            }
        }


        private void TurnOffFormLevelDoubleBuffering()
        {
            enableFormLevelDoubleBuffering = false;
            this.MaximizeBox = true;
        }

        public InspectionForm()
        {
            InitializeComponent();

            richTextBox1.SelectionBullet = true;
            comboBoxModel.LoadDirectoryNames($"{AppData.ProjectDirectory}/Models");
            chart1.ChartAreas[0].AxisX.MajorTickMark.Enabled = false;
            chart1.ChartAreas[0].AxisX.MajorGrid.Enabled = false;
            chart1.ChartAreas[0].AxisY.Enabled = AxisEnabled.False;
            chart1.ChartAreas[0].AxisX.MinorGrid.Enabled = false;
            chart1.ChartAreas[0].AxisY.MinorGrid.Enabled = false;
            AppData.AppMode = AppData.Mode.Idol;

            PartInspectionData.TotalNgParts = Settings.Default.TotalNg;
            PartInspectionData.TotalOkParts = Settings.Default.TotalOk;
            PartInspectionData.PartsInspected = Settings.Default.TotalInspected;


        }

        SpinCamManager camera1 = new SpinCamManager();
        SpinCamManager camera2 = new SpinCamManager();
        PartInspectionData inspectionData1 = new PartInspectionData();
        PartInspectionData inspectionData2 = new PartInspectionData();
        StatusControl statusControlSonaServer;
        StatusControl statusControlApi1 = null;
        StatusControl statusControlApi2 = null;
        private void InspectionForm_Load(object sender, EventArgs e)
        {
            PLCControl.connectToPLC();
            AppData.ThresholdValue = Settings.Default.ThreshValue;

            camera1.SerialNumber = "23335134";
            camera2.SerialNumber = "23335128";
            PartInspectionData.OnPartInspected += PartInspectionData_OnPartInspected;
            bool camStatus1 = camera1.ConnectCamera();
            if (camStatus1)
            {
                camera1.ConfigureExposure(camera1.exposure);
                camera1.ConfigureTrigger(SpinCamManager.chosenTrigger);
                Thread.Sleep(1000);
                camera1.myImageEventListener.BitmapRecievedEvent += MyImageEventListener_BitmapRecievedEvent;
            }

            StatusControl statusControlCam1 = new StatusControl("Camera 1 Status: ", camStatus1);
            panelModule1.Controls.Add(statusControlCam1);
            SaveImageEvent += InspectionForm_SaveImageEvent;

            bool camStatus2 = camera2.ConnectCamera();
            if (camStatus2)
            {
                camera2.ConfigureExposure(camera2.exposure);
                camera2.ConfigureTrigger(SpinCamManager.chosenTrigger);
                Thread.Sleep(1000);
                camera2.myImageEventListener.BitmapRecievedEvent += MyImageEventListener_BitmapRecievedEvent1;
            }


            StatusControl statusControlCam2 = new StatusControl("Camera 2 Status: ", camStatus2);
            panelModule1.Controls.Add(statusControlCam2);

            SpinCamManager.chosenTrigger = SpinCamManager.TriggerType.Hardware;

            SecurityChecks.CamerasReady = (camStatus1 && camStatus2);

            StatusControl statusControlPlc = new StatusControl("PLC Status: ", Convert.ToBoolean(PLCControl.readDataPLC((int)PlcReg.PlcSatus)));
            panelModule1.Controls.Add(statusControlPlc);

            //camera1.InitializeSpinnaker();
            //camera1.InitializeSpinnaker();
            bool sonaServer = Directory.Exists(@"Z:\");

            statusControlSonaServer = new StatusControl("Sona Server: ", sonaServer);
            panelModule1.Controls.Add(statusControlSonaServer);

            var statusApi1 = ApiController.RunAnacondaCmd(1);

            this.Invoke(new Action(() =>
            {
                statusControlApi1 = new StatusControl("Api 1 status: ", statusApi1.status);

                panelModule1.Controls.Add(statusControlApi1);
            }));
            Thread.Sleep(100);

            var statusApi2 = ApiController.RunAnacondaCmd(2);

            this.Invoke(new Action(() =>
            {
                statusControlApi2 = new StatusControl("Api 2 status: ", statusApi2.status);

                panelModule1.Controls.Add(statusControlApi2);
            }));
            Thread.Sleep(100);
            //Task.Run(() =>
            //{


            //});
            //Task.Run(() =>
            //{


            //});
            //stratPLCcomm();
            // timer1.Start();
            string modelName = comboBoxModel.SelectedItem.ToString();
            string folderPath = $@"{AppData.ProjectDirectory}/Models/{modelName}";
            //PLCControl.writeDataPLC(1, (int)PlcReg.SetHomeInter);
            PLCControl.writeDataPLC(1, (int)PlcReg.AutoManual);

            if (Directory.Exists(folderPath))
            {
                AppData.SelectedModel = JsonConvert.DeserializeObject<ModelData>(File.ReadAllText($@"{folderPath}/ModelData.json"));
                PLCControl.writeDataPLC(AppData.SelectedModel.Position, 20);
            }
            else
            {
                MessageBox.Show("No data for model");
            }
            comboBoxModel.SelectedIndexChanged += comboBoxModel_SelectedIndexChanged;
            timerPlc.Start();
            //buttonStart.PerformClick();
            backgroundWorker1.RunWorkerAsync();
            comboBoxModel.SelectedIndex = 0;
            modelName = comboBoxModel.SelectedItem.ToString();
            folderPath = $@"{AppData.ProjectDirectory}/Models/{modelName}";
            if (Directory.Exists(folderPath))
            {
                AppData.SelectedModel = JsonConvert.DeserializeObject<ModelData>(File.ReadAllText($@"{folderPath}/ModelData.json"));
                ConsoleExtension.WriteWithColor($"Position sent to plc {AppData.SelectedModel.Position}");
                PLCControl.writeDataPLC(AppData.SelectedModel.Position, 20);
            }
            else
            {
                MessageBox.Show("No data for model");
            }
            radioButtonInspection.Checked = true;

            BtnSelectFolder.Text = Program.SaveModelFolder;
            ////Sachin code Start
            //Selection_Model_Form selection_Model_Form = new Selection_Model_Form();
            //selection_Model_Form.ShowDialog();
            ////Sachin Code end
            trackBarThresh.Value = AppData.ThresholdValue;


            // Initialize and configure the Timer
            dailyTimer = new Timer();
            dailyTimer.Interval = CalculateMillisecondsUntil7AM();
            dailyTimer.Tick += DailyTimer_Tick;

            // Start the Timer
            dailyTimer.Start();

        }
        private Timer dailyTimer;

        private int CalculateMillisecondsUntil7AM()
        {
            // Calculate the time until the next 7:00:00 am
            DateTime now = DateTime.Now;
            DateTime next7AM = now.Date.AddHours(7);

            if (now > next7AM)
            {
                next7AM = next7AM.AddDays(1);
            }

            return (int)(next7AM - now).TotalMilliseconds;
        }

        private void DailyTimer_Tick(object sender, EventArgs e)
        {
            // This method will be called every day at 7:00:00 am

            Database.CreateDailyExcel();

            // Reset the Timer for the next day
            dailyTimer.Interval = CalculateMillisecondsUntil7AM();
        }
        //static Tuple<double, double> CalculatePercentages(int totalOkParts, int totalNgParts, int totalInspectedParts)
        //{
        //    if (totalInspectedParts == 0)
        //    {
        //        return new Tuple<double, double>(0, 0);
        //    }

        //    double okPercentage = ((double)totalOkParts / totalInspectedParts) * 100;
        //    double ngPercentage = ((double)totalNgParts / totalInspectedParts) * 100;

        //    return new Tuple<double, double>(okPercentage, ngPercentage);
        //}
        static Tuple<double, double> CalculatePercentages(int totalOkParts, int totalNgParts, int totalInspectedParts)
        {
            if (totalInspectedParts == 0)
            {
                return new Tuple<double, double>(0, 0);
            }

            double okPercentage = ((double)totalOkParts / totalInspectedParts) * 100;
            double ngPercentage = ((double)totalNgParts / totalInspectedParts) * 100;

            if (okPercentage + ngPercentage > 100)
            {
                // If the sum exceeds 100, adjust the NG percentage to make the total 100
                ngPercentage = 100 - okPercentage;
            }

            return new Tuple<double, double>(okPercentage, ngPercentage);
        }

        private void InspectionForm_SaveImageEvent()
        {
            Task.Run(() =>
            {
                bool sonaServer = Directory.Exists(@"Z:\");
                if (sonaServer)
                {
                    try
                    {
                        Console.WriteLine($"bitmapsProcess count {bitmapsProcessCam1.Count}");
                        Thread.Sleep(300);
                        string partNumber = Database.GetPartNoData(dateTimeSql);

                        Console.WriteLine($"bitmapsCam1 count : {bitmapsCam1.Count}");
                        Console.WriteLine($"bitmapsCam2 count : {bitmapsCam2.Count}");

                        while (bitmapsCam1.Count > 0)
                        {
                            if (Program.SaveModelFolder != null)
                            {
                                bitmapsCam1.Dequeue().Save($@"Z:\Images\{1}\{Program.SaveModelFolder}\{partNumber}_{DateTime.Now.ToString("dd_MM_yyyy_HH_mm_ss")}_{bitmapsCam1.Count}.bmp");
                            }
                            else
                            {
                                bitmapsCam1.Dequeue().Save($@"Z:\Images\{1}\{partNumber}_{DateTime.Now.ToString("dd_MM_yyyy_HH_mm_ss")}_{bitmapsCam1.Count}.bmp");
                            }

                            Thread.Sleep(20);
                        }
                        while (bitmapsCam2.Count > 0)
                        {
                            if (Program.SaveModelFolder != null)
                            {
                                bitmapsCam2.Dequeue().Save($@"Z:\Images\{2}\{Program.SaveModelFolder}\{partNumber}_{DateTime.Now.ToString("dd_MM_yyyy_HH_mm_ss")}_{bitmapsCam2.Count}.bmp");
                            }
                            else
                            {
                                bitmapsCam2.Dequeue().Save($@"Z:\Images\{2}\{partNumber}_{DateTime.Now.ToString("dd_MM_yyyy_HH_mm_ss")}_{bitmapsCam2.Count}.bmp");
                            }


                            Thread.Sleep(20);
                        }
                        while (bitmapsProcessCam1.Count > 0)
                        {

                            //Bitmap compressbmp = new Bitmap(Program.CompressBitmapImage(bitmapsProcessCam1.Dequeue(), 10));
                            //compressbmp.Save($@"Z:\Images\Processed\{1}\{partNumber}_{DateTime.Now.ToString("dd_MM_yyyy_HH_mm_ss")}.bmp");

                            bitmapsProcessCam1.Dequeue().Save($@"Z:\Images\Processed\{1}\{partNumber}_{DateTime.Now.ToString("dd_MM_yyyy_HH_mm_ss")}_{bitmapsProcessCam1.Count}.bmp");

                            Thread.Sleep(20);
                        }
                        while (bitmapsProcessCam2.Count > 0)
                        {
                            bitmapsProcessCam2.Dequeue().Save($@"Z:\Images\Processed\{2}\{partNumber}_{DateTime.Now.ToString("dd_MM_yyyy_HH_mm_ss")}_{bitmapsProcessCam2.Count}.bmp");
                            Thread.Sleep(20);
                        }
                    }
                    catch (Exception ex)
                    {

                        Console.WriteLine(ex.Message);
                    }

                }
                else
                {
                    bitmapsCam1.Clear();
                    bitmapsCam2.Clear();
                    bitmapsProcessCam1.Clear();
                    bitmapsProcessCam2.Clear();
                    MessageBox.Show("SonaComstar server not connected. Can not save images");
                }
            });
        }

        public bool PartInspected = false;
        private void PartInspectionData_OnPartInspected()
        {
            PartInspected = true;
        }

        public class ApiDefectDetails
        {
            public string defImage { get; set; }
            public List<string> serialized_Defects = new List<string>();
        }
        public static Bitmap ConvertBase64ToBitmap(string base64String)
        {
            try
            {
                byte[] imageBytes = Convert.FromBase64String(base64String);

                using (MemoryStream ms = new MemoryStream(imageBytes))
                {
                    Bitmap bitmap = new Bitmap(ms);
                    return new Bitmap(bitmap); // Clone the bitmap to prevent memory leaks
                }
            }
            catch (Exception ex)
            {
                // Handle any exceptions that may occur during the conversion
                Console.WriteLine($"Error converting base64 to bitmap: {ex.Message}");
                return null;
            }
        }

        public event Action SaveImageEvent = null;


        int _rotaionCount = 0;
        public int RotationCount
        {
            get { return _rotaionCount; }
            set
            {
                _rotaionCount = value;
                if (_rotaionCount >= 30)
                {
                    string result = "OK";
                    this.Invoke(new Action(() =>
                    {
                        Thread.Sleep(1000);

                        ConsoleExtension.WriteWithColor($@"PartInspectionData.NgPartDetected: {PartInspectionData.NgPartDetected}\n 
                                        flowLayoutPanel1.Controls.Count: {flowLayoutPanel1.Controls.Count}\n
                                        flowLayoutPanel2.Controls.Count: {flowLayoutPanel2.Controls.Count}", ConsoleColor.Yellow);

                        if (PartInspectionData.NgPartDetected && (flowLayoutPanel1.Controls.Count > 0 || flowLayoutPanel2.Controls.Count > 0))
                        {
                            PartInspectionData.TotalNgParts++;
                            labelResult.BackColor = Color.Red;
                            labelResult.Text = "NG";
                            if (checkBoxInspect1.Checked)
                            {
                                PLCControl.writeDataPLC(2, (int)PlcReg.InspectionResult);
                            }
                            result = "NG";
                        }
                        else
                        {
                            PartInspectionData.TotalOkParts++;
                            labelResult.BackColor = Color.LimeGreen;
                            labelResult.Text = "OK";
                            if (checkBoxInspect1.Checked)
                            {
                                PLCControl.writeDataPLC(1, (int)PlcReg.InspectionResult);
                            }
                        }
                        PartInspectionData.NgPartDetected = false;
                        PartInspected = false;
                        string defectsCam1 = JsonConvert.SerializeObject(inspectionData1.InspectionList.Select(item => item.Item2));
                        string defectsCam2 = JsonConvert.SerializeObject(inspectionData2.InspectionList.Select(item => item.Item2)/*.ToList()*/);

                        Database.InsertDataIntoGearReport(comboBoxModel.SelectedItem.ToString(),
                           Convert.ToDateTime(DateTime.Now.ToString("yyyy-MM-dd")),
                           Convert.ToDateTime(DateTime.Now.ToString("HH:mm:ss")),
                           result,
                            defectsCam1/*.ToList()*/,
                           defectsCam2);

                        Database.InsertData(partNumber, comboBoxModel.SelectedItem.ToString(), result, defectsCam1, defectsCam2);
                        DisplayChart(Database.GetChartDataForDate(Settings.Default.ChartTime));
                    }));
                    //if (!backgroundWorker1.IsBusy)
                    //{
                    //    backgroundWorker1.RunWorkerAsync();
                    //}
                    if (SaveImageEvent != null)
                    {
                        SaveImageEvent?.Invoke();
                    }
                    _rotaionCount = 0;
                    return;
                }
                else if (_rotaionCount == 1)
                {
                    this.Invoke(new Action(() =>
                    {
                        dateTimeSql = DateTime.Now;
                        inspectionData1.InspectionList.Clear();
                        inspectionData2.InspectionList.Clear();
                        flowLayoutPanel1.Controls.Clear();
                        flowLayoutPanel2.Controls.Clear();
                        chart1.Series[0].Points.Clear();
                        maxDefCam1 = 0;
                        maxDefCam2 = 0;
                        labelResult.Invoke(new Action(() =>
                        {
                            labelResult.BackColor = Color.FromArgb(113, 133, 245);
                            labelResult.Text = "RESULT";
                        }));
                        partNumber = Database.GetPartNoData(dateTimeSql);
                        labelPartNumber.Text = partNumber;
                    }));
                }
            }
        }
        static string partNumber = "";
        private async Task InspectGear(PartInspectionData partInspectionData, Bitmap bitmap, FlowLayoutPanel flowLayoutPanelImage, PictureBox pictureBox, CheckBox checkBoxSaveImg, int cameraNum, string port, int maxDef)
        {

            Stopwatch stopwatch = Stopwatch.StartNew();
            await Task.Delay(1);
            //bitmap = new Bitmap($@"{AppData.ProjectDirectory}/TestImage.jpg");
            Bitmap defectImage = null;
            //Console.WriteLine(JsonConvert.SerializeObject(detectionData));
            List<DefectData> defects = new List<DefectData>();
            List<string> strings = new List<string>();
            ApiDefectDetails detectionData = new ApiDefectDetails();
            if (radioButtonInspection.Checked)
            {
                try
                {
                    detectionData = JsonConvert.DeserializeObject<ApiDefectDetails>(ApiController.ProcessImage(bitmap.DeepClone(), cameraNum));
                    defectImage = ConvertBase64ToBitmap(detectionData.defImage);
                }
                catch (Newtonsoft.Json.JsonReaderException ex)
                {
                    detectionData = new ApiDefectDetails();
                    defectImage = bitmap.DeepClone();

                    ConsoleExtension.WriteWithColor(ex.Message, ConsoleColor.Red);
                }
                catch (Exception ex)
                {

                    detectionData = new ApiDefectDetails();
                    defectImage = bitmap.DeepClone();

                    ConsoleExtension.WriteWithColor(ex.Message, ConsoleColor.Red);
                }


            }
            this.Invoke(new Action(() =>
            {
                try
                {
                    if (checkBoxSaveImg.Checked)
                    {
                        //this.Invoke(new Action(() =>
                        //{
                        //    Selection_Model_Form selection_Model_Form = new Selection_Model_Form();
                        //    selection_Model_Form.ShowDialog();
                        //}));


                        if (cameraNum == 1)
                        {
                            bitmapsCam1.Enqueue(bitmap.DeepClone());
                            if (radioButtonInspection.Checked)
                            {
                                bitmapsProcessCam1.Enqueue(defectImage.DeepClone());
                            }

                        }
                        else
                        {
                            bitmapsCam2.Enqueue(bitmap.DeepClone());
                            if (radioButtonInspection.Checked)
                            {
                                bitmapsProcessCam2.Enqueue(defectImage.DeepClone());
                            }


                        }
                        //Bitmap saveImage = bitmap.DeepClone();
                        //saveImage.Save($@"{AppData.ProjectDirectory}/Images/{cameraNum}/{DateTime.Now.ToString("dd_MM_yyyy_HH_mm_ss")}.bmp");
                        //saveImage.Save($@"Z:\Images\{cameraNum}\{Database.GetPartNoData(DateTime.Now)}_{DateTime.Now.ToString("dd_MM_yyyy_HH_mm_ss")}.bmp");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }

            }));
            if (radioButtonInspection.Checked && detectionData.serialized_Defects.Count > 0 && detectionData.serialized_Defects.Contains("Ng", StringComparer.OrdinalIgnoreCase))
            {
                PartInspectionData.NgPartDetected = true;
                this.Invoke(new Action(() =>
                {

                    for (int i = 0; i < detectionData.serialized_Defects.Count; i++)
                    {
                        //converting coordinates from (TL, BR) to (TL, W, H)
                        //Rectangle rectangle = new Rectangle(detectionData.serialized_Defects[i].coordinates[0], detectionData.serialized_Defects[i].coordinates[1],
                        //    detectionData.serialized_Defects[i].coordinates[2] - detectionData.serialized_Defects[i].coordinates[0], detectionData.serialized_Defects[i].coordinates[3] - detectionData.serialized_Defects[i].coordinates[1]);

                        //DrawDefectOnImage(bitmap, rectangle);

                        DefectData defectData = new DefectData(cameraNum, PartInspectionData.DegreeInOneFrame * PartInspectionData.TriggerCount, detectionData.serialized_Defects[i]);
                        //DefectControl defectControl = new DefectControl(defectData, i);
                        string deftype = defectData.defType;

                        string folderPath = $@"Z:\Defect Images\{cameraNum}\{AppData.SelectedModel}\{deftype}";

                        if (!Directory.Exists(folderPath))
                        {
                            Directory.CreateDirectory(folderPath);
                        }
                        defectImage.DeepClone().Save($@"{folderPath}\{partNumber}_{DateTime.Now.ToString("dd_MM_yyyy_HH_mm_ss")}_{i}.bmp");

                        defects.Add(defectData);

                    }

                    pictureBox.SetImage(defectImage.DeepClone());

                    partInspectionData.InspectionList.Add((defectImage.DeepClone(), defects));

                    DefectImageControl defectImageControl = new DefectImageControl(defectImage.DeepClone(), defects);
                    int defCount = defects.Count;


                    flowLayoutPanelImage.Controls.Add(defectImageControl);
                    if (defCount >= maxDef)
                    {
                        maxDef = defCount;
                        flowLayoutPanelImage.Controls.SetChildIndex(defectImageControl, 0);
                    }
                    else
                    {
                        flowLayoutPanelImage.Controls.SetChildIndex(defectImageControl, flowLayoutPanelImage.Controls.Count - 1);

                    }

                    DisplayDefectsOnChart(defects);
                    //Task.Run(() => {

                    //});

                    //pictureBox.Image = bitmap;

                }));
            }
            else if (radioButtonInspection.Checked)
            {
                pictureBox.SetImage(defectImage.DeepClone());

            }
            else if (radioButtonDataset.Checked)
            {
                pictureBox.SetImage(bitmap.DeepClone());

            }
            stopwatch.Stop();
            ConsoleExtension.WriteWithColor($"Time taken to process image {stopwatch.ElapsedMilliseconds}", ConsoleColor.Yellow);




        }

        int maxDefCam1 = 0;
        int maxDefCam2 = 0;

        private async void MyImageEventListener_BitmapRecievedEvent(object sender, Bitmap e)
        {

            if (AppData.AppMode == AppData.Mode.Inspection)
            {
                //this.Invoke(new Action(() =>
                //{
                //    if (PartInspectionData.NgPartDetected)
                //    {
                //        labelResult.BackColor = Color.Red;
                //        labelResult.Text = "NG";
                //    }
                //    else
                //    {
                //        labelResult.BackColor = Color.LimeGreen;
                //        labelResult.Text = "OK";
                //    }
                //}));



                Bitmap bitmap = e.DeepClone();
                //rotate image 90 degrees from camera 1 
                bitmap.RotateFlip(RotateFlipType.Rotate90FlipNone);
                //Bitmap bitmapProcess = bitmap.TransformAndCrop();
                await InspectGear(inspectionData1, bitmap, flowLayoutPanel1, pictureBoxCam1, checkBoxSaveImage1, 1, "5000", maxDefCam1);
                RotationCount++;
                PartInspectionData.TriggerCount++;
                var percentages = CalculatePercentages(PartInspectionData.TotalOkParts, PartInspectionData.TotalNgParts, PartInspectionData.PartsInspected);
                this.Invoke(new Action(() =>
                {
                    labelTriggerCount.Text = $"Trigger Count: {PartInspectionData.TriggerCount}";
                    labelTotal.Text = $"Total Parts Inspected: {PartInspectionData.TotalOkParts + PartInspectionData.TotalNgParts}";
                    labelOk.Text = $"Total OK: {PartInspectionData.TotalOkParts}";
                    labelNg.Text = $"Total NG: {PartInspectionData.TotalNgParts}";
                    labelOkPercent.Text = $"OK Percent: {percentages.Item1}";
                    labelNgPercent.Text = $"NG Percent: {percentages.Item2}";
                    chartPie.Series[0].Points.Clear();
                    chartPie.Series[0].Points.AddXY($"OK {percentages.Item1.ToString("N0")}%", percentages.Item1);
                    chartPie.Series[0].Points.AddXY($"NG {percentages.Item2.ToString("N0")}%", percentages.Item2);
                }));

                //pictureBox1.Invoke(new Action(() =>
                //{
                //    pictureBox1.Image = bitmap;

                //}));

            }
            //}
            //else
            //{
            //    pictureBoxCam1.SetImage(e.DeepClone());
            //}

        }

        private async void MyImageEventListener_BitmapRecievedEvent1(object sender, Bitmap e)
        {
            //if (checkBoxInspect1.Checked)
            //{
            if (AppData.AppMode == AppData.Mode.Inspection)
            {
                //this.Invoke(new Action(() =>
                //{
                //    if (checkBoxSaveImage1.Checked)
                //    {
                //        Bitmap saveImage = e.DeepClone();
                //        saveImage.Save($@"{AppData.ProjectDirectory}/Images/2/{DateTime.Now.ToString("dd_MM_yyyy_HH_mm_ss")}.bmp");
                //    }
                //}));
                Bitmap bitmap = e.DeepClone();
                bitmap.RotateFlip(RotateFlipType.Rotate270FlipNone);
                await InspectGear(inspectionData2, bitmap, flowLayoutPanel2, pictureBoxCam2, checkBoxSaveImage1, 2, "5001", maxDefCam2);
                //PartInspectionData.TriggerCount++;

                //rotate image 270 degrees from camera 2
                //bitmap.RotateFlip(RotateFlipType.Rotate270FlipNone);
                //pictureBox2.Invoke(new Action(() =>
                //{
                //    pictureBox2.Image = bitmap;
                //}));
            }
            //}
            //else
            //{
            //    pictureBoxCam2.SetImage(e.DeepClone());
            //}
        }

        void DrawDefectOnImage(Bitmap bmp, Rectangle rectangle)
        {
            using (var graphics = Graphics.FromImage(bmp))
            {
                graphics.DrawRectangle(Pens.Red, rectangle);
            }
        }

        private void buttonTest_Click(object sender, EventArgs e)
        {
            if (PartInspected)
            {


                chart1.Series[0].Points.Clear();
                inspectionData1.InspectionList.Clear();
                inspectionData2.InspectionList.Clear();
                flowLayoutPanel1.Controls.Clear();
                flowLayoutPanel2.Controls.Clear();


                if (PartInspectionData.NgPartDetected)
                {
                    PartInspectionData.TotalNgParts++;
                }
                else
                {
                    PartInspectionData.TotalOkParts++;
                }
                PartInspectionData.NgPartDetected = false;
                PartInspected = false;

            }
            Bitmap bitmap = new Bitmap($@"{AppData.ProjectDirectory}/TestImage.jpg");


            //InspectGear(inspectionData1, bitmap, flowLayoutPanel1, 1, "5000");
            //InspectGear(inspectionData2, bitmap, flowLayoutPanel2, 2, "5001");
            PartInspectionData.TriggerCount++;
            labelTriggerCount.Text = $"Trigger Count: {PartInspectionData.TriggerCount}";
            labelTotal.Text = $"Total Parts Inspected: {PartInspectionData.PartsInspected}";
            labelOk.Text = $"Total OK: {PartInspectionData.TotalOkParts}";
            labelNg.Text = $"Total NG: {PartInspectionData.TotalNgParts}";

            //camera1.myImageEventListener.InvokeBitmapEvent(bitmap);
            //camera2.myImageEventListener.InvokeBitmapEvent(bitmap);

            //Bitmap bitmap = new Bitmap($@"{AppData.ProjectDirectory}/TestImage.jpg");

            //List<ApiDefectData> detectionData = JsonConvert.DeserializeObject<List<ApiDefectData>>(ApiController.ProcessImage(bitmap));

            //panelDefectCam1.Controls.Clear();
            //for (int i = 0; i < detectionData.Count; i++)
            //{
            //    Rectangle rectangle = new Rectangle(detectionData[i].coordinates[0], detectionData[i].coordinates[1], detectionData[i].coordinates[2], detectionData[i].coordinates[3]);

            //    DrawDefectOnImage(bitmap, rectangle);

            //    DefectData defectData = new DefectData(1, 90, detectionData[i].defType);
            //    DefectControl defectControl = new DefectControl(defectData);
            //    panelDefectCam1.Controls.Add(defectControl);

            //}

            //pictureBox1.Image = bitmap;

        }
        private bool asyncCloseHack = true;
        bool formClosingEventFired = false;
        DialogResult dialogResult = DialogResult.Yes;
        Mode mode = Mode.Idol;
        private async void InspectionForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            await Console.Out.WriteLineAsync($"formClosingEventFired {formClosingEventFired} AppData.AppMode {AppData.AppMode} mode {mode}");

            if (!formClosingEventFired && AppData.AppMode != AppData.Mode.Restart && mode != AppData.Mode.Restart)
            {
                dialogResult = MessageBox.Show("Do you really want to close the application ?", "Confirmation", MessageBoxButtons.YesNo);
                formClosingEventFired = true;
            }
            if (dialogResult == DialogResult.No)
            {
                e.Cancel = true;
                formClosingEventFired = false;

                return;

            }
            else if (dialogResult == DialogResult.Yes || AppData.AppMode == AppData.Mode.Restart || mode == AppData.Mode.Restart)
            {
                this.Invoke(new Action(async () =>
                {
                    mode = Mode.Restart;
                    Settings.Default.TotalNg = PartInspectionData.TotalNgParts;
                    Settings.Default.TotalOk = PartInspectionData.TotalOkParts;
                    Settings.Default.TotalInspected = PartInspectionData.PartsInspected;
                    Settings.Default.Save();
                    timerPlc.Stop();
                    commthreadFlag = false;
                    Cursor = Cursors.WaitCursor;
                    PLCControl.writeDataPLC(0, (int)PlcReg.LightForceOn);
                    PLCControl.writeDataPLC(0, (int)PlcReg.ModelSetup);
                    PLCControl.writeDataPLC(0, (int)PlcReg.ModelSetup);
                    PLCControl.writeDataPLC(0, (int)PlcReg.AutoManual);
                    PLCControl.writeDataPLC(0, (int)PlcReg.SwReady);
                    PLCControl.writeDataPLC(0, (int)PlcReg.SetIntermediate);


                    // waiting for the servers to shut down before initiating the closing process
                    if (asyncCloseHack)
                    {
                        e.Cancel = true;
                        await ApiController.ShutdownServer(5000);
                        await ApiController.ShutdownServer(5001);
                        // once task is completed we can start the form closing process
                        asyncCloseHack = false;
                        Close();
                    }

                    ConsoleExtension.WriteWithColor("Closing app and all the servers.", ConsoleColor.Red);
                }));

            }
        }

        #region PlcControl


        bool commthreadFlag;
        bool sensorOn = false;

        public int NewCycle
        {
            get { return _newCycle; }
            set
            {
                int oldValue = _newCycle;
                _newCycle = value;
                if (_newCycle > 0 && oldValue != _newCycle)
                {
                    PartInspectionData.TriggerCount = 0;
                    RotationCount = 0;
                }
            }
        }

        //status
        int homeStatus = -1;
        int _newCycle = -1;
        int errOverTravel = -1;
        int errNoData = -1;
        int swReady = -1;

        int autoManMode = -1;
        int actuatorState = -1;
        int overTravel = -1;
        int triggerPulseCnt = 0;

        //actions
        bool updateActions = false;
        int lightForceOn = 0;
        int cycleStart = 0;
        int moveToHome = 0;
        void updateLabel(bool state, Label lbl, String okText, String ngText, Color okColor_backGround, Color ngColor_backGround)
        {
            if (lbl.Created)
            {
                lbl.Invoke(new Action(() =>
                {
                    if (state)//ok
                    {
                        lbl.Text = okText;
                        lbl.BackColor = okColor_backGround;
                    }
                    else//ng
                    {
                        lbl.Text = ngText;
                        lbl.BackColor = ngColor_backGround;
                    }
                }));
            }


        }

        void updateLabel(int state, Label lbl, Color okColor_backGround, Color ngColor_backGround, params string[] texts)
        {
            if (lbl.Created)
            {
                lbl.Invoke(new Action(() =>
                {
                    if (state == 0)//ok
                    {
                        lbl.Text = texts[0];
                        lbl.BackColor = okColor_backGround;
                    }
                    else if (state > 0)
                    {
                        lbl.Text = texts[state];
                        lbl.BackColor = ngColor_backGround;
                    }
                }));
            }


        }

        int updatePLCLabels()
        {
            //if (autoManMode == 0)
            //    updateLabel(false, lblPLCautoMan, "Auto", "Manual", Color.Green, Color.Orange);
            //else if (autoManMode == 1)
            //    updateLabel(true, lblPLCautoMan, "Auto", "Manual", Color.Green, Color.Orange);

            updateLabel(Convert.ToBoolean(autoManMode), lblPLCautoMan, "Auto", "Manual", Color.Green, Color.Orange);
            //updateLabel(Convert.ToBoolean(homeStatus - 1), labelHome, "Home", "Intermediate", Color.Green, Color.Orange);
            updateLabel(Convert.ToBoolean(NewCycle), labelNewCycle, "Cycle Running", "Cycle Stop", Color.Green, Color.Orange);
            updateLabel(overTravel, lblActuatorOverTravel, Color.Green, Color.Red, new string[] { "In Range", "Emergency Pressed", "Limit FWD Abort", "Limit BWD Abort" });
            //updateLabel(Convert.ToBoolean(errNoData), labelErrorNoData, "Data received", "No data for model", Color.Green, Color.Orange);
            //updateLabel(Convert.ToBoolean(swReady) && SecurityChecks.AllChecksPassed(), labelSwReady, "Software Ready", "Software not ready", Color.Green, Color.Orange);
            if (homeStatus == 0)
            {
                if (labelHome.Created)
                {
                    labelHome.Invoke(new Action(() =>
                    {
                        labelHome.Text = "Not Home";
                        labelHome.BackColor = Color.Red;
                        labelHome.ForeColor = Color.White;
                    }));
                }
            }
            else if (homeStatus == 1)
            {
                if (labelHome.Created)
                {
                    labelHome.Invoke(new Action(() =>
                    {
                        labelHome.Text = "Home";
                        labelHome.BackColor = Color.Green;
                        labelHome.ForeColor = Color.Black;
                    }));
                }
            }
            else if (homeStatus == 2)
            {
                if (labelHome.Created)
                {
                    labelHome.Invoke(new Action(() =>
                    {
                        labelHome.Text = "Intermediate";
                        labelHome.BackColor = Color.Orange;
                        labelHome.ForeColor = Color.Black;
                    }));
                }
            }

            //if (actuatorState == 0)
            //    lblPLCCamState.Text = "Error";
            //else if (actuatorState == 1)
            //    lblPLCCamState.Text = "Home";
            //else if (actuatorState == 2)
            //    lblPLCCamState.Text = "Scanning";
            //else if (actuatorState == 3)
            //    lblPLCCamState.Text = "Returning";

            //if (overTravel != 0)
            //{
            //    if (overTravel == 1)
            //    { lblActuatorOverTravel.Text = "Limit Exceeded above Home Sensor"; }
            //    else if (overTravel == 2)
            //    { lblActuatorOverTravel.Text = "Limit Exceeded above End Sensor"; }
            //    lblActuatorOverTravel.Visible = true;
            //}
            //else
            //{
            //    lblActuatorOverTravel.Visible = false;

            //}


            return 1;
        }

        private void plcComm()
        {
            bool sensorOnP = (PLCControl.readDataPLC(10) == 1);
            //while (commthreadFlag == true)
            //{

            sensorOn = PLCControl.readDataPLC(10) == 1;

            SecurityChecks.PlcConnected = sensorOn;



            if (sensorOn)
            {
                autoManMode = PLCControl.readDataPLC((int)PlcReg.AutoManual);
                homeStatus = PLCControl.readDataPLC((int)PlcReg.Home);
                overTravel = PLCControl.readDataPLC((int)PlcReg.ErrorOverTravel);
                NewCycle = PLCControl.readDataPLC((int)PlcReg.NewCycle);
                errNoData = PLCControl.readDataPLC((int)PlcReg.ErrorNoData);
                //swReady = PLCControl.readDataPLC((int)PlcReg.SwReady);
                //triggerPulseCnt = PLCControl.readDataPLC(6);

                //if (updateActions)
                //{
                //    PLCControl.writeDataPLC(lightForceOn, 2);
                //    PLCControl.writeDataPLC(cycleStart, 3);
                //    PLCControl.writeDataPLC(moveToHome, 5);
                //    updateActions = false;
                //    cycleStart = 0;
                //    moveToHome = 0;
                //}
                updatePLCLabels();
            }
            //Thread.Sleep(3000);

            //}
        }
        System.Threading.Thread commThread = null;
        public void stratPLCcomm()
        {
            commthreadFlag = true;
            commThread = new System.Threading.Thread(
            () =>
            {
                plcComm();
            }
            );

            commThread.Start();
        }

        public int readDataPLC(int reg)
        {
            int intValue = 0;
            try
            {
                reg = reg + 4096;

                //string hexValue = value.ToString("X4");
                string regNum = reg.ToString("X4");
                clsInputValidation.function(3);
                string tx = string.Concat("01", "03", regNum, "0004");
                //Console.WriteLine("data sent {0}", tx);
                string resp = clsComms.Read(tx);
                //Console.WriteLine("data received{0}", resp);
                // lblResp.Text = resp;

                if (resp != "")
                {
                    string dataHex = resp.Substring(7, 4);
                    intValue = int.Parse(dataHex, System.Globalization.NumberStyles.HexNumber);
                    // lblIntVal.Text = intValue.ToString();
                }
            }
            catch (Exception exx)
            {
                Console.WriteLine(exx.Message);
                return 0;
            }
            return intValue;

        }
        public static string writeDataPLC(int value, int reg)
        {
            reg = reg + 4096;

            string hexValue = value.ToString("X4");
            string regNum = reg.ToString("X4");
            clsInputValidation.function(6);
            string tx = string.Concat("01", "06", regNum, hexValue);
            // Console.WriteLine("data sent {0}", tx);
            string resp = clsComms.Read(tx);
            // lblResp.Text = resp;
            // Console.WriteLine("data received{0}", resp);
            return "all good";

        }
        #endregion



        private void InspectionForm_FormClosed(object sender, FormClosedEventArgs e)
        {

            //Process.GetProcessById(ApiController.processAnaconda.Id).Kill();
            //Environment.Exit(0);
        }
        int CNT = 1;
        private void button1_Click(object sender, EventArgs e)
        {
            //int reg = 10;
            //PLCControl.writeDataPLC(CNT, reg);
            //int VAL = PLCControl.readDataPLC(reg);
            //Console.WriteLine("VAL{0}", VAL);
            //PLCControl.writeDataPLC(7, 20);
            //ConsoleExtension.WriteWithColor($"Plc D{reg} value {PLCControl.readDataPLC(20)}", ConsoleColor.Yellow);
            //ApiController.processAnaconda.Kill();
            //ApiController.ShutdownServer(5000);
            //ApiController.ShutdownServer(5001);

            string s = JsonConvert.SerializeObject(inspectionData1.InspectionList.Select(item => item.Item2), Formatting.Indented);
            ConsoleExtension.WriteWithColor(s, ConsoleColor.DarkMagenta);
            List<List<DefectData>> list = JsonConvert.DeserializeObject<List<List<DefectData>>>(s);

            //PartInspectionData partInspectionData = new PartInspectionData();
            //partInspectionData.InspectionList= JsonConvert.DeserializeObject<List<(Bitmap, List<DefectData>)>>(s);

        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            //updatePLCLabels();
            chart1.Series[2].Points.Clear();
            chart1.Series[2].Points.AddXY(12 * angle2, 70);
            angle2++;
            if (angle2 >= 30)
            {
                angle2 = 0;
            }

        }
        ModelSetup modelSetup = new ModelSetup();

        private void buttonModelSetup_Click(object sender, EventArgs e)
        {
            //if (SecurityChecks.CamerasReady && SecurityChecks.PlcConnected) 
            //{
            modelSetup = new ModelSetup();
            try
            {
                //camera1.myImageEventListener.BitmapRecievedEvent += DisplayImagesOnModelSetupScreen;
                //camera2.myImageEventListener.BitmapRecievedEvent += DisplayImagesOnModelSetupScreen2;
                //modelSetup.buttonBackwardJog.Click += ButtonBackwardJog_Click;
                //modelSetup.buttonForwardJog.Click += ButtonForwardJog_Click;
                modelSetup.FormClosed += ModelSetup_FormClosed;
                //modelSetup.FormClosing += ModelSetup_FormClosing; ;
                PLCControl.writeDataPLC(1, (int)PlcReg.ModelSetup);
                modelSetup.Show();
                //buttonModelSetup.Enabled = false;
            }
            catch (Exception ex)
            {
                ConsoleExtension.WriteWithColor(ex.Message, ConsoleColor.Red);
                MessageBox.Show("Model setup page not ready. Check cameras and plc connection to continue.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            //}
            //else
            //{
            //    MessageBox.Show("Model setup page not ready. Check cameras and plc connection to continue.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

            //}


        }

        // Model Setup form event handeling code. Written in same file to share PLC functions without creating
        // New instances of plc 
        private void ModelSetup_FormClosing(object sender, FormClosingEventArgs e)
        {
            AppData.AppMode = AppData.Mode.Idol;
            camera1.myImageEventListener.BitmapRecievedEvent -= DisplayImagesOnModelSetupScreen;
            camera2.myImageEventListener.BitmapRecievedEvent -= DisplayImagesOnModelSetupScreen2;
            //modelSetup.buttonBackwardJog.Click -= ButtonBackwardJog_Click;
            //modelSetup.buttonForwardJog.Click -= ButtonForwardJog_Click;
            buttonModelSetup.Enabled = true;

        }

        private void ModelSetup_FormClosed(object sender, FormClosedEventArgs e)
        {

            PLCControl.writeDataPLC(0, (int)PlcReg.ModelSetup);
            comboBoxModel.LoadDirectoryNames($"{AppData.ProjectDirectory}/Models");

        }

        private void ButtonForwardJog_Click(object sender, EventArgs e)
        {
            Cursor = Cursors.WaitCursor;
            ConsoleExtension.WriteWithColor("Foward Jog", ConsoleColor.Green);
            PLCControl.writeDataPLC(1, (int)PlcReg.SetTestPose);
            bool poseDone = Convert.ToBoolean(PLCControl.readDataPLC((int)PlcReg.TestPoseDone));
            if (poseDone)
            {
                AppData.ModelDataObj.Position += 1;
            }
            //modelSetup.buttonForwardJog.Enabled = false;
            Thread.Sleep(900);
            Cursor = Cursors.Default;
            //modelSetup.buttonForwardJog.Enabled = true;

        }

        private void ButtonBackwardJog_Click(object sender, EventArgs e)
        {
            Cursor = Cursors.WaitCursor;

            ConsoleExtension.WriteWithColor("Backward Jog", ConsoleColor.Yellow);
            PLCControl.writeDataPLC(2, (int)PlcReg.SetTestPose);
            bool poseDone = Convert.ToBoolean(PLCControl.readDataPLC((int)PlcReg.TestPoseDone));
            if (poseDone)
            {
                AppData.ModelDataObj.Position -= 1;
            }
            //modelSetup.buttonBackwardJog.Enabled = false;
            Thread.Sleep(900);
            Cursor = Cursors.Default;
            //modelSetup.buttonBackwardJog.Enabled = true;
        }

        private void DisplayImagesOnModelSetupScreen(object sender, Bitmap e)
        {
            try
            {
                if (AppData.AppMode == AppData.Mode.Setup)
                {
                    modelSetup.pictureBox1.Invoke(new Action(() => { modelSetup.pictureBox1.Image = e.DeepClone(); }));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

        }

        private void DisplayImagesOnModelSetupScreen2(object sender, Bitmap e)
        {
            try
            {
                if (AppData.AppMode == AppData.Mode.Setup)
                {
                    modelSetup.pictureBox2.Invoke(new Action(() => { modelSetup.pictureBox2.Image = e.DeepClone(); }));

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }


        }

        private void buttonStart_Click(object sender, EventArgs e)
        {
            //if (SecurityChecks.AllChecksPassed())
            //{
            AppData.AppMode = AppData.Mode.Inspection;

            labelSwReady.Invoke(new Action(() => { labelSwReady.Text = "Software Ready"; labelSwReady.BackColor = Color.Green; }));

            //}
            //else
            //{
            //    MessageBox.Show("Software not ready for inspection.");
            //    labelSwReady.Invoke(new Action(() => { labelSwReady.Text = "Software Not  Ready"; labelSwReady.BackColor = Color.Red; }));

            // }
            //if (autoManMode > 0)
            //{
            PLCControl.writeDataPLC(1, (int)PlcReg.SwReady);
            //}
        }

        private void buttonStop_Click(object sender, EventArgs e)
        {
            AppData.AppMode = AppData.Mode.Idol;
            //PLCControl.writeDataPLC(0, (int)PlcReg.SwReady);

        }

        private void buttonPlcData_Click(object sender, EventArgs e)
        {
            PLCDataForm pLCDataForm = new PLCDataForm();
            pLCDataForm.ShowDialog();
        }

        private void buttonReturnHome_Click(object sender, EventArgs e)
        {
            PLCControl.writeDataPLC(1, (int)PlcReg.ReturnHome);
        }

        private void buttonErrorReset_Click(object sender, EventArgs e)
        {
            PLCControl.writeDataPLC(1, (int)PlcReg.ErrorReset);

        }

        private void InspectionForm_Shown(object sender, EventArgs e)
        {
            TurnOffFormLevelDoubleBuffering();
        }

        private void button2_Click(object sender, EventArgs e)
        {

            chart1.ChartAreas[0].AxisX.MajorTickMark.Enabled = false;
            chart1.ChartAreas[0].AxisX.MajorGrid.Enabled = false;
            chart1.ChartAreas[0].AxisY.Enabled = AxisEnabled.False;
            chart1.ChartAreas[0].AxisX.MinorGrid.Enabled = false;
            chart1.ChartAreas[0].AxisY.MinorGrid.Enabled = false;

            //chart1.ChartAreas[0].AxisY.Minimum = -20;
            //chart1.ChartAreas[0].AxisY.MajorGrid.IntervalOffset = 15;
            //chart1.ChartAreas[0].AxisY.MajorGrid.Interval = 5;
            //chart1.ChartAreas[0].AxisY.MajorGrid.LineDashStyle = ChartDashStyle.DashDot;
            //chart1.ChartAreas[0].AxisX.MajorGrid.LineDashStyle = ChartDashStyle.Dash;
            //chart1.ChartAreas[0].AxisX.Enabled = AxisEnabled.False;
            //chart1.ChartAreas[0].AxisX.MajorGrid.Enabled = false;
            //chart1.ChartAreas[0].AxisY.MajorGrid.Enabled = false;
            //chart1.ChartAreas[0].AxisX.LineColor = chart1.BackColor;
            //chart1.BackColor= Color.Blue;
            // Generate random data points and add them to the chart
            Random random = new Random();
            for (int i = 0; i < 30; i++) // Add 10 random data points
            {
                // double angle = random.NextDouble() * 360; // Random angle in degrees
                double value = random.Next(1, 100); // Random value

                chart1.Series[0].Points.AddXY(12 * i, 100);
            }
            Series series3 = new Series();
            series3.ChartArea = "ChartArea1";
            series3.ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Polar;
            series3.CustomProperties = "PolarDrawingStyle=Marker";
            series3.MarkerSize = 10;
            series3.MarkerStyle = System.Windows.Forms.DataVisualization.Charting.MarkerStyle.Circle;
            series3.Name = "Series3";
            chart1.Series.Add(series3);
            Series series4 = new Series();
            series4.ChartArea = "ChartArea1";
            series4.ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Polar;
            series4.CustomProperties = "PolarDrawingStyle=Marker";
            series4.MarkerSize = 10;
            series4.MarkerStyle = System.Windows.Forms.DataVisualization.Charting.MarkerStyle.Circle;
            series4.Name = "Series4";
            chart1.Series.Add(series4);

        }
        int angle = 0;
        int angle2 = 0;

        void DisplayDefectsOnChart(List<DefectData> defectData)
        {
            chart1.Invoke(new Action(() =>
            {
                foreach (DefectData data in defectData)
                {
                    chart1.Series[0].Points.AddXY(data.Degree, 100);

                }
            }));

        }

        private void timer2_Tick(object sender, EventArgs e)
        {
            chart1.Series[1].Points.Clear();
            chart1.Series[1].Points.AddXY(12 * angle, 80);
            angle++;
            if (angle >= 30)
            {
                angle = 0;
            }
            //for (int i = 0; i < 30; i++) // Add 10 random data points
            //{
            //    // double angle = random.NextDouble() * 360; // Random angle in degrees
            //    double value = random.Next(1, 100); // Random value

            //    chart1.Series[0].Points.AddXY(12 * i, 100);
            //}
        }

        private void buttonReport_Click(object sender, EventArgs e)
        {
            ReportForm reportForm = new ReportForm();
            reportForm.Show();
        }


        private void lblPLCautoMan_Click(object sender, EventArgs e)
        {
            Cursor = Cursors.WaitCursor;
            if (Convert.ToBoolean(autoManMode))
            {
                PLCControl.writeDataPLC(0, (int)PlcReg.AutoManual);
                autoManMode = PLCControl.readDataPLC((int)PlcReg.AutoManual);
                SecurityChecks.MachineInAutoMode = false;
            }
            else
            {
                PLCControl.writeDataPLC(1, (int)PlcReg.AutoManual);
                autoManMode = PLCControl.readDataPLC((int)PlcReg.AutoManual);
                SecurityChecks.MachineInAutoMode = true;

            }
            Thread.Sleep(900);
            Cursor = Cursors.Default;
        }

        private void label4_Click(object sender, EventArgs e)
        {

        }
        bool lightOn = false;
        private void buttonLight_Click(object sender, EventArgs e)
        {

            if (lightOn)
            {
                PLCControl.writeDataPLC(0, (int)PlcReg.LightForceOn);
                buttonLight.BackColor = Color.Red; buttonLight.ForeColor = Color.White;
                lightOn = false;
            }
            else
            {
                PLCControl.writeDataPLC(1, (int)PlcReg.LightForceOn);
                buttonLight.BackColor = Color.LimeGreen; buttonLight.ForeColor = Color.Black;
                lightOn = true;

            }

        }

        private void comboBoxModel_SelectedIndexChanged(object sender, EventArgs e)
        {
            string modelName = comboBoxModel.SelectedItem.ToString();
            string folderPath = $@"{AppData.ProjectDirectory}/Models/{modelName}";
            if (Directory.Exists(folderPath))
            {
                AppData.SelectedModel = JsonConvert.DeserializeObject<ModelData>(File.ReadAllText($@"{folderPath}/ModelData.json"));
                ConsoleExtension.WriteWithColor($"Position sent to plc {AppData.SelectedModel.Position}");
                PLCControl.writeJogValuePLC(AppData.SelectedModel.Position, (int)PlcReg.ModelPosValue);
            }
            else
            {
                MessageBox.Show("No data for model");
            }
        }

        private void buttonResetCount_Click(object sender, EventArgs e)
        {
            chart1.Series[0].Points.Clear();

            PartInspectionData.TotalNgParts = 0;
            PartInspectionData.TotalOkParts = 0;
            PartInspectionData.PartsInspected = 0;
            RotationCount = 0;
            PartInspectionData.TriggerCount = 0;
            this.Invoke(new Action(() =>
            {
                labelTriggerCount.Text = $"Trigger Count: {PartInspectionData.TriggerCount}";
                labelTotal.Text = $"Total Parts Inspected: {PartInspectionData.PartsInspected}";
                labelOk.Text = $"Total OK: {PartInspectionData.TotalOkParts}";
                labelNg.Text = $"Total NG: {PartInspectionData.TotalNgParts}";
            }));
            Settings.Default.ChartTime = DateTime.Now;
            chart2.Series.Clear();

            Settings.Default.TotalNg = PartInspectionData.TotalNgParts;
            Settings.Default.TotalOk = PartInspectionData.TotalOkParts;
            Settings.Default.TotalInspected = PartInspectionData.PartsInspected;
            Settings.Default.Save();
        }

        private void timer1_Tick_1(object sender, EventArgs e)
        {
            plcComm();
            bool sonaServer = Directory.Exists(@"Z:\");
            if (sonaServer)
            {
                statusControlSonaServer.buttonStatus.BackColor = Color.LimeGreen;
            }
            else
            {
                statusControlSonaServer.buttonStatus.BackColor = Color.Red;

            }
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

        private void DisplayChart(Dictionary<string, int> defectCounts)
        {


            // Create a new chart area
            chart2.Series.Clear();
            // Create a new series for the defect counts
            Series series = new Series();
            series.ChartType = SeriesChartType.Bar;
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
            chart2.Series.Add(series);
        }



        private void labelOk_Click(object sender, EventArgs e)
        {
            Console.WriteLine(Database.GetPartNoData(Convert.ToDateTime("2023-10-16 12:57:24")));
        }
        Queue<Bitmap> bitmapsCam1 = new Queue<Bitmap>();
        Queue<Bitmap> bitmapsProcessCam1 = new Queue<Bitmap>();
        Queue<Bitmap> bitmapsCam2 = new Queue<Bitmap>();
        Queue<Bitmap> bitmapsProcessCam2 = new Queue<Bitmap>();



        async Task<bool> CheckServerStatus(string serverUrl)
        {
            using (HttpClient httpClient = new HttpClient())
            {
                try
                {
                    HttpResponseMessage response = await httpClient.GetAsync(serverUrl);

                    if (response.IsSuccessStatusCode)
                    {
                        string content = await response.Content.ReadAsStringAsync();
                        // You can check the response content to ensure it matches the expected response.
                        // For simplicity, we'll assume "OK" response indicates the server is OK.
                        return content.Contains("OK");
                    }
                    else
                    {
                        // Server returned a non-success status code.
                        return false;
                    }
                }
                catch (HttpRequestException)
                {
                    // An exception occurred while making the request.
                    return false;
                }
            }
        }

        bool apiStatus1 = false;
        bool apiStatus2 = false;
        DateTime dateTimeSql = DateTime.Now;
        private async void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            labelSeverStartStatus.Invoke(new Action(() =>
            {
                labelSeverStartStatus.BackColor = Color.Orange;
                labelSeverStartStatus.ForeColor = Color.Black;
                labelSeverStartStatus.Text = "Starting server. Do not run Inspection. Please wait...";
            }));
            int threadCounter = 0;
            Thread.Sleep(10000);

            while (!apiStatus1 || !apiStatus2)
            {

                apiStatus1 = await CheckServerStatus("http://127.0.0.1:5000/ServerCheck");
                apiStatus2 = await CheckServerStatus("http://127.0.0.1:5001/ServerCheck");
                this.Invoke(new Action(() => { statusControlApi1.Status = apiStatus1; statusControlApi2.Status = apiStatus2; }));
                Thread.Sleep(500);
                threadCounter++;
                if (threadCounter >= 25)
                {
                    AppData.AppMode = AppData.Mode.Restart;
                    Application.Restart();
                    break;
                }
            }
            labelSeverStartStatus.Invoke(new Action(() =>
            {
                labelSeverStartStatus.BackColor = Color.LimeGreen;
                labelSeverStartStatus.ForeColor = Color.White;
                labelSeverStartStatus.Text = "Servers started successfully";

            }));

            buttonStart.Invoke(new Action(() =>
            {
                buttonStart.PerformClick();
            }));
            //bool sonaServer = Directory.Exists(@"Z:\");
            //if (sonaServer)
            //{
            //    try
            //    {
            //        Console.WriteLine($"bitmapsProcess count {bitmapsProcessCam1.Count}");
            //        Thread.Sleep(300);
            //        while (bitmapsCam1.Count > 0)
            //        {
            //            bitmapsCam1.Dequeue().Save($@"Z:\Images\{1}\{Database.GetPartNoData(dateTimeSql)}_{DateTime.Now.ToString("dd_MM_yyyy_HH_mm_ss")}.bmp");
            //            Thread.Sleep(20);
            //        }
            //        while (bitmapsCam2.Count > 0)
            //        {

            //            bitmapsCam1.Dequeue().Save($@"Z:\Images\{2}\{Database.GetPartNoData(dateTimeSql)}_{DateTime.Now.ToString("dd_MM_yyyy_HH_mm_ss")}.bmp");

            //            Thread.Sleep(20);
            //        }
            //        while (bitmapsProcessCam1.Count > 0)
            //        {
            //            bitmapsProcessCam1.Dequeue().Save($@"Z:\Images\Processed\{1}\{Database.GetPartNoData(dateTimeSql)}_{DateTime.Now.ToString("dd_MM_yyyy_HH_mm_ss")}.bmp");
            //            Thread.Sleep(20);
            //        }
            //        while (bitmapsProcessCam2.Count > 0)
            //        {

            //            bitmapsProcessCam2.Dequeue().Save($@"Z:\Images\Processed\{2}\{Database.GetPartNoData(dateTimeSql)}_{DateTime.Now.ToString("dd_MM_yyyy_HH_mm_ss")}.bmp");

            //            Thread.Sleep(20);
            //        }
            //    }
            //    catch (Exception ex)
            //    {

            //        Console.WriteLine(ex.Message);
            //    }

            //}
            //else
            //{
            //    MessageBox.Show("SonaComstar server not connected. Can not save images");
            //}


        }


        private void pictureBoxCam1_Click(object sender, EventArgs e)
        {


        }

        private void checkBoxSaveImage1_CheckedChanged(object sender, EventArgs e)
        {
            //Selection_Model_Form selection_Model_Form = new Selection_Model_Form();
            //selection_Model_Form.ShowDialog();
        }

        private void BtnSelectFolder_Click(object sender, EventArgs e)
        {
            Selection_Model_Form selection_Model_Form = new Selection_Model_Form();
            selection_Model_Form.ShowDialog();
            Program.ChangeButtonText(BtnSelectFolder, Program.SaveModelFolder);
        }

        private void radioButtonDataset_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButtonDataset.Checked)
            {
                labelAppMode.Text = "Dataset Collection Mode";
                labelAppMode.BackColor = Color.Orange;
                labelAppMode.ForeColor = Color.Black;
            }
        }

        private void radioButtonInspection_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButtonInspection.Checked)
            {
                labelAppMode.Text = "Inspection Mode";
                labelAppMode.BackColor = Color.Green;
                labelAppMode.ForeColor = Color.White;
            }
        }

        private void trackBar1_ValueChanged(object sender, EventArgs e)
        {
            labelThreshVal.Text = trackBarThresh.Value.ToString();
            AppData.ThresholdValue = trackBarThresh.Value;
            Settings.Default.ThreshValue = trackBarThresh.Value;
            Settings.Default.Save();
        }

        private void trackBarThresh_Scroll(object sender, EventArgs e)
        {
            labelThreshVal.Text = trackBarThresh.Value.ToString();

        }

        private void button3_Click(object sender, EventArgs e)
        {
            RotationCount = 0;
            _rotaionCount = 0;
            PartInspectionData.TriggerCount = 0;
            labelTriggerCount.Text = $"Trigger Count: {PartInspectionData.TriggerCount}";

        }

        private void label10_Click(object sender, EventArgs e)
        {

        }

        private void radioButtonChart1_CheckedChanged(object sender, EventArgs e)
        {
            chart1.Visible = radioButtonChart1.Checked;

        }

        private void radioButtonChart2_CheckedChanged(object sender, EventArgs e)
        {
            chart2.Visible = radioButtonChart2.Checked;
        }

        private void labelHome_Click(object sender, EventArgs e)
        {

            //HomeStatus => 0 = Not Home, 1 = Dome at home, 2 = Intermediate

            Cursor = Cursors.WaitCursor;
            if (homeStatus == 1)
            {
                PLCControl.writeDataPLC(1, (int)PlcReg.SetIntermediate);
                homeStatus = PLCControl.readDataPLC((int)PlcReg.Home);
            }
            else if (homeStatus == 2 || homeStatus == 0)
            {
                PLCControl.writeDataPLC(1, (int)PlcReg.ReturnHome);
                homeStatus = PLCControl.readDataPLC((int)PlcReg.Home);

            }
            Thread.Sleep(300);
            Cursor = Cursors.Default;
        }


    }
}
