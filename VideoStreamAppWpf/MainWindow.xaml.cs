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
    public delegate void ErrorHandler(string msg);
    public delegate void ReceiveHandler(MemoryStream ms);
    public delegate void NotificationHandler(string msg);

    interface NetProvider
    {
        event ErrorHandler ErrorEvent;
        event ReceiveHandler ReceiveEvent;
        event NotificationHandler NotificationEvent;

        AddressFamily AddressFamily { get; set; }
        string RemoteIp { get; set; }
        string RemotePort { get; set; }
        string InputPort { get; set; }


        void Open();
        void Send(byte[] data);
        void Close();
    }

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
            this._protocolTypeComboBox.SelectionChanged += ProtocolSelectChanged;

            this._remoteIpTextBox.Text      = "2620:9b::1928:54e1";
            this._remotePortTextBox.Text    = "8000";
            this._inputPortTextBox.Text     = "8001";

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
                case "IPv4": _ipType = AddressFamily.InterNetwork;       break;
                case "IPv6": _ipType = AddressFamily.InterNetworkV6;     break;
            }
        }

        private void ProtocolSelectChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBoxItem comboBoxItem = this._protocolTypeComboBox.SelectedItem as ComboBoxItem;
            switch (comboBoxItem.Content.ToString())
            {
                case "UDP": _protocol = "UDP"; break;
                case "TCP": _protocol = "TCP"; break;
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
                _netProvider?.Close();
                _netProvider = null;

                _videoDevice?.Close();
                _videoDevice?.Open(this._deviceListBox.SelectedItem?.ToString());

                if (_protocol == "UDP") 
                { 
                    _netProvider = new UdpProvider(); 
                }
                else if (_protocol == "TCP") 
                { 
                    _netProvider = new TcpProvider(); 
                }

                _netProvider.RemoteIp           = this._remoteIpTextBox.Text;
                _netProvider.RemotePort         = this._remotePortTextBox.Text;
                _netProvider.InputPort          = this._inputPortTextBox.Text;
                _netProvider.AddressFamily      = _ipType;
                _netProvider.ReceiveEvent       += UdpProviderReceiveHandler;
                _netProvider.NotificationEvent  += ToLog;
                _netProvider.ErrorEvent         += ToLog;
                _netProvider?.Open();
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
                _netProvider?.Send(data);
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

        private string _protocol = "UDP";
        private AddressFamily _ipType = AddressFamily.InterNetwork;
        private NetProvider _netProvider;
        private VideoDevice _videoDevice = new VideoDevice(800, 600);
    }
}
