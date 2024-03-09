using Newtonsoft.Json;
using SpinnakerNET.GenApi;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace HelicalGearInspection
{


    

    public class ApiDefectData
    {
        public string defType;
    }

    class ApiController
    {
        public static string Ip = "127.0.0.1";
        public static string Port = "5000";
        public ApiController()
        {
            // ip = @"C:\Users\Administrator\Desktop\cylOCR\application\backed\app_code\testcode.py";
            // ip = ApiController.GetLocalIPAddress();
            // ip = GetLocalIPAddress();
            // ip = "192.168.0.116";
            // ip = "169.254.25.114";
            Console.WriteLine("ip from contructor " + Ip);
        }
        //D:\application\backed\app_code-test\testcode.py

        struct ApiPara
        {
            public string image;
            public int thr;
            public int camCode;
        }

        public static string ProcessImage(Bitmap inputImage, int camCode)
        {
            ApiPara para = new ApiPara();

            para.image = bitmap_to_base_64_string(inputImage);
            para.thr = AppData.ThresholdValue;
            para.camCode = camCode;
            string ResponseString = "";
            HttpWebResponse response = null;
            try
            {
                var request = (HttpWebRequest)WebRequest.Create($"http://{Ip}:{"5000"}/predict_cover");
                request.Accept = "application/json";
                request.Method = "POST";
                JavaScriptSerializer jss = new JavaScriptSerializer();

                //var imageDict = new Dictionary<string, string>
                //{
                //    { "image", imageString }
                //};
                var myContent = jss.Serialize(para);

                //Console.WriteLine(JsonConvert.SerializeObject(myContent));

                var data = Encoding.ASCII.GetBytes(myContent);

                request.ContentType = "application/json";
                request.ContentLength = data.Length;

                Console.WriteLine("request write ");
                using (var stream = request.GetRequestStream())
                {
                    stream.Write(data, 0, data.Length);
                }

                response = (HttpWebResponse)request.GetResponse();
                ResponseString = new StreamReader(response.GetResponseStream()).ReadToEnd();

                //ConsoleExtension.WriteWithColor(ResponseString);

            }
            catch (WebException ex)
            {
                Console.WriteLine(  ex.Message);
                return ex.ToString();
                //if (ex.Status == WebExceptionStatus.ProtocolError)
                //{
                //    response = (HttpWebResponse)ex.Response;

                //    ServerError error = new ServerError();
                //    error.Message = "server error";
                //    error.Prediction_time_var = "null";
                //    JavaScriptSerializer jss = new JavaScriptSerializer();
                //    ResponseString = new JavaScriptSerializer().Serialize(error);

                //}
                //else
                //{

                //    ServerError error = new ServerError();
                //    error.Message = "server error";
                //    error.Prediction_time_var = "null";
                //    JavaScriptSerializer jss = new JavaScriptSerializer();
                //    ResponseString = new JavaScriptSerializer().Serialize(error);


                //}
            }


            return ResponseString;
        }

        private static string bitmap_to_base_64_string(Bitmap _image)
        {
            string base64String = "";
            using (MemoryStream m = new MemoryStream())
            {
                _image.Save(m, ImageFormat.Jpeg);
                byte[] imageBytes = m.ToArray();
                base64String = Convert.ToBase64String(imageBytes);
            }

            return base64String;

        }//_base_64_string


        public string loadNetwork()
        {


            //MyObject ob = new MyObject();
            //ob.Name = "rabiranjan kumar ";
            //ob.image_url = @"test.bmp";

            string ResponseString = "";
            HttpWebResponse response = null;
            try
            {

                string req_url = "http://" + Ip + ":5030/api/v1/web/loadnetwork";

                var request = (HttpWebRequest)WebRequest.Create(req_url);
                request.Accept = "application/json";//*"application/xml";*/
                request.Method = "POST";
                JavaScriptSerializer jss = new JavaScriptSerializer();
                //var myContent = jss.Serialize(ob);

                //var data = Encoding.ASCII.GetBytes(myContent);

                //request.ContentLength = data.Length;
                request.ContentType = "application/json";

                using (var stream = request.GetRequestStream())
                {
                    //stream.Write(data, 0, data.Length);
                }

                response = (HttpWebResponse)request.GetResponse();


                ResponseString = new StreamReader(response.GetResponseStream()).ReadToEnd();

                ResponseString = "loaded";



            }
            catch (WebException ex)
            {
                if (ex.Status == WebExceptionStatus.ProtocolError)
                {

                    response = (HttpWebResponse)ex.Response;

                    ServerError error = new ServerError();
                    error.Message = "server error";
                    error.Prediction_time_var = "null";


                    JavaScriptSerializer jss = new JavaScriptSerializer();

                    ResponseString = new JavaScriptSerializer().Serialize(error);

                    ResponseString = "Notloaded";
                    Console.WriteLine(ex.Message);
                    Console.WriteLine((ex.Response as HttpWebResponse)?.StatusCode);

                    // Console.WriteLine("Load  Load ");

                }
                else
                {
                    ResponseString = "Some error occured: " + ex.Status.ToString();
                    ResponseString = "Notloaded";
                    Console.WriteLine(ex.Message);

                }
            }


            return ResponseString;
        }//loadNetwork


        public static async Task ShutdownServer(int port)
        {
            await Task.Delay(100);
            string baseUrl = $"http://{Ip}:{port}"; 

            using (var httpClient = new HttpClient())
            {
                try
                {
                    // Send a GET request to the /shutdown endpoint
                    HttpResponseMessage response = await httpClient.GetAsync($"{baseUrl}/shutdown");

                    if (response.IsSuccessStatusCode)
                    {
                        // Server shutdown request was successful
                        Console.WriteLine($"Server {port} shutdown request sent successfully.");
                    }
                    else
                    {
                        Console.WriteLine($"Server {port} shutdown request failed with status code: " + response.StatusCode);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("An error occurred: " + ex.Message);
                }
            }
        }

        public bool checkServerRuuning()
        {
            string ResponseString = "";

            HttpWebResponse response = null;
            try
            {
                var request = (HttpWebRequest)WebRequest.Create($"http://{Ip}:{Port}/api/v1/web/servercheck");
                request.Accept = "application/json"; //"application/xml";
                request.Method = "POST";
                JavaScriptSerializer jss = new JavaScriptSerializer();
                //var myContent = jss.Serialize(ob);

                //var data = Encoding.ASCII.GetBytes(myContent);

                request.ContentType = "application/json";
                //request.ContentLength = data.Length;

                using (var stream = request.GetRequestStream())
                {
                    //stream.Write(data, 0, data.Length);
                }

                response = (HttpWebResponse)request.GetResponse();
                // Console.Write((int)response.StatusCode + "\n");
                try
                {
                    ResponseString = new StreamReader(response.GetResponseStream()).ReadToEnd();

                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
            catch (WebException ex)
            {
                if (ex.Status == WebExceptionStatus.ProtocolError)
                {
                    response = (HttpWebResponse)ex.Response;
                    ResponseString = "Some error occured: " + response.StatusCode.ToString();
                    return false;
                }
                else
                {
                    ResponseString = "Some error occured: " + ex.Status.ToString();
                    return false;
                }
            }


            dynamic dynJson = JsonConvert.DeserializeObject(ResponseString);
            string temp = dynJson.code;

            if ("200".Equals(temp))
            {

                return true;
            }

            return false;

        }

        ////--------My Code
        public static string GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                    //Console.WriteLine("Local Ip=" + ip);
                }
            }
            throw new Exception("No network adapters with an IPv4 address in the system!");
        }
        ////--------My code End --------


       public static Process processAnaconda;

        public static (string text, bool status) RunAnacondaCmd(int appCode = 0)
        {
            try
            {

                // Set working directory and create process
                var workingDirectory = $@"{AppData.ProjectDirectory}\DeepLearningModule\yolov5_TeethDent";
                processAnaconda = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        RedirectStandardInput = true,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        WorkingDirectory = workingDirectory
                    }
                };
                processAnaconda.Start();
                // Pass multiple commands to cmd.exe
                using (var sw = processAnaconda.StandardInput)
                {
                    if (sw.BaseStream.CanWrite)
                    {
                        // Vital to activate Anaconda
                        sw.WriteLine("C:\\Users\\Admin\\anaconda3\\Scripts\\activate.bat");
                        // Activate your environment
                        sw.WriteLine("conda activate pytorch");
                        // run your script. You can also pass in arguments
                        //ConsoleExtension.WriteWithColor($"File name for server is {fileName}", ConsoleColor.Green);
                        //> uvicorn api_yoloV5_1: app1--host 127.0.0.1--port 5000
                        sw.WriteLine($"uvicorn api_yoloV5_{appCode}:app{appCode} --host 127.0.0.1 --port 500{appCode - 1}");
                    }
                }

                // read multiple output lines
                //while (!processAnaconda.StandardOutput.EndOfStream)
                //{
                //    var line = processAnaconda.StandardOutput.ReadLine();
                //    //Console.WriteLine(line);
                //}
                ConsoleExtension.WriteWithColor("Api Started successfully", ConsoleColor.Yellow);
                return ("Api Running", true);

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error while starting Detection API {ex.Message}");
                return ("Api not running", false);
            }
        }
    }

    class ServerModel
    {
        public string imageString { get; set; }
    }
    class ServerError
    {

        private string message;
        private string prediction_time_var;

        public string Message
        {
            get
            {
                return message;
            }

            set
            {
                message = value;
            }
        }

        public string Prediction_time_var
        {
            get
            {
                return prediction_time_var;
            }

            set
            {
                prediction_time_var = value;
            }
        }

    }
}
