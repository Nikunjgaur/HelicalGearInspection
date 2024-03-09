using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Media;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using SpinnakerNET;
using SpinnakerNET.GenApi;
using System.Threading;
using System.Drawing.Imaging;

namespace HelicalGearInspection
{
    public class SpinCamManager
    {

        public string SerialNumber ="";
        public double exposure = 1999;
        ManagedSystem spinnakerSystem;
        ManagedCameraList camList;
        IManagedCamera cam;

        // Configure image event handlers
        public ImageEventListener myImageEventListener = null;
        SemaphoreSlim sema = new SemaphoreSlim(1);

        private void InitializeSpinnaker()
        {
            spinnakerSystem = new ManagedSystem();
            // Print out current library version
            LibraryVersion spinVersion = spinnakerSystem.GetLibraryVersion();
            Console.WriteLine(
                "Spinnaker library version: {0}.{1}.{2}.{3}",
                spinVersion.major,
                spinVersion.minor,
                spinVersion.type,
                spinVersion.build);
        }

        // This function configures the example to execute image events by
        // preparing and registering an image event.
        int ConfigureImageEvents(IManagedCamera cam, ref ImageEventListener eventListenerToConfig)
        {
            int result = 0;
            try
            {
                eventListenerToConfig = new ImageEventListener(cam, ref sema);
                cam.RegisterEventHandler(eventListenerToConfig);
                Console.WriteLine("***Image Event Handler Registered***");
            }
            catch (SpinnakerException ex)
            {
                Console.WriteLine("Error: {0}", ex.Message);
                result = -1;
            }
            return result;
        }

        // Disables or enables heartbeat on GEV cameras so long waits during breakpoints do not cause cameras to drop off
        // timeout errors.
        static int ConfigureGVCPHeartbeat(IManagedCamera cam, bool enable)
        {
            // Retrieve TL device nodemap and print device information
            INodeMap nodeMapTLDevice = cam.GetTLDeviceNodeMap();

            // Retrieve GenICam nodemap
            INodeMap nodeMap = cam.GetNodeMap();

            IEnum iDeviceType = nodeMapTLDevice.GetNode<IEnum>("DeviceType");
            IEnumEntry iDeviceTypeGEV = iDeviceType.GetEntryByName("GigEVision");
            // We first need to confirm that we're working with a GEV camera
            if (iDeviceType != null && iDeviceType.IsReadable)
            {
                if (iDeviceType.Value == iDeviceTypeGEV.Value)
                {
                    if (enable)
                    {
                        Console.WriteLine("Resetting heartbeat");
                    }
                    else
                    {
                        Console.WriteLine("Disabling heartbeat");
                    }
                    IBool iGEVHeartbeatDisable = nodeMap.GetNode<IBool>("GevGVCPHeartbeatDisable");
                    if (iGEVHeartbeatDisable == null || !iGEVHeartbeatDisable.IsWritable)
                    {
                        Console.WriteLine(
                            "Unable to disable heartbeat on camera. Continuing with execution as this may be non-fatal...");
                    }
                    else
                    {
                        iGEVHeartbeatDisable.Value = enable;

                        if (!enable)
                        {
                            Console.WriteLine("         Heartbeat timeout has been disabled for this run. This allows pausing ");
                            Console.WriteLine("         and stepping through  code without camera disconnecting due to a lack ");
                            Console.WriteLine("         of a heartbeat register read.");
                        }
                        else
                        {
                            Console.WriteLine("         Heartbeat timeout has been enabled.");
                        }
                        Console.WriteLine();
                    }
                }
            }
            else
            {
                Console.WriteLine("Unable to access TL device nodemap. Aborting...");
                return -1;
            }

            return 0;
        }

