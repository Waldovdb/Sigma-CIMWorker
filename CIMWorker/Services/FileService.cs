#region [ using ]
using CIMWorker.Data.Entities;
using CIMWorker.Helpers;
using CIMWorker.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlTypes;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
#endregion

namespace CIMWorker.Services
{
   #region [ Interface ]
   public interface IFileService
   {
      Task<List<ImportDataFile>> GetFileConfigurations();
      Task<List<FileInfo>> GetFileList(string FileLocation, string Partial);
      Task<bool> ValidateHeader(FileInfo file, ImportDataFile model);
      Task<DataTable> SetDataTableSchema(string TableName);
      Task<string[]> GetDelimiterLine(string RawFileLine, ImportDataFile model, int LineID);

      bool LogFileDataTable(DataTable model, string Table);
      Task<bool> AddToQueue(List<PhoneQueue> phoneCollection);

      Task<bool> CloseTextFile(FileInfo file);
   }
   #endregion

   //--------------------------------------------//

   public class FileService : IFileService
   {
      private readonly AppSettings _appSettings;
      private readonly ILogService _logService;
      private readonly IDataService _dataService;

      #region [ Default Constructor ]
      public FileService(IOptions<AppSettings> appSettings, ILogService logService, IDataService dataService)
      {
         _appSettings = appSettings.Value;
         _logService = logService;
         _dataService = dataService;
      }
      #endregion

      //-----------------------------//

      #region [ Get File Configurations ]
      public async Task<List<ImportDataFile>> GetFileConfigurations()
      {
         List<ImportDataFile> collection = new List<ImportDataFile>();
         try
         {
            collection = await _dataService.SelectMany<ImportDataFile, dynamic>("SELECT * FROM [ImportDataFile]", new { });
         }
         catch (Exception ex)
         {
            await _logService.ErrorAsync(GetType().Name, MethodBase.GetCurrentMethod(), ex);
         }
         return collection;
      }
      #endregion

      #region [ Get File List ]
      public async Task<List<FileInfo>> GetFileList(string FileLocation, string Partial)
      {
         List<FileInfo> FilesToProcess = new List<FileInfo>();

         DirectoryInfo folder = new DirectoryInfo(FileLocation);
         FileInfo[] files = folder.GetFiles("*.*");

         foreach (FileInfo file in files.Where(x => x.Name.Contains(Partial)))
         {
            try
            {
               string source = Path.Combine(file.FullName);
               string destination = Path.Combine(_appSettings.FileDirectory.Busy, file.Name);

               if (!file.IsReadOnly)
               {
                  if (File.Exists(destination))
                     File.Delete(destination);

                  File.Move(source, destination);
                  await _logService.InfoAsync($"File Moved from SFTP to Busy : {file.Name}");
               }
               else
               {
                  await _logService.InfoAsync($"File Is Read Only in SFTP : {file.Name}");
               }
            }
            catch (Exception ex)
            {
               await _logService.ErrorAsync(GetType().Name, MethodBase.GetCurrentMethod(), ex);
            }
         }

         folder = new DirectoryInfo(_appSettings.FileDirectory.Busy);
         files = folder.GetFiles("*.*");

         foreach (FileInfo file in files.Where(x => x.Name.Contains(Partial)))
         {
            try
            {
               if (!file.IsReadOnly)
               {
                  FilesToProcess.Add(file);
                  await _logService.InfoAsync($"File Received in Busy : {file.Name}");
               }
               else
               {
                  await _logService.InfoAsync($"File Is Read Only in Busy : {file.Name}");
               }
            }
            catch (Exception ex)
            {
               await _logService.ErrorAsync(GetType().Name, MethodBase.GetCurrentMethod(), ex);
            }
         }

         return FilesToProcess;
      }
      #endregion

      #region [ Validate Header ]
      public async Task<bool> ValidateHeader(FileInfo file, ImportDataFile model)
      {
         try
         {
            if (model.HasHeader)
            {
               string Header = File.ReadLines(file.FullName).First();
               if (!string.IsNullOrEmpty(Header))
               {
                  if (!string.IsNullOrEmpty(model.HeaderText))
                  {
                     bool Pass = (Header == model.HeaderText) ? true : false;
                     if (Pass == false)
                     {
                        await _logService.InfoAsync($"File Header Failed (Header Text) : {file.Name}");
                        return false;
                     }
                     else
                     {
                        await _logService.InfoAsync($"File Header Passed (Header Text) : {file.Name}");
                        return true;
                     }
                  }
                  else
                  {
                     bool Pass = (Header.Length == model.HeaderChars) ? true : false;
                     if (Pass == false)
                     {
                        await _logService.InfoAsync($"File Header Failed (Header Characters) : {file.Name}");
                        return false;
                     }
                     else
                     {
                        await _logService.InfoAsync($"File Header Passed (Header Characters) : {file.Name}");
                        return true;
                     }
                  }
               }
               else
               {
                  await _logService.InfoAsync($"File is Blank / Empty : {file.Name}");
                  return false;
               }
            }
            else
            {
               return true;
            }
         }
         catch (Exception ex)
         {
            await _logService.ErrorAsync(GetType().Name, MethodBase.GetCurrentMethod(), ex);
            return false;
         }
      }
      #endregion

