using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PPKinecT
{
    class KCalibrator
    {
        public KCalibrator(int screenWidth, int screenHeight)
        {
            screenKDistance = 0;
            screenSetted = 0;
            this.screenWidth = screenWidth;
            this.screenHeight = screenHeight;
        }

        private int screenWidth;
        private int screenHeight;
        
        private float screenKDistance;
        /// <summary>
        /// Set distance of kinect to screen, useful only if status is NoneSetted
        /// </summary>
        /// <param name="depth">Distance of kinect to screen</param>
        public void SetDepth(int depth)
        {
            screenKDistance = depth / 1000.0f;
        }

        /// <summary>
        /// Get the point on screen from the hand and elbow vector
        /// </summary>
        /// <param name="hand">Hand position in skeleton frame</param>
        /// <param name="elbow">Elbow position in skeleton frame</param>
        /// <returns>Position of screen in skeleton frame</returns>
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
                Vector3f handVector = new Vector3f(hand.X - elbow.X,
                    hand.Y - elbow.Y, hand.Z - elbow.Z);
                float ratio = (screenKDistance - elbow.Z) /
                    (hand.Z - elbow.Z);
                result.X = (hand.X - elbow.X) * ratio + elbow.X;
                result.Y = (hand.Y - elbow.Y) * ratio + elbow.Y;
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
        public int ScreenSetted()
        {
            return screenSetted;
        }
        /// <summary>
        /// Set screen position according to current status.
        /// Order: TopLeft, TopRight, BottomLeft, BottomRight
        /// </summary>
        /// <param name="hand">Position of hand in depth frame</param>
        /// <param name="elbow">Position of elbow in depth frame</param>
        /// <return>If screen positions are all set</return>
        public bool SetScreenPosition(Vector3f hand, Vector3f elbow)
        {
            if (screenSetted < SCREEN_EDGE_CNT - 1)
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
            //if (screenSetted < SCREEN_EDGE_CNT)
            //{
            //    screenEdge[screenSetted] = ArmToScreen(hand, elbow);
            //    ++screenSetted;
            //    if (screenSetted < SCREEN_EDGE_CNT - 1)
            //    {
            //        return false;
            //    }
            //    else
            //    {
            //        float[] x = new float[SCREEN_EDGE_CNT];
            //        float[] y = new float[SCREEN_EDGE_CNT];
            //        for (int i = 0; i < SCREEN_EDGE_CNT; ++i)
            //        {
            //            x[i] = screenEdge[i].X;
            //            y[i] = screenEdge[i].Y;
            //        }
            //        Array.Sort(x);
            //        Array.Sort(y);

            //        // use the avg of the two smaller value to set for bottom and left
            //        screenLeft = (x[0] + x[1]) / 2.0f;
            //        screenBottom = (y[0] + y[1]) / 2.0f;
            //        // use the avg of the two larger value to set for top and right
            //        screenRight = (x[2] + x[3]) / 2.0f;
            //        screenTop = (y[2] + y[3]) / 2.0f;
            //        return true;
            //    }
            //}
            //else
            //{
            //    return true;
            //}
        }

        /// <summary>
        /// Check if one hand is pointing to screen
        /// </summary>
        /// <param name="hand">Hand position in depth frame</param>
        /// <param name="elbow">Elbow position in depth frame</param>
        /// <param name="xPos">Pointed x position on screen</param>
        /// <param name="yPos">Pointed y position on screen</param>
        /// <returns>If is pointed to screen</returns>
        public bool PointScreenPosition(Vector3f hand, Vector3f elbow, out int xPos, out int yPos)
        {
            if (hand.Z < elbow.Z)
            {
                xPos = -1;
                yPos = -1;
                return false;
            }
            Vector2f position = ArmToScreen(hand, elbow);
            xPos = (int)((position.X - screenLeft) / (screenRight - screenLeft) * screenWidth);
            xPos = screenWidth - xPos;
            yPos = (int)((screenTop - position.Y) / (screenTop - screenBottom) * screenHeight);
            if (xPos < 0 || xPos > screenWidth || yPos < 0 || yPos > screenHeight)
            {
                return false;
            }
            else
            {
                return true;
            }
        }


        public void tmp()
        {
            screenSetted = 4;
            screenLeft = 0.94726f;
            screenRight = 3.63516f;
            screenTop = 3.610335f;
            screenBottom = -2.0195f;
        }
    }
}