        private void ConfigureCamera(IManagedCamera cam)
        {
            INodeMap snodeMap = cam.GetTLStreamNodeMap();
            IEnum iHandlingMode = snodeMap.GetNode<IEnum>("StreamBufferHandlingMode");
            if (iHandlingMode != null && iHandlingMode.IsWritable && iHandlingMode.IsReadable)
            {
                // Default is oldest first
                IEnumEntry iHandlingModeEntry = iHandlingMode.GetEntryByName("NewestOnly");
                iHandlingMode.Value = iHandlingModeEntry.Value;
                Console.WriteLine("Camera Serial: {0} buffer handling mode set to NewestOnly", cam.DeviceSerialNumber);
            }
        }
        public bool ConnectCamera()
        {
            InitializeSpinnaker();
            bool camInitialized = false;

            // Retrieve list of cameras from the system
            camList = spinnakerSystem.GetCameras();
            Console.WriteLine("Number of cameras detected: {0}", camList.Count);
            if (camList.Count > 0)
            {
                // Grab the input from GUI (assuming it is an int and use it as index to camList[])
                //cam = camList[Int32.Parse(textBox1.Text)];
                for (int i = 0; i < camList.Count; i++)
                {
                    camList[i].Init();

                    if (!camList[i].IsInitialized())
                    {
                        Console.WriteLine("Initialized Camera:{0} Serial:{1} ", cam.DeviceModelName, cam.DeviceSerialNumber);

                    }


                    if (camList[i].DeviceSerialNumber == SerialNumber)
                    {
                        Console.WriteLine("Camera:{0} Serial:{1} ", camList[i].DeviceModelName, camList[i].DeviceSerialNumber);

                        cam = camList[i];
                        camInitialized = true;
                        break;
                    }
                }
               
                if (!camInitialized) { return camInitialized; }
                //if (!cam.IsInitialized())
                //{
                //    cam.Init();
                //    Console.WriteLine("Initialized Camera:{0} Serial:{1} ", cam.DeviceModelName, cam.DeviceSerialNumber);
                //}
                ConfigureTrigger(chosenTrigger);
                ConfigureCamera(cam);
                ConfigureImageEvents(cam, ref myImageEventListener);
                ConfigureGVCPHeartbeat(cam, false);
                cam.BeginAcquisition();
                Console.WriteLine("Acquisition Started");
            }
            else
            {
                Console.WriteLine("No cameras connected");
            }

            return camInitialized;
        }



        // Use the following enum and global static variable to select whether
        // a software or hardware trigger is used.
        public enum TriggerType
        {
            Software,
            Hardware
        }

        public static TriggerType chosenTrigger = TriggerType.Hardware;

        // This function configures the camera to use a trigger. First, trigger
        // mode is set to off in order to select the trigger source. Once the
        // trigger source has been selected, trigger mode is then enabled,
        // which has the camera capture only a single image upon the execution
        // of the chosen trigger.
        public int ConfigureTrigger(TriggerType triggerType)
        {

            INodeMap nodeMap = cam.GetNodeMap();
            int result = 0;

            try
            {
                Console.WriteLine("\n\n*** CONFIGURING TRIGGER ***\n\n");

                Console.WriteLine(
                    "Note that if the application / user software triggers faster than frame time, the trigger may be dropped / skipped by the camera.");
                Console.WriteLine(
                    "If several frames are needed per trigger, a more reliable alternative for such case, is to use the multi-frame mode.\n");

                if (chosenTrigger == TriggerType.Software)
                {
                    Console.WriteLine("Software trigger chosen...\n");
                }
                else if (chosenTrigger == TriggerType.Hardware)
                {
                    Console.WriteLine("Hardware trigger chosen...\n");
                }

                //
                // Ensure trigger mode off
                //
                // *** NOTES ***
                // The trigger must be disabled in order to configure whether
                // the source is software or hardware.
                //
                IEnum iTriggerMode = nodeMap.GetNode<IEnum>("TriggerMode");
                if (iTriggerMode == null || !iTriggerMode.IsWritable || !iTriggerMode.IsReadable)
                {
                    Console.WriteLine("Unable to disable trigger mode (enum retrieval). Aborting...");
                    return -1;
                }

                IEnumEntry iTriggerModeOff = iTriggerMode.GetEntryByName("Off");
                if (iTriggerModeOff == null || !iTriggerModeOff.IsReadable)
                {
                    Console.WriteLine("Unable to disable trigger mode (entry retrieval). Aborting...");
                    return -1;
                }

                iTriggerMode.Value = iTriggerModeOff.Value;

                Console.WriteLine("Trigger mode disabled...");

                //
                // Set TriggerSelector to FrameStart
                //
                // *** NOTES ***
                // For this example, the trigger selector should be set to frame start.
                // This is the default for most cameras.
                //
                IEnum iTriggerSelector = nodeMap.GetNode<IEnum>("TriggerSelector");
                if (iTriggerSelector == null || !iTriggerSelector.IsWritable || !iTriggerSelector.IsReadable)
                {
                    Console.WriteLine("Unable to set trigger selector (enum retrieval). Aborting...");
                    return -1;
                }

                // Set trigger mode to software
                IEnumEntry iTriggerSelectorFrameStarts = iTriggerSelector.GetEntryByName("FrameStart");
                if (iTriggerSelectorFrameStarts == null || !iTriggerSelectorFrameStarts.IsReadable)
                {
                    Console.WriteLine("Unable to set software trigger selector (entry retrieval). Aborting...");
                    return -1;
                }

                iTriggerSelector.Value = iTriggerSelectorFrameStarts.Value;

                Console.WriteLine("Trigger selector set to frame start...");

                //
                // Select trigger source
                //
                // *** NOTES ***
                // The trigger source must be set to hardware or software while
                // trigger mode is off.
                //
                IEnum iTriggerSource = nodeMap.GetNode<IEnum>("TriggerSource");
                if (iTriggerSource == null || !iTriggerSource.IsWritable || !iTriggerSource.IsReadable)
                {
                    Console.WriteLine("Unable to set trigger mode (enum retrieval). Aborting...");
                    return -1;
                }

                if (chosenTrigger == TriggerType.Software)
                {
                    // Set trigger mode to software
                    IEnumEntry iTriggerSourceSoftware = iTriggerSource.GetEntryByName("Software");
                    if (iTriggerSourceSoftware == null || !iTriggerSourceSoftware.IsReadable)
                    {
                        Console.WriteLine("Unable to set software trigger mode (entry retrieval). Aborting...");
                        return -1;
                    }

                    iTriggerSource.Value = iTriggerSourceSoftware.Value;

                    Console.WriteLine("Trigger source set to software...");
                }
                else if (chosenTrigger == TriggerType.Hardware)
                {
                    // Set trigger mode to hardware ('Line0')
                    IEnumEntry iTriggerSourceHardware = iTriggerSource.GetEntryByName("Line0");
                    if (iTriggerSourceHardware == null || !iTriggerSourceHardware.IsReadable)
                    {
                        Console.WriteLine("Unable to set hardware trigger mode (entry retrieval). Aborting...");
                        return -1;
                    }

                    iTriggerSource.Value = iTriggerSourceHardware.Value;

                    Console.WriteLine("Trigger source set to hardware...");
                }

                //
                // Turn trigger mode on
                //
                // *** LATER ***
                // Once the appropriate trigger source has been set, turn
                // trigger mode on in order to retrieve images using the
                // trigger.
                //

                IEnumEntry iTriggerModeOn = iTriggerMode.GetEntryByName("On");
                if (iTriggerModeOn == null || !iTriggerModeOn.IsReadable)
                {
                    Console.WriteLine("Unable to enable trigger mode (entry retrieval). Aborting...");
                    return -1;
                }

                iTriggerMode.Value = iTriggerModeOn.Value;

                // NOTE: Blackfly and Flea3 GEV cameras need 1 second delay after trigger mode is turned on

                Console.WriteLine("Trigger mode enabled...");
            }
            catch (SpinnakerException ex)
            {
                Console.WriteLine("Error: {0}", ex.Message);
                result = -1;
            }

            return result;
        }


