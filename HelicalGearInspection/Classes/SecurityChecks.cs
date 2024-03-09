using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HelicalGearInspection
{
    public static class SecurityChecks
    {

        public static bool CamerasReady = false;
        //public static bool ServersReady = false;
        public static bool PlcConnected = false;
        //public static bool SoftwareReadyTest = false;
        public static bool MachineInAutoMode = false;

        public static bool AllChecksPassed()
        {
            foreach (var field in typeof(SecurityChecks).GetFields())
            {
                if (field.FieldType == typeof(bool))
                {
                    bool value = (bool)field.GetValue(null);
                    Console.WriteLine($"{field.Name} {value}");
                    if (!value)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        //public static string[] GetFalseParameters()
        //{

        //}

    }
}
