
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
    using InTheHand.Net.Sockets;
    using InTheHand.Net.Bluetooth;
    using System.Net.Sockets;
    using Svg;
    using System.Collections.Generic;
    using Svg.Pathing;
    using Microsoft.Speech.Recognition;
    using Microsoft.Speech.AudioFormat;
    using System.Speech.Synthesis;
    using bbv;
    using HPI.HCI.Bachelorproject1617.PhotoBooth;
    using System.Timers;



    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, IDisposable
    {

        //skeleton vars 

        private const int scale = 100;
        private const int svgWidth = 200;
        private const int svgHeight = 200;
        private static BluetoothClient thisDevice;
        private Boolean alreadyPaired = false;


        String svgImage;

        //speech

        private SpeechRecognitionEngine speechEngine;
        
        RecognizerInfo ri;

        SpeechSynthesizer reader;

        SpeechInteraction speechInteraction;

        //timestamps
        private DateTime lastSkeletonTimeStamp;

        Timer timer;


        /// <summary>
        /// Width of output drawing
        /// </summary>
        private const float RenderWidth = 640.0f;

        /// <summary>
        /// Height of our output drawing
        /// </summary>
        private const float RenderHeight = 480.0f;

        /// <summary>
        /// Thickness of drawn joint lines
        /// </summary>
        private const double JointThickness = 3;

        /// <summary>
        /// Thickness of body center ellipse
        /// </summary>
        private const double BodyCenterThickness = 10;

        /// <summary>
        /// Thickness of clip edge rectangles
        /// </summary>
        private const double ClipBoundsThickness = 10;

        /// <summary>
        /// Brush used to draw skeleton center point
        /// </summary>
        private readonly Brush centerPointBrush = Brushes.Blue;

        /// <summary>
        /// Brush used for drawing joints that are currently tracked
        /// </summary>
        private readonly Brush trackedJointBrush = new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 68, 192, 68));

        /// <summary>
        /// Brush used for drawing joints that are currently inferred
        /// </summary>        
        private readonly Brush inferredJointBrush = Brushes.Yellow;

        /// <summary>
        /// Pen used for drawing bones that are currently tracked
        /// </summary>
        private readonly Pen trackedBonePen = new Pen(Brushes.Green, 6);

        /// <summary>
        /// Pen used for drawing bones that are currently inferred
        /// </summary>        
        private readonly Pen inferredBonePen = new Pen(Brushes.Gray, 1);

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


        private bool pictureTaken = false;

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
            this.speechInteraction = new SpeechInteraction(this, reader);

            //Bluetooth stuff here 
            if (BluetoothRadio.PrimaryRadio.Mode == RadioMode.Discoverable)
            {
                thisDevice = new BluetoothClient();
                BluetoothComponent bluetoothComponent = new BluetoothComponent(thisDevice);
                bluetoothComponent.DiscoverDevicesProgress += bluetoothComponent_DiscoverDevicesProgress;
                bluetoothComponent.DiscoverDevicesComplete += bluetoothComponent_DiscoverDevicesComplete;
                bluetoothComponent.DiscoverDevicesAsync(8, true, true, true, false, thisDevice);
                Console.WriteLine("Connectable");

            }
            else
            {
                reader.Speak("Please turn on your Bluetooth adapter");
                
            }

            timer = new Timer(20000);
            timer.Elapsed += new ElapsedEventHandler(HandleTimer);
            timer.Start();

            lastSkeletonTimeStamp = DateTime.Now;
        }

        private void HandleTimer(object source, ElapsedEventArgs evt)
        {
           speechInteraction.fsm.Fire(SpeechInteraction.Command.Repeat);
        }

        

        /// <summary>
        /// Finalizes an instance of the MainWindow class.
        /// This destructor will run only if the Dispose method does not get called.
        /// </summary>
        ~MainWindow()
        {
            this.Dispose(false);
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


            if (null != this.speechEngine)
            {
                this.speechEngine.SpeechRecognized -= SpeechRecognized;
                this.speechEngine.SpeechRecognitionRejected -= SpeechRejected;
                this.speechEngine.RecognizeAsyncStop();
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

            if (this.checkBoxDepthStream.IsChecked.GetValueOrDefault())
            {

            
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
            }
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

                    if (pictureTaken)
                    {
       
                        for (int i = 0; i < pixelData.Length; i += BackgroundRemovedColorFrame.BytesPerPixel)
                        {

                            pixelData[i] = 0;
                            pixelData[i + 1] = 255;
                            pixelData[i + 2] = 0;

                        }

                     
                    }

                    // Write the pixel data into our bitmap
                    this.foregroundBitmap.WritePixels(
                        new Int32Rect(0, 0, this.foregroundBitmap.PixelWidth, this.foregroundBitmap.PixelHeight),
                        pixelData,
                        this.foregroundBitmap.PixelWidth * sizeof(int),
                        0);


                    if (pictureTaken)
                    {
                        PrintSVGOutlines();
                       
                    }

                }
            }
        }



        private void PrintSVGOutlines()
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
            GenerateOutlineSVG(path);
            pictureTaken = false;

        }


        private static RecognizerInfo GetKinectRecognizer()
        {
            foreach (RecognizerInfo recognizer in SpeechRecognitionEngine.InstalledRecognizers())
            {
                string value;
                recognizer.AdditionalInfo.TryGetValue("Kinect", out value);
                if ("True".Equals(value, StringComparison.OrdinalIgnoreCase))
                { //&& "en-US".Equals(recognizer.Culture.Name, StringComparison.OrdinalIgnoreCase))
                    return recognizer;
                }
            }

            return null;
        }



        private void SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
          {
            // Speech utterance confidence below which we treat speech as if it hadn't been heard
            const double ConfidenceThreshold = 0.3;

            if (e.Result.Confidence >= ConfidenceThreshold)
            {
                String result = e.Result.Semantics.Value.ToString();
                Console.WriteLine(result);
              switch(result)
              {
                  case "BOTH":
                      ButtonPrintBoth(null,null);
                      break;
                  case "OUTLINES":
                      
                      speechInteraction.fsm.Fire(SpeechInteraction.Command.Outlines);
                      
                      break;
                  case "SKELETON":
                      speechInteraction.fsm.Fire(SpeechInteraction.Command.Skeleton);
                      
                      break;
                  case "BACK":
                      speechInteraction.fsm.Fire(SpeechInteraction.Command.Back);
                      
                      break;
                  case "PRINT":
                      speechInteraction.fsm.Fire(SpeechInteraction.Command.Print);
                      break;
                  
              }
            }
          }


        private void SpeechRejected(object sender, SpeechRecognitionRejectedEventArgs e)
        {
            Debug.WriteLine("Speech rejected");
        }


        private Bitmap BmpFromByteArray(byte[] pixelData, int w, int h)
        {
            Bitmap bmp = null;
            int ch = 3;
            unsafe
            {
                
                fixed (byte* p = pixelData)
                {
                    IntPtr unmanagedPointer = (IntPtr)p;

                    // Deduced from your buffer size
                    
                    bmp = new Bitmap(w, h, w * 3, System.Drawing.Imaging.PixelFormat.Format24bppRgb, unmanagedPointer);
                }
            }
            return bmp;
        }

        private String GenerateOutlineSVG(String bmpPath)
        {
            Process potrace = new Process {
                StartInfo = new ProcessStartInfo {
                FileName = "Z:\\Daten\\Bachelorprojekt1617\\Kinect\\potrace-1.14.win64\\potrace.exe",
                Arguments = bmpPath + " -s -u 1 -o Z:\\Daten\\Bachelorprojekt1617\\Kinect\\potrace-1.14.win64\\result.svg", //SVG // if svg should be saved: -o Z:\\Daten\\Bachelorprojekt1617\\Kinect\\potrace-1.14.win64\\result.svg --fillcolor #FFFFFF 
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
                },
                EnableRaisingEvents = false
            };

            StringBuilder svgBuilder = new StringBuilder();
            potrace.OutputDataReceived += (object sender2, DataReceivedEventArgs e2) => {
                svgBuilder.AppendLine(e2.Data);
            };
            if (true) {
                potrace.ErrorDataReceived += (object sender2, DataReceivedEventArgs e2) => {
                Console.WriteLine("Error: " + e2.Data);
                };
            }
            potrace.Start();
            potrace.BeginOutputReadLine();
            if (true) {
                potrace.BeginErrorReadLine();
            }

            BinaryWriter writer = new BinaryWriter(potrace.StandardInput.BaseStream);
            
            //bmp.Save(writer.BaseStream, ImageFormat.Bmp);
            potrace.StandardInput.WriteLine(); //Without this line the input to Potrace won't go through.
            potrace.WaitForExit();
            String svgString = File.ReadAllText("Z:\\Daten\\Bachelorprojekt1617\\Kinect\\potrace-1.14.win64\\result.svg");
            Console.WriteLine("SVG Outline Result " + svgString);
            //this.SendSvg(svgString);
            svgImage = svgString;
            return svgString;
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
            var nearestSkeleton = 0;

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
                    try
                    {
                        args.NewSensor.DepthStream.Range = this.checkBoxNearMode.IsChecked.GetValueOrDefault()
                                                    ? DepthRange.Near
                                                    : DepthRange.Default;
                        args.NewSensor.SkeletonStream.EnableTrackingInNearRange = true;
                    }
                    catch (InvalidOperationException)
                    {
                        // Non Kinect for Windows devices do not support Near mode, so reset back to default mode.
                        args.NewSensor.DepthStream.Range = DepthRange.Default;
                        args.NewSensor.SkeletonStream.EnableTrackingInNearRange = false;
                    }
                    InitializeSpeechRecognizer();
                    
                    

                }
                catch (InvalidOperationException)
                {
                    // KinectSensor might enter an invalid state while enabling/disabling streams or stream features.
                    // E.g.: sensor might be abruptly unplugged.
                }
            }
        }

        private void InitializeSpeechRecognizer()
        {
            ri = GetKinectRecognizer();
            if (null != ri)
            {
                this.speechEngine = new SpeechRecognitionEngine(ri.Id);
                var directions = new Choices();
                directions.Add(new SemanticResultValue("outlines", "OUTLINES"));
                directions.Add(new SemanticResultValue("skeleton", "SKELETON"));
                //directions.Add(new SemanticResultValue("print both", "BOTH"));
                directions.Add(new SemanticResultValue("take picture", "OUTLINES"));
                directions.Add(new SemanticResultValue("back", "BACK"));
                directions.Add(new SemanticResultValue("print", "PRINT"));

                var gb = new GrammarBuilder { Culture = ri.Culture };
                gb.Append(directions);

                var g = new Grammar(gb);
                speechEngine.LoadGrammar(g);
                speechEngine.SpeechRecognized += SpeechRecognized;
                speechEngine.SpeechRecognitionRejected += SpeechRejected;

                speechEngine.SetInputToAudioStream(
                    sensor.AudioSource.Start(), new SpeechAudioFormatInfo(EncodingFormat.Pcm, 16000, 16, 1, 32000, 2, null));

                speechEngine.RecognizeAsync(RecognizeMode.Multiple);
                
            }
        }


        /// <summary>
        /// Handles the user clicking on the screenshot button
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void ButtonPrintBoth(object sender, RoutedEventArgs e)
        {
            TakePictureOutlines(null,null);
            
            TakePictureSkeleton(null, null);

            
        }

        public void TakePictureOutlines(object sender, RoutedEventArgs e)
        {
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
            Console.WriteLine("Screenshot");
            //reader.Speak("Printing outlines");
            
            pictureTaken = true;
            //speechInteraction.fsm.Fire(SpeechInteraction.Command.Outlines);

        }


        public void TakePictureSkeleton(object sender, RoutedEventArgs e)
        {
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
                        String svgString = this.GenerateSkeletonSVG(skel);
                        svgImage = svgString;
                        //this.SendSvg(svgString);
                    }
                }
                Console.WriteLine("all skeletons done");
                //reader.Speak("Skeleton is being printed");
                
                
                
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
        
        /// <summary>
        /// Handles the checking or unchecking of the near mode combo box
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void CheckBoxNearModeChanged(object sender, RoutedEventArgs e)
        {
            if (null == this.sensorChooser || null == this.sensorChooser.Kinect)
            {
                return;
            }

            // will not function on non-Kinect for Windows devices
            try
            {
                this.sensor.SkeletonStream.TrackingMode = this.checkBoxNearMode.IsChecked.GetValueOrDefault()
                                                    ? SkeletonTrackingMode.Seated
                                                    : SkeletonTrackingMode.Default;
                
                this.sensor.DepthStream.Range = this.checkBoxNearMode.IsChecked.GetValueOrDefault()
                                                    ? DepthRange.Near
                                                    : DepthRange.Default;
                Console.WriteLine("Checkbox state " + this.checkBoxNearMode.IsChecked.GetValueOrDefault());
            }
            catch (InvalidOperationException)
            {
            }
        }

        


        //skeleton functions 

        /// <summary>
        /// Draws indicators to show which edges are clipping skeleton data
        /// </summary>
        /// <param name="skeleton">skeleton to draw clipping information for</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        private static void RenderClippedEdges(Skeleton skeleton, DrawingContext drawingContext)
        {
            if (skeleton.ClippedEdges.HasFlag(FrameEdges.Bottom))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(0, RenderHeight - ClipBoundsThickness, RenderWidth, ClipBoundsThickness));
            }

            if (skeleton.ClippedEdges.HasFlag(FrameEdges.Top))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(0, 0, RenderWidth, ClipBoundsThickness));
            }

            if (skeleton.ClippedEdges.HasFlag(FrameEdges.Left))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(0, 0, ClipBoundsThickness, RenderHeight));
            }

            if (skeleton.ClippedEdges.HasFlag(FrameEdges.Right))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(RenderWidth - ClipBoundsThickness, 0, ClipBoundsThickness, RenderHeight));
            }
        }



        private void bluetoothComponent_DiscoverDevicesComplete(object sender, DiscoverDevicesEventArgs e)
        {
            Console.WriteLine("Discovery finished");
        }

        private void bluetoothComponent_DiscoverDevicesProgress(object sender, DiscoverDevicesEventArgs e)
        {
            foreach (BluetoothDeviceInfo device in e.Devices)
            {
                Debug.WriteLine(device.DeviceName + " is a " + device.ClassOfDevice.MajorDevice.ToString());
                if (device.DeviceName.Contains("raspberrypi") && !alreadyPaired) //osboxes vs raspberry
                {
                    bool paired = BluetoothSecurity.PairRequest(device.DeviceAddress, "123456");
                    if (paired)
                    {
                        alreadyPaired = true;
                        Console.WriteLine("Paired!");
                        thisDevice.BeginConnect(device.DeviceAddress, BluetoothService.SerialPort, new AsyncCallback(Connect), device);

                    }
                    else
                    {
                        MessageBox.Show("There was a problem pairing.");
                    }
                }
            }
        }

        private void SendSvg(String svgString)
        {
            if (thisDevice.Connected)
            {
                Debug.WriteLine("Connected");
                NetworkStream stream = thisDevice.GetStream();
                
                if (stream.CanWrite)
                {
                    Guid uuid = System.Guid.NewGuid();
                    int length = Encoding.UTF8.GetBytes(svgString).Length;
                    byte[] rv = new byte[36 + 4 + length];
                    Console.WriteLine("length of svg is " + length);
                    System.Buffer.BlockCopy(Encoding.ASCII.GetBytes(uuid.ToString()), 0, rv, 0, 36);
                    byte[] intBytes = BitConverter.GetBytes(length);
                    if (BitConverter.IsLittleEndian)
                        Array.Reverse(intBytes);
                    byte[] result = intBytes;
                    System.Buffer.BlockCopy(result, 0, rv, 36, 4);
                    System.Buffer.BlockCopy(Encoding.UTF8.GetBytes(svgString), 0, rv, 36 + 4, length);
                    stream.Write(rv, 0, 36 + 4 + length);
                    Console.WriteLine(rv.ToString());

                }
            }
            else
            {
                Debug.WriteLine("Not Connected");
            }

        }


        private static void Connect(IAsyncResult result)
        {

            if (result.IsCompleted)
            {

                // client is connected now :)
                Console.WriteLine(thisDevice.Connected);

            }
        }



        /// <summary>
        /// Event handler for Kinect sensor's SkeletonFrameReady event
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void SensorSkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            if (this.checkBoxSkeleton.IsChecked.GetValueOrDefault())
            {
                skeletons = new Skeleton[0];

                using (SkeletonFrame skeletonFrame = e.OpenSkeletonFrame())
                {
                    if (skeletonFrame != null)
                    {
                        skeletons = new Skeleton[skeletonFrame.SkeletonArrayLength];
                        skeletonFrame.CopySkeletonDataTo(skeletons);
                    } 
                }

                using (DrawingContext dc = this.drawingGroup.Open())
                {
                    // Draw a transparent background to set the render size
                    dc.DrawRectangle(Brushes.Black, null, new Rect(0.0, 0.0, RenderWidth, RenderHeight));

                    bool noSkelTracked = true;
                    if (skeletons.Length != 0)
                    {
                        foreach (Skeleton skel in skeletons)
                        {
                            RenderClippedEdges(skel, dc);

                            if (skel.TrackingState == SkeletonTrackingState.Tracked)
                            {
                                this.DrawBonesAndJoints(skel, dc);
                                noSkelTracked = false;
                            }
                            else if (skel.TrackingState == SkeletonTrackingState.PositionOnly)
                            {
                                dc.DrawEllipse(
                                this.centerPointBrush,
                                null,
                                this.SkeletonPointToScreen(skel.Position),
                                BodyCenterThickness,
                                BodyCenterThickness);
                                noSkelTracked = false;
                            }
                        }
                    }

                    if (noSkelTracked)
                    {
                        if ((DateTime.Now - this.lastSkeletonTimeStamp).TotalMilliseconds > 1000)
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
                    this.drawingGroup.ClipGeometry = new RectangleGeometry(new Rect(0.0, 0.0, RenderWidth, RenderHeight));
                }

                
            }

        }

        private String GenerateSkeletonSVG(Skeleton skel)
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

            AddJointsToPath(path, arms, 100);
            AddJointsToPath(path, back, 100);
            AddJointsToPath(path, legs, 100);

            Console.WriteLine("svg output");
            float deltaX = leftHand.Position.X - leftElbow.Position.X;
            float deltaY = leftHand.Position.Y - leftElbow.Position.Y;
            float deltaZ = leftHand.Position.Z - leftElbow.Position.Z;

            float distance = (float)Math.Sqrt(deltaX * deltaX + deltaY * deltaY + deltaZ * deltaZ);
            float headRadius = (float)(distance * scale / 2.5);
            foreach (SvgPathSegment element in path.PathData)
            {

                Console.WriteLine(element.ToString());

            }

            Console.WriteLine("svg output end");


            //calculate intersecion point of head and neck
            //double shoulderToHead = Math.Sqrt(Math.Pow(head.Position.X - shoulderCenter.Position.X,2) + Math.Pow(head.Position.Y - shoulderCenter.Position.Y,2));
            Vector headVec = new Vector(head.Position.X, head.Position.Y);
            Vector distVector = headVec - new Vector(shoulderCenter.Position.X, shoulderCenter.Position.Y);
            distVector.Normalize();
            Vector intersectingPoint = headVec - distVector * headRadius;

            SvgCircle headCircle = new SvgCircle()
            {
                Radius = headRadius,

                FillOpacity = 0,
                Stroke = new SvgColourServer(System.Drawing.Color.Black),
                CenterX = new Svg.SvgUnit(TranslatePosition(head.Position.X)),
                CenterY = new Svg.SvgUnit(TranslatePosition(head.Position.Y)),
                StrokeWidth = 1
            };
            //add the neck
            path.PathData.Add(new SvgMoveToSegment(new PointF(TranslatePosition((float)intersectingPoint.X), TranslatePosition((float)intersectingPoint.Y))));
            path.PathData.Add(new SvgLineSegment(new PointF(TranslatePosition(shoulderCenter.Position.X), TranslatePosition(shoulderCenter.Position.Y)), new PointF(TranslatePosition((float)intersectingPoint.X), TranslatePosition((float)intersectingPoint.Y))));
            doc.Children.Add(path);


            doc.Children.Add(headCircle);
            var stream = new MemoryStream();
            doc.Write(stream);
            Console.WriteLine("SVG from skeleton " + Encoding.UTF8.GetString(stream.GetBuffer()));
            return Encoding.UTF8.GetString(stream.GetBuffer());

        }

        private void AddJointsToPath(SvgPath path, List<Joint> joints, int scale)
        {
            path.PathData.Add(new SvgMoveToSegment(new PointF(TranslatePosition(joints[0].Position.X), TranslatePosition(joints[0].Position.Y))));
            for (var i = 0; i < joints.Count - 1; i++)
            {
                var start = joints[i];
                var end = joints[i + 1];

                path.PathData.Add(new SvgLineSegment(new PointF(TranslatePosition(start.Position.X), TranslatePosition(start.Position.Y)), new PointF(TranslatePosition(end.Position.X), TranslatePosition(end.Position.Y))));
            }
        }

        private float TranslatePosition(float pos)
        {

            return svgHeight - ((pos + 1) * scale);

        }

        /// <summary>
        /// Draws a skeleton's bones and joints
        /// </summary>
        /// <param name="skeleton">skeleton to draw</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        private void DrawBonesAndJoints(Skeleton skeleton, DrawingContext drawingContext)
        {
            // Render Torso
            this.DrawBone(skeleton, drawingContext, JointType.Head, JointType.ShoulderCenter);
            this.DrawBone(skeleton, drawingContext, JointType.ShoulderCenter, JointType.ShoulderLeft);
            this.DrawBone(skeleton, drawingContext, JointType.ShoulderCenter, JointType.ShoulderRight);
            this.DrawBone(skeleton, drawingContext, JointType.ShoulderCenter, JointType.Spine);
            this.DrawBone(skeleton, drawingContext, JointType.Spine, JointType.HipCenter);
            this.DrawBone(skeleton, drawingContext, JointType.HipCenter, JointType.HipLeft);
            this.DrawBone(skeleton, drawingContext, JointType.HipCenter, JointType.HipRight);

            // Left Arm
            this.DrawBone(skeleton, drawingContext, JointType.ShoulderLeft, JointType.ElbowLeft);
            this.DrawBone(skeleton, drawingContext, JointType.ElbowLeft, JointType.WristLeft);
            this.DrawBone(skeleton, drawingContext, JointType.WristLeft, JointType.HandLeft);

            // Right Arm
            this.DrawBone(skeleton, drawingContext, JointType.ShoulderRight, JointType.ElbowRight);
            this.DrawBone(skeleton, drawingContext, JointType.ElbowRight, JointType.WristRight);
            this.DrawBone(skeleton, drawingContext, JointType.WristRight, JointType.HandRight);

            // Left Leg
            this.DrawBone(skeleton, drawingContext, JointType.HipLeft, JointType.KneeLeft);
            this.DrawBone(skeleton, drawingContext, JointType.KneeLeft, JointType.AnkleLeft);
            this.DrawBone(skeleton, drawingContext, JointType.AnkleLeft, JointType.FootLeft);

            // Right Leg
            this.DrawBone(skeleton, drawingContext, JointType.HipRight, JointType.KneeRight);
            this.DrawBone(skeleton, drawingContext, JointType.KneeRight, JointType.AnkleRight);
            this.DrawBone(skeleton, drawingContext, JointType.AnkleRight, JointType.FootRight);

            // Render Joints
            foreach (Joint joint in skeleton.Joints)
            {
                Brush drawBrush = null;

                if (joint.TrackingState == JointTrackingState.Tracked)
                {
                    drawBrush = this.trackedJointBrush;
                }
                else if (joint.TrackingState == JointTrackingState.Inferred)
                {
                    drawBrush = this.inferredJointBrush;
                }

                if (drawBrush != null)
                {
                    drawingContext.DrawEllipse(drawBrush, null, this.SkeletonPointToScreen(joint.Position), JointThickness, JointThickness);
                }
            }
        }

        /// <summary>
        /// Maps a SkeletonPoint to lie within our render space and converts to Point
        /// </summary>
        /// <param name="skelpoint">point to map</param>
        /// <returns>mapped point</returns>
        private Point SkeletonPointToScreen(SkeletonPoint skelpoint)
        {
            // Convert point to depth space.  
            // We are not using depth directly, but we do want the points in our 640x480 output resolution.
            
            DepthImagePoint depthPoint = this.sensor.CoordinateMapper.MapSkeletonPointToDepthPoint(skelpoint, DepthImageFormat.Resolution640x480Fps30);

            return new Point(depthPoint.X, depthPoint.Y);
        }

        /// <summary>
        /// Draws a bone line between two joints
        /// </summary>
        /// <param name="skeleton">skeleton to draw bones from</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        /// <param name="jointType0">joint to start drawing from</param>
        /// <param name="jointType1">joint to end drawing at</param>
        private void DrawBone(Skeleton skeleton, DrawingContext drawingContext, JointType jointType0, JointType jointType1)
        {
            Joint joint0 = skeleton.Joints[jointType0];
            Joint joint1 = skeleton.Joints[jointType1];

            // If we can't find either of these joints, exit
            if (joint0.TrackingState == JointTrackingState.NotTracked ||
                joint1.TrackingState == JointTrackingState.NotTracked)
            {
                return;
            }

            // Don't draw if both points are inferred
            if (joint0.TrackingState == JointTrackingState.Inferred &&
                joint1.TrackingState == JointTrackingState.Inferred)
            {
                return;
            }

            // We assume all drawn bones are inferred unless BOTH joints are tracked
            Pen drawPen = this.inferredBonePen;
            if (joint0.TrackingState == JointTrackingState.Tracked && joint1.TrackingState == JointTrackingState.Tracked)
            {
                drawPen = this.trackedBonePen;
            }

            drawingContext.DrawLine(drawPen, this.SkeletonPointToScreen(joint0.Position), this.SkeletonPointToScreen(joint1.Position));
        }

        /// <summary>
        /// Handles the checking or unchecking of the seated mode combo box
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void CheckBoxSeatedModeChanged(object sender, RoutedEventArgs e)
        {
            if (null != this.sensor)
            {
                if (this.checkBoxNearMode.IsChecked.GetValueOrDefault())
                {
                    this.sensor.SkeletonStream.TrackingMode = SkeletonTrackingMode.Seated;
                }
                else
                {
                    this.sensor.SkeletonStream.TrackingMode = SkeletonTrackingMode.Default;
                }
            }
        }

        private void CheckBoxDepthstreamChanged(object sender, RoutedEventArgs e)
        {
            return;
        }


        private void CheckBoxSkeletonChanged(object sender, RoutedEventArgs e)
        {
            return;
        }

       

    }


   
}