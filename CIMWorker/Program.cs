#region [ using ]
using CIMWorker.Helpers;
using CIMWorker.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.IO;
#endregion

namespace CIMWorker
{
   public class Program
   {
      #region [ Main ]
      public static void Main(string[] args)
      {
         var builder = new ConfigurationBuilder();
         BuildConfig(builder);
         CreateHostBuilder(args).Build().Run();
      }
      #endregion

      #region [ Build Config ]
      private static void BuildConfig(IConfigurationBuilder builder)
      {
         builder.SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
      }
      #endregion

      #region [ IHost Builder ]
      public static IHostBuilder CreateHostBuilder(string[] args)
      {
         return Host.CreateDefaultBuilder(args)
            .UseWindowsService()
            .UseDefaultServiceProvider(options => options.ValidateScopes = false)
            .ConfigureServices((hostContext, services) =>
            {
               IConfiguration config = hostContext.Configuration;

               services.Configure<AppSettings>(config.GetSection("AppSettings"));

               services.AddTransient<IDataService, DataService>();
               services.AddTransient<IDiallerService, DiallerService>();
               services.AddTransient<IEmailService, EmailService>();
               services.AddTransient<IFileService, FileService>();
               services.AddTransient<ILogService, LogService>();
               services.AddTransient<IRuleService, RuleService>();
               services.AddTransient<ISchedulerService, SchedulerService>();

               services.AddHostedService<Worker>();
            });
      }
      #endregion
   }
}