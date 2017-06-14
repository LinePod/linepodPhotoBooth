
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
        Socket client;

        //skeleton vars 
        
        private const int scale = 100;
        private const int svgWidth = 640;
        private const int svgHeight = 480;
        private static BluetoothClient thisDevice;
        private Boolean alreadyPaired = false;
        BluetoothDeviceInfo device;
        int nearestSkeleton = 0;

        public String svgImage = @"<?xml version=""1.0"" standalone=""no""?>
<!DOCTYPE svg PUBLIC ""-//W3C//DTD SVG 20010904//EN""
 ""http://www.w3.org/TR/2001/REC-SVG-20010904/DTD/svg10.dtd"">
<svg version=""1.0"" xmlns=""http://www.w3.org/2000/svg""
 width=""640.000000pt"" height=""480.000000pt"" viewBox=""0 0 640.000000 480.000000""
 preserveAspectRatio=""xMidYMid meet"">
<metadata>
Created by potrace 1.14, written by Peter Selinger 2001-2017
</metadata>
<g transform=""translate(0.000000,480.000000) scale(1.000000,-1.000000)""
fill=""#000000"" stroke=""none"">
<path d=""M270 367 c0 -1 -1 -1 -3 -1 -1 0 -3 -1 -4 -2 -1 -1 -2 -2 -3 -2 0 0
-4 -2 -7 -5 -5 -6 -5 -6 -5 -10 0 -3 0 -5 1 -5 1 0 1 -2 1 -4 0 -2 0 -4 1 -4
1 0 1 -4 1 -9 0 -5 0 -9 1 -9 1 0 1 -1 1 -3 0 -2 0 -3 1 -3 1 0 1 -3 1 -7 0
-4 0 -7 -1 -7 -1 0 -1 -1 -1 -2 0 -2 -6 -8 -8 -8 -1 0 -2 0 -2 -1 0 -1 -1 -1
-2 -1 -1 0 -2 0 -2 -1 0 -1 -1 -1 -3 -1 -2 0 -3 0 -3 -1 0 -1 -3 -1 -6 -1 -3
0 -6 0 -6 -1 0 -1 -2 -1 -4 -1 -2 0 -4 0 -4 -1 0 -1 -1 -1 -2 -1 -1 0 -2 0 -2
-1 0 -1 -1 -1 -2 -1 -1 0 -4 -2 -4 -4 0 -1 -1 -2 -2 -3 -1 -1 -2 -2 -2 -3 0
-1 -1 -2 -3 -4 -2 -2 -3 -3 -3 -4 0 -1 -1 -2 -2 -3 -1 -1 -2 -2 -2 -3 0 -1 0
-2 -1 -2 -1 0 -1 -1 -1 -2 0 -1 0 -2 -1 -2 -1 0 -1 -1 -1 -2 0 -1 0 -2 -1 -2
-1 0 -1 -1 -1 -3 0 -2 0 -3 -1 -3 -1 0 -1 -1 -1 -2 0 -1 0 -2 -1 -2 -1 0 -1
-3 -1 -7 0 -4 0 -7 1 -7 1 0 1 -1 1 -2 0 -1 0 -2 -1 -2 -1 0 -1 -1 -1 -2 0 -1
0 -2 -1 -2 -1 0 -1 0 -1 -1 0 -1 1 -1 2 -1 1 0 2 0 2 -1 0 -1 2 -1 5 -1 3 0 5
1 7 3 4 5 12 4 12 -1 0 -1 0 -2 1 -2 1 0 1 -1 1 -3 0 -2 0 -3 1 -3 1 0 1 -1 1
-2 0 -1 0 -2 1 -2 1 0 1 -6 1 -17 0 -11 0 -17 -1 -17 -1 0 -1 -3 -1 -7 0 -4 0
-7 -1 -7 -1 0 -1 -2 -1 -5 0 -3 0 -5 -1 -5 -1 0 -1 -2 -1 -4 0 -2 0 -4 -1 -4
-1 0 -1 -1 -1 -3 0 -2 0 -3 -1 -3 -1 0 -1 -1 -1 -3 0 -2 0 -3 -1 -3 -1 0 -1
-1 -1 -3 0 -2 0 -3 -1 -3 -1 0 -1 -2 -1 -4 0 -2 0 -4 -1 -4 -1 0 -1 -1 -1 -2
0 -1 0 -2 -1 -2 -1 0 -1 -1 -1 -2 0 -1 0 -2 -1 -2 -1 0 -1 -3 -1 -6 0 -3 0 -6
1 -6 1 0 1 -1 1 -3 0 -2 0 -3 1 -3 1 0 1 -2 1 -5 0 -3 0 -5 -1 -5 -1 0 -1 -1
-1 -3 0 -2 0 -3 -1 -3 -1 0 -1 -3 -1 -6 0 -3 0 -6 -1 -6 -1 0 -1 -4 -1 -11 0
-7 0 -11 -1 -11 -1 0 -1 -4 -1 -10 0 -6 0 -10 -1 -10 -1 0 -1 -3 0 -5 3 -2 61
-2 64 0 1 2 1 5 0 5 -1 1 -1 1 0 2 0 0 1 2 1 4 0 2 0 4 1 4 1 0 1 1 1 2 0 1 0
2 1 2 1 0 1 1 1 3 0 3 0 3 5 3 5 0 5 0 5 -3 0 -2 0 -3 1 -3 1 0 1 -3 1 -7 0
-4 0 -7 -1 -7 -1 0 -1 -1 -1 -3 l0 -3 27 0 c26 0 27 0 27 2 0 1 1 2 1 2 1 1 1
1 0 2 -1 0 -2 22 0 22 1 0 1 10 1 29 0 19 0 29 -1 29 -1 0 -1 6 -1 15 0 9 0
15 -1 15 -1 0 -1 2 -1 4 0 2 0 4 -1 4 -1 0 -1 1 -1 3 0 2 0 3 -1 3 -1 0 -1 6
-1 15 0 9 0 15 -1 15 -1 0 -1 4 -1 11 0 7 0 11 1 11 1 0 1 3 1 6 0 3 0 6 1 6
1 0 1 2 1 4 0 4 4 8 7 8 1 0 5 -2 8 -6 3 -3 7 -6 7 -6 1 0 2 0 2 -1 0 -1 2 -1
4 -1 l4 0 0 13 c0 8 0 13 -1 13 -1 0 -1 1 -1 2 0 1 -1 2 -2 3 -1 1 -2 2 -2 3
0 1 0 2 -1 2 -1 0 -1 1 -1 2 0 1 0 2 -1 2 -1 0 -1 3 -1 7 0 4 0 7 -1 7 -1 0
-1 1 -1 3 0 1 -1 3 -2 4 -1 1 -2 2 -2 3 0 1 0 2 -1 2 -1 0 -1 1 -1 2 0 1 -1 2
-2 3 -1 1 -2 2 -2 3 0 2 -3 4 -4 4 -1 0 -2 0 -2 1 0 1 -2 1 -4 1 -2 0 -4 0 -4
1 0 1 -1 1 -3 1 -2 0 -3 0 -3 1 0 1 -1 1 -2 1 -1 0 -2 0 -2 1 0 1 -1 1 -3 1
-1 0 -3 1 -4 2 -1 1 -2 2 -3 2 -2 0 -6 5 -6 6 0 1 0 2 -1 2 -1 0 -1 2 -1 5 0
3 0 5 1 5 1 0 1 1 1 2 0 1 0 2 1 2 1 0 1 1 1 2 0 1 6 8 8 8 2 0 6 5 6 6 0 1 1
2 2 3 1 1 2 2 2 3 0 1 0 2 1 2 1 0 1 1 1 2 0 1 0 2 1 2 1 0 1 2 1 5 0 3 0 5
-1 5 -1 0 -1 1 -1 3 0 2 0 3 -1 3 -1 0 -1 1 -1 2 0 1 -1 2 -2 3 -1 1 -2 2 -2
3 0 2 -2 4 -4 4 -1 0 -2 1 -3 2 -1 1 -2 2 -3 2 -1 0 -2 0 -2 1 0 1 -2 1 -4 1
-2 0 -4 0 -4 1 0 1 -3 1 -6 1 -3 0 -6 0 -6 -1z""/>
</g>
</svg>";

        //speech

        public SpeechRecognitionEngine speechEngine;
        
        RecognizerInfo ri;

        static SpeechSynthesizer reader;

        public SpeechInteraction speechInteraction;

        //timestamps
        public DateTime lastSkeletonTimeStamp;

        Timer timer;


        SkeletonHandler skeletonHandler;

        AsynchronousClient asyncClient;
        
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
            
            //Bluetooth stuff here 
            if (BluetoothOn)
            {
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
            }
            else
            {
                asyncClient = new AsynchronousClient(speechInteraction);
                InitAsyncTCPClient();
            }

            


            lastSkeletonTimeStamp = DateTime.Now;
        }


        private void InitAsyncTCPClient()
        {
            try
            {
                
                client = asyncClient.StartClient();
            }
            catch (System.Net.Sockets.SocketException)
            {
                InitAsyncTCPClient();
            }
        }
        

        /// <summary>
        /// Finalizes an instance of the MainWindow class.
        /// This destructor will run only if the Dispose method does not get called.
        /// </summary>
        ~MainWindow()
        {
            this.Dispose(false);
            if (client != null)
            {
                AsynchronousClient.StopClient(client);
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
                                PrintSVGOutlines();
                                AlreadyConvertedToSVG = true;
                            }
                        }
                        
                        
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
            String svgOutlineString = GenerateOutlineSVG(path);
            Console.WriteLine("before adding frame");
            AddSVGFrame(svgOutlineString);

        }

        private String AddSVGFrame(String svg)
        {
            Console.WriteLine("adding svg frame");
            //String svgFrame = "<?xml version=\"1.0\" standalone=\"no\"?> <!DOCTYPE svg PUBLIC \"-//W3C//DTD SVG 20010904//EN\" \"http://www.w3.org/TR/2001/REC-SVG-20010904/DTD/svg10.dtd\"> <svg version=\"1.0\" width=\"200\" height=\"130\" viewBox=\"0 0 640 480\" xmlns=\"http://www.w3.org/2000/svg\" preserveAspectRatio=\"xMidYMid meet\"> </svg>";
            //String svgFrame = @"<?xml version=""1.0"" standalone=""no""?> <!DOCTYPE svg PUBLIC ""-//W3C//DTD SVG 20010904//EN"" ""http://www.w3.org/TR/2001/REC-SVG-20010904/DTD/svg10.dtd""> <svg version=""1.0"" width=""200"" height=""130"" viewBox=""0 0 640 480"" xmlns=""http://www.w3.org/2000/svg"" preserveAspectRatio=""xMidYMid meet""> </svg>";
            XmlDocument doc = new XmlDocument();
            Console.WriteLine("frame");
            if (speechInteraction.outlines)
            {
                doc.LoadXml("<?xml version=\"1.0\" standalone=\"no\"?> <svg version=\"1.0\" width=\"200\" height=\"130\" viewBox=\"0 0 640 480\" xmlns=\"http://www.w3.org/2000/svg\" preserveAspectRatio=\"xMidYMid meet\"> </svg>");
            }
            else
            {
                doc.LoadXml("<?xml version=\"1.0\" standalone=\"no\"?> <svg version=\"1.0\" width=\"200\" height=\"130\" viewBox=\"0 0 2 2\" xmlns=\"http://www.w3.org/2000/svg\" preserveAspectRatio=\"xMidYMid meet\"> </svg>");
            }
            
            Console.WriteLine("still alive");
            
            string pattern = "<!.*>";
            string replacement = " ";
            Regex rgx = new Regex(pattern);
            Console.WriteLine("rgx match");
            
            Console.WriteLine(rgx.Match(svg));
            //svg = rgx.Replace(svg, replacement);

            //Remove comments
            svg = svg.Replace("﻿<!DOCTYPE svg PUBLIC \"-//W3C//DTD SVG 1.1//EN\" \"http://www.w3.org/Graphics/SVG/1.1/DTD/svg11.dtd\">","");
            svg = svg.Replace("<!DOCTYPE svg PUBLIC \"-//W3C//DTD SVG 20010904//EN\"\n \"http://www.w3.org/TR/2001/REC-SVG-20010904/DTD/svg10.dtd\">","");
            svg = svg.Replace("viewBox=\"0 0 640.000000 480.000000\"", "");
            XmlNode root = doc.DocumentElement;
            Console.WriteLine("root " + root.OuterXml);
            //Console.WriteLine(svg);

            XmlDocument doc1 = new XmlDocument();
            Console.WriteLine("________________________");
            doc1.LoadXml(svg);
            Console.WriteLine("________________________");
            root.AppendChild(doc.ImportNode(doc1.DocumentElement, true));
            Console.WriteLine("added svg frame ");
            Console.WriteLine(doc.OuterXml);
            svgImage = doc.OuterXml;

            
            return svgImage;
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
                      ButtonPrint(null,null);
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
                  case "YES":
                      speechInteraction.fsm.Fire(SpeechInteraction.Command.Print);
                      break;
                  case "TEST":
                      speechInteraction.fsm.Fire(SpeechInteraction.Command.Test);
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
            //this.SendSvg(svgString);
            svgImage = svgString;
            //Console.WriteLine(svgImage);
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
                //directions.Add(new SemanticResultValue("print", "PRINT"));
                directions.Add(new SemanticResultValue("test", "TEST"));
                directions.Add(new SemanticResultValue("yes", "YES"));

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
            Console.WriteLine("Screenshot");
            //reader.Speak("Printing outlines");
            
            pictureTaken = true;
            //speechInteraction.fsm.Fire(SpeechInteraction.Command.Outlines);

        }




        public void TakePictureSkeleton()
        {
            speechInteraction.outlines = false;
            Console.WriteLine("Taking picture of skeleton");
            if (this.sensor.SkeletonStream.IsEnabled )
            {
                bool noSkelTracked = CheckIfPersonInImage();

                if (noSkelTracked)
                {
                    reader.Speak("No person identified");
                    return;
                }
                Console.WriteLine("Skel stream enabled");
                foreach (Skeleton skel in skeletons)
                {
                    if (skel.TrackingState == SkeletonTrackingState.Tracked)
                    {
                       Console.WriteLine("found tracked skeleton");
                        String svgString = GenerateSkeletonSVG(skel);
                        this.svgImage = svgString;
                        //this.SendSvg(svgString);
                        break;
                    }
                }
                Console.WriteLine("all skeletons done");
                //reader.Speak("Skeleton is being printed");

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
                /*this.sensor.SkeletonStream.TrackingMode = this.checkBoxNearMode.IsChecked.GetValueOrDefault()
                                                    ? SkeletonTrackingMode.Seated
                                                    : SkeletonTrackingMode.Default;
                
                this.sensor.DepthStream.Range = this.checkBoxNearMode.IsChecked.GetValueOrDefault()
                                                    ? DepthRange.Near
                                                    : DepthRange.Default;
                Console.WriteLine("Checkbox state " + this.checkBoxNearMode.IsChecked.GetValueOrDefault());*/
                this.sensor.SkeletonStream.TrackingMode = SkeletonTrackingMode.Default;
                this.sensor.DepthStream.Range = DepthRange.Default;
            }
            catch (InvalidOperationException)
            {
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
                if (device.DeviceName.Contains("linepod-photobooth") && !alreadyPaired) //osboxes vs raspberry
                {
                    this.device = device;
                    
                }
            }
        }

        public void SendSvgBluetooth(String svgString)
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
                    byte[] result = IntToByteArray(length);
                    System.Buffer.BlockCopy(result, 0, rv, 36, 4);
                    System.Buffer.BlockCopy(Encoding.UTF8.GetBytes(svgString), 0, rv, 36 + 4, length);
                    stream.Write(rv, 0, 36 + 4 + length);
                    //Console.WriteLine(rv.ToString());

                    pictureTaken = false;
                    AlreadyConvertedToSVG = false;
                }
            }
            else
            {
                Debug.WriteLine("Not Connected");
            }

        }

        public void SendSvgTCP(String svgString)
        {
            Console.WriteLine("Sending");
            if (client != null)
            {

                if (client.Connected)
                {
                    Debug.WriteLine("Connected");

                    AsynchronousClient.Send(client, svgString);
                    pictureTaken = false;
                    AlreadyConvertedToSVG = false;

                }
                else
                {
                    Debug.WriteLine("Not Connected");
                }
            }
            else
            {
                Console.WriteLine("Client is null");
            }
        }


        public static byte[] IntToByteArray(int input){
            byte[] intBytes = BitConverter.GetBytes(input);
                    if (BitConverter.IsLittleEndian)
                        Array.Reverse(intBytes);
            byte[] result = intBytes;
            return result;
        }

        public void BluetoothConnect(){


            
            if (!thisDevice.Connected)
            {

                bool paired = BluetoothSecurity.PairRequest(device.DeviceAddress, "123456");
                if (paired)
                {
                    alreadyPaired = true;
                    Console.WriteLine("Paired!");
                    thisDevice.BeginConnect(device.DeviceAddress, BluetoothService.SerialPort, result => Connected(result, speechInteraction), device);

                }
                else
                {
                    Console.WriteLine("There was a problem pairing.");
                }
            }
            else
            {
                speechInteraction.fsm.Fire(SpeechInteraction.Command.Connected);
                Console.WriteLine("Woohoo we are already connected!");
            }
        }

        private void Connected(IAsyncResult result, SpeechInteraction speechInteraction)
        {
            
            if (result.IsCompleted )
            {
                if (thisDevice.Connected)
                {


                
                    Action lambdaConnected = () => speechInteraction.fsm.Fire(SpeechInteraction.Command.Connected);
                    Application.Current.Dispatcher.Invoke(
                    (Delegate)lambdaConnected);
                    // client is connected now :)
                    NetworkStream stream = thisDevice.GetStream();
                    if (stream.CanRead)
                    {
                        byte[] myReadBuffer = new byte[1024];
                        StringBuilder myCompleteMessage = new StringBuilder();
                        int numberOfBytesRead = 0;

                        // Incoming message may be larger than the buffer size. 
                        do
                        {
                            
                            if (stream.DataAvailable)
                            { 
                                numberOfBytesRead = stream.Read(myReadBuffer, 0, myReadBuffer.Length);
                                /*byte[] typeBytes = new byte[4];
                                Buffer.BlockCopy(myReadBuffer,0,type,0,4);
                                if (BitConverter.IsLittleEndian)
                                    Array.Reverse(typeBytes);

                                int type = BitConverter.ToInt32(typeBytes, 0);*/
                                Console.WriteLine("Received Bytes");
                                Console.WriteLine(BitConverter.ToString(myReadBuffer));
                                //int[] bytesAsInts = Array.ConvertAll(myReadBuffer, c => (int)c);  //myReadBuffer.Select(x => (int)x).ToArray();
                                Console.WriteLine("Casted bytes to int array");
                                //int type = bytesAsInts[0];
                                //Console.WriteLine("first int " + type);
                                int value = 0;
                                for (int i = 0; i < 4; i++)
                                {
                                    value = (value << 4) + (myReadBuffer[i] & 0xff);
                                }
                                Console.WriteLine("value " + value);
                                switch (value)
                                {
                                    case 0:
                                        /*Console.WriteLine("x1 " + bytesAsInts[1]);
                                        Console.WriteLine("y1 " + bytesAsInts[2]);
                                        Console.WriteLine("x2 " + bytesAsInts[3]);
                                        Console.WriteLine("y3 " + bytesAsInts[4]);*/

                                        Console.WriteLine("Received input data from airbar");
                                        break;
                                    case 1:
                                        int status = 0;
                                        for (int i = 4; i < 8; i++)
                                        {
                                            status = (status << 4) + (myReadBuffer[i] & 0xff);
                                        }
                                        //int status = bytesAsInts[1];
                                        Console.WriteLine("Status " + status);
                                        Console.WriteLine("received uuid");
                                        byte[] subArray = new byte[36];
                                        int start = sizeof(int) + sizeof(int);
                                        //Buffer.BlockCopy(myReadBuffer, 0, subArray, start, 36);

                                        //String uuid = Encoding.ASCII.GetString(subArray);
                                        //Console.WriteLine("received uuid " + uuid);
                                        switch (status)
                                        {
                                            case 0:
                                                Console.WriteLine("Finished printing");
                                                Action lambda = () => speechInteraction.fsm.Fire(SpeechInteraction.Command.Printed);
                                                Application.Current.Dispatcher.Invoke(
                                                (Delegate)lambda);
                                                break;


                                        }
                                        break;
                                }
                                Console.WriteLine("Stepped through decision tree");
                                //MainWindow.DecodeReceivedData(myReadBuffer,speechInteraction);
                            }
                            
                            //myCompleteMessage.AppendFormat("{0}", Encoding.ASCII.GetString(myReadBuffer, 0, numberOfBytesRead));
                        }
                        
                        while (stream.CanRead && thisDevice.Connected);

                        // Print out the received message to the console.
                        //Console.WriteLine("You received the following message : " + myCompleteMessage);
                    }
                    else
                    {
                        Console.WriteLine("could not connect");
                    }
                }
                
                
                

            }
            else
            {
                Console.WriteLine("Could not connect");
            }
        }

