
namespace Hpi.Hci.Bachelorproject1617.PhotoBooth
{
    using System;
    using System.Globalization;
    using System.IO;
    using System.Windows;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using Microsoft.Kinect;
    using Microsoft.Kinect.Toolkit;
    using Microsoft.Kinect.Toolkit.BackgroundRemoval;
    using System.Drawing.Imaging;
    using System.Runtime.InteropServices;
    using Color = System.Drawing.Color;
    using Point = System.Windows.Point;
    using Size = System.Windows.Size;
    using Brush = System.Windows.Media.Brush;
    using Brushes = System.Windows.Media.Brushes;
    using Bitmap = System.Drawing.Bitmap;
    using PointF = System.Drawing.PointF;
    using System.Diagnostics;
    using System.Text;
    using System.Net.Sockets;
    using Svg;
    using System.Collections.Generic;
    using Svg.Pathing;
    using Microsoft.Speech.Recognition;
    using Microsoft.Speech.AudioFormat;
    using System.Speech.Synthesis;
 
    using HPI.HCI.Bachelorproject1617.PhotoBooth;
    using System.Timers;
    using System.Linq;
    using System.Xml;
    using System.Text.RegularExpressions;



    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 
    public partial class MainWindow : Window, IDisposable
    {

        public bool AlreadyConvertedToSVG = false;
        public bool BluetoothOn = true;
        
        ConnectionManager connectionManager;

        //skeleton vars 
        
        private const int scale = 100;
        private const int svgWidth = 640;
        private const int svgHeight = 480;
        int nearestSkeleton = 0;
        

        public String svgImage = "";

        //speech

        
        RecognizerInfo ri;
        SpeechHandler speechHandler; 
        static SpeechSynthesizer reader;

        public SpeechInteraction speechInteraction;

        //timestamps
        public DateTime lastSkeletonTimeStamp;

        Timer timer;


        SkeletonHandler skeletonHandler;

        
        /// <summary>
        /// Active Kinect sensor
        /// </summary>
        private KinectSensor sensor;

        /// <summary>
        /// Drawing group for skeleton rendering output
        /// </summary>
        private DrawingGroup drawingGroup;

        /// <summary>
        /// Drawing image that we will display
        /// </summary>
        private DrawingImage imageSource;


        public bool pictureTaken = false;

        /// <summary>
        /// Format we will use for the depth stream
        /// </summary>
        private const DepthImageFormat DepthFormat = DepthImageFormat.Resolution320x240Fps30;

        /// <summary>
        /// Format we will use for the color stream
        /// </summary>
        private const ColorImageFormat ColorFormat = ColorImageFormat.RgbResolution640x480Fps30;

        /// <summary>
        /// Bitmap that will hold color information
        /// </summary>
        private WriteableBitmap foregroundBitmap;

        /// <summary>
        /// Active Kinect sensor
        /// </summary>
        private KinectSensorChooser sensorChooser;

        /// <summary>
        /// Our core library which does background 
        /// </summary>
        private BackgroundRemovedColorStream backgroundRemovedColorStream;

        /// <summary>
        /// Intermediate storage for the skeleton data received from the sensor
        /// </summary>
        private Skeleton[] skeletons;

        /// <summary>
        /// the skeleton that is currently tracked by the app
        /// </summary>
        private int currentlyTrackedSkeletonId;

        /// <summary>
        /// Track whether Dispose has been called
        /// </summary>
        private bool disposed;

        /// <summary>
        /// Initializes a new instance of the MainWindow class.
        /// </summary>
        public MainWindow()
        {
            this.InitializeComponent();

            // initialize the sensor chooser and UI
            this.sensorChooser = new KinectSensorChooser();
            this.sensorChooserUi.KinectSensorChooser = this.sensorChooser;
            this.sensorChooser.KinectChanged += this.SensorChooserOnKinectChanged;
            this.sensorChooser.Start();
            this.drawingGroup = new DrawingGroup();

            // Create an image source that we can use in our image control
            this.imageSource = new DrawingImage(this.drawingGroup);

            // Display the drawing using our image control
            this.Image.Source = this.imageSource;
            

            //speech-in and -out
            reader = new SpeechSynthesizer();
            foreach (InstalledVoice voice in reader.GetInstalledVoices())
            {
                Console.WriteLine(voice.VoiceInfo.Description + " " + voice.VoiceInfo.Name);
            }
            reader.SelectVoice("Microsoft Zira Desktop");
            speechInteraction = new SpeechInteraction(this, reader);
            
            //Connection stuff here 

            connectionManager = new ConnectionManager(BluetoothOn, reader, speechInteraction);

            lastSkeletonTimeStamp = DateTime.Now;
        }


        

