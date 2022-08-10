#region [ using ]
using CIMWorker.Helpers;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
#endregion

namespace CIMWorker.Services
{
   #region [ Interface ]
   public interface ISchedulerService
   {
      bool IsActive();
      int GetDefault();
      int GetInterval();
   }
   #endregion

   //--------------------------------------------//

   public class SchedulerService : ISchedulerService
   {
      private readonly AppSettings _appSettings;

      #region [ Default Constructor ]
      public SchedulerService(IOptions<AppSettings> appSettings)
      {
         _appSettings = appSettings.Value;
      }
      #endregion

      //-----------------------------//

      #region [ Is Active ]
      public bool IsActive()
      {
         string DayOfWeek = DateTime.Now.ToString("dddd");
         int year = DateTime.Now.Year;
         int month = DateTime.Now.Month;
         int day = DateTime.Now.Day;

         try
         {
            #region [ Monday ]
            if (DayOfWeek == "Monday")
            {
               if (_appSettings.Scheduler.Monday.Active)
               {
                  string[] start = _appSettings.Scheduler.Monday.Start.Split(':').ToArray();
                  string[] end = _appSettings.Scheduler.Monday.End.Split(':').ToArray();

                  DateTime StartTime = new DateTime(year, month, day, int.Parse(start[0]), int.Parse(start[1]), 0);
                  DateTime EndTime = new DateTime(year, month, day, int.Parse(end[0]), int.Parse(end[1]), 0);

                  return (DateTime.Now >= StartTime && DateTime.Now <= EndTime);
               }
               else
               {
                  return false;
               }
            }
            #endregion

            #region [ Tuesday ]
            if (DayOfWeek == "Tuesday")
            {
               if (_appSettings.Scheduler.Tuesday.Active)
               {
                  string[] start = _appSettings.Scheduler.Tuesday.Start.Split(':').ToArray();
                  string[] end = _appSettings.Scheduler.Tuesday.End.Split(':').ToArray();

                  DateTime StartTime = new DateTime(year, month, day, int.Parse(start[0]), int.Parse(start[1]), 0);
                  DateTime EndTime = new DateTime(year, month, day, int.Parse(end[0]), int.Parse(end[1]), 0);

                  return (DateTime.Now >= StartTime && DateTime.Now <= EndTime);
               }
               else
               {
                  return false;
               }
            }
            #endregion

            #region [ Wednesday ]
            if (DayOfWeek == "Wednesday")
            {
               if (_appSettings.Scheduler.Wednesday.Active)
               {
                  string[] start = _appSettings.Scheduler.Wednesday.Start.Split(':').ToArray();
                  string[] end = _appSettings.Scheduler.Wednesday.End.Split(':').ToArray();

                  DateTime StartTime = new DateTime(year, month, day, int.Parse(start[0]), int.Parse(start[1]), 0);
                  DateTime EndTime = new DateTime(year, month, day, int.Parse(end[0]), int.Parse(end[1]), 0);

                  return (DateTime.Now >= StartTime && DateTime.Now <= EndTime);
               }
               else
               {
                  return false;
               }
            }
            #endregion

            #region [ Thursday ]
            if (DayOfWeek == "Thursday")
            {
               if (_appSettings.Scheduler.Thursday.Active)
               {
                  string[] start = _appSettings.Scheduler.Thursday.Start.Split(':').ToArray();
                  string[] end = _appSettings.Scheduler.Thursday.End.Split(':').ToArray();

                  DateTime StartTime = new DateTime(year, month, day, int.Parse(start[0]), int.Parse(start[1]), 0);
                  DateTime EndTime = new DateTime(year, month, day, int.Parse(end[0]), int.Parse(end[1]), 0);

                  return (DateTime.Now >= StartTime && DateTime.Now <= EndTime);
               }
               else
               {
                  return false;
               }
            }
            #endregion

            #region [ Friday ]
            if (DayOfWeek == "Friday")
            {
               if (_appSettings.Scheduler.Friday.Active)
               {
                  string[] start = _appSettings.Scheduler.Friday.Start.Split(':').ToArray();
                  string[] end = _appSettings.Scheduler.Friday.End.Split(':').ToArray();

                  DateTime StartTime = new DateTime(year, month, day, int.Parse(start[0]), int.Parse(start[1]), 0);
                  DateTime EndTime = new DateTime(year, month, day, int.Parse(end[0]), int.Parse(end[1]), 0);

                  return (DateTime.Now >= StartTime && DateTime.Now <= EndTime);
               }
               else
               {
                  return false;
               }
            }
            #endregion

            #region [ Saturday ]
            if (DayOfWeek == "Saturday")
            {
               if (_appSettings.Scheduler.Saturday.Active)
               {
                  string[] start = _appSettings.Scheduler.Saturday.Start.Split(':').ToArray();
                  string[] end = _appSettings.Scheduler.Saturday.End.Split(':').ToArray();

                  DateTime StartTime = new DateTime(year, month, day, int.Parse(start[0]), int.Parse(start[1]), 0);
                  DateTime EndTime = new DateTime(year, month, day, int.Parse(end[0]), int.Parse(end[1]), 0);

                  return (DateTime.Now >= StartTime && DateTime.Now <= EndTime);
               }
               else
               {
                  return false;
               }
            }
            #endregion

            #region [ Sunday ]
            if (DayOfWeek == "Sunday")
            {
               if (_appSettings.Scheduler.Sunday.Active)
               {
                  string[] start = _appSettings.Scheduler.Sunday.Start.Split(':').ToArray();
                  string[] end = _appSettings.Scheduler.Sunday.End.Split(':').ToArray();

                  DateTime StartTime = new DateTime(year, month, day, int.Parse(start[0]), int.Parse(start[1]), 0);
                  DateTime EndTime = new DateTime(year, month, day, int.Parse(end[0]), int.Parse(end[1]), 0);

                  return (DateTime.Now >= StartTime && DateTime.Now <= EndTime);
               }
               else
               {
                  return false;
               }
            }
            #endregion
         }
         catch (Exception)
         {

         }

         return false;
      }
      #endregion

      #region [ Get Default ]
      public int GetDefault()
      {
         return _appSettings.Scheduler.Default;
      }
      #endregion

      #region [ Get Interval ]
      public int GetInterval()
      {
         try
         {
            return DateTime.Now.ToString("dddd") switch
            {
               "Monday" => _appSettings.Scheduler.Monday.Interval,
               "Tuesday" => _appSettings.Scheduler.Tuesday.Interval,
               "Wednesday" => _appSettings.Scheduler.Wednesday.Interval,
               "Thursday" => _appSettings.Scheduler.Thursday.Interval,
               "Friday" => _appSettings.Scheduler.Friday.Interval,
               "Saturday" => _appSettings.Scheduler.Saturday.Interval,
               "Sunday" => _appSettings.Scheduler.Sunday.Interval,
               _ => 0,
            };
         }
         catch (Exception)
         {
            return 0;
         }
      }
      #endregion
   }
}