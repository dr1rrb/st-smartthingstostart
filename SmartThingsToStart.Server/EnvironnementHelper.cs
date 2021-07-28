using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace SmartThingsToStart.Server
{
    public static class EnvironnementHelper
    {
	    public static IWebHostBuilder ConfigureHost(IWebHostBuilder builder)
		{
#if DEBUG && false
			return builder.UseUrls("http://0.0.0.0:5001");
#else
			return builder;
#endif

		}

		public static IApplicationBuilder ConfigureApplication(IApplicationBuilder app)
		{
#if DEBUG || true // Https requirement disabled for azure free hosting
			return app;
#else
			return app.Use(async (context, next) =>
	        {
		        if (context.Request.IsHttps)
		        {
			        await next();
		        }
		        else
		        {
			        //var query = new Uri(context.Request.GetEncodedUrl()).GetComponents(UriComponents.AbsoluteUri & ~UriComponents.Scheme, UriFormat.SafeUnescaped);
			        //context.Response.Redirect($"https://{query}");
			        context.Response.Redirect($"https://{context.Request.Host}{context.Request.Path}");
		        }
	        });
#endif

		}

		public static string GetServerUri(HttpRequest request)
		{
		    return Constants.ProxyBaseUri;
		}
	}
}
