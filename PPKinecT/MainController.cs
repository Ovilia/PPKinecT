using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Threading;

#if WITH_KINECT
using Microsoft.Kinect;
#endif

namespace PPKinecT
{
    class MainController
    {
        public MainController(MainWindow mainWindow)
        {
            Status = MainStatus.Init;

            this.mainWindow = mainWindow;
            screenWidth = 1024;
            screenHeight = 800;

            pptCtrl = new PPTController();
            pptDetectTimer = new DispatcherTimer();
            pptDetectTimer.Interval = TimeSpan.FromMilliseconds(1000);
            pptDetectTimer.Tick += PptDetectTimeOut;

            edgeTimer = new DispatcherTimer();
            edgeTimer.Interval = TimeSpan.FromMilliseconds(1000);
            edgeTimer.Tick += EdgeCoolingTimeOut;

            clickTimer = new DispatcherTimer();
            clickTimer.Interval = TimeSpan.FromMilliseconds(5000);
            clickTimer.Tick += ClickTimerOut;

            ReadPreferenceFile("preference.txt");

            kCalibrator = new KCalibrator(screenWidth, screenHeight);

#if WITH_KINECT
            leftHandQueue = new Queue<Joint>();
            rightHandQueue = new Queue<Joint>();
            leftElbowQueue = new Queue<Joint>();
            rightElbowQueue = new Queue<Joint>();

            edgeCooling = false;
            clickCooling = false;

            isZoomed = false;
#else
            Status = MainStatus.PptWaiting;
            pptDetectTimer.Start();
#endif
        }

        private KCalibrator kCalibrator;
        private MainWindow mainWindow;
        private int screenWidth;
        private int screenHeight;

        private DispatcherTimer edgeTimer;

        private PPTController pptCtrl;
        private String pptProcessName;
        private DispatcherTimer pptDetectTimer;

        private DispatcherTimer clickTimer;

        private bool isZoomed;

        public enum MainStatus
        {
            Init,
            KinectDetecting,
            DepthDetecting,
            EdgeDetecting,
            PptWaiting,
            PptPresenting,
            Complete
        };
        public MainStatus Status
        {
            get;
            set;
        }

#if WITH_KINECT
        private const int QUEUE_SIZE = 1024;

        /// <summary>
        /// Queue of left hand position with Kinect
        /// </summary>
        private Queue<Joint> leftHandQueue;
        private Joint lastLeftHand;

        /// <summary>
        /// Queue of right hand position with Kinect
        /// </summary>
        private Queue<Joint> rightHandQueue;
        private Joint lastRightHand;

        /// <summary>
        /// Queue of left elbow position with Kinect
        /// </summary>
        private Queue<Joint> leftElbowQueue;
        private Joint lastLeftElbow;

        /// <summary>
        /// Queue of right elbow position with Kinect
        /// </summary>
        private Queue<Joint> rightElbowQueue;
        private Joint lastRightElbow;

        /// <summary>
        /// Enqueue usable position of joints of current frame,
        /// untracked joints will be ignored
        /// </summary>
        /// <param name="jointCollection">Joint collection of the frame</param>
        public void EnqueueJoints(JointCollection jointCollection)
        {
            if (jointCollection[JointType.HandLeft].TrackingState != JointTrackingState.NotTracked)
            {
                lastLeftHand = jointCollection[JointType.HandLeft];
                leftHandQueue.Enqueue(jointCollection[JointType.HandLeft]);
                if (leftHandQueue.Count > QUEUE_SIZE)
                {
                    leftHandQueue.Dequeue();
                }
            }
            if (jointCollection[JointType.HandRight].TrackingState != JointTrackingState.NotTracked)
            {
                lastRightHand = jointCollection[JointType.HandRight];
                rightHandQueue.Enqueue(jointCollection[JointType.HandRight]);
                if (rightHandQueue.Count > QUEUE_SIZE)
                {
                    rightHandQueue.Dequeue();
                }
            }
            if (jointCollection[JointType.ElbowLeft].TrackingState != JointTrackingState.NotTracked)
            {
                lastLeftElbow = jointCollection[JointType.ElbowLeft];
                leftElbowQueue.Enqueue(jointCollection[JointType.ElbowLeft]);
                if (leftElbowQueue.Count > QUEUE_SIZE)
                {
                    leftElbowQueue.Dequeue();
                }
            }
            if (jointCollection[JointType.ElbowRight].TrackingState != JointTrackingState.NotTracked)
            {
                LastRightElbow = jointCollection[JointType.ElbowRight];
                rightElbowQueue.Enqueue(jointCollection[JointType.ElbowRight]);
                if (rightElbowQueue.Count > QUEUE_SIZE)
                {
                    rightElbowQueue.Dequeue();
                }
            }
        }

