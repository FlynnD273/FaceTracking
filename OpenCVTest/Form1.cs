﻿using DlibDotNet;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using Drawing = System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using OpenCvSharp.XImgProc;
using DlibDotNet.ImageDatasetMetadata;

namespace OpenCVTest
{
    public partial class Form1 : Form
    {
        private Mat img;
        private Bitmap eyeImage;
        private Bitmap mustacheImage;

        private FrontalFaceDetector detector;
        private ShapePredictor predictor;
        private DlibDotNet.Rectangle[] faces;
        private List<FullObjectDetection> shapes;
        private VideoCapture capture;
        public Form1()
        {
            InitializeComponent();
            DoubleBuffered = true;

            shapes = new List<FullObjectDetection>();

            img = new Mat();
            eyeImage = new Bitmap(Properties.Resources.Star);
            mustacheImage = new Bitmap(Properties.Resources.Mustache);

            detector = Dlib.GetFrontalFaceDetector();
            predictor = ShapePredictor.Deserialize("Resources\\shape_predictor_68_face_landmarks.dat");

            capture = new VideoCapture();
            capture.Open(0);
            Application.Idle += OnCameraFrame;
        }

        private void OnCameraFrame (object sender, EventArgs e)
        {
            capture.Read(img);
            Cv2.Flip(img, img, FlipMode.Y);

            var array = new byte[img.Width * img.Height * img.ElemSize()];
            Marshal.Copy(img.Data, array, 0, array.Length);
            var i = img.ElemSize();
            var image = Dlib.LoadImageData<BgrPixel>(array, (uint)img.Height, (uint)img.Width, (uint)(img.Width * img.ElemSize()));
            faces = detector.Operator(image);

            shapes.Clear();
            foreach (var rect in faces)
            {
                DlibDotNet.Rectangle face = rect;
                shapes.Add(predictor.Detect(image, face));
            }


            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            if (img.Width == 0)
                return;

            Bitmap screen = img.ToBitmap();
            Graphics g = Graphics.FromImage(screen);
            Pen p = new Pen(Color.Black, 5);

            for (int i = 0; i < shapes.Count; i++)
            {
                p = new Pen(Brushes.White, 1);
                var shape = shapes[i];
                //for (var j = 0; j < shape.Parts; j++)
                //{
                //    DlibDotNet.Point point = shape.GetPart((uint)j);

                //    g.DrawEllipse(p, point.X, point.Y, 7, 7);
                //}

                Drawing.Point[] faceRotationLandmarks = (from index in new int[] { 30, 8, 36, 45, 48, 54 }
                                          let pt = shape.GetPart((uint)index)
                                          select new Drawing.Point(pt.X, pt.Y)).ToArray();

                Drawing.Point[] landmarks = (from index in new int[] { 48, 54, 36, 39, 42, 45, 51, 33 }
                                          let pt = shape.GetPart((uint)index)
                                          select new Drawing.Point(pt.X, pt.Y)).ToArray();

                p = new Pen(Brushes.Red, 3);
                foreach (Drawing.Point point in landmarks)
                {
                    //g.DrawEllipse(p, point.X, point.Y, 10, 10);
                }

                //ImageBetweenPoints(g, eyeImage, landmarks[2], landmarks[3]);
                //ImageBetweenPoints(g, eyeImage, landmarks[4], landmarks[5]);
                int d = (int)Dist(landmarks[7], landmarks[6]) * 4;
                Bitmap bmp = RotateImage(new Bitmap(mustacheImage, new Drawing.Size(d * 2, d)), (float)Angle(landmarks[6], landmarks[7]));
                g.DrawImage(bmp, new Drawing.Point((landmarks[0].X + landmarks[1].X) / 2 - bmp.Width / 2, (landmarks[6].Y + landmarks[7].Y) / 2 - bmp.Height / 2));
                //ImageBetweenPoints(g, , (float)Angle(landmarks[6], landmarks[7])), landmarks[0], landmarks[1], 0, (landmarks[7].Y - landmarks[6].Y) / 2);
            }


            Drawing.Size winSize = e.Graphics.VisibleClipBounds.Size.ToSize();
            Drawing.Size s = screen.Size;
            Drawing.Size imgSize;
            double ratio = (double)s.Width / s.Height;
            if ((double)winSize.Width / s.Width < (double)winSize.Height / s.Height)
            {
                imgSize = new Drawing.Size(winSize.Width, (int)(winSize.Width / ratio));
            }
            else
            {
                imgSize = new Drawing.Size((int)(winSize.Height * ratio), winSize.Height);
            }

            e.Graphics.DrawImage(screen, new Drawing.Rectangle(new Drawing.Point(0, 0), imgSize));
        }

        private double Angle(Drawing.Point start, Drawing.Point end)
        {
            return ((Math.PI / 2) - Math.Atan2(start.Y - end.Y, end.X - start.X)) * 180 / Math.PI;
        }

        private Bitmap RotateImage(Bitmap b, float angle)
        {
            //create a new empty bitmap to hold rotated image
            double scale = Dist(new Drawing.Point(0, 0), new Drawing.Point(b.Width, b.Height));
            Bitmap returnBitmap = new Bitmap((int)(scale), (int)(scale));
            //make a graphics object from the empty bitmap
            using (Graphics g = Graphics.FromImage(returnBitmap))
            {
                //move rotation point to center of image
                g.TranslateTransform((float)returnBitmap.Width / 2, (float)returnBitmap.Height / 2);
                //rotate
                g.RotateTransform(angle);
                //move image back
                g.TranslateTransform(-(float)returnBitmap.Width / 2, -(float)returnBitmap.Height / 2);
                //draw passed in image onto graphics object
                g.DrawImage(b, new Drawing.Point((returnBitmap.Width - b.Width) / 2, (returnBitmap.Height - b.Height) / 2));
            }
            return returnBitmap;
        }

        private void ImageBetweenPoints(Graphics g, Bitmap bmp, Drawing.Point left, Drawing.Point right, int xOff = 0, int yOff = 0)
        {
            //int d = (int)Dist(left, right);
            g.DrawImage(bmp, new Drawing.Point((left.X + right.X) / 2 - bmp.Width / 2 + xOff, (left.Y + right.Y) / 2 - bmp.Height / 2 + yOff));
        }

        private double Dist (Drawing.Point a, Drawing.Point b)
        {
            return Math.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y));
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            Invalidate();
        }
    }
}
