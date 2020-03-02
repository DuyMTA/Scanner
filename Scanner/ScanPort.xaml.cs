using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
using System.Windows.Shapes;

namespace Scanner
{
    /// <summary>
    /// Interaction logic for ScanPort.xaml
    /// </summary>
    public partial class ScanPort : Window
    {
        public ScanPort()
        {
            InitializeComponent();
        }
        public async void Scan(string ip, int portFrom, int portTo)
        {
            await Task.Run(async () =>
            {
                Debug.WriteLine(Thread.CurrentThread.ManagedThreadId);
                PortScannerInside.ScanPort(ip, portFrom, portTo);
                listPort.Dispatcher.Invoke(() =>
                {
                    listPort.ItemsSource = PortScannerInside.scanResults.portScanResults;
                }
                );
                Debug.WriteLine("Writing !");
            });
        }
    }
}
