#if DEBUG
#define DEV_APP             // Target the DEVELOPPEMENT store application ID
#define DEV_PROXY           // Target the proxy running on local machine
#define DEV_SMARTTHINGS     // Authenticates to the "Simulator" smartthings installation
#endif

using System;
using System.Collections.Generic;
using System.Text;

namespace SmartThingsToStart
{
	internal static class Constants
    {
		public const string StAuthRedirectUri = "http://smartthingstostart.torick.net/";

#if DEV_PROXY
        public const string ProxyBaseUri = "http://smartthingstostartproxydev.azurewebsites.net/";
#else
		public const string ProxyBaseUri = "http://smartthingstostartproxy.azurewebsites.net/";
#endif

#if DEV_SMARTTHINGS
        public const string VaultKey = "smartthings.DEV";
#else
        public const string VaultKey = "smartthings";
#endif

        public const string AppId = "{58A061B9-51C1-49ED-90EC-3088A4E094CB}";
		public const string AppToProxyTokenPwd = @"AppToProxyTokenPwd";
		public const string AppToProxyTokenSalt = @"AppToProxyTokenSalt";

#if SERVER // Do not include in application package private secrets of the server
      	public const string StToProxyTokenPwd = @"StToProxyTokenPwd";
		public const string StToProxyTokenSalt = @"StToProxyTokenSalt";

#if DEV_APP
	    public const string ProxyToWnsId = "ms-app://s-1-15-2-ProxyToWnsId";
	    public const string ProxyToWnsSecret = "ProxyToWnsSecret";
#else
   	    public const string ProxyToWnsId = "ms-app://S-1-15-2-ProxyToWnsId";
	    public const string ProxyToWnsSecret = "ProxyToWnsSecret";
#endif

#endif
	}
}
