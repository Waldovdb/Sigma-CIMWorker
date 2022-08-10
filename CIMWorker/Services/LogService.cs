#region [ using ]
using CIMWorker.Helpers;
using Microsoft.Extensions.Options;
using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
#endregion

namespace CIMWorker.Services
{
   #region [ Interface ]
   public interface ILogService
   {
      Task InfoAsync(string message);
      void Info(string message);

      Task ErrorAsync(string className, MethodBase method, Exception error);
      void Error(string className, MethodBase method, Exception error);
   }
   #endregion

   //--------------------------------------------//

   public class LogService : ILogService
   {
      private readonly AppSettings _appSettings;

      #region [ Default Constructor ]
      public LogService(IOptions<AppSettings> appSettings)
      {
         _appSettings = appSettings.Value;
      }
      #endregion

      //-----------------------------//

      #region [ Info Async ]
      public async Task InfoAsync(string message)
      {
         try
         {
            using (StreamWriter outputFile = new StreamWriter(Path.Combine(_appSettings.WorkerLogs.Process, $"CIM Worker Process {DateTime.Now:dd-MM-yyyy}.txt"), append: true))
            {
               await outputFile.WriteLineAsync($"{DateTime.Now:HH:mm:ss} -> {message}");
            }

            Console.WriteLine($"{DateTime.Now:HH:mm:ss} -> {message}");
         }
         catch (Exception ex)
         {
            await ErrorAsync(GetType().Name, MethodBase.GetCurrentMethod(), ex);
         }
      }
      #endregion

      #region [ Info ]
      public void Info(string message)
      {
         try
         {
            using (StreamWriter outputFile = new StreamWriter(Path.Combine(_appSettings.WorkerLogs.Process, $"CIM Worker Process {DateTime.Now:dd-MM-yyyy}.txt"), append: true))
            {
               outputFile.WriteLine($"{DateTime.Now:HH:mm:ss} -> {message}");
            }

            Console.WriteLine($"{DateTime.Now:HH:mm:ss} -> {message}");
         }
         catch (Exception ex)
         {
            Error(GetType().Name, MethodBase.GetCurrentMethod(), ex);
         }
      }
      #endregion

      #region [ Error Async ]
      public async Task ErrorAsync(string className, MethodBase method, Exception error)
      {
         try
         {
            using (StreamWriter outputFile = new StreamWriter(Path.Combine(_appSettings.WorkerLogs.Error, $"CIM Worker Error {DateTime.Now:dd-MM-yyyy}.txt"), append: true))
            {
               await outputFile.WriteLineAsync($"Error: {DateTime.Now:HH:mm:ss} -> {error.Message}");
            }
         }
         catch (Exception)
         {
            // Do Nothing
         }

         Console.WriteLine($"Error: {DateTime.Now:HH:mm:ss} -> {error.Message}");
      }
      #endregion

      #region [ Error ]
      public void Error(string className, MethodBase method, Exception error)
      {
         try
         {
            using (StreamWriter outputFile = new StreamWriter(Path.Combine(_appSettings.WorkerLogs.Error, $"CIM Worker Error {DateTime.Now:dd-MM-yyyy}.txt"), append: true))
            {
               outputFile.WriteLine($"Error: {DateTime.Now:HH:mm:ss} -> {error.Message}");
            }
         }
         catch (Exception)
         {
            // Do Nothing
         }

         Console.WriteLine($"Error: {DateTime.Now:HH:mm:ss} -> {error.Message}");
      }
      #endregion
   }
}