      #region [ Set Data Table Schema ]
      public async Task<DataTable> SetDataTableSchema(string TableName)
      {
         DataTable table = new DataTable();
         try
         {
            string query = @"SELECT [COLUMN_NAME],[ORDINAL_POSITION],[DATA_TYPE] FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = @TableName ORDER BY [ORDINAL_POSITION]";
            List<DataSchema> schema = await _dataService.SelectMany<DataSchema, dynamic>(query, new { TableName });

            foreach (var item in schema)
            {
               if (item.DATA_TYPE.ToLower() == "char")
               {
                  table.Columns.Add(item.COLUMN_NAME, typeof(string));
                  continue;
               }

               if (item.DATA_TYPE.ToLower() == "int")
               {
                  table.Columns.Add(item.COLUMN_NAME, typeof(int));
                  continue;
               }

               if (item.DATA_TYPE.ToLower().Contains("numeric"))
               {
                  table.Columns.Add(item.COLUMN_NAME, typeof(decimal));
                  continue;
               }

               if (item.DATA_TYPE.ToLower().Contains("money"))
               {
                  table.Columns.Add(item.COLUMN_NAME, typeof(decimal));
                  continue;
               }

               if (item.DATA_TYPE.ToLower() == "datetime")
               {
                  table.Columns.Add(item.COLUMN_NAME, typeof(DateTime));
                  continue;
               }
               else
               {
                  table.Columns.Add(item.COLUMN_NAME, typeof(string));
                  continue;
               }
            }
         }
         catch (Exception ex)
         {
            await _logService.ErrorAsync(GetType().Name, MethodBase.GetCurrentMethod(), ex);
         }

         return table;
      }
      #endregion

      #region [ Build Delimiter ]
      public async Task<string[]> GetDelimiterLine(string RawFileLine, ImportDataFile model, int LineID)
      {
         try
         {
            RawFileLine = RawFileLine.Replace('\t', ' ').Replace('\r', ' ').Replace('\n', ' ').Replace('\\', ' ');
            return RawFileLine.Split(model.Delimiter).ToArray();
         }
         catch (Exception ex)
         {
            await _logService.ErrorAsync(GetType().Name, MethodBase.GetCurrentMethod(), ex);
            return new string[] { };
         }
      }
      #endregion

      #region [ Add To Queue ]
      public async Task<bool> AddToQueue(List<PhoneQueue> phoneCollection)
      {
         string query = @"INSERT INTO [dbo].[PhoneQueue]
                        (
	                        [Command],[Input],[InputName],[Status],[Received],[NextExecute],[Actioned],[RetryCount],[RetryDate],
	                        [PersonID],[ExternalID],[SourceID],[ServiceID],[LoadID],[Name],[Phone],[ScheduleDate],[Priority],
	                        [CapturingAgent],[Phone01],[Phone02],[Phone03],[Phone04],[Phone05],[Phone06],[Phone07],[Phone08],[Phone09],[Phone10],
	                        [Comments],[CustomData1],[CustomData2],[CustomData3],[CallerID],[CallerName]
                        )
                        VALUES
                        (
                           @Command,@Input,@InputName,@Status,@Received,@NextExecute,@Actioned,@RetryCount,@RetryDate,
	                        @PersonID,@ExternalID,@SourceID,@ServiceID,@LoadID,@Name,@Phone,@ScheduleDate,@Priority,
	                        @CapturingAgent,@Phone1,@Phone2,@Phone3,@Phone4,@Phone5,@Phone6,@Phone7,@Phone8,@Phone9,@Phone10,
	                        @Comments,@CustomData1,@CustomData2,@CustomData3,@CallerID,@CallerName
                        )";

         bool IsDone = await _dataService.InsertMany<PhoneQueue, PhoneQueue>(query, phoneCollection);
         return IsDone;
      }
      #endregion

      #region [ Close Text File ]
      public async Task<bool> CloseTextFile(FileInfo file)
      {
         try
         {
            if (!file.IsReadOnly)
            {
               string source = Path.Combine(file.FullName);
               string destination = Path.Combine(_appSettings.FileDirectory.Done, file.Name);

               if (File.Exists(destination))
                  File.Delete(destination);

               File.Move(source, destination);
               await _logService.InfoAsync($"File Moved from Busy to Done : {file.Name}");
            }
            else
            {
               await _logService.InfoAsync($"File Is Read Only in Busy : {file.Name}");
            }
         }
         catch (Exception ex)
         {
            await _logService.ErrorAsync(GetType().Name, MethodBase.GetCurrentMethod(), ex);
         }
         return true;
      }
      #endregion

      //-----------------------------//

      #region [ Log File Data Table ]
      public bool LogFileDataTable(DataTable model, string Table)
      {
         try
         {
            Table = Table.Replace("[dbo].", "").Replace("[", "").Replace("]", "");
            Table = string.Format("[dbo].[{0}]", Table);

            using (SqlBulkCopy SqlBulk = new SqlBulkCopy(_dataService.GetConnectionString("InovoCIM")))
            {
               SqlBulk.DestinationTableName = Table;
               SqlBulk.BatchSize = 9500;
               SqlBulk.WriteToServer(model);
            }
            return true;
         }
         catch (Exception ex)
         {
            _logService.Error(GetType().Name, MethodBase.GetCurrentMethod(), ex);
            return false;
         }
      }
      #endregion

      //-----------------------------//
   }
}