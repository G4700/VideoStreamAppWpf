using AForge.Video.DirectShow;
using Emgu.CV;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VideoStreamAppWpf
{
    public partial class VideoDevice
    {
        public class DeviceInfo
        {
            public String Name { get; }
            public String Descriptor { get; }

            public float VResolution { get; set; }
            public float HResolution { get; set; }

            public DeviceInfo() { }
            public DeviceInfo(String name, String descriptor)
            {
                Name = name;
                Descriptor = descriptor;
            }

            public static Collection<DeviceInfo> GetList()
            {
                FilterInfoCollection devices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
                Collection<DeviceInfo> videoDeviceInfos = new Collection<DeviceInfo>();
                foreach (FilterInfo device in devices) videoDeviceInfos.Add(new DeviceInfo(device.Name, device.MonikerString));
                return videoDeviceInfos;
            }
        }

        private Collection<DeviceInfo> _deviceList { get; set; }
        public Collection<DeviceInfo> DeviceList() { return DeviceInfo.GetList(); }
        public void RefreshDeviceList() { _deviceList = DeviceInfo.GetList(); }

        private VideoCaptureDevice      _videoSourceDevice;
  
        
        public VideoDevice() 
        {

        }
    }
}