        /// <summary>
        /// Get last element in left hand queue
        /// </summary>
        /// <returns>Last element</returns>
        public Joint LastLeftHand()
        {
            return leftHandQueue.Last();
        }

        /// <summary>
        /// Get last element in right hand queue
        /// </summary>
        /// <returns>Last element</returns>
        public Joint LastRightHand()
        {
            return rightHandQueue.Last();
        }

        /// <summary>
        /// Get last element in left elbow queue
        /// </summary>
        /// <returns>Last element</returns>
        public Joint LastLeftElbow()
        {
            return leftElbowQueue.Last();
        }

        /// <summary>
        /// Get last element in right elbow queue
        /// </summary>
        /// <returns>Last element</returns>
        public Joint LastRightElbow()
        {
            return rightElbowQueue.Last();
        }

        // tolerance of hand position used for checking if hand is stable in depth detecting
        private const float DEPTH_HAND_TOLERANCE = 0.05f;
        // count of hand position used for checking if hand is stable in depth detecting
        private const int DEPTH_HAND_COUNT = 50;
        // radius with center of right hand in depth image
        private const int DEPTH_NEAR_RADIUS = 20;
        // count of points in each line(top, bottom, left, right) in depth image
        // used for get depth of area near hand
        private const int DEPTH_NEAR_COUNT = 10;
        // span of two neighbor points of area near hand
        const float DEPTH_SPAN = 2.0f * DEPTH_NEAR_RADIUS / DEPTH_NEAR_COUNT;

        /// <summary>
        /// Check queue if right hand and elbow positions are stable
        /// under the condition that Status is DepthDetecting.
        /// If is stable, the Status will be changed to next after doing depth calibration.
        /// If not, nothing will be changed.
        /// </summary>
        public void DoDepthDetecting(KinectSensor sensor, DepthImageFrame depthFrame)
        {
            if (Status == MainStatus.DepthDetecting)
            {
                if (IsStable(rightHandQueue, DEPTH_HAND_TOLERANCE, DEPTH_HAND_COUNT))
                {
                    // if is stable, calibrate depth according to avg of position near hand
                    SkeletonPoint handPoint = rightHandQueue.Last().Position;
                    DepthImagePoint centerDepthPoint = sensor.MapSkeletonPointToDepth(
                        handPoint, sensor.DepthStream.Format);
                    int[] depthArr = new int[DEPTH_NEAR_COUNT * 4];
                    int index = 0;
                    for (int i = 0; i < DEPTH_NEAR_COUNT; ++i)
                    {
                        // top
                        SkeletonPoint topSke = depthFrame.MapToSkeletonPoint(
                            centerDepthPoint.X - DEPTH_NEAR_RADIUS + (int)(DEPTH_SPAN * i),
                            centerDepthPoint.Y - DEPTH_NEAR_RADIUS);
                        depthArr[index++] = depthFrame.MapFromSkeletonPoint(topSke).Depth;
                        // bottom
                        SkeletonPoint bottomSke = depthFrame.MapToSkeletonPoint(
                            centerDepthPoint.X - DEPTH_NEAR_RADIUS + (int)(DEPTH_SPAN * i),
                            centerDepthPoint.Y + DEPTH_NEAR_RADIUS);
                        depthArr[index++] = depthFrame.MapFromSkeletonPoint(bottomSke).Depth;
                        // left
                        SkeletonPoint leftSke = depthFrame.MapToSkeletonPoint(
                            centerDepthPoint.X - DEPTH_NEAR_RADIUS,
                            centerDepthPoint.Y - DEPTH_NEAR_RADIUS + (int)(DEPTH_SPAN * i));
                        depthArr[index++] = depthFrame.MapFromSkeletonPoint(leftSke).Depth;
                        // right
                        SkeletonPoint rightSke = depthFrame.MapToSkeletonPoint(
                            centerDepthPoint.X + DEPTH_NEAR_RADIUS,
                            centerDepthPoint.Y - DEPTH_NEAR_RADIUS + (int)(DEPTH_SPAN * i));
                        depthArr[index++] = depthFrame.MapFromSkeletonPoint(rightSke).Depth;
                    }
                    // set median(rather than mean) depth of the list
                    Array.Sort(depthArr);
                    kCalibrator.SetDepth(depthArr[DEPTH_NEAR_COUNT / 2]);

                    Status = MainStatus.EdgeDetecting;
                    mainWindow.textBlock.Text = "Detecting edge now. Arm at four corners of the screen.";
                    // cooling time timer
                    DispatcherTimer timer = new DispatcherTimer();
                    timer.Interval = TimeSpan.FromMilliseconds(COOLING_TIME);
                    timer.Tick += EdgeCoolingTimeOut;
                    timer.Start();
                    edgeCooling = true;
                }
            }
        }

