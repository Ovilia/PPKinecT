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
            status = CalStatus.NoneSetted;

            screenKDistance = 0;
        }

        public enum CalStatus
        {
            NoneSetted,
            DepthSetted,
            TopLeftSetted,
            TopRightSetted,
            BottomLeftSetted,
            BottomRightSetted
        };
        private CalStatus status;
        public CalStatus Status
        {
            get
            {
                return status;
            }
        }

        private int screenKDistance;
        /// <summary>
        /// Set distance of kinect to screen, useful only if status is NoneSetted
        /// </summary>
        /// <param name="depth">Distance of kinect to screen</param>
        public void SetDepth(int depth)
        {
            if (status == CalStatus.NoneSetted)
            {
                screenKDistance = depth;
                status = CalStatus.DepthSetted;
            }
        }
    }
}
