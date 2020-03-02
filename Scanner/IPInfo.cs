namespace Scanner
{
    partial class ipScanner
    {
        public class IPInfo
        {
            public int STT { get; set; }

            public string RoundTripTime { get; set; }

            public string PingAble { get; set; }

            public string IPAddress { get; set; }

            private string _HostName = string.Empty;
            public string HostName
            {
                get
                {
                    if (string.IsNullOrEmpty(this._HostName) && this.PingAble == "True")
                    {
                        try
                        {
                            this._HostName = ReverseIPLookup(System.Net.IPAddress.Parse(this.IPAddress));
                        }
                        catch
                        {
                            this._HostName = string.Empty;
                        }
                    }
                    return this._HostName;
                }
            }
        }
    }
}
