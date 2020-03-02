namespace Scanner
{


    internal partial class PortScanner
    {
        internal class PortScanResult
        {
            public int stt { get; set; }
            public int PortNum { get; set; }
            public bool IsPortOpen { get; set; }
        }
    }
}
