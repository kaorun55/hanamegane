using System;
using System.Windows;
using Coding4Fun.Kinect.Wpf;
using Microsoft.Kinect;
using OpenCvSharp;

namespace hanamegane
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        KinectSensor kinect;

        CvHaarClassifierCascade cascade = CvHaarClassifierCascade.FromFile( "../../haarcascade_eye.xml" );


        public MainWindow()
        {
            try {
                if ( KinectSensor.KinectSensors.Count == 0 ) {
                    throw new Exception( "Kinectが接続されていません" );
                }

                InitializeComponent();

                kinect = KinectSensor.KinectSensors[0];
                kinect.ColorStream.Enable();
                kinect.SkeletonStream.Enable();
                kinect.AllFramesReady += new EventHandler<AllFramesReadyEventArgs>( kinect_AllFramesReady );

                kinect.Start();
            }
            catch ( Exception ex ) {
                MessageBox.Show( ex.Message );
            }
        }

        void kinect_AllFramesReady( object sender, AllFramesReadyEventArgs e )
        {
            image1.Source = e.OpenColorImageFrame().ToBitmapSource();

            // スケルトンフレームを取得する
            SkeletonFrame skeletonFrame = e.OpenSkeletonFrame();
            if ( skeletonFrame != null ) {
                // スケルトンデータを取得する
                Skeleton[] skeletonData = new Skeleton[skeletonFrame.SkeletonArrayLength];
                skeletonFrame.CopySkeletonDataTo( skeletonData );

                // プレーヤーごとのスケルトンを描画する
                foreach ( var skeleton in skeletonData ) {
                    var head = skeleton.Joints[JointType.Head];
                    if ( head.TrackingState == JointTrackingState.Tracked ) {
                        ColorImagePoint point = kinect.MapSkeletonPointToColor( head.Position, kinect.ColorStream.Format );
                        var x = image2.Width / 2;
                        var y = image2.Height / 2;

                        image2.Margin = new Thickness( point.X - x, point.Y - y, 0, 0 );
                        image2.Visibility = System.Windows.Visibility.Visible;
                    }
                }
            }
        }

        private void Window_Closing( object sender, System.ComponentModel.CancelEventArgs e )
        {
        }
    }
}
