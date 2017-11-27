using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace libterminal
{
    public class Connection
    {
        public Uri Destination { get; set; }

        public Guid Id { get; } = Guid.NewGuid();

        public ConnectionProtocol Protocol { get; set; }

        public string ReceiverBuffer { get; set; } = string.Empty;

        public long Position { get; set; } = 0;

        public List<long> PositionStack { get; set; } = new List<long>();

        private AutoResetEvent BufferChanged { get; set; } = new AutoResetEvent(false);

        public bool IsConnected
        {
            get
            {
                return Protocol != null && Protocol.IsConnected;
            }
        }

        public Connection()
        {
            ConnectionManager.Instance.AddConnection(this);
        }

        public void Connect(Uri destination)
        {
            Destination = destination;

            switch (destination.Scheme)
            {
                case "ssh":
                    Protocol = new ConnectionProtocolSsh(this);
                    Protocol.OnReceived += dataReceived;
                    Protocol.ConnectTo(destination);
                    break;
                case "telnet":
                    Protocol = new ConnectionProtocolTelnet(this);
                    Protocol.OnReceived += dataReceived;
                    Protocol.ConnectTo(destination);
                    break;
                default:
                    throw new ArgumentException("Destination : " + destination.ToString() + " uses unknown protocol type : " + destination.Scheme);
            }
        }

        public void Mark()
        {
            Position = ReceiverBuffer.Length;
        }

        public bool SendData(byte [] buffer)
        {
            System.Diagnostics.Debug.WriteLine("Send -> [" + Encoding.UTF8.GetString(buffer) + "]");
            return Protocol.SendData(buffer);
        }

        public void PushBuffer()
        {
            lock (PositionStack) PositionStack.Add(ReceiverBuffer.Length);
        }

        public void PopBuffer()
        {
            lock(PositionStack)
            {
                if (PositionStack.Count == 0)
                    throw new Exception("Popping from an empty position stack for connection : " + Destination.ToString());

                PositionStack.RemoveAt(PositionStack.Count - 1);
            }
        }

        public string GetActiveBuffer()
        {
            long position = -1;
            lock(PositionStack)
            {
                if (PositionStack.Count == 0)
                    position = 0;
                else
                    position = PositionStack.Last();
            }

            string result = string.Empty;
            lock (ReceiverBuffer) result = ReceiverBuffer.Substring(Convert.ToInt32(position));

            return result;
        }

        public Match WaitFor(string expression, int timeOutMs)
        {
            var startTime = DateTime.Now;

            var ex = new Regex(expression, RegexOptions.ExplicitCapture);
            Match result;
            lock (ReceiverBuffer) result = ex.Match(ReceiverBuffer, Convert.ToInt32(Position));


            while ((result == null || !result.Success) && (DateTime.Now - startTime).TotalMilliseconds < timeOutMs)
            {
                bool changed = BufferChanged.WaitOne(Convert.ToInt32((DateTime.Now - startTime).TotalMilliseconds));
                if (changed)
                {
                    System.Diagnostics.Debug.WriteLine(ReceiverBuffer.Substring(Convert.ToInt32(Position)));
                    lock (ReceiverBuffer) result = ex.Match(ReceiverBuffer, Convert.ToInt32(Position));
                }
            }

            return result;
        }

        private void dataReceived(object sender, DataReceivedEventArgs e)
        {
            var protocol = (ConnectionProtocol)sender;

            string receivedString = Encoding.UTF8.GetString(e.Data);

            System.Diagnostics.Debug.WriteLine("Received -> [" + receivedString + "]");

            lock (ReceiverBuffer) { 
                ReceiverBuffer += receivedString;
                BufferChanged.Set();
            }
        }
    }
}
