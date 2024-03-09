using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Web;

namespace HelicalGearInspection.Classes
{
    public class DefectData
    {
        public enum DefectType { Ok, SurfaceDefect, ChamferMissUpper, ChamferMissLower };
        [JsonIgnore]
        //public List<string> DefectCategory =  new List<string>{ "Ok", "SurfaceDefect", "ChamferMissUpper", "ChamferMissLower" };
        public int CameraNum =0;
        public int Degree =0;
        public string defType = "";
        public DefectType Type = DefectType.Ok;

        public DefectData(int camNum, int degree, string type)
        {
            CameraNum = camNum;
            Degree = degree;
            defType = type;
        }
        public DefectData()
        {
            
        }
    }
}
