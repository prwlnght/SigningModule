using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Navigation;
using System.Windows.Media.Effects;
using System.ComponentModel;
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
using NDtw;


namespace WpfApplication1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public static String[] id;
        double[] a = { .5, 1, 1.5, 2, 0, 0,  2.5, 3};
        double[] b = { .5, 1, 1.5, 2, 2.5, 3, 3.5, 4};
        

        #region Writing to file
        static StringBuilder csv = new StringBuilder(); // to write data
        static StringBuilder csv1 = new StringBuilder(); // to write the file header 
                              
        String filePath = "C:\\Users\\ppaudyal\\Google Drive\\School\\Fall2016\\NLP\\Project\\Data\\";

        Boolean writeFlag = false;
        static String[] toWrite = { "NA", "NA", "NA", "NA", "NA", "NA", "NA", "NA", "NA", "NA", "NA", "NA", "NA", "NA", "NA", "NA", "NA", "NA", "NA", "NA", "NA", "NA", "NA", "NA", "NA" };
        String fileheader = "HL, HR, ORL, OPL, OYL, ORR, OPR, OYR, ML, MR, LXL, LYL, LZL, LXR, LYR, LZR, HX, HY, HZ, TLX, TLY, TLZ, TRX, TRY, TRZ";
        String Segmenter = "#, #, #, #, #, #, #, #, #, #, #, #, #, #, #, #, #, #, #,  #, #, #, #, #, # ";
        #endregion
        #region Myo Related 
        private readonly IChannel channel, channel2;
        private readonly IHub hub;
        String fileName;

        #endregion
        #region kinect related  declarations

        KinectSensor sensor;
        InfraredFrameReader irReader;
        ColorFrameReader c_reader;
        MultiSourceFrameReader m_reader;
        Boolean colorDisplay, depthDisplay, irDisplay;
        IList<Body> _bodies;
        // index for the currently tracked body
        private int bodyIndex;
        CameraSpacePoint[] CameraSpacePoints;
        KinectJointFilter filter = new KinectJointFilter();
        static int frameCount;
        private bool bodyTracked = false; 
        // change params if you want
        #endregion
        #region skeletal related declarations
        private const double HandSize = 30;
        private const double JointThickness = 3;
        private const double ClipBoundsThickness = 10; //thickness of the clipedge rectangles
        private const float InferredPositionClamp = 0.1f;
        private readonly Brush handClosedBrush = new SolidColorBrush(Color.FromArgb(128, 255, 0, 0));
        private readonly Brush handOpenBrush = new SolidColorBrush(Color.FromArgb(128, 0, 255, 0));
        private readonly Brush handLassoBrush = new SolidColorBrush(Color.FromArgb(128, 0, 0, 255));
        private readonly Brush trackedJointBrush = new SolidColorBrush(Color.FromArgb(255, 68, 192, 68));
        private readonly Brush inferredJointBrush = Brushes.Yellow;
        private readonly Pen inferredBonePen = new Pen(Brushes.Gray, 1);
        private DrawingGroup drawingGroup;
        private DrawingImage imageSource;
        private CoordinateMapper coordinateMapper = null;
        private BodyFrameReader bodyFrameReader = null;
        private List<Tuple<JointType, JointType>> bones;
        private int displayWidth, displayHeight;
        private List<Pen> bodyColors;
        private string statusText = null;
        Joint headJoint;
        DrawingVisual drawingVisual; 
        #endregion

        //private List list = new List[8];
        public static string EMGtxxt, EMGtxxtR;

        public event PropertyChangedEventHandler PropertyChanged;

        public MainWindow()
        {
            InitializeComponent();
            sensor = KinectSensor.GetDefault();
            
            //init class variables 
            frameCount = 0;
            #region Myo related //Initialize Myo Channels from Myo Sharp
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

            }
            #endregion Myo related
            this.coordinateMapper = this.sensor.CoordinateMapper;


            #region skeletal overlay
            this.drawingGroup = new DrawingGroup();
            drawingVisual = new DrawingVisual();
            FrameDescription frameDescription = this.sensor.DepthFrameSource.FrameDescription;//set the display height and width as that of the depth display
            // get size of joint space
            this.displayWidth = frameDescription.Width;
            this.displayHeight = frameDescription.Height;
            #endregion Skeletal overlay
            this.Loaded += MainPage_Loaded;

            var cost = new Dtw(a, b ).GetCost();
            gesture_name.Text = cost.ToString();



        }

        void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            
            
            channel.StartListening();
            m_reader = sensor.OpenMultiSourceFrameReader(FrameSourceTypes.Body | FrameSourceTypes.BodyIndex | FrameSourceTypes.Color | FrameSourceTypes.Depth |
                FrameSourceTypes.Infrared);
            sensor.Open();
            m_reader.MultiSourceFrameArrived += Reader_MultiSourceFrameArrived;
            irDisplay = false;
            StartButton.Click += StartButton_Click;
            StopButton.Click += StopButton_Click;
            Segment.Click += Segment_Click;
            filter.Init();

            
        }

        void Reader_MultiSourceFrameArrived(object sender, MultiSourceFrameArrivedEventArgs e)
        {
            //NOTE this syncs the kinect and myo frequency of data 
            Status_handPose.Text = EMGtxxt + EMGtxxtR;
            //get reference to the multi-frame

            var reference = e.FrameReference.AcquireFrame();
            bool dataReceived = false;
            //open color frame
            using (var frame = reference.BodyFrameReference.AcquireFrame())
            {
                if (frame != null)
                {
                    _bodies = new Body[frame.BodyFrameSource.BodyCount];
                    dataReceived = true;
                    this.DataContext = this;
                    frame.GetAndRefreshBodyData(_bodies);
                }
            }

            //now draw the data to overlay on top
            if (dataReceived)
            {
                Body body = null;
                if (this.bodyTracked)
                {
                    if (this._bodies[this.bodyIndex].IsTracked)
                    {
                        body = this._bodies[this.bodyIndex];
                    }
                    else
                    {
                        bodyTracked = false;
                    }
                }
                if (!bodyTracked)
                {
                    for (int i = 0; i < this._bodies.Count; ++i)
                    {
                        if (this._bodies[i].IsTracked)
                        {
                            this.bodyIndex = i;
                            this.bodyTracked = true;
                            break;
                        }
                    }
                }

                if (body != null && this.bodyTracked && body.IsTracked)
                {
                    #region writetocsv
                    CameraSpacePoint[] filteredJoints = filter.GetFilteredJoints();
                    Status_handPose.Text = "Detected";
                    Joint handRight = body.Joints[JointType.HandRight];
                    Joint handLeft = body.Joints[JointType.HandLeft];
                    Joint thumbLeft = body.Joints[JointType.ThumbLeft];
                    Joint thumbRight = body.Joints[JointType.ThumbRight];

                    headJoint = body.Joints[JointType.Head];
                    toWrite[0] = body.HandRightState.ToString();
                    toWrite[1] = body.HandLeftState.ToString();
                    toWrite[10] = (handLeft.Position.X).ToString();
                    toWrite[11] = (handLeft.Position.Y).ToString();
                    toWrite[12] = (handLeft.Position.Z).ToString();
                    toWrite[13] = (handRight.Position.X).ToString();
                    toWrite[14] = (handRight.Position.Y).ToString();
                    toWrite[15] = (handRight.Position.Z).ToString();
                    toWrite[16] = (headJoint.Position.X).ToString();
                    toWrite[17] = (headJoint.Position.Y).ToString();
                    toWrite[18] = (headJoint.Position.Z).ToString();
                    
                    toWrite[19] = (thumbLeft.Position.X).ToString();
                    toWrite[20] = (thumbLeft.Position.Y).ToString();
                    toWrite[21] = (thumbLeft.Position.Z).ToString();

                    toWrite[22] = (thumbRight.Position.X).ToString();
                    toWrite[23] = (thumbRight.Position.Y).ToString();
                    toWrite[24] = (thumbRight.Position.Z).ToString();

                    if (writeFlag)
                    {
                        String toWrite_string = String.Join(",", toWrite);
                        csv.AppendLine(toWrite_string + ",");
                    }
                    #endregion writetocsv
                    // body represents your single tracked skeleton
                    using (DrawingContext dc = this.drawingGroup.Open())
                    {

                        #region skeletal overlay
                        this.bones = new List<Tuple<JointType, JointType>>();
                        // Torso
                        this.bones.Add(new Tuple<JointType, JointType>(JointType.Head, JointType.Neck));
                        this.bones.Add(new Tuple<JointType, JointType>(JointType.Neck, JointType.SpineShoulder));
                        this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineShoulder, JointType.SpineMid));
                        this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineMid, JointType.SpineBase));
                        this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineShoulder, JointType.ShoulderRight));
                        this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineShoulder, JointType.ShoulderLeft));
                        this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineBase, JointType.HipRight));
                        this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineBase, JointType.HipLeft));

                        // Right Arm
                        this.bones.Add(new Tuple<JointType, JointType>(JointType.ShoulderRight, JointType.ElbowRight));
                        this.bones.Add(new Tuple<JointType, JointType>(JointType.ElbowRight, JointType.WristRight));
                        this.bones.Add(new Tuple<JointType, JointType>(JointType.WristRight, JointType.HandRight));
                        this.bones.Add(new Tuple<JointType, JointType>(JointType.HandRight, JointType.HandTipRight));
                        this.bones.Add(new Tuple<JointType, JointType>(JointType.WristRight, JointType.ThumbRight));

                        // Left Arm
                        this.bones.Add(new Tuple<JointType, JointType>(JointType.ShoulderLeft, JointType.ElbowLeft));
                        this.bones.Add(new Tuple<JointType, JointType>(JointType.ElbowLeft, JointType.WristLeft));
                        this.bones.Add(new Tuple<JointType, JointType>(JointType.WristLeft, JointType.HandLeft));
                        this.bones.Add(new Tuple<JointType, JointType>(JointType.HandLeft, JointType.HandTipLeft));
                        this.bones.Add(new Tuple<JointType, JointType>(JointType.WristLeft, JointType.ThumbLeft));

                        // Right Leg
                        this.bones.Add(new Tuple<JointType, JointType>(JointType.HipRight, JointType.KneeRight));
                        this.bones.Add(new Tuple<JointType, JointType>(JointType.KneeRight, JointType.AnkleRight));
                        this.bones.Add(new Tuple<JointType, JointType>(JointType.AnkleRight, JointType.FootRight));

                        // Left Leg
                        this.bones.Add(new Tuple<JointType, JointType>(JointType.HipLeft, JointType.KneeLeft));
                        this.bones.Add(new Tuple<JointType, JointType>(JointType.KneeLeft, JointType.AnkleLeft));
                        this.bones.Add(new Tuple<JointType, JointType>(JointType.AnkleLeft, JointType.FootLeft));

                        // populate body colors, one for each BodyIndex
                        this.bodyColors = new List<Pen>();

                        this.bodyColors.Add(new Pen(Brushes.Red, 6));
                        this.bodyColors.Add(new Pen(Brushes.Orange, 6));
                        this.bodyColors.Add(new Pen(Brushes.Green, 6));
                        this.bodyColors.Add(new Pen(Brushes.Blue, 6));
                        this.bodyColors.Add(new Pen(Brushes.Indigo, 6));
                        this.bodyColors.Add(new Pen(Brushes.Violet, 6));

                        this.drawingGroup.ClipGeometry = new RectangleGeometry(new Rect(0.0, 0.0, this.displayWidth, this.displayHeight));//prevent spillage outside drawing area

                        dc.DrawRectangle(Brushes.Black, null, new Rect(0.0, 0.0, this.displayWidth, this.displayWidth));

                       
                        int penIndex = 0;

                        Pen drawPen = this.bodyColors[penIndex++];
                        
                        this.DrawClippedEdges(body, dc);
                        IReadOnlyDictionary<JointType, Joint> joints = body.Joints;
                        Dictionary<JointType, Point> jointPoints = new Dictionary<JointType, Point>(); //convert joint points to depth(display) space
                       // bodyCanvas.Children.Clear();
                        foreach (JointType jointType in joints.Keys)
                        {
                            //sometimes the depth(Z) of an inferred joint may show as negative
                            //clamp down to 0.1 to prevent coordinate mapper from returning (-Infinity, -Infitinity)
                            CameraSpacePoint position = joints[jointType].Position;
                            if (position.Z < 0)
                            {
                                position.Z = InferredPositionClamp;
                            }
                            DepthSpacePoint depthSpacePoint = this.coordinateMapper.MapCameraPointToDepthSpace(position); //to map from depth space to camera space ? 
                            jointPoints[jointType] = new Point(depthSpacePoint.X, depthSpacePoint.Y);
                            Ellipse headCircle = new Ellipse() { Width = 50, Height = 50, Fill = new SolidColorBrush(Color.FromArgb(0, 255, 0, 0)) };
                           // bodyCanvas.Children.Add(headCircle);
                           // Canvas.SetLeft(bodyCanvas, depthSpacePoint.X-25);
                            //Canvas.SetTop(bodyCanvas, depthSpacePoint.Y-25);

                    
                        }
                        this.DrawBody(joints, jointPoints, dc, drawPen);

                        this.DrawHand(body.HandLeftState, jointPoints[JointType.HandLeft], dc);
                        this.DrawHand(body.HandRightState, jointPoints[JointType.HandRight], dc);
                    }//end drawingContext 

                    image.Source = new DrawingImage(this.drawingGroup);

                 
                    }//end BodyTracked
                }
    
                    #region skeletal overlay with smoothing
                    #endregion skeletal overlay without smoothing
                    //CameraSpacePoint c = filter.GetFilteredJoints((int)jt);
                    // image.Source = new DrawingImage(this.drawingGroup);
                
                using (var frame = reference.BodyIndexFrameReference.AcquireFrame())
            {
                if (frame != null)
                {
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
                  //  bodyCanvas.Children.Clear();
                   // if (headJoint.TrackingState == TrackingState.Tracked)
                    //{
                    /*
                        DepthSpacePoint dsp = sensor.CoordinateMapper.MapCameraPointToDepthSpace(headJoint.Position);
                        Ellipse headCircle = new Ellipse() { Width = 50, Height = 50, Fill = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0)) };
                        bodyCanvas.Children.Add(headCircle);
                        Canvas.SetLeft(bodyCanvas, 300);
                        Canvas.SetTop(bodyCanvas, 300);*/
                    //}
                }
            }

        }
      

        //Draws a body (bones as connectors between joints
        /// <param name="joints">joints to draw</param>
        /// <param name="jointPoints">translated positions of joints to draw</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        /// <param name="drawingPen">specifies color to draw a specific body</param>

        private void DrawBody (IReadOnlyDictionary<JointType, Joint> joints, IDictionary<JointType, Point> jointPoints, DrawingContext drawingContext, Pen drawingPen)
        {
            foreach(var bone in this.bones)
            {
                this.DrawBone(joints, jointPoints, bone.Item1, bone.Item2, drawingContext, drawingPen);
            }

            foreach(JointType jointType in joints.Keys)
            {
                Brush drawBrush = null;
                TrackingState trackingState = joints[jointType].TrackingState;

                if(trackingState == TrackingState.Tracked)
                {
                    drawBrush = this.trackedJointBrush;
                }
                else if (trackingState == TrackingState.Inferred)
                {
                    drawBrush = this.inferredJointBrush;
                }
                if (drawBrush != null)
                {
                    drawingContext.DrawEllipse(drawBrush, null, jointPoints[jointType], JointThickness, JointThickness);
                }

            }
        }

        /// <summary>
        /// Draws one bone of a body (joint to joint)
        /// </summary>
        /// <param name="joints">joints to draw</param>
        /// <param name="jointPoints">translated positions of joints to draw</param>
        /// <param name="jointType0">first joint of bone to draw</param>
        /// <param name="jointType1">second joint of bone to draw</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        /// /// <param name="drawingPen">specifies color to draw a specific bone</param>
        private void DrawBone(IReadOnlyDictionary<JointType, Joint> joints, IDictionary<JointType, Point> jointPoints, JointType jointType0, JointType jointType1, DrawingContext drawingContext, Pen drawingPen)
        {
            Joint joint0 = joints[jointType0];
            Joint joint1 = joints[jointType1];

            // If we can't find either of these joints, exit
            if (joint0.TrackingState == TrackingState.NotTracked ||
                joint1.TrackingState == TrackingState.NotTracked)
            {
                return;
            }

            // We assume all drawn bones are inferred unless BOTH joints are tracked
            Pen drawPen = this.inferredBonePen;
            if ((joint0.TrackingState == TrackingState.Tracked) && (joint1.TrackingState == TrackingState.Tracked))
            {
                drawPen = drawingPen;
            }

            drawingContext.DrawLine(drawPen, jointPoints[jointType0], jointPoints[jointType1]);
        }

        /// <summary>
        /// Draws a hand symbol if the hand is tracked: red circle - closed, green circle open, blue circle lasso
        /// </summary>
        /// <param name="handState">state of the hand</param>
        /// <param name="handPosition">position of the hand</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        /// <returns></returns>

        private void DrawHand(HandState handState, Point handPosition, DrawingContext drawingContext)
        {
            switch (handState)
            {
                case HandState.Closed:
                    drawingContext.DrawEllipse(this.handClosedBrush, null, handPosition, HandSize, HandSize);
                    break;

                case HandState.Open:
                    drawingContext.DrawEllipse(this.handOpenBrush, null, handPosition, HandSize, HandSize);
                    break;

                case HandState.Lasso:
                    drawingContext.DrawEllipse(this.handLassoBrush, null, handPosition, HandSize, HandSize);
                    break;

            }
        }
        /// <summary>
        /// Draws indicators to show which edges are clipping body data
        /// </summary>
        /// <param name="body">body to draw clipping information for</param>
        /// <param name="drawingContext">drawing context to draw to</param>

        private void DrawClippedEdges(Body body, DrawingContext drawingContext)
        {
            FrameEdges clippedEdges = body.ClippedEdges;

            if (clippedEdges.HasFlag(FrameEdges.Bottom))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(0, this.displayHeight - ClipBoundsThickness, this.displayWidth, ClipBoundsThickness));
            }

            if (clippedEdges.HasFlag(FrameEdges.Top))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(0, 0, this.displayWidth, ClipBoundsThickness));
            }

            if (clippedEdges.HasFlag(FrameEdges.Left))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(0, 0, ClipBoundsThickness, this.displayHeight));
            }

            if (clippedEdges.HasFlag(FrameEdges.Right))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(this.displayWidth - ClipBoundsThickness, 0, ClipBoundsThickness, this.displayHeight));
            }
        }

        #region ImageSources to convert various streams to bitmaps and display as video
        private ImageSource ToBitmap(ColorFrame frame)
        {
            int width = frame.FrameDescription.Width;
            int height = frame.FrameDescription.Height;
            var format = PixelFormats.Bgr32;

            byte[] pixels = new byte[width * height * ((PixelFormats.Bgr32.BitsPerPixel + 7) / 8)];

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
        #endregion Display
        #region Click functions 
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
        void Skeletal_Click(Object sender, EventArgs e)
        {
            irDisplay = false; colorDisplay = false; depthDisplay = false;

        }

        void StartButton_Click(Object sender, RoutedEventArgs e)
        {

            String gestureName = gesture_name.Text.ToString();

            fileName = "" + gestureName + DateTime.Now.ToString("HHmmsstt") + ".csv";
            filePath = filePath + fileName;
            csv1.AppendLine(fileheader);

            File.AppendAllText(filePath, csv1.ToString());
            writeFlag = true;

        }

        void Segment_Click(Object sender, RoutedEventArgs e)
        {
            { 
            //String gestureName = gesture_name.Text.ToString();

            //String fileName = "" + gestureName + DateTime.Now.ToString("HHmmsstt") + ".csv";
         //   filePath = filePath + fileName;
            csv.AppendLine(Segmenter);

           // File.AppendAllText(filePath, csv.ToString());
            }

        }

        void StopButton_Click(Object sender, RoutedEventArgs e)
        {
            writeFlag = false;
            File.AppendAllText(filePath, csv.ToString());
            csv.Clear();
            fileName = "";
        }
        #endregion Click functions
        #region Myo Event Handlers 
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
                toWrite[2] = roll.ToString();
                toWrite[3] = pitch.ToString();
                toWrite[4] = yaw.ToString();

            }
            if (e.Myo.Handle.ToString() == id[1])
            {
                EMGtxxtR = "\nRoll" + roll.ToString() + " Pitch" + pitch.ToString() + " Yaw" + yaw.ToString() + "Left\n";
                toWrite[5] = roll.ToString();
                toWrite[6] = pitch.ToString();
                toWrite[7] = yaw.ToString();
            }
            else
            {
                EMGtxxt = "Oh no!";
            }



        }
        private void Hub_MyoDisconnected(object sender, MyoEventArgs e)
        {
            e.Myo.EmgDataAcquired -= Myo_EmgDataAcquired;
            //Console.WriteLine("Oh no! It looks like {0} arm Myo has disconnected!", e.Myo.Arm);
        }

        private void Hub_MyoConnected(object sender, MyoEventArgs e)
        {

            EMGtxxt += e.Myo.Handle.ToString() + ",";


            id = EMGtxxt.Split(',');
            e.Myo.EmgDataAcquired += Myo_EmgDataAcquired;
            e.Myo.OrientationDataAcquired += Myo_OrientationDataAcquired;

            e.Myo.SetEmgStreaming(true);
        }
        #endregion Myo Event Handlers
        

    }
}
#endregion