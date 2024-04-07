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
using System.ComponentModel;

namespace VideoStreamAppWpf
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    /// 
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            this._connectButton.Click                   += ConnectionButtonClick;
            this._refreshDeviceListButton.Click         += RefreshDeviceListButtonClick;
            this._clearLogButton.Click                  += ClearLogButtonClick;

            this._videoDevice.NewFrameEvent             += VideoDeviceNewFrameEventHandler;
            this._videoDevice.NotificationEvent         += ToLog;
            this._videoDevice.ErrorEvent                += ToLog;
            this._videoDevice.ParametersChangedEvent    += VideoDeviceParametersUpdateHandler;
            this._ipVersionComboBox.SelectionChanged    += IpVersionSelectChanged;
            
            this._udpProvider.ReceiveEvent              += UdpProviderReceiveHandler;
            this._udpProvider.ErrorEvent                += ToLog;

            this._remoteIpTextBox.Text = "::1";
            this._remotePortTextBox.Text = "8000";
            this._inputPortTextBox.Text = "8000";

            this._parametersTextBlock.DataContext = _videoDevice.GetParameters();
        }

        private void UdpProviderReceiveHandler(MemoryStream ms)
        {
            Dispatcher.Invoke(() =>
            {
                UpdateImage(ms, this._inputWebCameraImage);
            });
        }

        private void VideoDeviceParametersUpdateHandler(VideoDevice.Parameters parameters)
        {
            Dispatcher.Invoke(() => 
            {
                this._parametersTextBlock.Text = 
                $"Raw Frame Size: {parameters.RawFrameSize}\n" +
                $"Output Frame Size: {parameters.OutputFrameSize}\n" +
                $"Frame Bytes Received: {parameters.FrameBytesReceived}\n";
            });
        }

        private void IpVersionSelectChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBoxItem comboBoxItem = this._ipVersionComboBox.SelectedItem as ComboBoxItem;
            switch(comboBoxItem.Content.ToString())
            {
                case "IPv4": _udpProvider.AddressFamily = AddressFamily.InterNetwork;       break;
                case "IPv6": _udpProvider.AddressFamily = AddressFamily.InterNetworkV6;     break;
            }
        }

        private void RefreshDeviceListButtonClick(Object o, RoutedEventArgs a)
        {
            this._deviceListBox.Items.Clear();
            foreach (var item in _videoDevice.GetDeviceNames()) this._deviceListBox.Items.Add(item);
            ToLog("Refreshing devices");
        }

        private void ClearLogButtonClick(object o, RoutedEventArgs a)
        {
            this._logTextBlock.Text = "";
        }

        private void ConnectionButtonClick(object sender, RoutedEventArgs e)
        {
            try
            {
                _videoDevice.Open(this._deviceListBox.SelectedItem?.ToString());

                _udpProvider.RemoteIp = this._remoteIpTextBox.Text;
                _udpProvider.RemotePort = this._remotePortTextBox.Text;
                _udpProvider.InputPort = this._inputPortTextBox.Text;
                _udpProvider.Open();
            }
            catch(Exception ex) 
            {
                ToLog("App: " + ex.Message);
            }
        }

        private void ToLog(string message) { Dispatcher.Invoke(() => 
        { 
            this._logTextBlock.Text += message + "\n"; });
        }

        private void VideoDeviceNewFrameEventHandler(MemoryStream ms)
        {
            Dispatcher.Invoke(() => 
            {
                UpdateImage(ms, this._outputWebCameraImage);
                byte[] data = ms.ToArray();
                _udpProvider?.SendSync(data);
                _statusTextBlock.Text = "Bytes sended: " + data.Length.ToString();
            });
        }

        private void UpdateImage(MemoryStream ms, System.Windows.Controls.Image image) 
        {
            BitmapImage bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.StreamSource = ms;
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.EndInit();

            image.Source = bmp;
        }

        private VideoDevice _videoDevice = new VideoDevice(800, 600);
        private UdpProvider _udpProvider = new UdpProvider();
    }
}
