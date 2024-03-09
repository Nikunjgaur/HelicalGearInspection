using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace HelicalGearInspection.CustomControl
{
    public partial class StatusControl : UserControl
    {
        public StatusControl(string text, bool status)
        {
            InitializeComponent();
            labelStatus.Text = text;
            if (status ) { buttonStatus.BackColor = Color.LimeGreen; }
            else { buttonStatus.BackColor = Color.Red;}
        }

        public StatusControl(string labelText, string buttonText)
        {
            InitializeComponent();
            labelStatus.Text = labelText;
            buttonStatus.Text = buttonText;
            buttonStatus.Enabled = false;
        }

        bool _status = false;
        public bool Status {
            set 
            {
                _status = value;
                if (_status) { buttonStatus.BackColor = Color.LimeGreen; }
                else { buttonStatus.BackColor = Color.Red; }
            } 
        }

        public StatusControl()
        {
            InitializeComponent();
        }

        private void StatusControl_Load(object sender, EventArgs e)
        {

        }
    }
}
