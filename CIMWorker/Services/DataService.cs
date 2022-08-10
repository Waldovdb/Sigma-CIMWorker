#region [ using ]
using CIMWorker.Helpers;
using CIMWorker.Models;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
#endregion

namespace CIMWorker.Services
{
   #region [ Interface ]
   public interface IDataService
   {
      string GetConnectionString(string Type);

      Task<long> CountAsync<T>(string Table) where T : class;

      Task<bool> InsertSingle<T, U>(string Query, U Input) where T : class;

        Task<bool> InsertSinglePresence<T, U>(string Query, U Input) where T : class;
        Task<bool> InsertMany<T, U>(string Query, List<U> InputList) where T : class;

      Task<T> SelectSingle<T, U>(string Query, U Input) where T : class, new();
        Task<T> SelectSinglePresence<T, U>(string Query, U Input) where T : class, new();

        Task<List<T>> SelectMany<T, U>(string Query, U Input) where T : class, new();

      Task<bool> UpdateSingle<T, U>(string Query, U Input) where T : class;
      Task<bool> DeleteSingle<T, U>(string Query, U Input) where T : class;

        Task<bool> StoredProcLong<T, U>(string Query, U Input) where T : class;

      Task<bool> DeleteCustom(string Query, string Connection);

      bool Truncate(string Table);
      bool Truncate(string Table, string Connection);

      bool BulkUpload(DataTable model, string Table);
      bool BulkUpload(DataTable model, string Table, string Connection);

      Task<List<string>> MapTableColumns(string Connection, string TableName);

      void CopyTable(string sourceConnection, string destinationConnection, string tableNameWithSchema, string DestinationTableName, string[] columns);
   }
   #endregion

   //--------------------------------------------//

   public class DataService : IDataService
   {
      private readonly AppSettings _appSettings;
      private readonly string _dbConnection;

      #region [ Default Constructor ]
      public DataService(IOptions<AppSettings> appSettings)
      {
         _appSettings = appSettings.Value;
         _dbConnection = $"Data Source={_appSettings.DataSettings.Server};Initial Catalog={_appSettings.DataSettings.Database};User Id={_appSettings.DataSettings.User};Password={_appSettings.DataSettings.Password};";
      }
      #endregion

      //-----------------------------//

      #region [ Get Connection String ]
      public string GetConnectionString(string Type)
      {
         return Type switch
         {
            "Reporting" => $"Data Source={_appSettings.ReportSettings.Server};Initial Catalog={_appSettings.ReportSettings.Database};User Id={_appSettings.ReportSettings.User};Password={_appSettings.ReportSettings.Password};",
            "Presence" => $"Data Source={_appSettings.DiallerSettings.Server};Initial Catalog={_appSettings.DiallerSettings.Database};User Id={_appSettings.DiallerSettings.User};Password={_appSettings.DiallerSettings.Password};",
            _ => $"Data Source={_appSettings.DataSettings.Server};Initial Catalog={_appSettings.DataSettings.Database};User Id={_appSettings.DataSettings.User};Password={_appSettings.DataSettings.Password};",
         };
      }
      #endregion

      #region [ Count Async ]
      public async Task<long> CountAsync<T>(string Table) where T : class
      {
         long total = 0;
         try
         {
            using var conn = new SqlConnection(_dbConnection);
            total = await conn.ExecuteScalarAsync<long>($"SELECT COUNT(*) FROM {Table}");
            return total;
         }
         catch (Exception ex)
         {
            throw ex;
         }
      }
      #endregion

      //------------------//

      #region [ Insert Single ]
      public async Task<bool> InsertSingle<T, U>(string Query, U Input) where T : class
      {
         try
         {
            if (Input != null)
            {
               using var conn = new SqlConnection(_dbConnection);
               await conn.ExecuteAsync(Query, Input, commandTimeout: 1500);
               return true;
            }

            return false;
         }
         catch (Exception ex)
         {
            throw ex;
         }
      }
        #endregion

