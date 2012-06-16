using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PPKinecT
{
    class KCalibrator
    {
        public KCalibrator()
        {
            screenKDistance = 0;
            screenSetted = 0;
        }
        
        private int screenKDistance;
        /// <summary>
        /// Set distance of kinect to screen, useful only if status is NoneSetted
        /// </summary>
        /// <param name="depth">Distance of kinect to screen</param>
        public void SetDepth(int depth)
        {
            screenKDistance = depth;
        }

        /// <summary>
        /// Get the point on screen from the hand and elbow vector
        /// </summary>
        /// <param name="hand">Hand position in depth frame</param>
        /// <param name="elbow">Elbow position in depth frame</param>
        /// <returns>Position of screen in depth frame</returns>
        public Vector2f ArmToScreen(Vector3f hand, Vector3f elbow)
        {
            Vector2f result = new Vector2f(0, 0);
            if (elbow.Z - hand.Z == 0 ||
                elbow.Z == 0)
            {
                // return an invisible position
                result.X = -1.0f;
                result.Y = -1.0f;
            }
            else
            {
                result.X = elbow.Z / (elbow.Z - hand.Z)
                    * (hand.X - hand.Z / elbow.Z * elbow.X);
                result.Y = elbow.Z / (elbow.Z - hand.Z)
                    * (hand.Y - hand.Z / elbow.Z * elbow.Y);
            }
            return result;
        }

        private float screenLeft;
        private float screenRight;
        private float screenBottom;
        private float screenTop;

        private const int SCREEN_EDGE_CNT = 4;
        private Vector2f[] screenEdge = new Vector2f[SCREEN_EDGE_CNT];
        private int screenSetted;
        /// <summary>
        /// Set screen position according to current status.
        /// Order: TopLeft, TopRight, BottomLeft, BottomRight
        /// </summary>
        /// <param name="hand">Position of hand in depth frame</param>
        /// <param name="elbow">Position of elbow in depth frame</param>
        /// <return>If screen positions are all set</return>
        public bool SetScreenPosition(Vector3f hand, Vector3f elbow)
        {
            if (screenSetted < SCREEN_EDGE_CNT)
            {
                screenEdge[screenSetted] = ArmToScreen(hand, elbow);
                ++screenSetted;
                return false;
            }
            else
            {
                float[] x = new float[SCREEN_EDGE_CNT];
                float[] y = new float[SCREEN_EDGE_CNT];
                for (int i = 0; i < SCREEN_EDGE_CNT; ++i)
                {
                    x[i] = screenEdge[i].X;
                    y[i] = screenEdge[i].Y;
                }
                Array.Sort(x);
                Array.Sort(y);

                // use the avg of the two smaller value to set for bottom and left
                screenLeft = (x[0] + x[1]) / 2.0f;
                screenBottom = (y[0] + y[1]) / 2.0f;
                // use the avg of the two larger value to set for top and right
                screenRight = (x[2] + x[3]) / 2.0f;
                screenTop = (y[2] + y[3]) / 2.0f;
                return true;
            }
        }
    }
}
