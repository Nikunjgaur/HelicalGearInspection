using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using HelicalGearInspection.Classes;

namespace HelicalGearInspection
{
    class AppData
    {
        public enum Mode {Idol, Inspection, Setup, Restart }
        static string globalProjectDirectory = Environment.CurrentDirectory;
        public static string ProjectDirectory = Directory.GetParent(globalProjectDirectory).Parent.Parent.FullName;
        public static Mode AppMode = Mode.Idol;
        public static ModelData ModelDataObj = new ModelData();
        public static ModelData SelectedModel = new ModelData();
        public static string SelectedModelFolder=null;
        public static int ThresholdValue = 100;
    }
    enum PlcReg
    {
        Home,
        AutoManual,
        ReturnHome,
        SetTestPose,
        TestPoseDone,
        NewCycle,
        ErrorOverTravel,
        ErrorNoData,
        SwReady,
        ErrorReset,
        PlcSatus,
        ModelSetup,
        LightForceOn,
        IncompleteTrigger,
        InspectionResult,
        SetIntermediate,
        OnIntermediate,
        ModelPosValue
    }
}
