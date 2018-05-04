using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SpaServices.Webpack;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Driver;

namespace smutna_biedronka {
    public class Startup {
        public Startup(IConfiguration configuration) {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services) {
            services.AddSingleton<Try<IMongoDatabase>, TryMongoService>();
            services.AddTransient<IEnvironmentService, EnvironmentService>();
            services.AddTransient<IShell, ShellService>();
            services.AddSingleton<IProcessManager, ProcessManager>();
            services.AddSingleton<NewWebsocekthandler, NewWebsocekthandler>();

            ISocketServicec service = null;
            services.AddSingleton<ISocketServicec>(p => {
                if (service != null) {
                    return service;
                } else {
                    service = new SocketService();
                    return service;
                }
            });
            services.AddSingleton<IMonitorService, MonitorService>();
            services.AddMvc();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(
            IApplicationBuilder app,
            IHostingEnvironment env,
            IMonitorService monitor,
            NewWebsocekthandler websocketHandler,
            IProcessManager processManager,
            IApplicationLifetime applicationLifetime
            ) {

            applicationLifetime.ApplicationStopping.Register(() => {
                System.Console.WriteLine("kill all");
                processManager.KillAll();
            });
            monitor.Run();
            app.UseWebSockets();

            app.UseDeveloperExceptionPage();
            app.UseWebpackDevMiddleware(new WebpackDevMiddlewareOptions {
                HotModuleReplacement = true,
                ReactHotModuleReplacement = true
            });
            if (env.IsDevelopment()) {
            } else {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseMiddleware<ServeWebsocketMiddleware>();

            app.UseStaticFiles();

            app.UseMvc(routes => {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");

                routes.MapSpaFallbackRoute(
                    name: "spa-fallback",
                    defaults: new { controller = "Home", action = "Index" });
            });
        }
    }
}
