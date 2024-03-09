using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using S7.Net;
using S7.Net.Types;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;


//Required nuget package S7netplus
namespace HelicalGearInspection
{
    class PLCControl
    {
        public static bool plcConnected = false;
        static Plc mplc;

        public static CpuType PLCmodel = CpuType.S71200;
        public static string PLCip = "192.168.1.1";
        public static int rack_no = 0;
        public static int slot_no = 1;

        public static string DB_no = "DB1";// include BD ie. DB12 // use DB1 for for S7-200 Series
     
        public static bool connectToPLC()
        {
            if (!(mplc == null))
            {
                if (mplc.IsConnected)
                {
                    plcConnected = true;
                    Console.WriteLine("PLC already connected. returning");
                    return true;
                }
            }
            mplc = new Plc(CpuType.S71200, "192.168.1.1", 0, 1);
            try
            {
                mplc.Open();
                if (mplc.IsConnected)
                {
                    plcConnected = true;

                    Console.WriteLine("Plc connected successfuly.");
                }
                else
                {
                    plcConnected = false;

                    Console.WriteLine("Unable to connect to PLC.");

                }
                return plcConnected;
            }
            catch (Exception e)
            {
                plcConnected = false;

                Console.WriteLine("Unable to connect to PLC" + e.Message.ToString());
                return plcConnected;
            }

        }
        public static bool disconnectPLC()
        {
            if (mplc == null)
            {
                plcConnected = false;

                return true;

            }
            try
            {
                if (mplc.IsConnected)
                {
                    mplc.Close();
                    plcConnected = false;

                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Unable to disconnect PLC" + ex.Message.ToString());
                plcConnected = false;

                return false;
            }
            return false;
        }
        public static int readDataPLC(dynamic register)
        {
            // Multipying register value with 4 as register values are in the multiple of 4
            register *= 4;
            if (plcConnected)
            {
                return Conversion.ConvertToInt((uint)mplc.Read(DB_no + ".DBD" + register));
            }
            else
            {
                connectToPLC();
                if (plcConnected)
                    return Conversion.ConvertToInt((uint)mplc.Read(DB_no + ".DBD" + register));
                else

                    return 0;
            }
        }
       
        public static bool writeDataPLC(dynamic value, int register)
        {
            // Multipying register value with 4 as register values are in the multiple of 4

            value = Conversion.ConvertToFloat((uint)value);
            register *= 4;
            if (plcConnected)
            {
                mplc.Write(DB_no + ".DBD" + register, value);
                return true;
            }
            else
            {
                connectToPLC();
                if (plcConnected)
                {
                    mplc.Write(DB_no + ".DBD" + register, value);
                    return true;
                }
                else
                {
                    return false;
                }

            }
        }


        public static bool writeJogValuePLC(dynamic value, float register)
        {
            // Multipying register value with 4 as register values are in the multiple of 4

            register *= 4;

            if (plcConnected)
            {
                mplc.Write(DB_no + ".DBD" + register, value);
                return true;
            }
            else
            {
                connectToPLC();
                if (plcConnected)
                {
                    mplc.Write(DB_no + ".DBD" + register, value);
                    return true;
                }
                else
                {
                    return false;
                }

            }
        }


        public static float ReadReg()
        {
            string variableAddress = "DB1.DBD120";

            if (plcConnected)
            {
                return Conversion.ConvertToFloat((uint)mplc.Read(variableAddress));

            }
            else
            {
                connectToPLC();
                if (plcConnected)
                    return Conversion.ConvertToFloat((uint)mplc.Read(variableAddress));
                else
                    return -1;
            }

        }


    }
}
