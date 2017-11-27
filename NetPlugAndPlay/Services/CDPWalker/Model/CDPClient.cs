using libterminal.Helpers.Model;
using libterminal.Helpers.Parsers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace NetPlugAndPlay.Services.CDPWalker.Model
{
    public class CDPClient
    {
        public Guid NetworkDeviceId { get; set; } = Guid.Empty;
        public IPAddress ManagementIP { get; set; }
        private string showCDPText = string.Empty;
        public string ShowCDPText
        {
            get
            {
                return showCDPText;
            }
            set
            {
                showCDPText = value;
                CDPEntries = parseShowCDPEntries(value);
            }
        }
        public List<ShowCDPEntryItem> CDPEntries { get; set; }
        public DateTimeOffset TimeOriginallyObserved { get; set; } = DateTimeOffset.FromUnixTimeSeconds(0);
        public DateTimeOffset TimeLastObserved { get; set; } = DateTimeOffset.FromUnixTimeSeconds(0);

        private List<ShowCDPEntryItem> parseShowCDPEntries(string text)
        {
            try
            {
                var parser = new ShowCDPEntry();
                return parser.Parse(text);
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.Message);
            }

            return null;
        }
    }
}
