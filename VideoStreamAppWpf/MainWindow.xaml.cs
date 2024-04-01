using Emgu;
using Emgu.CV;
using Emgu.CV.Util;
using Emgu.CV.Structure;
using Emgu.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Drawing;
using System.Drawing.Imaging;
using AForge.Video.DirectShow;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Linq.Expressions;

namespace VideoStreamAppWpf
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private void UpdateDeviceList()
        {
            this._videoDeviceList.Items.Clear();
            var list = VideoDevice.DeviceInfo.GetList();
            foreach (var item in list) this._videoDeviceList.Items.Add(item.Name);
        }

        private UdpListener _udpListener;
        private UdpSender _udpSender;

        public MainWindow()
        {
            InitializeComponent();
            this.Closed += MainWindow_Closed;

            UpdateDeviceList();

            this._openVideoDeviceButton.Click += _openVideoDeviceButton_Click;
            this._connectionButton.Click += _connectionButton_Click;
            this._refreshVideoDeviceButton.Click += _refreshVideoDeviceButton_Click;
        }

        private void _udpListener_DataReceivedEvent(byte[] data)
        {
            try
            {
                Dispatcher.Invoke(() =>
                {
                    using (var ms = new MemoryStream(data))
                    {
                        BitmapImage bmp = new BitmapImage();
                        bmp.BeginInit();
                        bmp.CacheOption = BitmapCacheOption.OnLoad;
                        bmp.StreamSource = ms;
                        bmp.EndInit();

                        this._inputWebCameraImage.Source = bmp;
                    }
                });
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
           
        }

        private void _refreshVideoDeviceButton_Click(object sender, RoutedEventArgs e)
        {
            UpdateDeviceList();
        }

        private void _connectionButton_Click(object sender, RoutedEventArgs e)
        {
            if (_udpListener != null) _udpListener = null;
            if (_udpSender != null) _udpSender = null;

            _udpSender = new UdpSender(IPAddress.Parse(this._destinationIp.Text), int.Parse(this._destinationPort.Text));

            _udpListener = new UdpListener(int.Parse(this._inputUdpPort.Text));
            _udpListener.DataReceivedEvent += _udpListener_DataReceivedEvent;
        }

        private void MainWindow_Closed(object sender, EventArgs e)
        {
            _udpSender = null;
            _udpListener = null;
            if (_videoCaptureDevice.IsRunning) _videoCaptureDevice.Stop();
        }

        private VideoCaptureDevice _videoCaptureDevice = null;


        private void _openVideoDeviceButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedItem = this._videoDeviceList.SelectedItem;

            var deviceList = VideoDevice.DeviceInfo.GetList();
            string descriptor = "";
            foreach (var item in deviceList) if (item.Name == selectedItem.ToString()) descriptor = item.Descriptor;

            if (descriptor != "")
            {
                if (_videoCaptureDevice == null)
                {
                    _videoCaptureDevice = new VideoCaptureDevice(descriptor);
                    _videoCaptureDevice.VideoSourceError += _videoCaptureDevice_VideoSourceError;
                    _videoCaptureDevice.NewFrame += _videoCaptureDevice_NewFrame;
                    _videoCaptureDevice.Start();
                }
                else
                {
                    if (_videoCaptureDevice.IsRunning) _videoCaptureDevice.Stop();
                    _videoCaptureDevice.Source = descriptor;
                    _videoCaptureDevice.Start();
                }
            }
        }

        private void _videoCaptureDevice_VideoSourceError(object sender, AForge.Video.VideoSourceErrorEventArgs eventArgs)
        {
            Console.WriteLine(eventArgs.Description);
        }

        private void _videoCaptureDevice_NewFrame(object sender, AForge.Video.NewFrameEventArgs eventArgs)
        {
            try
            {
                Dispatcher.Invoke(() => 
                {
                    using (var memStream = new MemoryStream())
                    {
                        var frame = eventArgs.Frame;
                        frame.Save(memStream, ImageFormat.Jpeg);
                        memStream.Seek(0, SeekOrigin.Begin);


                        int bps = frame.Width * frame.Height * System.Drawing.Image.GetPixelFormatSize(frame.PixelFormat) * frame.GetFrameCount(FrameDimension.Resolution);
                        

                        BitmapImage bmp = new BitmapImage();
                        bmp.BeginInit();
                        bmp.CacheOption = BitmapCacheOption.OnLoad;
                        bmp.StreamSource = memStream;
                        bmp.EndInit();

                        this._outputWebCameraImage.Source = bmp;
                        
                        if (_udpSender != null)
                        {
                            using (var zipMemStream = new MemoryStream())
                            {
                                Bitmap outputBmp = new Bitmap(eventArgs.Frame, 800, 600);
                                var data = zipMemStream.ToArray();
                                _udpSender.Send(data);
                            }
                        }
                      
                    }
                });
               
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }


        }
    }
}
