using Habanero.Faces.Base;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Net;
using System.Text;
using System.Threading;
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
using static Scanner.ipScanner;

namespace Scanner
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }
        public async void ScanIp_Click(object sender, EventArgs e)
        {
            //Debug.WriteLine("Running !");
            IPResult data = new IPResult();
            string IpRangeFrom = ipFromBox.Text;
            string IpRangeTo = ipToBox.Text;
            ipScanner ipScanner = new ipScanner();
            await Task.Run(async () =>
            {
                //Debug.WriteLine("Thread : " + Thread.CurrentThread.ManagedThreadId + " is alive " + Thread.CurrentThread.IsAlive);
                data = ipScanner.PingAndGetIP(IpRangeFrom, IpRangeTo);
                int i = 0;
                foreach (var x in data.Results)
                {
                    //Debug.WriteLine("Thread : " + Thread.CurrentThread.ManagedThreadId);
                    i++;
                    x.STT = i;
                }
            });
            listIP.ItemsSource = data.Results;
        }
        public void ScanPort(object sender , EventArgs e)
        {
            ScanPort scanPort = new ScanPort();
            scanPort.Scan((listIP.SelectedItem as IPInfo).IPAddress , 1 , 200 );
            scanPort.Visibility = Visibility.Visible;
        }
        public static string getOS(string ip)
        {
            string OS = null;
            IPAddress addr = IPAddress.Parse(ip);
            IPHostEntry host = Dns.GetHostEntry(addr);
            string hostName = host.HostName;
            string searchClass = "Win32_OperatingSystem";
            string param = "Caption";
            string cmd = "\\\\" + hostName + "\\root\\CIMV2";
            string query = "SELECT * FROM " + searchClass;
            try
            {
                ManagementObjectSearcher searcher = new ManagementObjectSearcher(cmd, query);
                foreach (ManagementObject obj in searcher.Get())
                {
                    OS += obj.GetPropertyValue(param);
                }
            }
            catch
            {
                MessageBox.Show("Couldnt retrieve IP: " + ip, "Error");
            }
            return OS;
        }
    }
}