        #region [ Insert Single Presence ]
        public async Task<bool> InsertSinglePresence<T, U>(string Query, U Input) where T : class
        {
            try
            {
                if (Input != null)
                {
                    using var conn = new SqlConnection(GetConnectionString("Presence"));
                    await conn.ExecuteAsync(Query, Input, commandTimeout: 1500);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        #endregion

        #region [ Insert Many ]
        public async Task<bool> InsertMany<T, U>(string Query, List<U> InputList) where T : class
      {
         if (InputList.Count > 0)
         {
            foreach (var item in InputList)
            {
               try
               {
                  using var conn = new SqlConnection(_dbConnection);
                  await conn.ExecuteAsync(Query, item, commandTimeout: 1500);
               }
               catch (Exception ex)
               {
                  string error = ex.Message;
                  continue;
               }
            }
            return true;
         }
         return false;
      }
      #endregion

      //------------------//

      #region [ Select Single ]
      public async Task<T> SelectSingle<T, U>(string Query, U Input) where T : class, new()
      {
         try
         {
            using var conn = new SqlConnection(_dbConnection);
            var data = await conn.QueryAsync<T>(Query, Input, commandTimeout: 1500);
            return data.FirstOrDefault();
         }
         catch (Exception ex)
         {

         }
         return new T();
      }
        #endregion

        #region [ Select Single ]
        public async Task<T> SelectSinglePresence<T, U>(string Query, U Input) where T : class, new()
        {
            try
            {
                using var conn = new SqlConnection(GetConnectionString("Presence"));
                var data = await conn.QueryAsync<T>(Query, Input, commandTimeout: 1500);
                return data.FirstOrDefault();
            }
            catch (Exception ex)
            {

            }
            return new T();
        }
        #endregion

        #region [ Select Many ]
        public async Task<List<T>> SelectMany<T, U>(string Query, U Input) where T : class, new()
      {
         try
         {
            using var conn = new SqlConnection(_dbConnection);
            var data = await conn.QueryAsync<T>(Query, Input, commandTimeout: 1500);
            return data.ToList();
         }
         catch (Exception ex)
         {
            string error = ex.Message;
         }
         return null;
      }
      #endregion

      //------------------//

      #region [ Update Single ]
      public async Task<bool> UpdateSingle<T, U>(string Query, U Input) where T : class
      {
         try
         {
            if (Input != null)
            {
               using var conn = new SqlConnection(_dbConnection);
               await conn.ExecuteAsync(Query, Input, commandTimeout: 1500);
               return true;
            }

            return false;
         }
         catch (Exception ex)
         {
            throw ex;
         }
      }
        #endregion

        #region [ Update Single ]
        public async Task<bool> StoredProcLong<T, U>(string Query, U Input) where T : class
        {
            try
            {
                if (Input != null)
                {
                    using var conn = new SqlConnection(_dbConnection);
                    await conn.ExecuteAsync(Query, Input, commandTimeout: 3600);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        #endregion

        //------------------//

        #region [ Delete Single ]
        public async Task<bool> DeleteSingle<T, U>(string Query, U Input) where T : class
      {
         try
         {
            if (Input != null)
            {
               using var conn = new SqlConnection(_dbConnection);
               await conn.ExecuteAsync(Query, Input, commandTimeout: 1500);
               return true;
            }

            return false;
         }
         catch (Exception ex)
         {
            throw ex;
         }
      }
      #endregion

      #region [ Delete Custom ]
      public async Task<bool> DeleteCustom(string Query, string Connection)
      {
         try
         {
            using (var conn = new SqlConnection(Connection))
            {
               await conn.ExecuteAsync(Query, new { }, commandTimeout: 1500);
            }

            return true;
         }
         catch (Exception ex)
         {
            throw ex;
         }
      }
      #endregion

      //------------------//

      #region [ Truncate ]
      public bool Truncate(string Table)
      {
         try
         {
            Table = Table.Replace("[dbo].", "").Replace("[", "").Replace("]", "");
            Table = string.Format("[dbo].[{0}]", Table);

            using (var conn = new SqlConnection(_dbConnection))
            {
               conn.Execute($"TRUNCATE TABLE {Table}", new { });
            }

            return true;
         }
         catch (Exception ex)
         {
            throw ex;
         }
      }
      #endregion

      #region [ Truncate ]
      public bool Truncate(string Table, string Connection)
      {
         try
         {
            Table = Table.Replace("[dbo].", "").Replace("[", "").Replace("]", "");
            Table = string.Format("[dbo].[{0}]", Table);

            using (var conn = new SqlConnection(Connection))
            {
               conn.Execute($"TRUNCATE TABLE {Table}", new { });
            }

            return true;
         }
         catch (Exception ex)
         {
            throw ex;
         }
      }
      #endregion

      #region [ Bulk Upload ]
      public bool BulkUpload(DataTable model, string Table)
      {
         try
         {
            Table = Table.Replace("[dbo].", "").Replace("[", "").Replace("]", "");
            Table = string.Format("[dbo].[{0}]", Table);

            using (SqlBulkCopy SqlBulk = new SqlBulkCopy(_dbConnection))
            {
               SqlBulk.DestinationTableName = Table;
               SqlBulk.BatchSize = 9500;
               SqlBulk.BulkCopyTimeout = 1500;
               SqlBulk.WriteToServer(model);
            }
            return true;
         }
         catch (Exception ex)
         {
            throw ex;
         }
      }
      #endregion

      #region [ Bulk Upload ]
      public bool BulkUpload(DataTable model, string Table, string Connection)
      {
         try
         {
            Table = Table.Replace("[dbo].", "").Replace("[", "").Replace("]", "");
            Table = string.Format("[dbo].[{0}]", Table);

            using (SqlBulkCopy SqlBulk = new SqlBulkCopy(Connection))
            {
               SqlBulk.DestinationTableName = Table;
               SqlBulk.BatchSize = 9500;
               SqlBulk.BulkCopyTimeout = 1500;
               SqlBulk.WriteToServer(model);
            }
            return true;
         }
         catch (Exception ex)
         {
            throw ex;
         }
      }
      #endregion

      //------------------//

      #region [ Map Table Columns ]
      public async Task<List<string>> MapTableColumns(string Connection, string TableName)
      {
         List<string> columns = new List<string>();
         try
         {
            string query = @"SELECT [COLUMN_NAME],[ORDINAL_POSITION],[DATA_TYPE] FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = @TableName ORDER BY [ORDINAL_POSITION]";
            using var conn = new SqlConnection(Connection);
            var data = await conn.QueryAsync<DataSchema>(query, new { TableName }, commandTimeout: 0);

            List<DataSchema> schema = data.ToList();
            foreach (var item in schema.OrderBy(x => x.ORDINAL_POSITION))
            {
               columns.Add(item.COLUMN_NAME);
            }
         }
         catch (Exception ex)
         {

         }
         return columns;
      }
      #endregion

      #region [ Copy Table ]
      public void CopyTable(string FromConn, string ToConn, string SelectQuery, string ToTableName, string[] columns)
      {
         try
         {
            SqlBulkCopyOptions o = SqlBulkCopyOptions.Default;
            o |= SqlBulkCopyOptions.KeepIdentity;
            o |= SqlBulkCopyOptions.KeepNulls;
            o |= SqlBulkCopyOptions.TableLock;

            using (SqlBulkCopy bcp = new SqlBulkCopy(ToConn, o))
            {
               bcp.BulkCopyTimeout = 0;
               bcp.BatchSize = 20000;
               bcp.DestinationTableName = ToTableName;

               foreach (string c in columns)
               {
                  SqlBulkCopyColumnMapping m = new SqlBulkCopyColumnMapping { DestinationColumn = c, SourceColumn = c };
                  bcp.ColumnMappings.Add(m);
               }

               using SqlConnection sourceConn = new SqlConnection(FromConn);
               sourceConn.Open();

               using SqlCommand sourceComm = new SqlCommand();
               sourceComm.CommandTimeout = 100;
               sourceComm.Connection = sourceConn;
               sourceComm.Parameters.Clear();
               sourceComm.CommandText = SelectQuery;
               sourceComm.CommandType = CommandType.Text;

               using SqlDataReader Reader = sourceComm.ExecuteReader();
               bcp.WriteToServer(Reader);
            }
         }
         catch (Exception ex)
         {
            string error = ex.Message;
         }
      }
      #endregion

   }
}