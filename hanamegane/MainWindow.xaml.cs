using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Research.Kinect.Nui;
using System.Threading;
using System.Windows.Threading;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using System.Diagnostics;

namespace hanamegane
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        private Thread readerThread;
        private bool shouldRun;

        public MainWindow()
        {
            try {
                InitializeComponent();

                Runtime kinect = Runtime.Kinects[0];
                kinect.Initialize( RuntimeOptions.UseColor | RuntimeOptions.UseDepthAndPlayerIndex |
                                RuntimeOptions.UseSkeletalTracking );
                kinect.VideoStream.Open( ImageStreamType.Video, 2,
                    ImageResolution.Resolution640x480, ImageType.Color );
                kinect.DepthStream.Open( ImageStreamType.Depth, 2,
                    ImageResolution.Resolution320x240, ImageType.DepthAndPlayerIndex );

                shouldRun = true;
                readerThread = new Thread( new ThreadStart( RenderThread ) );
                readerThread.Start();
            }
            catch ( Exception ex ) {
                MessageBox.Show( ex.Message );
            }
        }

        // ユーザーにつける色
        static Color[] userColor= new Color[] {
            Color.FromRgb( 0, 0, 0 ),   // ユーザーなし
            Colors.Red,
            Colors.Green,
            Colors.Blue,
            Colors.Yellow,
            Colors.Magenta,
            Colors.Pink
        };

        CvHaarClassifierCascade cascade = CvHaarClassifierCascade.FromFile( "../../haarcascade_eye.xml" );

        void RenderThread()
        {
            while ( shouldRun ) {
                Runtime kinect = Runtime.Kinects[0];
                var video = kinect.VideoStream.GetNextFrame( 100 );
                var depth = kinect.DepthStream.GetNextFrame( 100 );
                var skeleton = kinect.SkeletonEngine.GetNextFrame( 100 );
                if ( video == null || depth == null || skeleton == null ) {
                    continue;
                }

                this.Dispatcher.BeginInvoke( DispatcherPriority.Background, new Action( () =>
                {
                    try {
                        //bool isHanamegane = false;

                        DrawingVisual drawingVisual = new DrawingVisual();
                        using ( DrawingContext drawingContext = drawingVisual.RenderOpen() ) {
                            var b = DrawPixels( kinect, video, depth );
                            var img = b.ToIplImage();

                            foreach ( var s in skeleton.Skeletons ) {
                                if ( s.TrackingState == SkeletonTrackingState.Tracked ) {
                                    //IplImage gray = new IplImage( new CvSize( (int)b.Width, (int)b.Height ), BitDepth.U8, 1 );
                                    //Cv.CvtColor( img, gray, ColorConversion.BgraToGray );

                                    //var storage = Cv.CreateMemStorage( 0 );
                                    //Cv.ClearMemStorage( storage );
                                    //Cv.EqualizeHist( gray, gray );

                                    //Cv.SetImageROI( gray, new CvRect( 0, 0, 100, 100 ) );
                                    //var faces = Cv.HaarDetectObjects( gray, cascade, storage, 1.11, 4, 0 );
                                    //Cv.ResetImageROI( gray );

                                    //foreach ( var face in faces ) {
                                    //    CvRect r = face.Value.Rect;
                                    //    CvPoint center = new CvPoint
                                    //    {

                                    //        X = Cv.Round( r.X + r.Width * 0.5 ),
                                    //        Y = Cv.Round( r.Y + r.Height * 0.5 )
                                    //    };
                                    //    int radius = Cv.Round( (r.Width + r.Height) * 0.25 );
                                    //    Cv.Circle( img, center, radius, new CvScalar( 2550, 0, 0 ), 3, LineType.Link8 );
                                    //}



                                    // ﾊﾅﾒｶﾞﾈの位置あわせ
                                    var point = GetVideoPoint( s.Joints[JointID.Head] );
                                    var x = image2.Width / 2;
                                    var y = image2.Height / 2;

                                    image2.Margin = new Thickness( point.X - x, point.Y - y, 0, 0 );
                                    //isHanamegane = true;
                                }
                            }

                            drawingContext.DrawImage( img.ToWriteableBitmap(), new Rect( 0, 0, video.Image.Width, video.Image.Height ) );
                        }

                        //image2.Visibility = isHanamegane ? System.Windows.Visibility.Visible : System.Windows.Visibility.Hidden;

                        // 描画可能なビットマップを作る
                        // http://stackoverflow.com/questions/831860/generate-bitmapsource-from-uielement
                        RenderTargetBitmap bitmap = new RenderTargetBitmap( video.Image.Width, video.Image.Height,
                            96, 96, PixelFormats.Default );
                        bitmap.Render( drawingVisual );

                        image1.Source = bitmap;
                    }
                    catch ( Exception ex ) {
                        Trace.WriteLine( ex.Message );
                    }
                } ) );
            }
        }

        private WriteableBitmap DrawPixels( Runtime kinect, ImageFrame video, ImageFrame depth )
        {
            //バイト列をビットマップに展開
            //描画可能なビットマップを作る
            //http://msdn.microsoft.com/ja-jp/magazine/cc534995.aspx
            WriteableBitmap bitmap = new WriteableBitmap( video.Image.Width, video.Image.Height, 96, 96,
                                                          PixelFormats.Bgra32, null );

            // RGBAのAが未使用でゼロになっているので、255で透過なしにする
            for ( int i = 0; i < video.Image.Bits.Length; i += 4 ) {
                video.Image.Bits[i + 3] = 255;
            }

            bitmap.WritePixels( new Int32Rect( 0, 0, video.Image.Width, video.Image.Height ), video.Image.Bits,
                                video.Image.Width * video.Image.BytesPerPixel, 0 );

            return bitmap;
        }


        private Point GetVideoPoint( Joint joint )
        {
            Runtime kinect = Runtime.Kinects[0];
            float depthX = 0, depthY = 0;
            kinect.SkeletonEngine.SkeletonToDepthImage( joint.Position, out depthX, out depthY );
            depthX = Math.Min( depthX * kinect.DepthStream.Width, kinect.DepthStream.Width );
            depthY = Math.Min( depthY * kinect.DepthStream.Height, kinect.DepthStream.Height );

            int videoX = 0, videoY = 0;
            kinect.NuiCamera.GetColorPixelCoordinatesFromDepthPixel( ImageResolution.Resolution640x480,
                new ImageViewArea(), (int)depthX, (int)depthY, 0, out videoX, out videoY );

            return new Point( Math.Min( videoX, kinect.VideoStream.Width ), Math.Min( videoY, kinect.VideoStream.Height ) );
        }

        private void Window_Closing( object sender, System.ComponentModel.CancelEventArgs e )
        {
            shouldRun = false;
        }
    }
}
