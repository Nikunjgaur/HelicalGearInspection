using HelicalGearInspection.Classes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Windows.Forms;
using Newtonsoft.Json;
namespace HelicalGearInspection
{
    public partial class ModelSetup : Form
    {
        public ModelSetup()
        {
            InitializeComponent();
            comboBoxModel.LoadDirectoryNames($"{AppData.ProjectDirectory}/Models");
        }
        decimal domePosition = 0;
        private void ModelSetup_Load(object sender, EventArgs e)
        {
            AppData.AppMode = AppData.Mode.Setup;
            AppData.ModelDataObj.Position = 0;
        }

        private void ModelSetup_FormClosing(object sender, FormClosingEventArgs e)
        {
            AppData.AppMode = AppData.Mode.Inspection;
            
        }

        private void ModelSetup_FormClosed(object sender, FormClosedEventArgs e)
        {
            AppData.AppMode= AppData.Mode.Inspection;
        }

        private void buttonSave_Click(object sender, EventArgs e)
        {
            ModelData modelData = new ModelData();
            modelData.Name = comboBoxModel.Text;
            modelData.Position = (float)domePosition;
            modelData.Remarks = "remarks";
            string folderPath = $@"{AppData.ProjectDirectory}/Models/{modelData.Name}";
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
                File.WriteAllText($@"{folderPath}/ModelData.json", JsonConvert.SerializeObject(modelData, Formatting.Indented));
                MessageBox.Show("Model data saved successfully");
            }
            else
            {
                File.WriteAllText($@"{folderPath}/ModelData.json", JsonConvert.SerializeObject(modelData, Formatting.Indented));
                MessageBox.Show("Model data saved successfully");
            }
        }

        private void buttonMoveDome_Click(object sender, EventArgs e)
        {
            float jogValue = (float)numericUpDownJog.Value;
            PLCControl.writeJogValuePLC(jogValue, (int)PlcReg.ModelPosValue);
            PLCControl.writeDataPLC(1, (int)PlcReg.SetTestPose);

            domePosition += numericUpDownJog.Value;

            labelDomeLocation.Text = domePosition.ToString("N1");
        }
    }
}
