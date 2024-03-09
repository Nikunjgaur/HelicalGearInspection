using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Printing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HelicalGearInspection.Classes
{
    class PartInspectionData
    {

        // this list have the image and defect data wrt image
        public List<(Bitmap, List<DefectData>)> InspectionList = new List<(Bitmap, List<DefectData>)>();
        private const int TriggerLimit = 30;
        public static int DegreeInOneFrame = 12;

        public static int TriggerCount { get { return _triggerCount; } set 
            {
                _triggerCount = value;

                // TriggerLimit eqauls to one part inspected
                if (_triggerCount >= TriggerLimit) 
                {
                    _partsInspected++;
                    _triggerCount = 0;
                    //InspectionList.Clear();
                    OnPartInspected?.Invoke();
                }
            }
        }
        public static int TotalNgParts = 0;
        public static bool NgPartDetected = false;
        public static int TotalOkParts = 0;
        private static int _triggerCount = 0;
        public static int PartsInspected { get { return _partsInspected; } 
            set 
            {
                _partsInspected = value;
            } }
        private static int _partsInspected = 0;
        public static event Action OnPartInspected;
    }
}
