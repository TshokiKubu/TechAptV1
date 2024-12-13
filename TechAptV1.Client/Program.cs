using Microsoft.EntityFrameworkCore;
using Serilog;
using TechAptV1.Client.Components;
using TechAptV1.Client.Services;

namespace TechAptV1.Client
{
    public class Program
    {
        public static void Main(string[] args)
        {
            try
            {
                Console.Title = "Tech Apt V1";

                var builder = WebApplication.CreateBuilder(args);

                // Configure Serilog for logging
                Log.Logger = new LoggerConfiguration()
                    .ReadFrom.Configuration(builder.Configuration)
                    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
                    .CreateLogger();

                builder.Host.UseSerilog();

                // Add DbContext with SQLite configuration
                builder.Services.AddDbContext<DataContext>(options =>
                    options.UseSqlite(builder.Configuration.GetConnectionString("SQLiteConnection")));

                // Register services
                builder.Services.AddScoped<ThreadingService>();
                builder.Services.AddScoped<DataService>();

                // Add Razor Components with interactive server support
                builder.Services.AddRazorComponents().AddInteractiveServerComponents();

                var app = builder.Build();

                //builder.Services.AddHttpsRedirection(options =>
                //{
                //    options.HttpsPort = 443; // Specify the HTTPS port (adjust if not 443)
                //});


                // Configure the HTTP request pipeline
                if (!app.Environment.IsDevelopment())
                {
                    app.UseExceptionHandler("/Error");
                    app.UseHsts();
                }

                app.UseHttpsRedirection();
                app.UseStaticFiles();
                app.UseRouting();
                app.UseAntiforgery();

                app.MapRazorComponents<App>().AddInteractiveServerRenderMode();              
              
              
                app.Run();
            }
            catch (Exception exception)
            {
                Log.Fatal(exception, "Fatal exception in Program");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }
    }
}
