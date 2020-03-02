using System.Collections.Generic;

namespace Scanner
{
    partial class ipScanner
    {
        public class IPResult
        {
            public string ipRangeFrom { get; set; }
            public string ipRangeTo { get; set; }
            public List<IPInfo> Results { get; set; }
        }
    }
}