        // tolerance in edge detecting
        private const float EDGE_TOLERANCE = 0.05f;
        private const int EDGE_COUNT = 50;
        private const int COOLING_TIME = 5000;
        private bool edgeCooling;
        public void DoEdgeDetecting()
        {
            if (Status == MainStatus.EdgeDetecting && !edgeCooling)
            {
                if (IsStable(rightHandQueue, EDGE_TOLERANCE, EDGE_COUNT) &&
                    IsStable(rightElbowQueue, EDGE_TOLERANCE, EDGE_COUNT))
                {
                    SkeletonPoint handPoint = rightHandQueue.Last().Position;
                    Vector3f handVec = new Vector3f(handPoint.X, handPoint.Y, handPoint.Z);                    
                    SkeletonPoint elbowPoint = rightElbowQueue.Last().Position;
                    Vector3f elbowVec = new Vector3f(elbowPoint.X, elbowPoint.Y, elbowPoint.Z);
                    if (kCalibrator.SetScreenPosition(handVec, elbowVec))
                    {
                        // all screen position setted
                        Status = MainStatus.PptWaiting;        
                        pptDetectTimer.Start();
                    }
                    else
                    {
                        mainWindow.textBlock.Text = "Detecting another edge.";
                        // point to another screen edge, using cooling time to prevent pointing to the same point
                        edgeTimer.Start();
                        edgeCooling = true;
                    }
                }
            }
        }
        private void EdgeCoolingTimeOut(object source, EventArgs e)
        {
            edgeCooling = false;
            mainWindow.textBlock.Text = "Detecting edge now. Please arm your right hand and elbow" +
                " at four corners of the screen.";
            edgeTimer.Stop();
        }

#endif
        public void DoPptDetecting()
        {
            if (Status == MainStatus.PptWaiting)
            {
                System.Diagnostics.Process[] processList = System.Diagnostics.Process.GetProcesses();
                foreach (System.Diagnostics.Process process in processList)
                {
                    if (process.ProcessName == pptProcessName)
                    {
                        // ppt detected
                        Status = MainStatus.PptPresenting;
                        return;
                    }
                }
            }
        }

        // temp used to check ppt 
        private void PptDetectTimeOut(object source, EventArgs e)
        {
            Console.WriteLine("check");
            DoPptDetecting();
            if (Status != MainStatus.PptWaiting)
            {
                // status changed
                pptDetectTimer.Stop();
            }
        }

        private const int LEFT_HAND_MATCH_CNT = 20;
        private const int RIGHT_HAND_MATCH_CNT = 15;
        private const float RIGHT_HAND_LEFT_CONFIDENCE = 0.7f;
        private const float RIGHT_HAND_RIGHT_CONFIDENCE = 0.6f;
        private const float BOTH_HANDS_CONFIDENCE = 0.8f;

        private const float CLICK_TOLERANCE = 0.05f;
        private const int CLICK_CHECK_COUNT = 30;
        private bool clickCooling;

        private int lastXPoint = -1;
        private int lastYPoint = -1;
        public void DoPptPresenting()
        {
            if (Status == MainStatus.PptPresenting)
            {
                pptCtrl.RemoveMark();
                // check if is pointed to screen
                int xPos = -1, yPos = -1;
                bool isPointed = kCalibrator.PointScreenPosition(lastRightHand, lastRightElbow, out xPos, out yPos);
                if (isPointed)
                {
                    pptCtrl.ShowMark(xPos, yPos);
                    if (isZoomed)
                    {
                        // move zoomed screen
                        if (lastXPoint != -1 && lastYPoint != -1)
                        {
                            pptCtrl.MoveScreen(xPos, yPos);
                        }
                        lastXPoint = xPos;
                        lastYPoint = yPos;
                    }
                    else
                    {
                        if (!clickCooling && IsStable(rightHandQueue, CLICK_TOLERANCE, CLICK_CHECK_COUNT))
                        {
                            // click hypertext
                            pptCtrl.Click(xPos, yPos);
                            clickCooling = true;
                            clickTimer.Start();
                        }
                    }
                }
                else
                {
                    lastXPoint = -1;
                    lastYPoint = -1;
                    DoActionMatching();
                }
            }
        }