        /// <summary>
        /// Finalizes an instance of the MainWindow class.
        /// This destructor will run only if the Dispose method does not get called.
        /// </summary>
        ~MainWindow()
        {
            this.Dispose(false);
            if (connectionManager.client != null)
            {
                AsynchronousClient.StopClient(connectionManager.client);
            }
        }

        /// <summary>
        /// Dispose the allocated frame buffers and reconstruction.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);

            // This object will be cleaned up by the Dispose method.
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Frees all memory associated with the FusionImageFrame.
        /// </summary>
        /// <param name="disposing">Whether the function was called from Dispose.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (null != this.backgroundRemovedColorStream)
                {
                    this.backgroundRemovedColorStream.Dispose();
                    this.backgroundRemovedColorStream = null;
                }

                this.disposed = true;
            }
        }

       

        /// <summary>
        /// Execute shutdown tasks
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void WindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            this.sensorChooser.Stop();
            this.sensorChooser = null;
            if (null != this.sensor)
            {
                this.sensor.Stop();
                this.sensor.AudioSource.Stop();
                this.sensor = null;
            }


            if (null != this.speechHandler)
            {
                speechHandler.StopSpeechHandler();
            }      

        }

        
        /// <summary>
        /// Event handler for Kinect sensor's DepthFrameReady event
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void SensorAllFramesReady(object sender, AllFramesReadyEventArgs e)
        {
            // in the middle of shutting down, or lingering events from previous sensor, do nothing here.
            if (null == this.sensorChooser || null == this.sensorChooser.Kinect || this.sensorChooser.Kinect != sender)
            {
                return;
            }

            /*if (this.checkBoxDepthStream.IsChecked.GetValueOrDefault())
            {*/

            
                try
                {
                    using (var depthFrame = e.OpenDepthImageFrame())
                    {
                        if (null != depthFrame)
                        {
                            this.backgroundRemovedColorStream.ProcessDepth(depthFrame.GetRawPixelData(), depthFrame.Timestamp);
                        }
                    }

                    using (var colorFrame = e.OpenColorImageFrame())
                    {
                        if (null != colorFrame)
                        {
                            this.backgroundRemovedColorStream.ProcessColor(colorFrame.GetRawPixelData(), colorFrame.Timestamp);
                        }
                    }

                    using (var skeletonFrame = e.OpenSkeletonFrame())
                    {
                        
                        if (null != skeletonFrame)
                        {
                            
                            skeletonFrame.CopySkeletonDataTo(this.skeletons);
                            this.backgroundRemovedColorStream.ProcessSkeleton(this.skeletons, skeletonFrame.Timestamp);

                        }

                        
                    
                    }

                    this.ChooseSkeleton();
                }
                catch (InvalidOperationException)
                {
                    // Ignore the exception. 
                }
            //}
        }

        /// <summary>
        /// Handle the background removed color frame ready event. The frame obtained from the background removed
        /// color stream is in RGBA format.
        /// </summary>
        /// <param name="sender">object that sends the event</param>
        /// <param name="e">argument of the event</param>
        private void BackgroundRemovedFrameReadyHandler(object sender, BackgroundRemovedColorFrameReadyEventArgs e)
        {
            using (var backgroundRemovedFrame = e.OpenBackgroundRemovedColorFrame())
            {
                if (backgroundRemovedFrame != null)
                {
                    if (null == this.foregroundBitmap || this.foregroundBitmap.PixelWidth != backgroundRemovedFrame.Width 
                        || this.foregroundBitmap.PixelHeight != backgroundRemovedFrame.Height)
                    {
                        this.foregroundBitmap = new WriteableBitmap(backgroundRemovedFrame.Width, backgroundRemovedFrame.Height, 96.0, 96.0, PixelFormats.Bgra32, null);
                       
                        // Set the image we display to point to the bitmap where we'll put the image data
                        this.MaskedColor.Source = this.foregroundBitmap;
                    }

                    byte[] pixelData = new byte[backgroundRemovedFrame.PixelDataLength];
                    backgroundRemovedFrame.CopyPixelDataTo(pixelData);

                    for (int i = 0; i < pixelData.Length; i += BackgroundRemovedColorFrame.BytesPerPixel)
                    {
                        pixelData[i] = 0;
                        pixelData[i + 1] = 102;
                        pixelData[i + 2] = 0;
                    }

                    if (!pictureTaken)
                    {

                        // Write the pixel data into our bitmap
                
                        this.foregroundBitmap.WritePixels(
                        new Int32Rect(0, 0, this.foregroundBitmap.PixelWidth, this.foregroundBitmap.PixelHeight),
                        pixelData,
                        this.foregroundBitmap.PixelWidth * sizeof(int),
                        0);

                    }
                    else
                    {
                        if (speechInteraction.outlines)
                        {
                            if (!AlreadyConvertedToSVG)
                            {
                                GetSVGOutlinesString();
                                AlreadyConvertedToSVG = true;
                            }
                        }
                        
                    }

                }
            }
        }


        
        //generates the outline svg and saves it - the real printing is triggered from the speechinteraction-class
        private void GetSVGOutlinesString()
        {
            int colorWidth = this.foregroundBitmap.PixelWidth;
            int colorHeight = this.foregroundBitmap.PixelHeight;

            // create a render target that we'll render our controls to
            var renderBitmap = new RenderTargetBitmap(colorWidth, colorHeight, 96.0, 96.0, PixelFormats.Pbgra32);

            var dv = new DrawingVisual();
            using (var dc = dv.RenderOpen())
            {
                // render the backdrop
                var backdropBrush = new VisualBrush(Backdrop);
                dc.DrawRectangle(backdropBrush, null, new Rect(new Point(), new Size(colorWidth, colorHeight)));

                // render the color image masked out by players
                var colorBrush = new VisualBrush(MaskedColor);
                dc.DrawRectangle(colorBrush, null, new Rect(new Point(), new Size(colorWidth, colorHeight)));
            }

            renderBitmap.Render(dv);

            // create a bitmap encoder which knows how to save a .bmpfile
            BitmapEncoder encoder = new BmpBitmapEncoder();

            // create frame from the writable bitmap and add to encoder
            encoder.Frames.Add(BitmapFrame.Create(renderBitmap));



            var path = "Z:\\Daten\\Bachelorprojekt1617\\Kinect\\potrace-1.14.win64\\result.bmp";
            // write the new file to disk

            try
            {
                using (var fs = new FileStream(path, FileMode.Create))
                {
                    encoder.Save(fs);
                }

            }
            catch (IOException)
            {
            }

            //Bitmap bmp = BmpFromByteArray(pixelData, backgroundRemovedFrame.Width, backgroundRemovedFrame.Height);
            String svgOutlineString = GenerateOutlineSVGFromBitmap(path);
            svgImage = svgOutlineString;
            AddSVGFrame(svgOutlineString);

        }

        //start external program potrace to trace the image, later read the content of the output-file
        private String GenerateOutlineSVGFromBitmap(String bmpPath)
        {
            String OutputPath = "Z:\\Daten\\Bachelorprojekt1617\\Kinect\\potrace-1.14.win64\\result" + DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + ".svg";
            Console.WriteLine(OutputPath);
            Process potrace = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "Z:\\Daten\\Bachelorprojekt1617\\Kinect\\potrace-1.14.win64\\potrace.exe",
                    Arguments = bmpPath + " -s -u 1 -o " + OutputPath, //SVG // if svg should be saved: -o Z:\\Daten\\Bachelorprojekt1617\\Kinect\\potrace-1.14.win64\\result.svg --fillcolor #FFFFFF 
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                },
                EnableRaisingEvents = false
            };

            potrace.Start();
            potrace.BeginOutputReadLine();
           

            potrace.StandardInput.WriteLine(); //Without this line the input to Potrace won't go through.
            potrace.WaitForExit();
            String svgString = File.ReadAllText(OutputPath);
            return svgString;
        }



        private String AddSVGFrame(String svg)
        {
            XmlDocument doc = new XmlDocument();
            //special svg-strings that are needed for right alignment on the page 
            if (speechInteraction.outlines)
            {
                doc.LoadXml("<?xml version=\"1.0\" standalone=\"no\"?> <svg version=\"1.0\" width=\"200\" height=\"130\" viewBox=\"0 0 640 480\" xmlns=\"http://www.w3.org/2000/svg\" preserveAspectRatio=\"xMidYMid meet\"> </svg>");
            }
            else
            {
                doc.LoadXml("<?xml version=\"1.0\" standalone=\"no\"?> <svg version=\"1.0\" width=\"200\" height=\"130\" viewBox=\"0 0 2 2\" xmlns=\"http://www.w3.org/2000/svg\" preserveAspectRatio=\"xMidYMid meet\"> </svg>");
            }
            
            string pattern = "<!.*>";
            string replacement = " ";
            Regex rgx = new Regex(pattern);
            
            Console.WriteLine(rgx.Match(svg));
            
            //Remove comments because Xml-lib cannot handle them
            svg = svg.Replace("﻿<!DOCTYPE svg PUBLIC \"-//W3C//DTD SVG 1.1//EN\" \"http://www.w3.org/Graphics/SVG/1.1/DTD/svg11.dtd\">","");
            svg = svg.Replace("<!DOCTYPE svg PUBLIC \"-//W3C//DTD SVG 20010904//EN\"\n \"http://www.w3.org/TR/2001/REC-SVG-20010904/DTD/svg10.dtd\">","");
            svg = svg.Replace("viewBox=\"0 0 640.000000 480.000000\"", "");
            XmlNode root = doc.DocumentElement;

            XmlDocument doc1 = new XmlDocument();
            doc1.LoadXml(svg);
            root.AppendChild(doc.ImportNode(doc1.DocumentElement, true));
            Console.WriteLine(doc.OuterXml);
            svgImage = doc.OuterXml;
            
            return svgImage;
        }



        /// <summary>
        /// Use the sticky skeleton logic to choose a player that we want to set as foreground. This means if the app
        /// is tracking a player already, we keep tracking the player until it leaves the sight of the camera, 
        /// and then pick the closest player to be tracked as foreground.
        /// </summary>
        private void ChooseSkeleton()
        {
            var isTrackedSkeltonVisible = false;
            var nearestDistance = float.MaxValue;
            Skeleton nearestSkel = null;

            foreach (var skel in this.skeletons)
            {
                if (null == skel)
                {
                    continue;
                }

                if (skel.TrackingState != SkeletonTrackingState.Tracked)
                {
                    continue;
                }

                if (skel.TrackingId == this.currentlyTrackedSkeletonId)
                {
                    isTrackedSkeltonVisible = true;
                    break;
                }

                if (skel.Position.Z < nearestDistance)
                {
                    nearestDistance = skel.Position.Z;
                    nearestSkeleton = skel.TrackingId;
                    nearestSkel = skel;
                }
            }

            if (!isTrackedSkeltonVisible && nearestSkeleton != 0)
            {
                this.backgroundRemovedColorStream.SetTrackedPlayer(nearestSkeleton);
                this.currentlyTrackedSkeletonId = nearestSkeleton;
            }

        }

        /// <summary>
        /// Called when the KinectSensorChooser gets a new sensor
        /// </summary>
        /// <param name="sender">sender of the event</param>
        /// <param name="args">event arguments</param>
        private void SensorChooserOnKinectChanged(object sender, KinectChangedEventArgs args)
        {
            if (args.OldSensor != null)
            {
                try
                {
                    args.OldSensor.AllFramesReady -= this.SensorAllFramesReady;
                    args.OldSensor.DepthStream.Disable();
                    args.OldSensor.ColorStream.Disable();
                    args.OldSensor.SkeletonStream.Disable();

                    // Create the background removal stream to process the data and remove background, and initialize it.
                    if (null != this.backgroundRemovedColorStream)
                    {
                        this.backgroundRemovedColorStream.BackgroundRemovedFrameReady -= this.BackgroundRemovedFrameReadyHandler;
                        this.backgroundRemovedColorStream.Dispose();
                        this.backgroundRemovedColorStream = null;
                    }
                }
                catch (InvalidOperationException)
                {
                    // KinectSensor might enter an invalid state while enabling/disabling streams or stream features.
                    // E.g.: sensor might be abruptly unplugged.
                }
            }

            if (args.NewSensor != null)
            {
                try
                {
                    args.NewSensor.DepthStream.Enable(DepthFormat);
                    args.NewSensor.ColorStream.Enable(ColorFormat);
                    args.NewSensor.SkeletonStream.Enable();
                    this.backgroundRemovedColorStream = new BackgroundRemovedColorStream(args.NewSensor);
                    this.backgroundRemovedColorStream.Enable(ColorFormat, DepthFormat);
                    // Allocate space to put the depth, color, and skeleton data we'll receive
                    if (null == this.skeletons)
                    {
                        this.skeletons = new Skeleton[args.NewSensor.SkeletonStream.FrameSkeletonArrayLength];
                    }

                    // Add an event handler to be called when the background removed color frame is ready, so that we can
                    // composite the image and output to the app
                    this.backgroundRemovedColorStream.BackgroundRemovedFrameReady += this.BackgroundRemovedFrameReadyHandler;

                    // Add an event handler to be called whenever there is new depth frame data
                    args.NewSensor.AllFramesReady += this.SensorAllFramesReady;
                    args.NewSensor.SkeletonFrameReady += this.SensorSkeletonFrameReady;
                    this.sensor = args.NewSensor;
                    this.sensor.Start();
                    skeletonHandler = new SkeletonHandler(sensor, speechInteraction);
                    
                    try
                    {
                        /*args.NewSensor.DepthStream.Range = this.checkBoxNearMode.IsChecked.GetValueOrDefault()
                                                    ? DepthRange.Near
                                                    : DepthRange.Default;
                        args.NewSensor.SkeletonStream.EnableTrackingInNearRange = true;*/
                        args.NewSensor.DepthStream.Range = DepthRange.Default;
                        args.NewSensor.SkeletonStream.EnableTrackingInNearRange = true;
                    }
                    catch (InvalidOperationException)
                    {
                        // Non Kinect for Windows devices do not support Near mode, so reset back to default mode.
                        args.NewSensor.DepthStream.Range = DepthRange.Default;
                        args.NewSensor.SkeletonStream.EnableTrackingInNearRange = false;
                    }
                    this.speechHandler = new SpeechHandler(speechInteraction,args.NewSensor);
                    this.speechHandler.InitializeSpeechRecognizer();
                    this.speechInteraction.speechHandler = speechHandler;
                    

                }
                catch (InvalidOperationException)
                {
                    // KinectSensor might enter an invalid state while enabling/disabling streams or stream features.
                    // E.g.: sensor might be abruptly unplugged.
                }
            }
        }



        /// <summary>
        /// Handles the user clicking on the print button
        /// </summary>
        private void ButtonPrint(object sender, RoutedEventArgs e)
        {
            speechInteraction.fsm.Fire(SpeechInteraction.Command.Print);

            
        }

        private void ButtonSkeletonTriggerPhoto(object sender, RoutedEventArgs e)
        {
            speechInteraction.fsm.Fire(SpeechInteraction.Command.Skeleton);


        }


        private void ButtonOutlinesTriggerPhoto(object sender, RoutedEventArgs e)
        {
            speechInteraction.fsm.Fire(SpeechInteraction.Command.Outlines);


        }

        //gets called from speechInteraction-class when picture of the outlines is taken
        public void TakePictureOutlines()
        {
            speechInteraction.outlines = true;
            if (null == this.sensorChooser || null == this.sensorChooser.Kinect)
            {
                return;
            }
            bool noSkelTracked = CheckIfPersonInImage();
            
            if (noSkelTracked)
            {
                reader.Speak("No person identified");
                return;
            }
            
            pictureTaken = true;

        }



        //gets called from speechInteraction-class when picture of the skeleton is taken
        public void TakePictureSkeleton()
        {
            speechInteraction.outlines = false;
            if (this.sensor.SkeletonStream.IsEnabled )
            {
                bool noSkelTracked = CheckIfPersonInImage();

                if (noSkelTracked)
                {
                    reader.Speak("No person identified");
                    return;
                }
                foreach (Skeleton skel in skeletons)
                {
                    if (skel.TrackingState == SkeletonTrackingState.Tracked)
                    {
                        String svgString = GenerateSkeletonSVG(skel);
                        this.svgImage = svgString;
                        break;
                    }
                }
                
                pictureTaken = true;
                
            }

        }


        private bool CheckIfPersonInImage()
        {
            bool noSkelTracked = true;
            
            foreach (Skeleton skel in skeletons)
            {
                if (skel.TrackingState != SkeletonTrackingState.NotTracked)
                {
                    noSkelTracked = false;
                }
            }
            return noSkelTracked;
        }
        
       
        

        public void BluetoothConnect()
        {
            this.connectionManager.BluetoothConnect();
        }

        public void SendSvgBluetooth(String svgImage)
        {
            this.connectionManager.SendSvgBluetooth(svgImage,this);
        }

        public void SendSvgTCP(String svgImage)
        {
            this.connectionManager.SendSvgTCP(svgImage, this);
        }

        /// <summary>
        /// Event handler for Kinect sensor's SkeletonFrameReady event
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void SensorSkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            if (!pictureTaken)
            {

                    skeletons = new Skeleton[0];

                    using (SkeletonFrame skeletonFrame = e.OpenSkeletonFrame())
                    {
                        if (skeletonFrame != null)
                        {
                            skeletons = new Skeleton[skeletonFrame.SkeletonArrayLength];
                            skeletonFrame.CopySkeletonDataTo(skeletons);
                        }
                        /*else
                        {
                              skeletons = new Skeleton[0];

                            
                        }*/
                    }

                    using (DrawingContext dc = this.drawingGroup.Open())
                    {
                        
                        RenderSkeleton(dc, skeletons, drawingGroup);
 
                    }
                }

           
        }


        //generates the skeleton svg and saves it - the real printing is triggered from speechinteraction
        public String GenerateSkeletonSVG(Skeleton skel)
        {
            SvgDocument doc = new SvgDocument()
            {
                Width = svgWidth,
                Height = svgHeight
            };

            SvgPath path = new SvgPath()
            {
                FillOpacity = 0,
                Stroke = new SvgColourServer(System.Drawing.Color.Black)
            };

            Joint leftHand = skel.Joints[JointType.HandLeft];
            Joint rightHand = skel.Joints[JointType.HandRight];
            Joint leftWrist = skel.Joints[JointType.WristLeft];
            Joint rightWrist = skel.Joints[JointType.WristRight];
            Joint leftElbow = skel.Joints[JointType.ElbowLeft];
            Joint rightElbow = skel.Joints[JointType.ElbowRight];
            Joint leftShoulder = skel.Joints[JointType.ShoulderLeft];
            Joint rightShoulder = skel.Joints[JointType.ShoulderRight];
            Joint leftFoot = skel.Joints[JointType.FootLeft];
            Joint rightFoot = skel.Joints[JointType.FootRight];
            Joint leftAnkle = skel.Joints[JointType.AnkleLeft];
            Joint rightAnkle = skel.Joints[JointType.AnkleRight];
            Joint leftKnee = skel.Joints[JointType.KneeLeft];
            Joint rightKnee = skel.Joints[JointType.KneeRight];
            Joint leftHip = skel.Joints[JointType.HipLeft];
            Joint rightHip = skel.Joints[JointType.HipRight];
            Joint head = skel.Joints[JointType.Head];
            Joint shoulderCenter = skel.Joints[JointType.ShoulderCenter];
            Joint spine = skel.Joints[JointType.Spine];
            Joint hipCenter = skel.Joints[JointType.HipCenter];

            List<Joint> arms = new List<Joint>();
            arms.Add(leftHand);
            arms.Add(leftWrist);
            arms.Add(leftElbow);
            arms.Add(leftShoulder);
            arms.Add(shoulderCenter);
            arms.Add(rightShoulder);
            arms.Add(rightElbow);
            arms.Add(rightWrist);
            arms.Add(rightHand);

            List<Joint> back = new List<Joint>();
            //back.Add(head);
            back.Add(shoulderCenter);
            back.Add(spine);
            back.Add(hipCenter);

            List<Joint> legs = new List<Joint>();
            legs.Add(leftFoot);
            legs.Add(leftAnkle);
            legs.Add(leftKnee);
            legs.Add(leftHip);
            legs.Add(hipCenter);
            legs.Add(rightHip);
            legs.Add(rightKnee);
            legs.Add(rightAnkle);
            legs.Add(rightFoot);



            skeletonHandler.AddJointsToPath(path, arms, 100);
            skeletonHandler.AddJointsToPath(path, back, 100);
            skeletonHandler.AddJointsToPath(path, legs, 100);

            //calculate intersecion point of head and neck

            PointF headPointOnScreen = new PointF(head.Position.X + 1, skeletonHandler.TranslatePosition(head.Position.Y));
            PointF shoulderPointOnScreen = new PointF(shoulderCenter.Position.X +1, skeletonHandler.TranslatePosition(shoulderCenter.Position.Y));
            double deltaX = headPointOnScreen.X - shoulderPointOnScreen.X;
            double deltaY = headPointOnScreen.Y - shoulderPointOnScreen.Y;
            float distance = (float)Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
            float headRadius = (float)(distance / 2);
            Vector headVec = new Vector(headPointOnScreen.X, headPointOnScreen.Y);
            Vector shoulderVec = new Vector(shoulderPointOnScreen.X, shoulderPointOnScreen.Y);
            Vector distVector = shoulderVec - headVec;
            distVector.Normalize();


            Vector intersectingPoint = shoulderVec - distVector * headRadius;
            PointF intersectingPointF = new PointF((float)intersectingPoint.X, (float)intersectingPoint.Y);
            SvgCircle headCircle = new SvgCircle()
            {
                Radius = headRadius,

                FillOpacity = 0,
                Stroke = new SvgColourServer(System.Drawing.Color.Black),
                CenterX = new Svg.SvgUnit(head.Position.X +1),
                CenterY = new Svg.SvgUnit(skeletonHandler.TranslatePosition(head.Position.Y)),
                StrokeWidth = 1
            };
           

            SvgPath path2 = new SvgPath()
            {
                FillOpacity = 0,
                Stroke = new SvgColourServer(System.Drawing.Color.Black)
            };
            //add the neck
            path.PathData.Add(new SvgMoveToSegment(shoulderPointOnScreen));
            path.PathData.Add(new SvgLineSegment(shoulderPointOnScreen, intersectingPointF));

           
            
            doc.Children.Add(path);
            doc.Children.Add(headCircle);
            //doc.Children.Add(path2);
            var stream = new MemoryStream();
            doc.Write(stream);
            //Console.WriteLine("SVG from skeleton " + Encoding.UTF8.GetString(stream.GetBuffer()));

            String svgString = Encoding.UTF8.GetString(stream.GetBuffer());
            stream.Close();

            Console.WriteLine("svg output");
            Debug.WriteLine(svgString);
            Console.WriteLine("adding frame");
            
            String finalSvg = AddSVGFrame(svgString);


            return finalSvg;

        }



        public void RenderSkeleton(DrawingContext dc, Skeleton[] skeletons, DrawingGroup drawingGroup)
        {
            // Draw a transparent background to set the render size
            dc.DrawRectangle(Brushes.White, null, new Rect(0.0, 0.0, SkeletonHandler.RenderWidth, SkeletonHandler.RenderHeight));
            
            bool noSkelTracked = true;
            if (skeletons.Length != 0)
            {
                Skeleton skel = null;
                ChooseSkeleton(); // Track this skeleton
                foreach (Skeleton skeleton in skeletons)
                {
                    if (nearestSkeleton == skeleton.TrackingId)
                    {
                        skel = skeleton;
                    }
                }
                if (skel != null)
                {
                
                    SkeletonHandler.RenderClippedEdges(skel, dc);

                    if (skel.TrackingState == SkeletonTrackingState.Tracked)
                    {
                        skeletonHandler.DrawBonesAndJoints(skel, dc);
                        noSkelTracked = false;
                        lastSkeletonTimeStamp = DateTime.Now;
                    }
                    else if (skel.TrackingState == SkeletonTrackingState.PositionOnly)
                    {
                        dc.DrawEllipse(
                        skeletonHandler.centerPointBrush,
                        null,
                        skeletonHandler.SkeletonPointToScreen(skel.Position),
                        SkeletonHandler.BodyCenterThickness,
                        SkeletonHandler.BodyCenterThickness);
                        noSkelTracked = false;
                        lastSkeletonTimeStamp = DateTime.Now;
                    }
                }
            }

            if (noSkelTracked)
            {
                if ((DateTime.Now - lastSkeletonTimeStamp).TotalMilliseconds > 1000)
                {
                    speechInteraction.fsm.Fire(SpeechInteraction.Command.FramesNotReady);
                    lastSkeletonTimeStamp = DateTime.Now;
                }
            }
            else
            {
                speechInteraction.fsm.Fire(SpeechInteraction.Command.FramesReady);
            }

            // prevent drawing outside of our render area
            drawingGroup.ClipGeometry = new RectangleGeometry(new Rect(0.0, 0.0, SkeletonHandler.RenderWidth, SkeletonHandler.RenderHeight));
        }
        
    }

}