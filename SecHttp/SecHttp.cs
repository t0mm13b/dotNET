using NetTools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Web;

namespace SecurityNS {
    /// <summary>
    /// Summary description for SecHttp
    /// Requirements:
    /// Web.config:
    /// 1) Add the section at the top of Web.config - 
    ///   <system.webServer><modules><add name="SecHttp" type="ONtrackSecurityHttp.SecHttp"/></modules></system.webServer>
    /// 2) Add two keys to AppSettings section 
    ///    a) IsSecured, value of "1" for allow whitelist, "0" to allow ALL and 
    ///    b) SecuredIPs, comma delimited IPv4 addresses, if IsSecured is "0", can safely omit this key!
    ///    c) Custom403Message, a string value to spit out to default error page, default is "Is mhaith liomsa cáca milís!"
    /// 3) DONE! \o/
    /// </summary>
    public class SecHttp : IHttpModule {
        private const string CUSTOM_403_MSG = "Is mhaith liomsa cáca milís!";
        private HttpApplication _context = null;
        private EventHandler _ehBeginRequest;
        private string _strCustom403Msg;
        private bool _bIsSecured = false;
        private bool _bIsBRRegistered = false;
        private Regex _regIP = new Regex(@"^(?<firstOct>25[0-5]|2[0-4][0-9]|[0-1]{1}[0-9]{2}|[1-9]{1}[0-9]{1}|[1-9])\.(?<secondOct>25[0-5]|2[0-4][0-9]|[0-1]{1}[0-9]{2}|[1-9]{1}[0-9]{1}|[1-9]|0)\.(?<thirdOct>25[0-5]|2[0-4][0-9]|[0-1]{1}[0-9]{2}|[1-9]{1}[0-9]{1}|[1-9]|0)\.(?<fourthOct>25[0-5]|2[0-4][0-9]|[0-1]{1}[0-9]{2}|[1-9]{1}[0-9]{1}|[0-9])$", RegexOptions.Compiled | RegexOptions.Singleline);
        private List<IPWhiteListEntry> _listIPAddress;
        
        /// <summary>
        /// Default constructor, initializes and sets up flags and whitelist if applicable based on Web.config settings
        /// </summary>
        public SecHttp() {
            // Do not try use anonymous event handler as it screws things up!
            this._ehBeginRequest = new EventHandler(Application_BeginRequest);
            Boolean isSecured = ConfigSettings.IsSecured;
            if (isSecured && !this._bIsBRRegistered) {
                this._bIsSecured = true;
                //
                string sCustom403Msg = ConfigSettings.Custom403Message;
                if (string.IsNullOrEmpty(sCustom403Msg)) this._strCustom403Msg = CUSTOM_403_MSG;
                else this._strCustom403Msg = sCustom403Msg;
                //
                string sIPWhiteList = ConfigSettings.SecuredIPs;
                if (string.IsNullOrEmpty(sIPWhiteList)) {
                    // Whoops! Might as well ignore!
                    this._bIsSecured = false;
                    return;
                }
                string[] _strIPWhiteList;
                string cleansdIPWhiteList = sIPWhiteList.Replace(" ", "");
                if (cleansdIPWhiteList.IndexOf(',') > 0) {
                    _strIPWhiteList = cleansdIPWhiteList.Split(',');
                } else {
                    _strIPWhiteList = new string[] { cleansdIPWhiteList };
                }
                if (!IsWhiteListValid(_strIPWhiteList))
                {
                    _bIsSecured = false;
                }
            }
        }

        /// <summary>
        /// Default Dispose handler, just cleans up... being a good netizen 'ere... right? 
        /// </summary>
        public void Dispose() {
            if (this._bIsBRRegistered) {
                try {
                    this._context.BeginRequest -= this._ehBeginRequest;
                } catch (Exception eX) {
                    throw eX;
                    // Ignore!
                } finally {
                    this._bIsBRRegistered = false;
                    this._ehBeginRequest = null;
                }
                
            }
            
        }

        /// <summary>
        /// Validate the list of ip addresses, some can be single ip, others as range
        /// </summary>
        /// <param name="strIPWhiteList"></param>
        /// <returns></returns>
        /// <see cref="https://github.com/jsakamoto/ipaddressrange">IPAddress Range</see>
        private bool IsWhiteListValid(string[] strIPWhiteList)
        {
            _listIPAddress = new List<IPWhiteListEntry>();
            foreach (string ipAddr in strIPWhiteList) {
                if (_regIP.IsMatch(ipAddr)) {
                    _listIPAddress.Add(new IPWhiteListEntry { IPAddress = ipAddr, IsRange = false, RangeIPAddress = null });
                } else {
                    // possibly has CIDR notation or a range?
                    try {
                        IPAddressRange range = new IPAddressRange(ipAddr);
                        _listIPAddress.Add(new IPWhiteListEntry { IPAddress = ipAddr, IsRange = true, RangeIPAddress = range });
                    } catch (FormatException formEx) {

                    }
                }
            }
            return (_listIPAddress.Count == 0) ? false : true;
        }

