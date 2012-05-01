using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

#if WITH_KINECT
using Microsoft.Kinect;
using System.Windows.Threading;
#endif

namespace PPKinecT
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            mainController = new MainController();

            InitializeComponent();
#if WITH_KINECT
            InitializeKinect();

            // Timer used for frame rate, call SecondTimeOut every second
            //Timer timer = new Timer(1000);
            //timer.Elapsed += new ElapsedEventHandler(SecondTimeOut);
            //timer.AutoReset = true;
            //timer.Enabled = true;
            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(1000);
            timer.Tick += SecondTimeOut;
            timer.Start();
#endif
        }

        private MainController mainController;

#if WITH_KINECT
        private KinectSensor sensor;

        private int frameCount;

        /// <summary>
        /// Init kinect and check if kinect is enabled
        /// </summary>
        private void InitializeKinect()
        {
            if (mainController.Status != MainController.MainStatus.Init)
            {
                return;
            }
            // Change to KinectDetecting
            mainController.ToNextState();

            sensor = KinectSensor.KinectSensors.FirstOrDefault();
            while (sensor == null)
            {
                if (MessageBox.Show("Kinect was not detected. Click OK to try again and Cancel to exit!",
                    "Kinect not detected", MessageBoxButton.OKCancel) == MessageBoxResult.Cancel)
                {
                    this.Close();
                    Application.Current.Shutdown();
                    return;
                }
                sensor = KinectSensor.KinectSensors.FirstOrDefault();
            }
            // Kinect detected, change to next status
            mainController.ToNextState();

            sensor.Start();

            sensor.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);
            sensor.ColorFrameReady += new EventHandler<ColorImageFrameReadyEventArgs>(SensorColorFrameReady);

            sensor.DepthStream.Enable(DepthImageFormat.Resolution320x240Fps30);
            sensor.DepthFrameReady += new EventHandler<DepthImageFrameReadyEventArgs>(SensorDepthFrameReady);
            sensor.SkeletonStream.Enable();
            sensor.SkeletonFrameReady += new EventHandler<SkeletonFrameReadyEventArgs>(SensorSkeletonFrameReady);

            sensor.ElevationAngle = 11;

            frameCount = 0;
        }

        void SensorColorFrameReady(object sender, ColorImageFrameReadyEventArgs e)
        {
            ++frameCount;

            // only show color frame when detecting depth or edge
            if (mainController.Status == MainController.MainStatus.DepthDetecting ||
                mainController.Status == MainController.MainStatus.EdgeDetecting)
            {
                using (var image = e.OpenColorImageFrame())
                {
                    if (image == null)
                    {
                        return;
                    }
                    
                    byte[] colorBytes = new byte[image.PixelDataLength];
                    image.CopyPixelDataTo(colorBytes);

                    // You could use PixelFormats.Bgr32 below to ignore the alpha,
                    // or if you need to set the alpha you would loop through the bytes 
                    // as in this loop below
                    int length = colorBytes.Length;
                    for (int i = 0; i < length; i += 4)
                    {
                        colorBytes[i + 3] = 255;
                    }

                    BitmapSource source = BitmapSource.Create(image.Width, image.Height,
                        96, 96,
                        PixelFormats.Bgra32,
                        null,
                        colorBytes,
                        image.Width * image.BytesPerPixel);
                    videoImage.Source = source;
                }
            }
        }

        void SensorSkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            using (var skeletonFrame = e.OpenSkeletonFrame())
            {
                if (skeletonFrame == null)
                {
                    return;
                }

                Skeleton[] skeletons = new Skeleton[skeletonFrame.SkeletonArrayLength];
                skeletonFrame.CopySkeletonDataTo(skeletons);
                Skeleton closestSkeleton = (from s in skeletons
                                            where s.TrackingState == SkeletonTrackingState.Tracked
                                            select s).OrderBy(s => s.Joints[JointType.HandRight].Position.Z)
                                                    .FirstOrDefault();
                if (closestSkeleton == null)
                {
                    return;
                }

                // enqueue usable position of joints
                mainController.EnqueueJoints(closestSkeleton.Joints);

                switch (mainController.Status)
                {
                    case MainController.MainStatus.DepthDetecting:
                        Joint rightHand = mainController.LastRightHand();
                        Joint rightElbow = mainController.LastRightElbow();
                        if (rightHand == null || rightElbow == null)
                        {
                            return;
                        }
                        // position of the screen
                        ColorImagePoint rightHandScreen = sensor.MapSkeletonPointToColor(
                            rightHand.Position, sensor.ColorStream.Format);
                        ColorImagePoint rightElbowScreen = sensor.MapSkeletonPointToColor(
                            rightElbow.Position, sensor.ColorStream.Format);

                        // Change circle position in canvas
                        Canvas.SetLeft(RightHand, rightHandScreen.X - RightHand.Width / 2);
                        Canvas.SetTop(RightHand, rightHandScreen.Y - RightHand.Height / 2);
                        Canvas.SetLeft(RightElbow, rightElbowScreen.X - RightElbow.Width / 2);
                        Canvas.SetTop(RightElbow, rightElbowScreen.Y - RightElbow.Height / 2);

                        break;
                }
            }
        }

        void SensorDepthFrameReady(object sender, DepthImageFrameReadyEventArgs e)
        {
            using (var depthFrame = e.OpenDepthImageFrame())
            {
                if (depthFrame == null)
                {
                    return;
                }

                switch (mainController.Status)
                {
                    case MainController.MainStatus.DepthDetecting:
                        // If is stable, the status will be changed to EdgeDetecting
                        mainController.DoDepthDetecting(sensor, depthFrame);
                        break;
                }
            }
        }

        //private void SecondTimeOut(object source, ElapsedEventArgs e)
        private void SecondTimeOut(object source, EventArgs e)
        {
            frameRate.Text = "Frame rate: " + frameCount;
            //MessageBox.Show(frameCount.ToString());
            frameCount = 0;
        }
#endif
    }
}
