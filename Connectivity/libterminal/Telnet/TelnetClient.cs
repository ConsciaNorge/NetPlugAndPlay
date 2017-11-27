using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;

namespace libterminal.Telnet
{
    // Hints for code found at : https://www.codeproject.com/Articles/19071/Quick-tool-A-minimalistic-Telnet-library
    enum Verbs
    {
        WILL = 251,
        WONT = 252,
        DO = 253,
        DONT = 254,
        IAC = 255
    }

    enum Options
    {
        SGA = 3
    }

    public class DataReceived
    {
        public byte [] Data { get; set; }
    }

    public class TelnetClient
    {
        TcpClient tcpSocket = new TcpClient();
        byte [] receiveBuffer = new byte[65536];

        public EventHandler<DataReceived> OnDataReceived = null;

        public TelnetClient()
        {
        }

        public void Connect(string Hostname, int Port)
        {
            tcpSocket.Connect(Hostname, Port);
            tcpSocket.Client.BeginReceive(receiveBuffer, 0, receiveBuffer.Length, SocketFlags.None, processIncoming, this);
        }

        private void processIncoming(IAsyncResult ar)
        {
            int bytesReceived;
            try
            {
                bytesReceived = tcpSocket.Client.EndReceive(ar);
            }
            catch
            {
                return;
            }

            var receivedData = new MemoryStream();

            int index = 0;
            while(index < bytesReceived)
            {
                var value = receiveBuffer[index++];
                switch(value)
                {
                    case (int)Verbs.IAC:
                        // interpret as command
                        if (index >= bytesReceived)
                            throw new Exception("Feature not implemented. must handle carry over from previous packet for this condition");

                        var inputverb = receiveBuffer[index++];

                        switch (inputverb)
                        {
                            case (byte)Verbs.IAC:
                                //literal IAC = 255 escaped, so append char 255 to string
                                receivedData.WriteByte(inputverb);
                                break;
                            case (byte)Verbs.DO:
                            case (byte)Verbs.DONT:
                            case (byte)Verbs.WILL:
                            case (byte)Verbs.WONT:
                                // reply to all commands with "WONT", unless it is SGA (suppres go ahead)
                                if (index >= bytesReceived)
                                    throw new Exception("Feature not implemented. must handle carry over from previous packet for this condition");


                                byte inputoption = receiveBuffer[index++];
                                byte outputVerb;

                                if (inputoption == (byte)Options.SGA)
                                    outputVerb  = inputverb == (byte)Verbs.DO ? (byte)Verbs.WILL : (byte)Verbs.DO;
                                else
                                    outputVerb = inputverb == (byte)Verbs.DO ? (byte)Verbs.WONT : (byte)Verbs.DONT;

                                tcpSocket.GetStream().Write(new byte[] { (byte)Verbs.IAC, outputVerb, inputoption }, 0, 3);
                                break;

                            default:
                                break;
                        }
                        break;
                    default:
                        receivedData.WriteByte(value);
                        break;
                }
            }

            if(receivedData.Length > 0 && OnDataReceived != null)
            {
                receivedData.Flush();
                receivedData.Position = 0;
                var signalBuffer = new byte[receivedData.Length];
                receivedData.Read(signalBuffer, 0, Convert.ToInt32(receivedData.Length));
                OnDataReceived.Invoke(this, new DataReceived
                {
                    Data = signalBuffer
                });
            }

            if (tcpSocket.Connected)
            {
                try
                {
                    tcpSocket.Client.BeginReceive(receiveBuffer, 0, receiveBuffer.Length, SocketFlags.None, processIncoming, this);
                }
                catch(Exception e)
                {
                    System.Diagnostics.Debug.WriteLine("Failed to begin connect again : " + e.Message);
                }
            }
        }

        public void Write(byte [] data)
        {
            if (!tcpSocket.Connected)
                return;

            tcpSocket.GetStream().Write(data, 0, data.Length);
            tcpSocket.GetStream().Flush();
        }

        public bool IsConnected
        {
            get { return tcpSocket.Connected; }
        }
    }
}
