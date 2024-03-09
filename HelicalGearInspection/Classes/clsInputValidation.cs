using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Modbus
{
    class clsInputValidation

    {
        private static string m_oldText = "";
        private static string m_functionCode = "";
        private static string m_functionData = "";
        private static int m_totalLengthData = 4;
        private static int m_lenOfReg = 4;

        
        
        public static string functionCode
        {
            set { m_functionCode = value; }
            get { return m_functionCode; }
        }

        public static string oldText
        {
            set { m_oldText = value;}
            get {return m_oldText;}
        }

        public static string dataText
        {
            set { m_functionData = value; }
            get { return m_functionData; }
        }

        public static int totalLengthData
        {
            set { m_totalLengthData = value; }
            get { return m_totalLengthData; }
        }

        public static int lengthOfRegis
        {
            set { m_lenOfReg = value; }
            get { return m_lenOfReg; }
        }


        #region Check for hex
        public static void ValidHex(string register)
        {
            Regex rx = new Regex("[^A-F|^0-9|^ |^\t]");
            if (rx.IsMatch(register))

                throw new Exception("Enter a valid HEX number");
        }
        #endregion

        #region set function parameters

        public static void function(int item)
        {
            switch (item)
            {

                case 0:
                    functionCode = "01";
                    totalLengthData = 2;
                    break;
                case 1:
                    functionCode = "02";
                    totalLengthData = 2;
                    break;
                case 2:
                    functionCode = "03";
                    totalLengthData = 4;
                    break;
                case 3:
                    functionCode = "05";
                    totalLengthData = 1;
                    break;
                case 4:
                    functionCode = "06";
                    totalLengthData = 4;
                    break;
                case 5:
                    functionCode = "0F";
                    totalLengthData = 2;
                    break;
                case 6:
                    functionCode = "10";
                    totalLengthData = 2;
                    break;

            }

              

        }
        #endregion 

        #region Check for Dec

        public static void validDec(string textin)
        {
            Regex rx = new Regex("[^0-9]");
            if (rx.IsMatch(textin))

                throw new Exception("Enter a valid HEX number");
        }
        #endregion

        #region DataCode

        public static string dataCode(string textIn)
        {
            return textIn;
        }
        #endregion 




    }
}