/*
        public static void DecodeReceivedData(byte[] myReadBuffer, SpeechInteraction speechInteraction)
        {
            Console.WriteLine("Casted bytes to int array");
            //int type = bytesAsInts[0];
            //Console.WriteLine("first int " + type);
            int value = 0;
            for (int i = 0; i < 4; i++)
            {
                value = (value << 4) + (myReadBuffer[i] & 0xff);
            }
            Console.WriteLine("value " + value);
            switch (value)
            {
                case 0:
                    /*Console.WriteLine("x1 " + bytesAsInts[1]);
                    Console.WriteLine("y1 " + bytesAsInts[2]);
                    Console.WriteLine("x2 " + bytesAsInts[3]);
                    Console.WriteLine("y3 " + bytesAsInts[4]);*/

              /*      Console.WriteLine("Received input data from airbar");
                    break;
                case 1:
                    int status = 0;
                    for (int i = 4; i < 8; i++)
                    {
                        status = (status << 4) + (myReadBuffer[i] & 0xff);
                    }
                    //int status = bytesAsInts[1];
                    Console.WriteLine("Status " + status);
                    Console.WriteLine("received uuid");
                    byte[] subArray = new byte[36];
                    int start = sizeof(int) + sizeof(int);
                    //Buffer.BlockCopy(myReadBuffer, 0, subArray, start, 36);

                    //String uuid = Encoding.ASCII.GetString(subArray);
                    //Console.WriteLine("received uuid " + uuid);
                    switch (status)
                    {
                        case 0:
                            Console.WriteLine("Finished printing");
                            Action lambda = () => speechInteraction.fsm.Fire(SpeechInteraction.Command.Printed);
                            Application.Current.Dispatcher.Invoke(
                            (Delegate)lambda);
                            break;


                    }
                    break;
            }
            Console.WriteLine("Stepped through decision tree");

        }
*/

        /// <summary>
        /// Event handler for Kinect sensor's SkeletonFrameReady event
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void SensorSkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            if (!pictureTaken)
            {

                /*if (this.checkBoxSkeleton.IsChecked.GetValueOrDefault())
                {*/
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

                
            //}

        }


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
/*
            float xMin = float.MaxValue;
            float xMax = 0;
            float yMin = float.MaxValue;
            float yMax = 0;

            foreach (var joint in skel.Joints)
            {
                List<PointF> points = new List<PointF>() { joint., p.End };
                foreach (var point in points)
                {
                    if (point.X < xMin)
                    {
                        xMin = point.X;
                    }
                    else if (point.X > xMax)
                    {
                        xMax = point.X;
                    }
                    if (point.Y < yMin)
                    {
                        yMin = point.Y;
                    }
                    else if (point.Y > yMax)
                    {
                        yMax = point.Y;
                    }
                }
            }
            if (headCircle.CenterX - headCircle.Radius < xMin)
            {
                xMin = headCircle.Center.X;
            }
            else if (headCircle.CenterX + headCircle.Radius > xMin)
            {
                xMax = headCircle.Center.X;
            }
            if (headCircle.CenterY - headCircle.Radius < yMin)
            {
                yMin = headCircle.Center.Y;
            }
            else if (headCircle.CenterY + headCircle.Radius > yMin)
            {
                yMax = headCircle.Center.Y;
            }*/

            skeletonHandler.AddJointsToPath(path, arms, 100);
            skeletonHandler.AddJointsToPath(path, back, 100);
            skeletonHandler.AddJointsToPath(path, legs, 100);

            //calculate intersecion point of head and neck
            //double shoulderToHead = Math.Sqrt(Math.Pow(head.Position.X - shoulderCenter.Position.X,2) + Math.Pow(head.Position.Y - shoulderCenter.Position.Y,2));
            /*float deltaX = leftHand.Position.X - leftElbow.Position.X;
            float deltaY = leftHand.Position.Y - leftElbow.Position.Y;
            float deltaZ = leftHand.Position.Z - leftElbow.Position.Z;
            float distance = (float)Math.Sqrt(deltaX * deltaX + deltaY * deltaY + deltaZ * deltaZ);
            float headRadius = (float)(distance * scale / 2.5);
            Vector headVec = new Vector(head.Position.X, head.Position.Y);
            Vector distVector = headVec - new Vector(shoulderCenter.Position.X, shoulderCenter.Position.Y);
            distVector.Normalize();
            Vector intersectingPoint = headVec - distVector * headRadius;
            */
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
        

        /// <summary>
        /// Handles the checking or unchecking of the seated mode combo box
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void CheckBoxSeatedModeChanged(object sender, RoutedEventArgs e)
        {
            if (null != this.sensor)
            {
                //if (this.checkBoxNearMode.IsChecked.GetValueOrDefault())
                //{
                    this.sensor.SkeletonStream.TrackingMode = SkeletonTrackingMode.Seated;
                /*}
                else
                {*/
                    this.sensor.SkeletonStream.TrackingMode = SkeletonTrackingMode.Default;
                //}
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