using DlibDotNet;
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
            img = capture.RetrieveMat();
            Cv2.Flip(img, img, FlipMode.Y);

            var array = new byte[img.Cols * img.Rows * img.ElemSize()];
            Marshal.Copy(img.Data, array, 0, array.Length);
            var image = Dlib.LoadImageData<RgbPixel>(array, (uint)img.Rows, (uint)img.Cols, (uint)(img.Cols * img.ElemSize()));
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

            if (img.Cols == 0)
                return;

            Bitmap screen = img.ToBitmap();
            Graphics g = Graphics.FromImage(screen);
            Pen p = new Pen(Color.Black, 5);

            for (int i = 0; i < shapes.Count; i++)
            {
                p = new Pen(Brushes.White, 1);
                var shape = shapes[i];

                Drawing.Point[] facePosLandmarks = (from index in new int[] { 30, 8, 36, 45, 48, 54 }
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

                int d = (int)Dist(landmarks[7], landmarks[6]) * 4;
                Bitmap bmp = RotateImage(new Bitmap(mustacheImage, new Drawing.Size(d * 2, d)), (float)Angle(landmarks[6], landmarks[7]));
                g.DrawImage(bmp, new Drawing.Point((landmarks[0].X + landmarks[1].X) / 2 - bmp.Width / 2, (landmarks[6].Y + landmarks[7].Y) / 2 - bmp.Height / 2));
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
            double scale = Dist(new Drawing.Point(0, 0), new Drawing.Point(b.Width, b.Height));
            Bitmap returnBitmap = new Bitmap((int)(scale), (int)(scale));
            using (Graphics g = Graphics.FromImage(returnBitmap))
            {
                g.TranslateTransform((float)returnBitmap.Width / 2, (float)returnBitmap.Height / 2);
                g.RotateTransform(angle);
                g.TranslateTransform(-(float)returnBitmap.Width / 2, -(float)returnBitmap.Height / 2);
                g.DrawImage(b, new Drawing.Point((returnBitmap.Width - b.Width) / 2, (returnBitmap.Height - b.Height) / 2));
            }
            return returnBitmap;
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
