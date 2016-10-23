﻿using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Kinect;
using MyoSharp.Communication;

using MyoSharp.Device;
using MyoSharp.Poses;
using MyoSharp.Exceptions;
using MyoSharp.ConsoleSample.Internal;



namespace WpfApplication1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static String[] id;

        private readonly IChannel channel, channel2;
        private readonly IHub hub;
        Boolean writeFlag = false;

        String[] toWrite = { "NA", "NA", "NA", "NA", "NA", "NA", "NA", "NA", "NA", "NA", "NA", "NA", "NA", "NA", "NA", "NA" };
        String fileheader = "HL, HR, ORL, OPL, OYL, ORR, OPR, OYR, ML, MR, LXL, LYL, LZL, LXR, LYR, LZR"; 

        static StringBuilder csv = new StringBuilder();
        static StringBuilder csv1 = new StringBuilder();
        String filePath =  "C:\\Users\\ppaudyal\\Google Drive\\School\\Fall2016\\NLP\\Project\\Data\\";


        //private List list = new List[8];
        public static string EMGtxxt, EMGtxxtR;
        public MainWindow()
        {
            
            InitializeComponent();
            channel = Channel.Create(ChannelDriver.Create(ChannelBridge.Create()));
            channel2 = Channel.Create(ChannelDriver.Create(ChannelBridge.Create()));

            hub = Hub.Create(channel);
            {

                // listen for when the Myo connects
                hub.MyoConnected += Hub_MyoConnected;

                // listen for when the Myo disconnects
                hub.MyoDisconnected += (sender1, e1) =>
                {
                    //  Console.WriteLine("Oh no! It looks like {0} arm Myo has disconnected!", e.Myo.Arm);
                    e1.Myo.PoseChanged -= Myo_PoseChanged;
                };

                // wait on user input
                //ConsoleHelper.UserInputLoop(hub);
                
            }

            this.Loaded += MainPage_Loaded;
        }

        KinectSensor sensor;
        InfraredFrameReader irReader;
        ColorFrameReader c_reader;
        MultiSourceFrameReader m_reader;
        Boolean colorDisplay, depthDisplay, irDisplay;
        IList<Body> _bodies;

        void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            //only one that is connected
            sensor = KinectSensor.GetDefault();
            channel.StartListening();
    
            //irreader for this
           // irReader = sensor.InfraredFrameSource.OpenReader();

            m_reader = sensor.OpenMultiSourceFrameReader(FrameSourceTypes.Body | FrameSourceTypes.BodyIndex | FrameSourceTypes.Color | FrameSourceTypes.Depth |
                FrameSourceTypes.Infrared);

            c_reader = sensor.ColorFrameSource.OpenReader();
            

            sensor.Open();
            m_reader.MultiSourceFrameArrived += Reader_MultiSourceFrameArrived;

            c_reader.FrameArrived += Reader_rgbSourceFrameArrived;

            colorDisplay = true;

            StartButton.Click += StartButton_Click;
            StopButton.Click += StopButton_Click;


        }

       void Reader_rgbSourceFrameArrived(object sender, ColorFrameArrivedEventArgs e)
        {

            //combine towrite in to a string
          if (writeFlag)
           {
                String toWrite_string = String.Join(",", toWrite);
                csv.AppendLine(toWrite_string);

                File.AppendAllText(filePath, csv.ToString());
           }

        }

        void Reader_MultiSourceFrameArrived (object sender, MultiSourceFrameArrivedEventArgs e)
        {
            //NOTE this syncs the kinect and myo frequency of data 
            Status_handPose.Text = EMGtxxt + EMGtxxtR;
            //get reference to the multi-frame

            var reference = e.FrameReference.AcquireFrame();

            //open color frame
            using (var frame = reference.BodyFrameReference.AcquireFrame())
            {
                if(frame != null)
                {

                    _bodies = new Body[frame.BodyFrameSource.BodyCount];

                    frame.GetAndRefreshBodyData(_bodies); 

                    foreach(var body in _bodies)
                    {
                        if(body != null)
                        {
                            if (body.IsTracked)
                            {
                                Joint handRight = body.Joints[JointType.HandRight];
                                Joint thumbRight = body.Joints[JointType.ThumbRight];

                                float thumbRight_px = thumbRight.Position.X;
                                float thumbRight_py = thumbRight.Position.Y;
                                float thumbRight_pz = thumbRight.Position.Z;

                                Joint handLeft = body.Joints[JointType.HandLeft];
                                Joint thumbLeft = body.Joints[JointType.ThumbLeft];

                                toWrite[0] = body.HandRightState.ToString(); 
                                toWrite[1] = body.HandLeftState.ToString(); 

                                //var newLine = string.Format("O,C,L,U");



                                // Status_handPose.Text = "RT" + " X:" + thumbRight_px + " Y:" + thumbRight_py + " Z:" + thumbRight_pz;


                                //now getting hand states from the data 
                                // Status_handPose.Text = "R: " + body.HandRightState + "L: " + body.HandLeftState + "thumbs X:"+thumbRight_px.ToString()+ "thumbs Y:" + thumbRight_py.ToString() + "thumbs Z:" + thumbRight_pz.ToString();

                            }
                        }
                    }
                    //do something. 
                }
            }
            using (var frame = reference.BodyIndexFrameReference.AcquireFrame())
            {
                if (frame != null)
                {
                   
                    //do something. 
                }
            }
            using (var frame = reference.ColorFrameReference.AcquireFrame())
            {
                if ((frame != null) && colorDisplay) 
                {
                    image.Source = ToBitmap(frame); 
                }
            }
            using (var frame = reference.DepthFrameReference.AcquireFrame())
            {
                if ((frame != null) && depthDisplay)
                {
                    image.Source = ToBitmap(frame);
                }
            }
            using (var frame = reference.InfraredFrameReference.AcquireFrame())
            {
                if ((frame != null) && irDisplay)
                {
                    image.Source = ToBitmap(frame);
                }
            }
           
        }

        private ImageSource ToBitmap(ColorFrame frame)
        {
            int width = frame.FrameDescription.Width;
            int height = frame.FrameDescription.Height;
            var format = PixelFormats.Bgr32;

            byte[] pixels = new byte[width * height * ((PixelFormats.Bgr32.BitsPerPixel + 7)/8)];

            if (frame.RawColorImageFormat == ColorImageFormat.Bgra)
            {
                frame.CopyRawFrameDataToArray(pixels); 
            }
            else
            {
                frame.CopyConvertedFrameDataToArray(pixels, ColorImageFormat.Bgra);
            }

            int stride = width * format.BitsPerPixel / 8;
            return BitmapSource.Create(width, height, 96, 96, format, null, pixels, stride); 
        }
        private ImageSource ToBitmap(DepthFrame frame)
        {
            int width = frame.FrameDescription.Width;
            int height = frame.FrameDescription.Height;
            var format = PixelFormats.Bgr32;

            

            ushort minDepth = frame.DepthMinReliableDistance;
            ushort maxDepth = frame.DepthMaxReliableDistance;

            ushort[] depthData = new ushort[width * height];
            byte[] pixelData = new byte[width * height * (PixelFormats.Bgr32.BitsPerPixel + 7) / 8];

            frame.CopyFrameDataToArray(depthData);

            int colorIndex = 0;
            for (int depthIndex = 0; depthIndex < depthData.Length; ++depthIndex)
            {
                ushort depth = depthData[depthIndex];
                byte intensity = (byte)(depth >= minDepth && depth <= maxDepth ? depth : 0);

                pixelData[colorIndex++] = intensity; // Blue
                pixelData[colorIndex++] = intensity; // Green
                pixelData[colorIndex++] = intensity; // Red

                ++colorIndex;
            }

            int stride = width * format.BitsPerPixel / 8;

            return BitmapSource.Create(width, height, 96, 96, format, null, pixelData, stride);
        }
        private ImageSource ToBitmap(InfraredFrame frame)
        {
            int width = frame.FrameDescription.Width;
            int height = frame.FrameDescription.Height;
            var format = PixelFormats.Bgr32;

            ushort[] infraredData = new ushort[width * height];
            byte[] pixelData = new byte[width * height * (PixelFormats.Bgr32.BitsPerPixel + 7) / 8];

            frame.CopyFrameDataToArray(infraredData);

            int colorIndex = 0;
            for (int infraredIndex = 0; infraredIndex < infraredData.Length; ++infraredIndex)
            {
                ushort ir = infraredData[infraredIndex];
                byte intensity = (byte)(ir >> 8);

                pixelData[colorIndex++] = intensity; // Blue
                pixelData[colorIndex++] = intensity; // Green   
                pixelData[colorIndex++] = intensity; // Red

                ++colorIndex;
            }

            int stride = width * format.BitsPerPixel / 8;

            return BitmapSource.Create(width, height, 96, 96, format, null, pixelData, stride);
        }

        void Infrared_Click(Object sender, EventArgs e)
        {
            irDisplay = true; colorDisplay = false; depthDisplay = false;

        }
        void Depth_Click(Object sender, EventArgs e)
        {
            irDisplay = false; colorDisplay = false; depthDisplay = true;

        }
        void Color_Click(Object sender, EventArgs e)
        {
            irDisplay = false; colorDisplay = true; depthDisplay = false;

        }

        void StartButton_Click(Object sender, RoutedEventArgs e)
        {

            String gestureName = gesture_name.ToString(); 

            String fileName = "gestureName" + DateTime.Now.ToString("HHmmsstt") + ".csv";
            filePath = filePath + fileName;
            csv1.AppendLine(fileheader);

            File.AppendAllText(filePath, csv1.ToString());
            writeFlag = true;

        }

        void StopButton_Click(Object sender, RoutedEventArgs e)
        {
            writeFlag = false;
        }

        private static void Myo_PoseChanged(object sender, PoseEventArgs e)
        {
            //Status_handPose.Text = "Myo pose:" + e.Myo.Pose;
            // Console.WriteLine("{0} arm Myo detected {1} pose!", e.Myo.Arm, e.Myo.Pose);
            //return e.Myo.Pose;
        }

        private static void Myo_Unlocked(object sender, MyoEventArgs e)
        {
           // Console.WriteLine("{0} arm Myo has unlocked!", e.Myo.Arm);
        }

        private static void Myo_Locked(object sender, MyoEventArgs e)
        {
            //Console.WriteLine("{0} arm Myo has locked!", e.Myo.Arm);
        }
        private void Myo_EmgDataAcquired(object sender, EmgDataEventArgs e)
        {

          
            
           
        }
        private static void Myo_OrientationDataAcquired(object sender, OrientationDataEventArgs e)
        {
            const float PI = (float)System.Math.PI;

            // convert the values to a 0-9 scale (for easier digestion/understanding)
            var roll = (int)((e.Roll + PI) / (PI * 2.0f) * 10);
            var pitch = (int)((e.Pitch + PI) / (PI * 2.0f) * 10);
            var yaw = (int)((e.Yaw + PI) / (PI * 2.0f) * 10);
            if (e.Myo.Handle.ToString() == id[0])
            {
                EMGtxxt = "\nRoll" + roll.ToString() + " Pitch" + pitch.ToString() + " Yaw" + yaw.ToString() + "Right\n";
            }
            if (e.Myo.Handle.ToString() == id[1])
            {
                    EMGtxxtR = "\nRoll" + roll.ToString() + " Pitch" + pitch.ToString() + " Yaw" + yaw.ToString() + "Left\n";
            }
            else
            {
                EMGtxxt = "Oh no!";
            }
            
            

        }
        private void Hub_MyoDisconnected(object sender, MyoEventArgs e)
        {
            e.Myo.EmgDataAcquired -= Myo_EmgDataAcquired;
        }

        private void Hub_MyoConnected(object sender, MyoEventArgs e)
        {

            EMGtxxt += e.Myo.Handle.ToString()+",";


            
            id = EMGtxxt.Split(',');
            e.Myo.EmgDataAcquired += Myo_EmgDataAcquired;
            e.Myo.OrientationDataAcquired += Myo_OrientationDataAcquired;

            e.Myo.SetEmgStreaming(true);
        }


        
    }
}
