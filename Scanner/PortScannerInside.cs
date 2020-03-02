using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Scanner
{
    class PortScannerInside
    {
        public static int i = 0;
        public static PortScanResults scanResults = new PortScanResults();
        public static int ScanPort(string ip, int portFrom, int portTo)
        {
            int returnCode = 0;
            try
            {
                Debug.WriteLine(Thread.CurrentThread.ManagedThreadId);
                var task = RunPortScanAsync(ip, portFrom, portTo);
                task.Wait();
            }
            catch (Exception ex)
            {
                returnCode = 1;
                Console.WriteLine("  error : {0}",
                    ex.InnerException != null
                        ? ex.InnerException.Message
                        : ex.Message);
            }

            if (returnCode == 0)
            {
                Console.WriteLine("Finished.");
            }

            return returnCode;
        }

        public static async Task RunPortScanAsync(string ip, int portFrom, int portTo)
        {
            Debug.WriteLine(Thread.CurrentThread.ManagedThreadId);

            // do a specific range
            //Console.WriteLine("> Checking ports 75-85 on localhost...\n");
            PortScanner cps = new PortScanner(ip, portFrom, portTo);

            var progress = new Progress<PortScanner.PortScanResult>();
            //progress.ProgressChanged += (sender, args) =>
            //{
            //    Debug.WriteLine(
            //        $"Port {args.PortNum} is " +
            //        $"{(args.IsPortOpen ? "open" : "closed")}");
            //};
            await cps.ScanAsync(progress);
            scanResults.portScanResults = cps.Results.ToList();
            Debug.WriteLine("Scan complete !");
            //cps.LastPortScanSummary();

            // do the local machine, whole port range 1-65535
            //cps = new CheapoPortScanner();
            //await cps.Scan(progress);
            //cps.LastPortScanSummary();
        }
    }


    internal partial class PortScanner
    {
        private const int PORT_MIN_VALUE = 1;
        private const int PORT_MAX_VALUE = 65535;

        private List<PortScanResult> _Results;
        //private List<PortScanResult> _closedPorts;

        public ReadOnlyCollection<PortScanResult> Results => new ReadOnlyCollection<PortScanResult>(_Results);
        //public ReadOnlyCollection<PortScanResult> ClosedPorts => new ReadOnlyCollection<PortScanResult>(_closedPorts);

        public int MinPort { get; } = PORT_MIN_VALUE;
        public int MaxPort { get; } = PORT_MAX_VALUE;

        public string Host { get; } = "127.0.0.1"; // localhost

        public PortScanner()
        {
            // defaults are already set for ports & localhost
            SetupLists();
        }

        public PortScanner(string host, int minPort, int maxPort)
        {
            if (minPort > maxPort)
                throw new ArgumentException("Min port cannot be greater than max port");

            if (minPort < PORT_MIN_VALUE || minPort > PORT_MAX_VALUE)
                throw new ArgumentOutOfRangeException(
                    $"Min port cannot be less than {PORT_MIN_VALUE} " +
                    $"or greater than {PORT_MAX_VALUE}");

            if (maxPort < PORT_MIN_VALUE || maxPort > PORT_MAX_VALUE)
                throw new ArgumentOutOfRangeException(
                    $"Max port cannot be less than {PORT_MIN_VALUE} " +
                    $"or greater than {PORT_MAX_VALUE}");

            Host = host;
            MinPort = minPort;
            MaxPort = maxPort;
            SetupLists();
        }

        private void SetupLists()
        {
            // set up lists with capacity to hold half of range
            // since we can't know how many ports are going to be open
            // so we compromise and allocate enough for half

            // rangeCount is max - min + 1
            int rangeCount = (MaxPort - MinPort) + 1;

            // if there are an odd number, bump by one to get one extra slot
            if (rangeCount % 2 != 0)
            {
                rangeCount += 1;
            }

            // reserve half the ports in the range for each
            _Results = new List<PortScanResult>(rangeCount);
            //_closedPorts = new List<PortScanResult>(rangeCount / 2);
        }

        private async Task CheckPortAsync(int port, IProgress<PortScanResult> progress)
        {
            Debug.WriteLine("This thread"+Thread.CurrentThread.ManagedThreadId+" inside !");
            Debug.WriteLine(Thread.CurrentThread.ManagedThreadId);
            if (await IsPortOpenAsync(port))
            {
                // if we got here it is open
                _Results.Add(new PortScanResult { PortNum = port, IsPortOpen = true , stt = PortScannerInside.i});
                PortScannerInside.i++;
                // notify anyone paying attention
                //progress?.Report(new PortScanResult { PortNum = port, IsPortOpen = true });
            }
            else
            {
                // server doesn't have that port open
                _Results.Add(new PortScanResult { PortNum = port, IsPortOpen = false , stt = PortScannerInside.i });
                PortScannerInside.i++;
                //progress?.Report(new PortScanResult() { PortNum = port, IsPortOpen = false });
            }
        }

        private async Task<bool> IsPortOpenAsync(int port)
        {
            Debug.WriteLine("This thread" + Thread.CurrentThread.ManagedThreadId + " inside !.");
            bool connected = false;
            Debug.WriteLine(Thread.CurrentThread.ManagedThreadId);
            Socket socket = null;
            //try
            //{
                // make a TCP based socket
                socket = new Socket(AddressFamily.InterNetwork, SocketType
                    .Stream, ProtocolType.Tcp);
                Debug.WriteLine(Thread.CurrentThread.ManagedThreadId);
                // connect
                    try
                    {
                        Debug.WriteLine(Thread.CurrentThread.ManagedThreadId);
                        socket.Connect(Host, port);
                        if (socket.Connected == true) connected = true;
                    }
                   catch(SocketException e)
                    {
                        Debug.WriteLine("Handed here! ");
                    }
            //}
            //catch (SocketException ex)
            //{
                //if (ex.SocketErrorCode == SocketError.ConnectionRefused)
                //{
                //    return false;
                //}

                //An error occurred when attempting to access the socket
                //Debug.WriteLine(ex.ToString());
                //Console.WriteLine(ex);
            //}
            //finally
            //{
                if (socket.Connected == true)
                {
                    socket.Disconnect(false);
                }
                socket.Close();
            //}
            return connected;
        }

        public class Data
        {
            public IProgress<PortScanResult> _progress;
            public int _port;
        }


        public void ThreadScanFunctionAsync(object x)
        {
            Debug.WriteLine(Thread.CurrentThread.ManagedThreadId + "is started !");
            Data data = (Data)x;
            CheckPortAsync(data._port,data._progress);
        }

        

        public async Task ScanAsync(IProgress<PortScanResult> progress)
        {
            Thread thread;
            List<Thread> threads = new List<Thread>();
            for (int port = MinPort; port <= MaxPort; port++)
            {
                thread = new Thread(ThreadScanFunctionAsync);
                threads.Add(thread);
                thread.Start(new Data {_port = port , _progress = progress});
            }
            foreach(var x in threads)
            {
                try
                {
                    if (x.IsAlive)
                        x.Join();
                }
                catch (Exception e)
                { }
            }
        }
    }
}
