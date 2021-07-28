using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SmartThingsToStart.Entities;
using SmartThingsToStart.Server.Business;
using SmartThingsToStart.Server.Controllers;
using SmartThingsToStart.Server.Entities;

namespace SmartThingsToStart.Server
{
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();
            Configuration = builder.Build();
        }

        public IConfigurationRoot Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // Add framework services.
            services.AddMvc();
			services.AddSingleton<IWnsAuthenticationService, WnsAuthenticationService>();
	        services.AddSingleton<ISecurityService<AppToProxyToken>>(s => new AesSecurityService<AppToProxyToken>(
				SmartThingsToStart.Constants.AppToProxyTokenPwd,
				SmartThingsToStart.Constants.AppToProxyTokenSalt));
			services.AddSingleton<ISecurityService<StToProxyToken>>(s => new AesSecurityService<StToProxyToken>(
				SmartThingsToStart.Constants.StToProxyTokenPwd,
				SmartThingsToStart.Constants.StToProxyTokenSalt));
		}

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();

	        app = EnvironnementHelper.ConfigureApplication(app);

            app
				.UseMvc()
				.UseStaticFiles();
        }
    }
}
