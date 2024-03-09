using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Net.Mime.MediaTypeNames;

namespace HelicalGearInspection
{
    public partial class Selection_Model_Form : Form
    {
        private void CreateModelFolder()
        {
            //foreach (var str in CbBoxModel.Items)
            //{
            //    Directory.CreateDirectory(Directory.CreateDirectory(CbBoxModel.str.Item));

            //}

            //Z:\Images\1
            //Directory.CreateDirectory("Z:/Images/1WithoutGrinding");
            List<string> ModelFolderName = new List<string>();
            ModelFolderName.Clear();
            foreach (string item in CbBoxModel.Items) // load all item into list
            {
                ModelFolderName.Add(item);
            }
            foreach (string folderName in ModelFolderName)  //Create Model Folder into Cam 1
            {
                string trimmedFolderName = "Z:/Images/1/" + folderName;
                //  string trimmedfolderName = "$@\"Z:\\Images\\1" +""+ trimmedfolderName = folderName.Trim();
                Console.WriteLine(trimmedFolderName);
                //foreach (char invalidChar in Path.GetInvalidFileNameChars())
                //{
                //    trimmedFolderName = trimmedFolderName.Replace(invalidChar, '_');
                //}
                if (!Directory.Exists(trimmedFolderName))
                {
                    Directory.CreateDirectory(trimmedFolderName);
                    Console.WriteLine("Create Directory Successfully");
                }
                else
                {
                    Console.WriteLine("Already Created Directory");
                }
            }
            foreach (string folderName in ModelFolderName)  //Create Model Folder into Cam 2
            {
                string trimmedFolderName = "Z:/Images/2/" + folderName;
                //  string trimmedfolderName = "$@\"Z:\\Images\\1" +""+ trimmedfolderName = folderName.Trim();
                if (!Directory.Exists(trimmedFolderName))
                {
                    Directory.CreateDirectory(trimmedFolderName);
                    Console.WriteLine("Create Directory Successfully");
                }
                else
                {
                    Console.WriteLine("Already Created Directory");
                }
            }


        }
        public Selection_Model_Form()
        {
            InitializeComponent();
        }


        private void Selection_Model_Form_Load(object sender, EventArgs e)
        {
            CreateModelFolder();
            Console.WriteLine("Model by Sachin loaded  succesfully");
        }

        private void CbBoxModel_SelectedIndexChanged(object sender, EventArgs e)
        {
            string SelectedModel = CbBoxModel.SelectedItem.ToString();
            Console.WriteLine("Save Model by Sachin=" + SelectedModel);
            Program.SaveModelFolder = SelectedModel;           

            this.Close();

            //AppData.SelectedModelFolder = SelectedModel;
        }
        
    }
}
