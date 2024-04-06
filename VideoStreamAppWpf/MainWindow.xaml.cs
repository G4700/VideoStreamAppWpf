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
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace VideoStreamAppWpf
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    /// 
    // сделать открытие вебки через кнопку подключения
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
            this._deviceListBox.Items.Clear();
            var list = _videoDevice.GetDeviceNames();
            foreach (var item in list) this._deviceListBox.Items.Add(item);
        }

        private VideoDevice _videoDevice = new VideoDevice(800, 600);

        public MainWindow()
        {
            InitializeComponent();

            this._connectButton.Click += _connectionButton_Click;
            this._refreshDeviceListButton.Click += new RoutedEventHandler(delegate (Object o, RoutedEventArgs a) { UpdateDeviceList(); ToLog("Refreshing devices..."); });
            this._clearLogButton.Click += new RoutedEventHandler(delegate (Object o, RoutedEventArgs a) { this._logTextBlock.Text = ""; });

            this._videoDevice.NewFrameEvent += VideoDeviceNewFrameEventHandler;
            this._videoDevice.NotificationEvent += VideoDeviceNotificationEventHandler;
            this._videoDevice.ErrorEvent += VideoDeviceErrorEventHandler;


            Task.Run(ReceivedMessageAsync);
        }

        private void ToLog(string message)
        {
            Dispatcher.Invoke(() => 
            {
                this._logTextBlock.Text += message + "\n";
            });
        }

        private void _connectionButton_Click(object sender, RoutedEventArgs e)
        {
            ToLog("Connection Init");
            var selectedItem = this._deviceListBox.SelectedItem;
            if (!_videoDevice.Open(selectedItem.ToString())) ToLog("Device Open Error");

            udpReceiver = new UdpClient(int.Parse(this._inputPortTextBox.Text), AddressFamily.InterNetworkV6);
            remotePort = int.Parse(this._remotePortTextBox.Text);
            remoteIp = this._remoteIpTextBox.Text;
        }

        private void _videoCaptureDevice_VideoSourceError(object sender, AForge.Video.VideoSourceErrorEventArgs eventArgs)
        {
            Console.WriteLine(eventArgs.Description);
        }

        private string remoteIp;
        private int remotePort;

        private void VideoDeviceErrorEventHandler(string msg) { ToLog(msg); }
        private void VideoDeviceNotificationEventHandler(string msg) { ToLog(msg); }

        private void VideoDeviceNewFrameEventHandler(MemoryStream ms)
        {
            Dispatcher.Invoke(() => 
            {
                BitmapImage image = new BitmapImage();
                image.BeginInit();
                image.StreamSource = ms;
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.EndInit();

                this._outputWebCameraImage.Source = image;
                byte[] data = ms.ToArray();
                UdpClient udpSender = new UdpClient(AddressFamily.InterNetworkV6);
                if (data.Length < 65535) udpSender.Send(data, data.Length, new IPEndPoint(IPAddress.Parse(remoteIp), remotePort));
                _statusTextBlock.Text = "Bytes sended: " + data.Length.ToString();
            });
        }
    }
}
