using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace HelicalGearInspection
{
    internal static class Program
    {
        public static string SaveModelFolder ="Default";
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new InspectionForm());
        }

        //public static Bitmap CompressBitmapImage(Bitmap bitmap ,int quality)
        //{


        //}
       
        public static Bitmap CompressBitmapImage(Bitmap bmp,int quality)
        {
            using(Bitmap bmp1 = new Bitmap(bmp))
            {
                ImageCodecInfo jpgEncoder =  GetEncoder(ImageFormat.Jpeg);
                System.Drawing.Imaging.Encoder qualityEncoder = System.Drawing.Imaging.Encoder.Quality;
                EncoderParameters myEncoderParameters = new EncoderParameters(1);
                EncoderParameter myEncoderParameter = new EncoderParameter(qualityEncoder,quality);
                myEncoderParameters.Param[0]= myEncoderParameter;
                using(MemoryStream memoryStream = new MemoryStream())
                {
                    bmp1.Save(memoryStream,jpgEncoder,myEncoderParameters);
                    Bitmap compressBitmap = new Bitmap(memoryStream);
                    return compressBitmap;
                }
            }
        }
            
        private static ImageCodecInfo GetEncoder(ImageFormat format)
        {
            ImageCodecInfo[] codes = ImageCodecInfo.GetImageDecoders();
            foreach (ImageCodecInfo code in codes)
            {
                if (code.FormatID == format.Guid)
                {
                    return code;
                }
            }
            return null;

        }
        public static void ChangeButtonText(Button button,string btnText)
        {
            button.Invoke(new Action(() => 
            {
                button.Text = btnText;
              
                button.Invalidate();
                button.Refresh();

            }));
        }



    }
}