        public int ConfigureExposure(double exposureTimeToSet)
        {
            INodeMap nodeMap = cam.GetNodeMap();

            int result = 0;

            Console.WriteLine("\n\n*** CONFIGURING EXPOSURE ***\n");

            try
            {
                //
                // Turn off automatic exposure mode
                //
                // *** NOTES ***
                // Automatic exposure prevents the manual configuration of
                // exposure time and needs to be turned off.
                //
                // *** LATER ***
                // Exposure time can be set automatically or manually as needed.
                // This example turns automatic exposure off to set it manually
                // and back on in order to return the camera to its default
                // state.
                //
                IEnum iExposureAuto = nodeMap.GetNode<IEnum>("ExposureAuto");
                if (iExposureAuto == null || !iExposureAuto.IsReadable || !iExposureAuto.IsWritable)
                {
                    Console.WriteLine("Unable to disable automatic exposure (enum retrieval). Aborting...\n");
                    return -1;
                }

                IEnumEntry iExposureAutoOff = iExposureAuto.GetEntryByName("Off");
                if (iExposureAutoOff == null || !iExposureAutoOff.IsReadable)
                {
                    Console.WriteLine("Unable to disable automatic exposure (entry retrieval). Aborting...\n");
                    return -1;
                }

                iExposureAuto.Value = iExposureAutoOff.Value;

                Console.WriteLine("Automatic exposure disabled...");

                //
                // Set exposure time manually; exposure time recorded in microseconds
                //
                // *** NOTES ***
                // The node is checked for availability and writability prior
                // to the setting of the node. Further, it is ensured that the
                // desired exposure time does not exceed the maximum. Exposure
                // time is counted in microseconds. This information can be
                // found out either by retrieving the unit with the GetUnit()
                // method or by checking SpinView.
                //
                //const double exposureTimeToSet = 500000.0;

                IFloat iExposureTime = nodeMap.GetNode<IFloat>("ExposureTime");
                if (iExposureTime == null || !iExposureTime.IsReadable || !iExposureTime.IsWritable)
                {
                    Console.WriteLine("Unable to set exposure time. Aborting...\n");
                    return -1;
                }

                // Ensure desired exposure time does not exceed the maximum
                iExposureTime.Value = (exposureTimeToSet > iExposureTime.Max ? iExposureTime.Max : exposureTimeToSet);

                Console.WriteLine("Exposure time set to {0} us...\n", iExposureTime.Value);
            }
            catch (SpinnakerException ex)
            {
                Console.WriteLine("Error: {0}", ex.Message);
                result = -1;
            }

            return result;
        }



