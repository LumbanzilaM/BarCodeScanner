using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using ZXing;

namespace WpfApp1
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        private const string NotDetected = "No Bar code detected";
        private const string ZXingLibName = "ZXing";
        private const string IronLibName = "Iron";
        private const string ErrorPath = "Wrong Path";
        private DispatcherTimer timer;
        private VideoCapture capture;
        private BitmapSource currentFrame;
        private string barCodeText;
        private string libActive;
        private bool isVideoMode;
        private string imagePath;
        private bool isImageMode;
        private bool isDebugMode;
        private bool isDetectorActive;
        private System.Windows.Media.Brush stateColor;

        public MainWindowViewModel()
        {
            ZXingButtonCommand = new RelayCommand(ZXingScanBarCode);
            IronButtonCommand = new RelayCommand(IronScanBarCode);
            ScanButtonCommand = new RelayCommand(ScanImage);
            LibActive = ZXingLibName;
            ImagePath = @"C:\Users\NyrOne\Pictures\CodeBar1.png";
            IsVideoMode = false;
            StateColor = System.Windows.Media.Brushes.Red;
            IsDetectorActive = true;
            StartVideo();
        }


        public event PropertyChangedEventHandler PropertyChanged;

        public ICommand ZXingButtonCommand { get; set; }
        public ICommand IronButtonCommand { get; set; }
        public ICommand ScanButtonCommand { get; set; }

        public string LibActive
        {
            get { return libActive; }
            set
            {
                libActive = value;
                NotifyPropertyChanged();
            }
        }

        public string ImagePath
        {
            get { return imagePath; }
            set
            {
                imagePath = value;
                NotifyPropertyChanged();
            }
        }

        public System.Windows.Media.Brush StateColor
        {
            get { return stateColor;  }
            set
            {
                stateColor = value;
                NotifyPropertyChanged();
            }
        }

        public BitmapSource CurrentFrame
        {
            get { return currentFrame; }
            set
            {
                if (currentFrame != value)
                {
                    currentFrame = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public bool IsDetectorActive
        {
            get { return isDetectorActive; }
            set
            {
                isDetectorActive = value;
                NotifyPropertyChanged();
            }
        }


        public bool IsVideoMode
        {
            get { return isVideoMode; }
            set
            {
                if (!value)
                {
                    CurrentFrame = null;
                }
                isVideoMode = value;
                IsImageMode = !value;
                NotifyPropertyChanged();
            }
        }

        public bool IsDebugMode
        {
            get { return isDebugMode; }
            set
            {
                isDebugMode = value;
                NotifyPropertyChanged();
            }
        }

        public bool IsImageMode
        {
            get { return isImageMode; }
            set
            {
                isImageMode = value;
                NotifyPropertyChanged();
            }
        }

        public string BarCodeText
        {
            get { return barCodeText; }
            set
            {
                barCodeText = value;
                NotifyPropertyChanged();
            }
        }


        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void StartVideo()
        {
            capture = new VideoCapture();
            capture.SetCaptureProperty(Emgu.CV.CvEnum.CapProp.XiWidth, 1920);
            capture.SetCaptureProperty(Emgu.CV.CvEnum.CapProp.XiHeight, 1080);
            timer = new DispatcherTimer();
            //framerate of 10fps
            // timer.Interval = TimeSpan.FromMilliseconds(100);
            timer.Tick += new EventHandler(async (object s, EventArgs a) =>
            {
                if (isVideoMode)
                {


                    //draw the image obtained from camera
                    using (Mat frame = capture.QueryFrame())
                    {
                        if (frame != null)
                        {
                            CurrentFrame = BitmapSourceConvert.MatToBitmapSource(frame);
                            ScanVideo();
                        }
                    }
                }
            });
            timer.Start();
        }

        private void ScanImage(object obj)
        {
            try
            {
                Mat show = new Mat(ImagePath);
                Scan(show);
            }
            catch(ArgumentException e)
            {
                BarCodeText = ErrorPath;
                StateColor = System.Windows.Media.Brushes.Red;
            }

        }

        private void ZXingScanBarCode(object obj)
        {
            LibActive = ZXingLibName;
        }

        private void IronScanBarCode(object obj)
        {
            LibActive = IronLibName;
        }

        private void ScanVideo()
        {
            Mat show = capture.QueryFrame();
            Scan(show);
        }

        private void Scan(Mat source)
        {
            Mat crop = source;
            if (isDetectorActive)
             crop = PreProcessImage(source);
            CurrentFrame = BitmapSourceConvert.MatToBitmapSource(source);
            ShowImage("Crop", crop);

            if (LibActive == ZXingLibName)
            {
                IBarcodeReader reader = new ZXing.BarcodeReader();
                var stream = new MemoryStream();
                crop.Bitmap.Save(stream, ImageFormat.Jpeg);
                Bitmap barcodeBitmap = (Bitmap)Image.FromStream(stream);
                var result = reader.Decode(barcodeBitmap);
                BarCodeText = result == null ? NotDetected : result.Text;
                StateColor = result == null ? System.Windows.Media.Brushes.Red : System.Windows.Media.Brushes.Green;
            }
            else
            {
                //var PhotoResult = IronBarCode.BarcodeReader.ReadASingleBarcode(img.Bitmap, BarcodeEncoding.Code39, IronBarCode.BarcodeReader.BarcodeRotationCorrection.Medium, IronBarCode.BarcodeReader.BarcodeImageCorrection.DeepCleanPixels);
                var PhotoResult = IronBarCode.BarcodeReader.QuicklyReadOneBarcode(crop.Bitmap);
                BarCodeText = PhotoResult == null ? NotDetected : PhotoResult.Value;
                StateColor = PhotoResult == null ? System.Windows.Media.Brushes.Red : System.Windows.Media.Brushes.Green;
            }
        }

        private Mat PreProcessImage(Mat image)
        {
            ShowImage("Image", image);
            Mat tmpImg = new Mat();
            Mat sob1 = new Mat();
            Mat sob2 = new Mat();
            CvInvoke.CvtColor(image, tmpImg, Emgu.CV.CvEnum.ColorConversion.Bgra2Gray);
            CvInvoke.Sobel(tmpImg, sob1, Emgu.CV.CvEnum.DepthType.Cv8U, 1, 0, 1);
            CvInvoke.Sobel(tmpImg, sob2, Emgu.CV.CvEnum.DepthType.Cv8U, 0, 1, 1);
            CvInvoke.Subtract(sob1, sob2, tmpImg);
            ShowImage("AfterSobel", tmpImg);
            CvInvoke.ConvertScaleAbs(tmpImg, tmpImg, 1, 0);
            CvInvoke.Blur(tmpImg, tmpImg, new Size(9, 9), new Point(0, 0));
            CvInvoke.Threshold(tmpImg, tmpImg, 45, 255, Emgu.CV.CvEnum.ThresholdType.Binary);
            //CvInvoke.AdaptiveThreshold(tmpImg, tmpImg, 255, Emgu.CV.CvEnum.AdaptiveThresholdType.MeanC, Emgu.CV.CvEnum.ThresholdType.Binary, 3, 10);
            //CvInvoke.BitwiseNot(tmpImg, tmpImg);
            ShowImage("AfterThresh", tmpImg);
            var kernel = CvInvoke.GetStructuringElement(Emgu.CV.CvEnum.ElementShape.Rectangle, new Size(21, 7), new Point(0, 0));
            CvInvoke.MorphologyEx(tmpImg, tmpImg, Emgu.CV.CvEnum.MorphOp.Close, kernel, new Point(0, 0), 1, Emgu.CV.CvEnum.BorderType.Default, new MCvScalar(255));
            ShowImage("After Morphology", tmpImg);
            CvInvoke.Erode(tmpImg, tmpImg, null, new Point(0, 0), 7, Emgu.CV.CvEnum.BorderType.Default, new MCvScalar(255));
            CvInvoke.Dilate(tmpImg, tmpImg, null, new Point(0, 0), 7, Emgu.CV.CvEnum.BorderType.Default, new MCvScalar(255));
            Mat cts = new Mat(tmpImg.Size, Emgu.CV.CvEnum.DepthType.Cv8U, 1);
            ShowImage("After Erode/Dilate", tmpImg);
            VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint();
            CvInvoke.FindContours(tmpImg, contours, null, Emgu.CV.CvEnum.RetrType.External, Emgu.CV.CvEnum.ChainApproxMethod.ChainApproxSimple);
            DrawContours(contours, cts);
            ShowImage("contours", cts);
            Rectangle rect = FindGlobalContour(contours, tmpImg);
            CvInvoke.Rectangle(image, rect, new MCvScalar(255), 3);
            return new Mat(image, rect);
        }

        private VectorOfPoint FindBigestContour(VectorOfVectorOfPoint contours)
        {
            int biggest = 0;
            for (int i = 0; i < contours.Size; i++)
            {
                if (contours[i].Size > contours[biggest].Size)
                    biggest = i;
            }
            return contours[biggest];
        }

        private Rectangle FindGlobalContour(VectorOfVectorOfPoint contours, Mat img, float extraMarginRatio = 0.05f)
        {
            int minX = img.Size.Width;
            int minY = img.Size.Height;
            int maxX = 0;
            int maxY = 0;
            for (int i = 0; i < contours.Size; i++)
            {
                var lol = contours[i];
                for (int j = 0; j < contours[i].Size; j++)
                {
                    if (contours[i].Size > 8)
                    {
                        minX = minX > contours[i][j].X ? contours[i][j].X : minX;
                        maxX = maxX < contours[i][j].X ? contours[i][j].X : maxX;
                        minY = minY > contours[i][j].Y ? contours[i][j].Y : minY;
                        maxY = maxY < contours[i][j].Y ? contours[i][j].Y : maxY;
                    }
                }
            }
            if (maxX == 0 || maxY == 0)
            {
                return new Rectangle(new Point(0, 0), img.Size);
            }

            int extraPx = (int)((maxX - minX) * extraMarginRatio);

            minX = minX - extraPx < 0 ? 0 : minX - extraPx;
            minY = minY - extraPx < 0 ? 0 : minY - extraPx;

            return new Rectangle(
                minX,
                minY,
                ((maxX - minX) + extraPx) < (img.Size.Width - minX) ? (maxX - minX) + extraPx : (img.Size.Width - minX),
                ((maxY - minY) + extraPx) < (img.Size.Height - minY) ? (maxY - minY) + extraPx : (img.Size.Height - minY));
        }

        private void DrawContours(VectorOfVectorOfPoint contours, Mat img)
        {
            for (int i = 0; i < contours.Size; i++)
            {
                CvInvoke.DrawContours(img, contours, i, new MCvScalar(255));
                CvInvoke.PutText(img, i.ToString(), contours[i][0], Emgu.CV.CvEnum.FontFace.HersheyComplex, 1, new MCvScalar(255));
                CvInvoke.PutText(img, string.Format("({0}:{1})", contours[i][0].X, contours[i][0].Y), contours[i][contours[i].Size - 1], Emgu.CV.CvEnum.FontFace.HersheyComplex, 1, new MCvScalar(255));
            }
        }

        private void ShowImage(string windowName, IImage image)
        {
            if(IsDebugMode)
            {
                CvInvoke.Imshow(windowName, image);
            }
        }
    }
}
