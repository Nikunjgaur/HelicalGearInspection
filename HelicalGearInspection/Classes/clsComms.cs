using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.IO.Ports;
using System.Windows.Forms;


namespace Modbus
{
    public static class clsComms
    {
       
        #region Port Settings

        private static string m_port = "COM2";  // previous it was 7
        private static int m_baud = 9600;
        private static int m_stopBits = 1;
        private static int m_parity = 2 ;
        private static int m_dataBits = 7;

        
        public static string port
        {
            get { return m_port; }      
            set { m_port = value; }
        }
        public static int baudRate
        { 
            get { return m_baud; }
            set { m_baud = value; }
        }

        public static int stopBits
        {
            get { return m_stopBits; }
            set { m_stopBits = value; }
        }

        public static int parity
        {
            get { return m_parity; }
            set { m_parity = value; }
        }

        public static int dataBits
        {
            get { return m_dataBits; }
            set { m_dataBits = value; }
        }
        #endregion

        

        #region Transmit data

        public static string Read(string write)
        {
            string readBuffer = "";
            string sendWord = write;

            string hexTotal = checkSum(sendWord);

            SerialPort sp = new SerialPort();

            string writeBuffer = string.Concat(":", sendWord, hexTotal, "\r");

            switch (m_parity)
            {
                case 0:
                    sp.Parity = Parity.None;
                    break;
                case 1:
                    sp.Parity = Parity.Odd;
                    break;
                case 2:
                    sp.Parity = Parity.Even;
                    break;
            }

            switch (m_stopBits)
            {
                case 0:
                    sp.StopBits = StopBits.None;
                    break;
                case 1:
                    sp.StopBits = StopBits.One;
                    break;
                case 2:
                    sp.StopBits = StopBits.OnePointFive;
                    break;
                case 3:
                    sp.StopBits = StopBits.Two;
                    break;
            }

            sp.PortName = m_port;
            sp.BaudRate = m_baud;
            sp.DataBits = m_dataBits;
            sp.ReadTimeout = 3000;
            sp.WriteTimeout = 5000;

            try
            {
                sp.Open();
                sp.WriteLine(writeBuffer);

                try
                {
                   
                    
                    readBuffer = sp.ReadLine();
                }
                catch (System.Exception ex)
                {
                    readBuffer  = ex.Message;
                }
                        



                sp.Close();
            }
            catch (UnauthorizedAccessException )
            {
                //MessageBox.Show("Port Already in use by another application");
              //  GlobalVariable.plcOnline = false;
                return writeBuffer;
                
            }
          // GlobalVariable.plcOnline = true;
            return readBuffer;
        }       

        #endregion

        #region Convert to hex
        public static string toHex(int val)
        {
            int a, b;
            int decVal = val;
            string hexByte = "", hexTotal = "";
            double i;

            for (i = 0; decVal > 0; i++)
            {
                b = Convert.ToInt32(System.Math.Pow(16.0, i));
                a = decVal % 16;
                decVal /= 16;
                if (a <= 9)
                    hexByte = a.ToString();
                else
                {
                    switch (a)
                    {
                        case 10:
                            hexByte = "A";
                            break;
                        case 11:
                            hexByte = "B";
                            break;
                        case 12:
                            hexByte = "C";
                            break;
                        case 13:
                            hexByte = "D";
                            break;
                        case 14:
                            hexByte = "E";
                            break;
                        case 15:
                            hexByte = "F";
                            break;
                    }
                }
                hexTotal = String.Concat(hexByte, hexTotal);
            }
            for ( ; i < 4.0; i++)
                hexTotal = String.Concat("0", hexTotal);

            return hexTotal;
        }
        #endregion

        #region CheckSum
        public static string checkSum(string writeUncheck)
        {
            char[] hexArray = new char[writeUncheck.Length];
            hexArray = writeUncheck.ToCharArray();
            int decNum = 0, decNumMSB= 0, decNumLSB = 0;
            int decByte, decByteTotal = 0;

            bool msb = true;
            
            for( int t = 0; t <= hexArray.GetUpperBound(0); t++)
            {
                if((hexArray[t]>= 48) && (hexArray[t] <= 57))
                
                    decNum = (hexArray[t] - 48);
                
                else if ( (hexArray[t] >= 65) & (hexArray[t] <= 70))
                    decNum = 10 + (hexArray[t] - 65);

                if (msb)
                {
                    decNumMSB = decNum * 16;
                    msb = false;
                }
                else 
                {
                    decNumLSB = decNum;
                    msb = true;
                }
                if (msb)
                {
                    decByte = decNumMSB + decNumLSB;
                    decByteTotal += decByte;
                }
            }

            decByteTotal = (255 - decByteTotal) + 1;
            decByteTotal = decByteTotal & 255;

            int a, b = 0;
            
            string hexByte = "", hexTotal = "";
            double i;

            for (i = 0; decByteTotal > 0; i++)
            {
                b = Convert.ToInt32(System.Math.Pow(16.0, i));
                a = decByteTotal % 16;
                decByteTotal /= 16;
                if (a <= 9)
                    hexByte = a.ToString();
                else
                {
                    switch (a)
                    {
                        case 10:
                            hexByte = "A";
                            break;
                        case 11:
                            hexByte = "B";
                            break;
                        case 12:
                            hexByte = "C";
                            break;
                        case 13:
                            hexByte = "D";
                            break;
                        case 14:
                            hexByte = "E";
                            break;
                        case 15:
                            hexByte = "F";
                            break;
                    }
                }
                hexTotal = String.Concat(hexByte, hexTotal);
            }
            

            return hexTotal;
    
        }


        #endregion

        #region To String

        public static double toDec(string ch, int start, int numOfBytes)
        {
            char[] hexArray = new char[ch.Length];
            hexArray = ch.ToCharArray();

            double decTotal = 0;
            int x = numOfBytes - 1;

            int upper = hexArray.GetUpperBound(0);
            if ( upper >= start + x)
            {
                for (int i = 0; i < numOfBytes; i++)
                { 
                    int j = start + i;
                    if ((ch[j] >= 48) && ( ch[j] <= 57))
                        decTotal += (ch[j] - 48) * System.Math.Pow(16.0, Convert.ToDouble(x));
                    else if ((ch[j] >= 65) && (ch[j] <= 70))
                        decTotal += (ch[j] - 55) * System.Math.Pow(16.0, Convert.ToDouble(x));


                    x--;
                }
            }
            return decTotal;

        }



        #endregion



    }      
    
}
