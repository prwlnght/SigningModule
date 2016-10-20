using System;
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


namespace WpfApplication1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            this.Loaded += MainPage_Loaded;
        }

        KinectSensor sensor;
        InfraredFrameReader irReader;
        MultiSourceFrameReader m_reader;
        Boolean colorDisplay, depthDisplay, irDisplay;

        void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            //only one that is connected
            sensor = KinectSensor.GetDefault();

            //irreader for this
            irReader = sensor.InfraredFrameSource.OpenReader();

            m_reader = sensor.OpenMultiSourceFrameReader(FrameSourceTypes.Body | FrameSourceTypes.BodyIndex | FrameSourceTypes.Color | FrameSourceTypes.Depth |
                FrameSourceTypes.Infrared);

            sensor.Open();
            m_reader.MultiSourceFrameArrived += Reader_MultiSourceFrameArrived;

            colorDisplay = true;



        }

       void Reader_MultiSourceFrameArrived (object sender, MultiSourceFrameArrivedEventArgs e)
        {
            //get reference to the multi-frame
            var reference = e.FrameReference.AcquireFrame();

            //open color frame
            using (var frame = reference.BodyFrameReference.AcquireFrame())
            {
                if(frame != null)
                {
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
    }
}
