using AForge.Video.DirectShow;
using Emgu.CV;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

namespace VideoStreamAppWpf
{
    public partial class VideoDevice
    {
        public delegate void NewFrameHandler(MemoryStream ms);
        public delegate void NotificationHandler(string msg);
        public delegate void ErrorHandler(string msg);
        public delegate void ParametersChanged(Parameters prms);
        public event NewFrameHandler NewFrameEvent;
        public event ErrorHandler ErrorEvent;
        public event NotificationHandler NotificationEvent;
        public event ParametersChanged ParametersChangedEvent;

        public class Parameters : INotifyPropertyChanged
        {
            public void Update(VideoDevice dev, AForge.Video.NewFrameEventArgs e)
            {
                _rawFrameSize = e.Frame.Size;
                _outputFrameSize = new Size(dev._width, dev._height);
                _frameBytesReceived = dev._videoCaptureDevice.BytesReceived;

                dev.ParametersChangedEvent?.Invoke(this);
            }

            private Size    _rawFrameSize;
            private Size    _outputFrameSize;
            private long    _frameBytesReceived;

            public Size RawFrameSize           { get { return _rawFrameSize; } }
            public Size OutputFrameSize        { get { return _outputFrameSize; } }
            public long FrameBytesReceived     { get { return _frameBytesReceived; } }


            public event PropertyChangedEventHandler PropertyChanged;
            public void OnPropertyChanged([CallerMemberName] string prop = "")
            {
                if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs(prop));
            }
        }
        
        public class DeviceInfo
        {
            public String Name { get; }
            public String Descriptor { get; }

            public DeviceInfo() { }
            public DeviceInfo(String name, String descriptor)
            {
                Name = name;
                Descriptor = descriptor;
            }
        }

        private void FrameGetHandler(object sender, AForge.Video.NewFrameEventArgs e)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                Bitmap bitmap = new Bitmap(e.Frame, _width, _height);
                bitmap.Save(ms, ImageFormat.Jpeg);
                ms.Seek(0, SeekOrigin.Begin);
                _parameters.Update(this, e);
                NewFrameEvent?.Invoke(ms);
            }
        }

        private void VideoSourceErrorHandler(object sender, AForge.Video.VideoSourceErrorEventArgs e)
        {
            ErrorEvent?.Invoke(e.Description);
        }

        private static Collection<DeviceInfo> GetDevicesList()
        {
            FilterInfoCollection devices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            Collection<DeviceInfo> videoDeviceInfos = new Collection<DeviceInfo>();
            foreach (FilterInfo device in devices) videoDeviceInfos.Add(new DeviceInfo(device.Name, device.MonikerString));
            return videoDeviceInfos;
        }

        public Collection<string> GetDeviceNames()
        {
            _devices = GetDevicesList();
            Collection<string> names = new Collection<string>();
            foreach (DeviceInfo device in _devices) names.Add(device.Name);
            return names;
        }

        private string GetDescriptor(string name)
        {
            foreach (DeviceInfo device in _devices) if (device.Name == name) return device.Descriptor;
            return "";
        }

        public bool Open(string name)
        {
            string desriptor = GetDescriptor(name);

            if (desriptor == "") 
            {
                ErrorEvent?.Invoke("Video Device: null device descriptor");
                return false;
            }

            try
            {
                _videoCaptureDevice?.Stop();
                _videoCaptureDevice = null;
                _videoCaptureDevice = new VideoCaptureDevice(GetDescriptor(name));
                _videoCaptureDevice.NewFrame += FrameGetHandler;
                _videoCaptureDevice.VideoSourceError += VideoSourceErrorHandler;
                _videoCaptureDevice.Start();

                NotificationEvent?.Invoke("Video Device: Connection Opened");
            }
            catch(Exception ex)
            {
                ErrorEvent?.Invoke("Video Device: " + ex.Message);
                return false;
            }

            return true;
        }

        public void Close()
        {
            _videoCaptureDevice?.Stop();
            _videoCaptureDevice = null;
        }

        public VideoDevice(int width, int height) 
        {
            _width = width;
            _height = height;
        }

        ~VideoDevice()
        {
            this.Close();
        }

        public bool IsRunning() { return _videoCaptureDevice.IsRunning; }
        public Parameters GetParameters() { return _parameters; }

        private int _width;
        private int _height;
        private VideoCaptureDevice _videoCaptureDevice = new VideoCaptureDevice();
        private Collection<DeviceInfo> _devices = new Collection<DeviceInfo>(GetDevicesList());
        private Parameters _parameters = new Parameters();
    }
}
