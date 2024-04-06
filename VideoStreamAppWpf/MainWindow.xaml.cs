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
using System.Diagnostics.Tracing;

namespace VideoStreamAppWpf
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        UdpClient udpReceiver;
        async void ReceivedMessageAsync()
        {
            while(true)
            {
                if (udpReceiver != null)
                {
                    var result = await udpReceiver.ReceiveAsync();

                    Dispatcher.Invoke(() =>
                    {
                        using (MemoryStream ms = new MemoryStream(result.Buffer))
                        {
                            if (ms.Length != 0)
                            {
                                BitmapImage image = new BitmapImage();
                                image.BeginInit();
                                image.StreamSource = ms;
                                image.CacheOption = BitmapCacheOption.OnLoad;
                                image.EndInit();

                                this._inputWebCameraImage.Source = image;
                            }
                        }
                    });
                }
               
            }
        }

        private void UpdateDeviceList()
        {
            this._videoDeviceList.Items.Clear();
            var list = VideoDevice.DeviceInfo.GetList();
            foreach (var item in list) this._videoDeviceList.Items.Add(item.Name);
        }

        public MainWindow()
        {
            InitializeComponent();
            this.Closed += new EventHandler(delegate (Object o, EventArgs e) { ReleaseResources(); });

            UpdateDeviceList();

            this._openVideoDeviceButton.Click += _openVideoDeviceButton_Click;
            this._connectionButton.Click += _connectionButton_Click;
            this._refreshVideoDeviceButton.Click += new RoutedEventHandler(delegate (Object o, RoutedEventArgs a) { UpdateDeviceList(); });

            Task.Run(ReceivedMessageAsync);
        }

        private void ReleaseResources()
        {
            if (_videoCaptureDevice.IsRunning) _videoCaptureDevice.Stop();
        }

        private void _connectionButton_Click(object sender, RoutedEventArgs e)
        {
            udpReceiver = new UdpClient(int.Parse(this._inputUdpPort.Text), AddressFamily.InterNetworkV6);
            remotePort = int.Parse(this._destinationPort.Text);
            remoteIp = this._destinationIp.Text;
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
                if (_videoCaptureDevice != null && _videoCaptureDevice.IsRunning) _videoCaptureDevice.Stop();
                _videoCaptureDevice = null;
                _videoCaptureDevice = new VideoCaptureDevice(descriptor);
                _videoCaptureDevice.VideoSourceError += _videoCaptureDevice_VideoSourceError;
                _videoCaptureDevice.NewFrame += _videoCaptureDevice_NewFrame;
                _videoCaptureDevice.Start();
            }
        }

        private void _videoCaptureDevice_VideoSourceError(object sender, AForge.Video.VideoSourceErrorEventArgs eventArgs)
        {
            Console.WriteLine(eventArgs.Description);
        }

        private string remoteIp;
        private int remotePort;

        private void _videoCaptureDevice_NewFrame(object sender, AForge.Video.NewFrameEventArgs eventArgs)
        {
            try
            {
                Dispatcher.Invoke(() =>
                {
                    using (MemoryStream ms = new MemoryStream())
                    {
                        Bitmap bitmap = new Bitmap(eventArgs.Frame, 800, 600);
                        bitmap.Save(ms, ImageFormat.Jpeg);
                        ms.Seek(0, SeekOrigin.Begin);

                        BitmapImage image = new BitmapImage();
                        image.BeginInit();
                        image.StreamSource = ms;
                        image.CacheOption = BitmapCacheOption.OnLoad;
                        image.EndInit();

                        this._outputWebCameraImage.Source = image;

                        byte[] data = ms.ToArray();
                        UdpClient udpSender = new UdpClient(AddressFamily.InterNetworkV6);
                        udpSender.Send(data, data.Length, new IPEndPoint(IPAddress.Parse(remoteIp), remotePort));
                        _statusTextBlock.Text = "Bytes sended: " + data.Length.ToString();
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
