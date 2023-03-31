using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SignalRServer.Hubs;

namespace SignalRServer
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddCors();

            services.AddSignalR();

            services.AddSingleton<GameManager>();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseCors(builder =>
            {
                builder.WithOrigins("http://localhost:5544")
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials();
                builder.WithOrigins("https://cancer.scie.dev")
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials();
                builder.WithOrigins("https://cancercn.scie.dev")
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials();
            });

            // Provider mappings needed for Brotli compression format from Unity publishing settings
            var provider = new FileExtensionContentTypeProvider();
            provider.Mappings.Add(".unityweb", "application/octet-stream");
            provider.Mappings.Add(".data", "application/data");

            app.UseFileServer(new FileServerOptions
            {
                StaticFileOptions = { ContentTypeProvider = provider },
                EnableDirectoryBrowsing = true,
            });

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapHub<MainHub>("/MainHub");
            });
        }
    }
}
