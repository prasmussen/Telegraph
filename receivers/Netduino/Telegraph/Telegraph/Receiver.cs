using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Socket = System.Net.Sockets.Socket;
using System.Threading;
using Microsoft.SPOT;


namespace Telegraph {

    public class Receiver : TransmissionHandler {
        private TransmissionHandler handler;
        private Connection conn;

        public Receiver() {
            handler = this;
        }

        // This method will override the default transmission handler
        public void SetTransmissionHandler(TransmissionHandler handler) {
            this.handler = handler;
        }

        public void ConnectToTransmitter(String server, int port) {
            conn = new Connection(server, port);
            conn.Connect();
        }

        public void ListenForTransmissions() {
            while (true) {
                Debug.Print("Waiting for transmission...");
                String transmission = conn.ReadTransmission();
                handler.Handle(transmission);
            }
        }

        // This is the default transmission handler, this method will get the transmissions unless set otherwise
        public void Handle(String transmission) {
            Debug.Print("Default handler received transmission:");
            Debug.Print(transmission);
        }
    }

    public class Connection {
        private const int MaxTransmissionSize = 2048;
        private const byte EOL = 10;
        private const String KeepAlive = "PING";
        private Byte[] buffer = new Byte[1];
        private Byte[] transmission = new Byte[MaxTransmissionSize];
        private Socket socket;
        private String addr;
        private int port;
        
        public Connection(String addr, int port) {
            this.addr = addr;
            this.port = port;
        }

        public void Connect() {
            Start:
            IPHostEntry hostEntry = Dns.GetHostEntry(addr);
            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.ReceiveTimeout = 120 * 1000;
            try {
                Debug.Print("Connecting...");
                socket.Connect(new IPEndPoint(hostEntry.AddressList[0], port));
            }
            catch (SocketException e) {
                Debug.Print("Unable to connect: " + e.Message);
                Debug.Print("Waiting 60 seconds before retry...");
                Thread.Sleep(60 * 1000);
                goto Start;
            }
            Debug.Print("Connected!");
            this.socket = socket;
        }
    
        public String ReadTransmission() {
            Start:
            Array.Clear(transmission, 0, transmission.Length);

            for (int i = 0; i < MaxTransmissionSize; i++) {
                Array.Clear(buffer, 0, buffer.Length);
                int n = 0;
                try {
                    n = socket.Receive(buffer, 1, SocketFlags.None);
                }
                catch (SocketException e) {
                    // Did not receive data for 120 seconds, this probably means we are disconnected
                    Debug.Print("Receive timeout: " + e.Message);
                    Connect();
                    goto Start;
                }
                byte b = buffer[0];

                if (n == 0) {
                    // Received 0 bytes, this probably means we are disconnected
                    Debug.Print("Received 0 bytes");
                    Connect();
                    goto Start;
                }
                else if (b == EOL) {
                    break;
                }
                transmission[i] = b;
            }
            
            String str = new String(Encoding.UTF8.GetChars(transmission));
            if (str == KeepAlive) {
                Debug.Print("Recieved keep alive signal!");
                goto Start;
            }
            return str;
        }
    }

    public interface TransmissionHandler {
        void Handle(String str);
    }
}
