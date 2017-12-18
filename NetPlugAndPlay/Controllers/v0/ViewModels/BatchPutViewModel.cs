using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NetPlugAndPlay.Controllers.v0.ViewModels
{
    public class BatchPutViewModel
    {
        [JsonProperty("deviceTypes")]
        public NetworkDeviceTypeChangesViewModel DeviceTypes { get; set; }
        [JsonProperty("networkDevices")]
        public NetworkDevicesPutViewModel NetworkDevices { get; set; }
        [JsonProperty("templates")]
        public TemplateChangesViewModel Templates { get; set; }
        [JsonProperty("tftpFiles")]
        public TFTPFileChangesViewModel TFTPFiles { get; set; }
        [JsonProperty("connections")]
        public NetworkConnectionsViewModel Connections { get; set; }
    }
}
