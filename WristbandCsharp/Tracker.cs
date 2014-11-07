using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Emgu.CV.UI;
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.Util;
using Emgu.CV.Features2D;
using Emgu.CV.Util;
using CMT_Tracker;

namespace WristbandCsharp
{
    class Tracker
    {

        CMT cmtTracker;
        Image<Bgr, Byte> prevFrame;
        Image<Gray, Byte> curFrame;
        PointF[] keyPts;
        int[] classes;
        const float errThreshold = 0.5f;
        PointF[] keypoints_tracked;
        int[] keypoint_classes;
        public Rectangle roi;

        Image<Gray,Byte> frostedFlakes;
        VectorOfKeyPoint frostedFlakesKP;
        Matrix<float> frostedFlakesDescriptors;
        VectorOfKeyPoint observedKP;
        Matrix<float> observedDescriptors;
        BruteForceMatcher<float> matcher;
        Matrix<int> indices;
        Matrix<float> dist;
        Matrix<byte> mask;
        HomographyMatrix homography = null;

        public PointF centerOfObject = PointF.Empty;

        // This boolean specifies whether or not we'll use CMT to simplify tracking. 
        // When using CMT, we use SURF to find the object, and then pass it to CMT to be tracked.
        public Boolean trackWithCMT = true;



        SURFDetector surfDetector;

        public Tracker()
        {
            surfDetector = new SURFDetector(500, false);
            frostedFlakes  = new Image<Gray,byte>("C:\\Users\\hfs5022\\Downloads\\KelloggsFrostedFlakescereal_450.jpg");
            frostedFlakesKP = surfDetector.DetectKeyPointsRaw(frostedFlakes,null);
            frostedFlakesDescriptors = surfDetector.ComputeDescriptorsRaw(frostedFlakes, null, frostedFlakesKP);
            roi = Rectangle.Empty;

            // Preparing matcher
            matcher = new BruteForceMatcher<float>(DistanceType.L2);
            matcher.Add(frostedFlakesDescriptors);

            // I HAD TO CHANGE CMT TO PUBLIC FOR THIS TO WORK.
            cmtTracker = new CMT_Tracker.CMT();
        }

        public Image<Bgr,Byte> process(Image<Bgr,Byte> image)
        {

            Console.WriteLine(cmtTracker.Initialized);

            if (cmtTracker.Initialized == true && trackWithCMT)
            {
                try
                {
                    roi = cmtTracker.ProcessFrame(image, roi);
                    FindCenter();
                    return DrawCenterOfObjectOnImage(image);
                }
                catch (NullReferenceException e)
                {
                    return image;
                }
            }
            else detect(image);

            FindCenter();

            return DrawROIOnImage(image);

        }

        private void FindCenter()
        {
            if (roi == Rectangle.Empty) return;

            centerOfObject = new PointF(
                (roi.Left + roi.Right) / 2,
                (roi.Top + roi.Bottom) / 2
                );
        }

        public void detect(Image<Bgr, Byte> image)
        {
            
            // Detect KP and calculate descriptors...
            observedKP = surfDetector.DetectKeyPointsRaw(image.Convert<Gray,Byte>(), null);
            observedDescriptors = surfDetector.ComputeDescriptorsRaw(image.Convert<Gray, Byte>(), null, observedKP);

            // Matching
            int k = 2;
            indices = new Matrix<int>(observedDescriptors.Rows, k);
            dist = new Matrix<float>(observedDescriptors.Rows, k);
            matcher.KnnMatch(observedDescriptors, indices, dist, k, null);

            //
            mask = new Matrix<byte>(dist.Rows, 1);
            mask.SetValue(255);
            Features2DToolbox.VoteForUniqueness(dist, 0.8, mask);

            int nonZeroCount = CvInvoke.cvCountNonZero(mask);
            if (nonZeroCount >= 4)
            {
                nonZeroCount = Features2DToolbox.VoteForSizeAndOrientation(frostedFlakesKP, observedKP, indices, mask, 1.5, 20);
                if (nonZeroCount >= 4)
                    homography = Features2DToolbox.GetHomographyMatrixFromMatchedFeatures(frostedFlakesKP, observedKP, indices, mask, 3);
            }


            // Get keypoints.
            keyPts = new PointF[frostedFlakesKP.Size];
            classes = new int[frostedFlakesKP.Size];
            for (int i = 0; i < frostedFlakesKP.Size; i++)
            {
                keyPts[i] = frostedFlakesKP[i].Point;
                classes[i] = frostedFlakesKP[i].ClassId;
            }

            prevFrame = image;

            #region Initialize CMT with found data

            // Find ROI
            PointF minXY = new PointF();
            PointF maxXY = new PointF();
            for (int i = 0; i < frostedFlakesKP.Size; i++)
            {
                PointF pt = keyPts[i];
                if (pt.X < minXY.X) minXY.X = pt.X;
                if (pt.Y < minXY.Y) minXY.Y = pt.Y;
                if (pt.X > maxXY.X) maxXY.X = pt.X;
                if (pt.Y > maxXY.Y) maxXY.Y = pt.Y;
            }

            // Convert ROI to rect
            //roi = new Rectangle((int)minXY.X, (int)minXY.Y, (int)(maxXY.X - minXY.X), (int)(maxXY.Y - minXY.Y));

            Console.WriteLine("Position: ({0},{1}) \tWidth: {2}\tHeight: {3}", roi.X, roi.Y, roi.Width, roi.Height);
            
            #endregion

            
            PointF[] projectedPoints = null;
            if (homography != null) {
                Rectangle rect = frostedFlakes.ROI;
                projectedPoints = new PointF[] { 
                   new PointF(rect.Left, rect.Bottom),
                   new PointF(rect.Right, rect.Bottom),
                   new PointF(rect.Right, rect.Top),
                   new PointF(rect.Left, rect.Top)};
                homography.ProjectPoints(projectedPoints);

                roi = new Rectangle((int)(projectedPoints[3].X), (int)projectedPoints[3].Y, (int)(projectedPoints[1].X - projectedPoints[3].X), (int)(projectedPoints[1].Y - projectedPoints[3].Y));

                cmtTracker.Initialize(image, roi);
            }


            

        }

        Image<Bgr, Byte> DrawROIOnImage(Image<Bgr, Byte> image)
        {
            Point[] points = new Point[4];
            points[0] = new Point(roi.X, roi.Y);
            points[1] = new Point(roi.X + roi.Width, roi.Y);
            points[2] = new Point(roi.X, roi.Y + roi.Height);
            points[3] = new Point(roi.X + roi.Width, roi.Y + roi.Height);

            image.DrawPolyline(points, true, new Bgr(100,0,0), 5);
            
            return image;
        }

        Image<Bgr, Byte> DrawCenterOfObjectOnImage(Image<Bgr, Byte> image)
        {
            image.Draw(new CircleF(centerOfObject, 25), new Bgr(100,0,0), 0);

            return image;
        }

    }
}
