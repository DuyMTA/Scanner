using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Scanner
{
    partial class ipScanner
    {
        public static String ReverseIPLookup(IPAddress ipAddress)
        {
            if (ipAddress.AddressFamily != AddressFamily.InterNetwork)
                throw new ArgumentException("IP address is not IPv4.", "ipAddress");
            var domain = String.Join(
              ".", ipAddress.GetAddressBytes().Reverse().Select(b => b.ToString())
            ) + ".in-addr.arpa";
            return DnsGetPtrRecord(domain);
        }

        static String DnsGetPtrRecord(String domain)
        {
            const Int16 DNS_TYPE_PTR = 0x000C;
            const Int32 DNS_QUERY_STANDARD = 0x00000000;
            const Int32 DNS_ERROR_RCODE_NAME_ERROR = 9003;
            IntPtr queryResultSet = IntPtr.Zero;
            try
            {
                var dnsStatus = DnsQuery(
                  domain,
                  DNS_TYPE_PTR,
                  DNS_QUERY_STANDARD,
                  IntPtr.Zero,
                  ref queryResultSet,
                  IntPtr.Zero
                );
                if (dnsStatus == DNS_ERROR_RCODE_NAME_ERROR)
                    return null;
                if (dnsStatus != 0)
                    throw new Win32Exception(dnsStatus);
                DnsRecordPtr dnsRecordPtr;
                for (var pointer = queryResultSet; pointer != IntPtr.Zero; pointer = dnsRecordPtr.pNext)
                {
                    dnsRecordPtr = (DnsRecordPtr)Marshal.PtrToStructure(pointer, typeof(DnsRecordPtr));
                    if (dnsRecordPtr.wType == DNS_TYPE_PTR)
                        return Marshal.PtrToStringUni(dnsRecordPtr.pNameHost);
                }
                return null;
            }
            finally
            {
                const Int32 DnsFreeRecordList = 1;
                if (queryResultSet != IntPtr.Zero)
                    DnsRecordListFree(queryResultSet, DnsFreeRecordList);
            }
        }

        [DllImport("Dnsapi.dll", EntryPoint = "DnsQuery_W", ExactSpelling = true, CharSet = CharSet.Unicode, SetLastError = true)]
        static extern Int32 DnsQuery(String lpstrName, Int16 wType, Int32 options, IntPtr pExtra, ref IntPtr ppQueryResultsSet, IntPtr pReserved);

        [DllImport("Dnsapi.dll", SetLastError = true)]
        static extern void DnsRecordListFree(IntPtr pRecordList, Int32 freeType);

        [StructLayout(LayoutKind.Sequential)]
        struct DnsRecordPtr
        {
            public IntPtr pNext;
            public String pName;
            public Int16 wType;
            public Int16 wDataLength;
            public Int32 flags;
            public Int32 dwTtl;
            public Int32 dwReserved;
            public IntPtr pNameHost;
        }

        public IPResult PingAndGetIP(string ipRangeFrom, string ipRangeTo)
        {
            //Debug.WriteLine(Thread.CurrentThread.ManagedThreadId);
            Thread thread;
            List<IPInfo> iPInfos = new List<IPInfo>();
            List<Thread> threads = new List<Thread>();
            string IpRangeFromTemplate = ipRangeFrom.Substring(0, ipRangeFrom.LastIndexOf('.') + 1);
            int range = int.Parse(ipRangeFrom.Substring(ipRangeFrom.LastIndexOf('.') + 1));
            string IpRangeFrom_ = IpRangeFromTemplate + range.ToString();
            string IpRangeTo_ = ipRangeTo;
            IPResult results = new IPResult { ipRangeFrom = IpRangeFrom_, ipRangeTo = IpRangeTo_, Results = iPInfos };
            while (results.ipRangeFrom.CompareTo(results.ipRangeTo) != 0)
            {
                thread = new Thread(ThreadFunction);
                threads.Add(thread);
                thread.Start(results);
                thread.Join(5);
                range++;
                results.ipRangeFrom = IpRangeFromTemplate + range.ToString();
            }
            //foreach(var thread in threads)
            //{
            //    thread.Start(iPInfos);
            //}
            foreach (var x in threads)
            {
                try
                {
                    if (x.IsAlive)
                        x.Join();
                }
                catch (Exception)
                {

                }
            }
            //Debug.WriteLine("Thread inside is alive !");
            return results;
        }
        public void ThreadFunction(object data)
        {
            IPResult newData = (IPResult)data;
            string ip = newData.ipRangeFrom;
            //Debug.WriteLine("Thread " + Thread.CurrentThread.ManagedThreadId + " Started");
            bool pingable = false;
            Ping pinger = null;
            try
            {
                pinger = new Ping();
                PingReply reply = pinger.Send(ip);
                pingable = reply.Status == IPStatus.Success;
                newData.Results.Add(new IPInfo() { RoundTripTime = reply.RoundtripTime.ToString(), IPAddress = ip, PingAble = pingable.ToString() });
            }
            catch (PingException)
            {
                // Discard PingExceptions and return false;
            }
            finally
            {
                if (pinger != null)
                {
                    pinger.Dispose();
                }
            }
        }
    }
}
