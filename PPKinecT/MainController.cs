using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

#if WITH_KINECT
using Microsoft.Kinect;
#endif

namespace PPKinecT
{
    class MainController
    {
        public MainController()
        {
            status = MainStatus.Init;

            kCalibrator = new KCalibrator();

#if WITH_KINECT
            leftHandQueue = new Queue<Joint>();
            rightHandQueue = new Queue<Joint>();
            leftElbowQueue = new Queue<Joint>();
            rightElbowQueue = new Queue<Joint>();
#endif
        }

        private KCalibrator kCalibrator;

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
        private MainStatus status;
        public MainStatus Status
        {
            get
            {
                return status;
            }
        }
        public void ToNextState()
        {
            switch (status)
            {
                case MainStatus.Init:
                    status = MainStatus.KinectDetecting;
                    break;

                case MainStatus.KinectDetecting:
                    status = MainStatus.DepthDetecting;
                    break;

                case MainStatus.DepthDetecting:
                    status = MainStatus.EdgeDetecting;
                    break;

                case MainStatus.EdgeDetecting:
                    status = MainStatus.PptWaiting;
                    break;

                case MainStatus.PptWaiting:
                    status = MainStatus.PptPresenting;
                    break;

                case MainStatus.PptPresenting:
                    status = MainStatus.Complete;
                    break;

                case MainStatus.Complete:
                    break;
            }
        }

#if WITH_KINECT
        private const int QUEUE_SIZE = 1024;

        /// <summary>
        /// Queue of left hand position with Kinect
        /// </summary>
        private Queue<Joint> leftHandQueue;

        /// <summary>
        /// Queue of right hand position with Kinect
        /// </summary>
        private Queue<Joint> rightHandQueue;

        /// <summary>
        /// Queue of left elbow position with Kinect
        /// </summary>
        private Queue<Joint> leftElbowQueue;

        /// <summary>
        /// Queue of right elbow position with Kinect
        /// </summary>
        private Queue<Joint> rightElbowQueue;

        /// <summary>
        /// Enqueue usable position of joints of current frame,
        /// untracked joints will be ignored
        /// </summary>
        /// <param name="jointCollection">Joint collection of the frame</param>
        public void EnqueueJoints(JointCollection jointCollection)
        {
            if (jointCollection[JointType.HandLeft].TrackingState != JointTrackingState.NotTracked)
            {
                leftHandQueue.Enqueue(jointCollection[JointType.HandLeft]);
                if (leftHandQueue.Count > QUEUE_SIZE)
                {
                    leftHandQueue.Dequeue();
                }
            }
            if (jointCollection[JointType.HandRight].TrackingState != JointTrackingState.NotTracked)
            {
                rightHandQueue.Enqueue(jointCollection[JointType.HandRight]);
                if (rightHandQueue.Count > QUEUE_SIZE)
                {
                    rightHandQueue.Dequeue();
                }
            }
            if (jointCollection[JointType.ElbowLeft].TrackingState != JointTrackingState.NotTracked)
            {
                leftElbowQueue.Enqueue(jointCollection[JointType.ElbowLeft]);
                if (leftElbowQueue.Count > QUEUE_SIZE)
                {
                    leftElbowQueue.Dequeue();
                }
            }
            if (jointCollection[JointType.ElbowRight].TrackingState != JointTrackingState.NotTracked)
            {
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
        /// under the condition that status is DepthDetecting.
        /// If is stable, the status will be changed to next after doing depth calibration.
        /// If not, nothing will be changed.
        /// </summary>
        public void DoDepthDetecting(KinectSensor sensor, DepthImageFrame depthFrame)
        {
            if (status == MainStatus.DepthDetecting)
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
    }
}
