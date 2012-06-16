using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

#if WITH_KINECT
using Microsoft.Kinect;
using System.Windows.Threading;
#endif

namespace PPKinecT
{
    class Action
    {
        public enum ActionType
        {
            NONE,
            LEFT,
            RIGHT,
            UP,
            DOWN,
            LEFT_UP,
            LEFT_DOWN,
            RIGHT_UP,
            RIGHT_DOWN
        };

#if WITH_KINECT
        /// <summary>
        /// Get most matched action with given joint queue.
        /// Calculating match by multiplying similarity and count of points checked.
        /// </summary>
        /// <param name="joint">Array of joint positions, smaller index means former position</param>
        /// <param name="leaseCnt">Least amount of positions to be checked</param>
        /// <param name="similarity">Similarity of matched action</param>
        /// <returns>Most matched action type</returns>
        public static ActionType MatchedAction(Joint[] joint, int leaseCnt, out float similarity)
        {
            float maxSimi = 0.0f;
            ActionType maxType = ActionType.NONE;
            for (int i = leaseCnt; i < joint.Length; ++i)
            {
                for (int j = 0; j < 8; ++j)
                {
                    ActionType type = (ActionType)j;
                    float simi = Similarity(joint, i, type);
                    if (simi > maxSimi)
                    {
                        maxSimi = simi;
                        maxType = type;
                    }
                }
            }
            similarity = maxSimi;
            if (maxSimi > 0.25f)
            {
                return maxType;
            }
            else
            {
                return ActionType.NONE;
            }
        }

        /// <summary>
        /// Similarity of given joint positions and action type
        /// </summary>
        /// <param name="joint">Array of joint positions, smaller index means former position</param>
        /// <param name="lastCnt">Check for the last certain positions</param>
        /// <param name="type">ActionType to be checked</param>
        /// <returns>Similarity from 0 to 1</returns>
        private static float Similarity(Joint[] joint, int lastCnt, ActionType type)
        {
            if (type == ActionType.NONE)
            {
                return 0.0f;
            }

            int start = joint.Length - lastCnt;

            // find min and max x and y of joints
            float minX = joint[start].Position.X;
            float maxX = minX;
            float minY = joint[start].Position.Y;
            float maxY = minY;
            int increaseXCnt = 0;
            int increaseYCnt = 0;
            for (int i = start + 1; i < joint.Length; ++i)
            {
                float x = joint[i].Position.X;
                if (x > maxX) 
                {
                    maxX = x;
                } 
                else if (x < minX) 
                {
                    minX = x;
                }
                if (x > joint[i - 1].Position.X)
                {
                    ++increaseXCnt;
                }

                float y = joint[i].Position.Y;
                if (y > maxY) 
                {
                    maxY = y;
                } 
                else if (y < minY)
                {
                    minY = y;
                }
                if (y > joint[i - 1].Position.Y)
                {
                    ++increaseYCnt;
                }
            }

            float xDelta = maxX - minX;
            float yDelta = maxY - minY;
            if (yDelta == 0.0) {
                if (type == ActionType.LEFT)
                {
                    return (float)(joint.Length - increaseXCnt) / joint.Length;
                }
                else if (type == ActionType.RIGHT)
                {
                    return (float)increaseXCnt / joint.Length;
                }
                else
                {
                    return 0.0f;
                }
            }
            float ratio = xDelta / yDelta;
            // increase test
            float increaseX = (float)increaseXCnt / joint.Length;
            float increaseY = (float)increaseYCnt / joint.Length;

            if (type == ActionType.LEFT || type == ActionType.RIGHT)
            {
                if (ratio < 0.5f)
                {
                    return 0.0f;
                }
                else if (ratio < 1.0f)
                {
                    return 0.25f * increaseX;
                }
                else if (ratio < 2.0f)
                {
                    return 0.5f * increaseX;
                }
                else
                {
                    return 1.0f * increaseX;
                }
            }
            else if (type == ActionType.UP || type == ActionType.DOWN)
            {
                if (ratio < 0.5f)
                {
                    return 1.0f * increaseY;
                }
                else if (ratio < 1.0f)
                {
                    return 0.5f * increaseY;
                }
                else if (ratio < 2.0f)
                {
                    return 0.25f * increaseY;
                }
                else
                {
                    return 0.0f;
                }
            }
            else
            {
                // test bounding box
                float ratioTest = 0.0f;
                if (ratio > 2.0f || ratio < 0.5f)
                {
                    return 0.0f;
                }
                else if (ratio > 1.5f)
                {
                    ratioTest = 0.5f;
                }
                else if (ratio < 0.75f)
                {
                    ratioTest = 0.5f;
                }
                else
                {
                    ratioTest = 1.0f;
                }
                
                // test increase
                if (type == ActionType.RIGHT_UP)
                {
                    return ratioTest * increaseX * increaseY;
                }
                else if (type == ActionType.RIGHT_DOWN)
                {
                    return ratioTest * increaseX * (1.0f - increaseY);
                }
                else if (type == ActionType.LEFT_UP)
                {
                    return ratioTest * (1.0f - increaseX) * increaseY;
                }
                else
                {
                    return ratioTest * (1.0f - increaseX) * (1.0f - increaseY);
                }
            }
        }
#endif
    }
}
