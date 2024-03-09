using HelicalGearInspection.CustomControl;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace HelicalGearInspection
{
    public partial class PLCDataForm : Form
    {
        public PLCDataForm()
        {
            InitializeComponent();
        }

        private void PLCDataForm_Load(object sender, EventArgs e)
        {
            int regNumber = 0;
            foreach (string name in Enum.GetNames(typeof(PlcReg)))
            {
                string regName = name;
                switch (name)
                {
                    case "Home":
                    case "TestPoseDone":
                    case "NewCycle":
                    case "ErrorOverTravel":
                    case "ErrorNoData":
                    case "PlcSatus":
                        regName += " (read)";
                        break;

                    default:
                        regName += " (write)";
                        break;
                }
                StatusControl statusControl = new StatusControl(regName.AddSpacesBeforeUppercase(), $"D{regNumber}");
                panelPlcData.Controls.Add(statusControl);
               
                regNumber++;
            }
        }
    }
}
