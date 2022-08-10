#region [ using ]
using CIMWorker.Controllers;
using CIMWorker.Helpers;
using CIMWorker.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
#endregion

namespace CIMWorker
{
   public class Worker : BackgroundService
   {
      #region [ Variables ]
      private readonly ILogService _logService;
      private readonly ISchedulerService _schedulerService;

      private readonly DataQueueController _dataQueueController;
      private readonly ImportDatabaseController _importDatabaseController;
      private readonly ReportingController _reportingController;
      private readonly ImportFileController _importFileController;
      #endregion

      #region [ Default Constructor ]
      public Worker(IDataService dataService, IDiallerService diallerService, IEmailService emailService, IFileService fileService, ILogService logService, ISchedulerService schedulerService, IRuleService ruleService, IOptions<AppSettings> appSettings)
      {
         _schedulerService = schedulerService;
         _logService = logService;
         _dataQueueController = new DataQueueController(appSettings, dataService, diallerService, logService);
         _importDatabaseController = new ImportDatabaseController(appSettings, dataService, diallerService, logService, emailService, ruleService);
         _reportingController = new ReportingController(appSettings, dataService, diallerService, logService, emailService, ruleService);
            _importFileController = new ImportFileController(dataService, fileService, logService, emailService, diallerService);
      }
      #endregion

      //-----------------------------//

      #region [ Start Async ]
      public override Task StartAsync(CancellationToken cancellationToken)
      {
         try
         {
            _logService.Info("*** Application Service has been started");
         }
         catch (Exception ex)
         {
            _logService.Error(GetType().Name, MethodBase.GetCurrentMethod(), ex);
         }

         return base.StartAsync(cancellationToken);
      }
      #endregion

      #region [ Stop Async ]
      public override Task StopAsync(CancellationToken cancellationToken)
      {
         try
         {
            _logService.Info("*** Application Service has been stopped");
         }
         catch (Exception ex)
         {
            _logService.Error(GetType().Name, MethodBase.GetCurrentMethod(), ex);
         }

         return base.StopAsync(cancellationToken);
      }
      #endregion

      #region [ Execute Async ]
      protected override async Task ExecuteAsync(CancellationToken stoppingToken)
      {
         while (!stoppingToken.IsCancellationRequested)
         {
            try
            {
               if (_schedulerService.IsActive())
               {
                  await _logService.InfoAsync("--------------------------------------Application Started (Active)");

                        bool IsStep01Done = await _dataQueueController.MasterAsync();
                        //bool IsStep01Done = await _importDatabaseController.MasterAsync();
                        bool IsStep02Done = await _importFileController.MasterAsync();
                        await _logService.InfoAsync("---------------------End Cycle");
                  await Task.Delay(_schedulerService.GetInterval(), stoppingToken);
               }
               else
               {
                  await _logService.InfoAsync("Not Active");
                  await Task.Delay(_schedulerService.GetDefault(), stoppingToken);
               }
            }
            catch (Exception ex)
            {
               await _logService.ErrorAsync(GetType().Name, MethodBase.GetCurrentMethod(), ex);
                    Console.WriteLine(ex.Message);
                    Console.ReadLine();
            }
         }
      }
      #endregion
   }
}