        private void CleanupSpinnaker()
        {
            try
            {
                // Clear camera list before releasing system
                if (cam.IsValid())
                {
                    cam.UnregisterEventHandler(myImageEventListener);
                    cam.EndAcquisition();
                    Console.WriteLine("Stream Stopped");

                    // This enables heartbeat again
                    ConfigureGVCPHeartbeat(cam, true);
                    cam.DeInit();
                    camList.Clear();
                }
                // Release system
                spinnakerSystem.Dispose();
            }
            catch (SpinnakerException ex)
            {
                Console.WriteLine("Exception during cleanup: {0}", ex);
            }

        }

    }

    public class ImageEventListener : ManagedImageEventHandler
    {

        private string deviceSerialNumber;
        public int imageCnt;
        List<IManagedImage> ConvertList;
        IManagedImageProcessor processor;
        public event EventHandler<Bitmap> BitmapRecievedEvent;

        //public PictureBox imageEventPictureBox;
        SemaphoreSlim displayMutex;

        // The constructor retrieves the serial number and initializes the

        public void InvokeBitmapEvent(Bitmap bitmap)
        {
            BitmapRecievedEvent?.Invoke(this, bitmap.Clone(new Rectangle(0, 0, bitmap.Width, bitmap.Height), PixelFormat.Format24bppRgb));

        }

        // image counter to 0.
        public ImageEventListener(IManagedCamera cam, ref SemaphoreSlim displayMutexInput)
        {
            // Double buffer
            ConvertList = new List<IManagedImage>();
            ConvertList.Add(new ManagedImage());
            ConvertList.Add(new ManagedImage());

            // Initialize image counter to 0
            imageCnt = 0;
            //imageEventPictureBox = pictureBoxInput;
            displayMutex = displayMutexInput;
            deviceSerialNumber = "";

            // Retrieve device serial number
            INodeMap nodeMap = cam.GetTLDeviceNodeMap();
            IString iDeviceSerialNumber = nodeMap.GetNode<IString>("DeviceSerialNumber");
            if (iDeviceSerialNumber != null && iDeviceSerialNumber.IsReadable)
            {

                deviceSerialNumber = iDeviceSerialNumber.Value;
            }
            Console.WriteLine("ImageEvent initialized for camera serial: {0}", deviceSerialNumber);
            processor = new ManagedImageProcessor();
        }

        ~ImageEventListener()
        {
            //Cleanup double buffer
            if (ConvertList != null)
            {
                foreach (var item in ConvertList)
                {
                    item.Dispose();
                }
            }
        }
        // This method defines an image event. In it, the image that
        // triggered the event is converted and saved before incrementing
        // the count. Please see Acquisition_CSharp example for more
        // in-depth comments on the acquisition of images.
        override protected void OnImageEvent(ManagedImage image)
        {
            // Example console print
            // Print Only every x to not overwhelme the console log and cause slowdown
            if (image.FrameID % 100 == 0)
            {
                Console.WriteLine("Image event! (We are only printing every 100 counts..) FrameID:{0}, ImageStatus:{1}",
                   image.FrameID,
                   image.ImageStatus.ToString()
                   );
            }

            // Alternate between the two images we've pre-allocated (double buffering or ping-pong buffering); this is to make sure that the 
            // bitmap is not overwritten mid-rendering on the picturebox, which causes exceptions either on some painting
            // events or when picturebox is resized 
            IManagedImage doubleBufferImage = ConvertList[(int)image.FrameID % 2];

            // The mutex could potentially alleviate clashes if image events arrive faster 
            // than the image can be processed
            if (displayMutex.Wait(TimeSpan.Zero))
            {
                try
                {
                    using (IManagedImage convertedImage = processor.Convert(image, PixelFormatEnums.BGR8))
                    {
                        doubleBufferImage.DeepCopy(convertedImage);
                        //imageEventPictureBox.Image = doubleBufferImage.bitmap;
                        Bitmap bitmap = doubleBufferImage.bitmap;

                        BitmapRecievedEvent?.Invoke(this, bitmap.Clone(new Rectangle(0, 0, bitmap.Width, bitmap.Height), PixelFormat.Format24bppRgb));

                    } // ConvertedImage is disposed here and freed by falling out of 'using' scope
                }
                catch (SpinnakerException ex)
                {
                    Console.WriteLine("Exception: {0} ", ex);
                }
                finally
                {
                    image.Release();
                    displayMutex.Release();
                }
            }
            else
            {
                // If this line is being printed then our bitmap/rendering path is not fast enough
                Console.WriteLine("Not processing FrameID: {0} as previous one is still being processed", image.FrameID);
                // Make sure to release the image so it can be requeued
                image.Release();
            }
        }
    }

}
