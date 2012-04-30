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

            leftHandQueue = new Queue<Joint>();
            rightHandQueue = new Queue<Joint>();
            leftElbowQueue = new Queue<Joint>();
            rightElbowQueue = new Queue<Joint>();
        }

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

        private const int QUEUE_SIZE = 64;

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
    }
}