        // See http://msdn.microsoft.com/en-us/library/bb470252.aspx#Stages
        /// <summary>
        /// Event Handler - **APPLICATION SCOPE**
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        /// <remarks>Checks the incoming IP address and appropriately handles it</remarks>
        private void Application_BeginRequest(object source, EventArgs e) {
            HttpContext httpCntxt = ((HttpApplication)source).Context;
            string ipAddy = httpCntxt.Request.UserHostAddress;
            if (!isAuthorizedIP(ipAddy)) {
                httpCntxt.Response.StatusCode = 403;
                if (this._strCustom403Msg.Length > 0) {
                    httpCntxt.Response.StatusDescription = this._strCustom403Msg;
                }
                // Here, we could redirect to AccessDenied page... this could serve as blacklister!!!!
                //httpCntxt.Server.Transfer("AccessDenied.aspx", false);
            }
        }

        /// <summary>
        /// Default method that is called upon registration and startup of ASP.NET site, if not registered, registers itself once and binds to event handler...
        /// </summary>
        /// <param name="context"></param>
        public void Init(HttpApplication context) {
            if (this._bIsSecured) {
                if (!_bIsBRRegistered) {
                    this._context = context;
                    this._context.BeginRequest += this._ehBeginRequest;
                    this._bIsBRRegistered = true;
                }
            }
        }

        /// <summary>
        /// Checks if IP Address is authorized or not by looking it up in the whitelist, converts IPv6 address to IPv4... yeah
        /// </summary>
        /// <param name="ipAddress">IP address (can be either IPv6 or IPv4 depending on server configuration)</param>
        /// <returns>True if Authorized, otherwise False</returns>
        private bool isAuthorizedIP(string ipAddress) {
            bool rv = false;
            string sIPv4 = GetIPv4Address(ipAddress);
            if (string.IsNullOrEmpty(sIPv4)) {
                // oh bummer! Cannot get IPv4 addy.... bummer
                rv = false; // should we go ahead... allow, methinks, deny!
            } else {
                foreach (IPWhiteListEntry entry in this._listIPAddress) {
                    if (!entry.IsRange){
                        if (entry.IPAddress.Equals(sIPv4)) {
                            rv = true;
                            // yeah baby....
                            break;
                        }
                    }else{
                        if (entry.RangeIPAddress.Contains(IPAddress.Parse(sIPv4))){
                            rv = true;
                            // yeah baby....
                            break;
                        }
                    }
                }
                
            }
            System.Diagnostics.Debug.WriteLine(String.Format("IP Address is {0} OR {1}", ipAddress, sIPv4));
            return rv;
        }

        /// <summary>
        /// Returns the IPv4 address of the specified host name or IP address.
        /// </summary>
        /// <param name="sHostNameOrAddress">The host name or IP address to resolve.</param>
        /// <returns>The first IPv4 address associated with the specified host name, or null.</returns>
        /// http://stackoverflow.com/a/18503572/206367
        public static string GetIPv4Address(string sHostNameOrAddress) {
            try {
                // Get the list of IP addresses for the specified host
                IPAddress[] aIPHostAddresses = Dns.GetHostAddresses(sHostNameOrAddress);

                // First try to find a real IPV4 address in the list
                foreach (IPAddress ipHost in aIPHostAddresses)
                    if (ipHost.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                        return ipHost.ToString();

                // If that didn't work, try to lookup the IPV4 addresses for IPV6 addresses in the list
                foreach (IPAddress ipHost in aIPHostAddresses)
                    if (ipHost.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6) {
                        IPHostEntry ihe = Dns.GetHostEntry(ipHost);
                        foreach (IPAddress ipEntry in ihe.AddressList)
                            if (ipEntry.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                                return ipEntry.ToString();
                    }
            } catch (Exception ex) {
                System.Diagnostics.Trace.WriteLine(ex);
            }
            return null;
        }

    }
    public class IPWhiteListEntry
    {
        public string IPAddress { get; set; }
        public bool IsRange { get; set; }
        public IPAddressRange RangeIPAddress { get; set; }
    }
}
