using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using ASM_FORM_CLIENT.Model;
using ASM_FORM_CLIENT.SocketHandlerClient;

namespace ASM_FORM_CLIENT
{
    static class Program
    {

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        /// 
        static Thread BackGroundThread = new Thread(StartClient)
        {
            IsBackground = true
        };

        [STAThread]
        static void Main()
        {
            BackGroundThread.Start();

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }

        static void StartClient()
        {
            // Get settings and connection info.
            IPAddress IPADDRESS = IPAddress.Parse(Properties.SocketClientConfig.Default.HOST);
            int PORT = Properties.SocketClientConfig.Default.PORT;
            IPEndPoint IPEndPoint = new IPEndPoint(IPADDRESS, PORT);
            int TOTAL_CONNECTION = Properties.SocketClientConfig.Default.TOTAL_CONNECTION;
            int BUFFER_SIZE = Properties.SocketClientConfig.Default.BUFFER_SIZE;

            BaseModel baseModel = new BaseModel();

            try
            {
                String host = "192.168.1.4";// args[0];
                Int32 port = 9900;// Convert.ToInt32(args[1]);
                Int16 iterations = 5;
                SocketClient sa = new SocketClient(host, port);
                sa.Connect();
                for (Int32 i = 0; i < iterations; i++)
                {
                    var response = sa.SendRequest(new BaseModel());
                    if (response != null) response.Print();
                    else Console.WriteLine("Can not send data to Server!");
                }
            }
            catch (IndexOutOfRangeException)
            {
                Console.WriteLine("Usage: SocketAsyncClient <host> <port> [iterations]");
            }
            catch (FormatException)
            {
                Console.WriteLine("Usage: SocketAsyncClient <host> <port> [iterations]." +
                    "\r\n\t<host> Name of the host to connect." +
                    "\r\n\t<port> Numeric value for the host listening TCP port." +
                    "\r\n\t[iterations] Number of iterations to the host.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERROR: " + ex.Message);
            }
        }
    }
}