        // temp used to check ppt 
        private void ClickTimeOut(object source, EventArgs e)
        {
            clickCooling = false;
            clickTimer.Stop();
        }

#if WITH_KINECT
        private void DoActionMatching()
        {
            float rightSimi = 0.0f;
            // right hand
            Joint[] rightHand = rightHandQueue.ToArray();
            Action.ActionType rightType = Action.MatchedAction(rightHand, RIGHT_HAND_MATCH_CNT, out rightSimi);
            if (rightType == Action.ActionType.RIGHT && rightSimi > RIGHT_HAND_LEFT_CONFIDENCE)
            {
                // right-hand turn left
                pptCtrl.ToPreviousPage();
                return;
            }
            else if (rightType == Action.ActionType.LEFT && rightSimi > RIGHT_HAND_RIGHT_CONFIDENCE)
            {
                // right-hand turn right
                pptCtrl.ToNextPage();
            }
            else if (rightType == Action.ActionType.LEFT_DOWN || rightType == Action.ActionType.LEFT_UP ||
                rightType == Action.ActionType.RIGHT_DOWN || rightType == Action.ActionType.RIGHT_UP)
            {
                // left hand
                float leftSimi = 0.0f;
                Joint[] leftHand = leftHandQueue.ToArray();
                Action.ActionType leftType = Action.MatchedAction(rightHand, RIGHT_HAND_MATCH_CNT, out leftSimi);
                if (leftSimi > BOTH_HANDS_CONFIDENCE && rightSimi > BOTH_HANDS_CONFIDENCE)
                {
                    if (leftType == Action.ActionType.LEFT_UP && rightType == Action.ActionType.RIGHT_DOWN)
                    {
                        // left hand turn left-up, right hand turn right-down
                        pptCtrl.ZoomIn();
                        isZoomed = true;
                    }
                    else if (leftType == Action.ActionType.RIGHT_DOWN && rightType == Action.ActionType.LEFT_UP)
                    {
                        // left hand turn right-down, right hand turn left-up
                        pptCtrl.ZoomOut();
                        isZoomed = pptCtrl.isZoomed();
                    }
                }
            }
        }

        /// <summary>
        /// Check if a queue has stable elements in the end part
        /// </summary>
        /// <param name="queue">The queue to be checked</param>
        /// <param name="tolerance">Max difference allowed</param>
        /// <param name="count">Count of elements in the end part</param>
        /// <returns>If the tolerance of the end part is less than or equal to tolerance</returns>
        private bool IsStable(Queue<Joint> queue, float tolerance, int count)
        {
            int qCount = queue.Count;
            if (qCount < count)
            {
                // queue is not stable
                return false;
            }
            SkeletonPoint lastPoint = queue.ElementAt(qCount - 1).Position;
            for (int i = 0; i < count; ++i)
            {
                // point from end to front
                if (Distance(queue.ElementAt(qCount - 1 - i).Position,
                    lastPoint) > tolerance)
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Distance of two SkeletonPoints
        /// </summary>
        /// <param name="point1">Point 1</param>
        /// <param name="point2">Point 2</param>
        /// <returns>Distance of two points</returns>
        private float Distance(SkeletonPoint point1, SkeletonPoint point2)
        {
            Vector3f vector = new Vector3f(point1.X - point2.X,
                point1.Y - point2.Y, point1.Z - point2.Z);
            return vector.Modulus();
        }
#endif

        private bool ReadPreferenceFile(String fileName)
        {
            try
            {
                System.IO.StreamReader file = new System.IO.StreamReader(fileName);
                String line;
                while ((line = file.ReadLine()) != null)
                {
                    if (line.StartsWith("process_name"))
                    {
                        pptProcessName = line.Substring(line.IndexOf("=") + 1).Trim();
                    }
                    else if (line.StartsWith("screen_width"))
                    {
                        try
                        {
                            screenWidth = int.Parse(line.Substring(line.IndexOf("=") + 1).Trim());
                        }
                        catch (Exception e)
                        {
                            screenWidth = 1024;
                        }
                    }
                    else if (line.StartsWith("screen_height"))
                    {
                        try
                        {
                            screenHeight = int.Parse(line.Substring(line.IndexOf("=") + 1).Trim());
                        }
                        catch (Exception e)
                        {
                            screenHeight = 800;
                        }
                    }
                }
                file.Close();
                return true;
            }
            catch (System.IO.FileNotFoundException ex)
            {
                System.Windows.MessageBox.Show("File " + fileName + " not found");
                return false;
            }
        }

        public EventHandler ClickTimerOut { get; set; }
    }
}
