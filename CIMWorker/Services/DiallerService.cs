#region [ using ]
using CIMWorker.Data.Entities;
using CIMWorker.Helpers;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
#endregion

namespace CIMWorker.Services
{
   #region [ Interface ]
   public interface IDiallerService
   {
      Task<Dictionary<int, string>> GetLoadStatusList(string ConnString);
      Task<Dictionary<int, Models.OutboundQueue>> GetQueueSourceList(string ConnString);

        Task<List<int>> GetRPCList(string ConnString);

      bool IsPhoneNumbersValid(PhoneQueue data);

      Task<int> GetMaxPriority(string ConnString, int ServiceID, int LoadID);
   }
   #endregion

   //--------------------------------------------//

   public class DiallerService : IDiallerService
   {
      private readonly AppSettings _appSettings;

      #region [ Default Constructor ]
      public DiallerService(IOptions<AppSettings> appSettings)
      {
         _appSettings = appSettings.Value;
      }
      #endregion

      //-----------------------------//

      #region [ Get Load Status List ]
      public async Task<Dictionary<int, string>> GetLoadStatusList(string ConnString)
      {
         Dictionary<int, string> model = new Dictionary<int, string>();
         DataTable TempData = new DataTable();
         string query = @"SELECT [SERVICEID],[LOADID],[STATUS] FROM [PREP].[PCO_LOAD]";

         try
         {
            using (var conn = new SqlConnection(ConnString))
            {
               await conn.OpenAsync();
               using (SqlCommand cmd = conn.CreateCommand())
               {
                  cmd.CommandTimeout = 0;
                  cmd.CommandText = query;
                  SqlDataReader reader = cmd.ExecuteReader(CommandBehavior.CloseConnection);
                  TempData.Load(reader);
               }
               conn.Close();
            }

            for (int i = 0; i < TempData.Rows.Count; i++)
            {
               string ServiceID = TempData.Rows[i]["SERVICEID"].ToString();
               string LoadID = TempData.Rows[i]["LOADID"].ToString();
               string Status = TempData.Rows[i]["STATUS"].ToString();
               model.Add(i, ServiceID + "," + LoadID + "," + Status);
            }
            return model;
         }
         catch (Exception)
         {
            return model;
         }
      }
      #endregion

      #region [ Get Queue Source List ]
      public async Task<Dictionary<int, Models.OutboundQueue>> GetQueueSourceList(string ConnString)
      {
         Dictionary<int, Models.OutboundQueue> model = new Dictionary<int, Models.OutboundQueue>();
         DataTable TempData = new DataTable();
         string query = @"SELECT [ID],[SOURCEID],[LOADID],[SERVICEID] FROM [PREP].[PCO_OUTBOUNDQUEUE]";

         try
         {
            using (var conn = new SqlConnection(ConnString))
            {
               await conn.OpenAsync();
               using (SqlCommand cmd = conn.CreateCommand())
               {
                  cmd.CommandTimeout = 0;
                  cmd.CommandText = query;
                  SqlDataReader reader = cmd.ExecuteReader(CommandBehavior.CloseConnection);
                  TempData.Load(reader);
               }
               conn.Close();
            }

            for (int i = 0; i < TempData.Rows.Count; i++)
            {
               int ID = int.Parse(TempData.Rows[i]["ID"].ToString());
               int SourceID = int.Parse(TempData.Rows[i]["SOURCEID"].ToString());
               int ServiceID = int.Parse(TempData.Rows[i]["SERVICEID"].ToString());
               int LoadID = int.Parse(TempData.Rows[i]["LOADID"].ToString());
                    model.Add(ID, new Models.OutboundQueue() { LOADID = LoadID, SERVICEID = ServiceID, SOURCEID = ServiceID});
            }
            return model;
         }
         catch (Exception)
         {
            return model;
         }
      }
        #endregion

        #region [ Get Queue Source List ]
        public async Task<List<int>> GetRPCList(string ConnString)
        {
            List<int> RPCList = new List<int>();
            DataTable TempData = new DataTable();
            string query = @"SELECT SOURCEID FROM PREP.PCO_OUTBOUNDLOG OBL LEFT JOIN (SELECT * FROM PVIEW.SERVICEQCODE) QC ON OBL.QCODE = QC.QCODE AND OBL.SERVICEID = QC.SERVICEID WHERE CONVERT(DATE, RDATE) >= CONVERT(DATE, DATEADD(WEEK, -1, GETDATE())) AND QC.TYPE IN ('Positive useful','Negative useful') GROUP BY SOURCEID";

            try
            {
                using (var conn = new SqlConnection(ConnString))
                {
                    await conn.OpenAsync();
                    using (SqlCommand cmd = conn.CreateCommand())
                    {
                        cmd.CommandTimeout = 0;
                        cmd.CommandText = query;
                        SqlDataReader reader = cmd.ExecuteReader(CommandBehavior.CloseConnection);
                        TempData.Load(reader);
                    }
                    conn.Close();
                }

                for (int i = 0; i < TempData.Rows.Count; i++)
                {
                    int SourceID = int.Parse(TempData.Rows[i]["SOURCEID"].ToString());

                    RPCList.Add(SourceID);
                }
                return RPCList;
            }
            catch (Exception)
            {
                return RPCList;
            }
        }
        #endregion

        //-----------------------------//

        #region [ Is Phone Numbers Valid ]
        public bool IsPhoneNumbersValid(PhoneQueue data)
      {
         try
         {
            if (string.IsNullOrEmpty(data.Phone1) && string.IsNullOrEmpty(data.Phone2) && string.IsNullOrEmpty(data.Phone3) &&
                string.IsNullOrEmpty(data.Phone4) && string.IsNullOrEmpty(data.Phone5) && string.IsNullOrEmpty(data.Phone6) &&
                string.IsNullOrEmpty(data.Phone7) && string.IsNullOrEmpty(data.Phone8) && string.IsNullOrEmpty(data.Phone9) && string.IsNullOrEmpty(data.Phone10))
            {
               return false;
            }
            else
            {
               return true;
            }
         }
         catch (Exception)
         {
            return false;
         }
      }
      #endregion

      #region [ Get Max Priority ]
      public async Task<int> GetMaxPriority(string ConnString, int ServiceID, int LoadID)
      {
         DataTable TempData = new DataTable();
         string query = $"SELECT ISNULL(MAX([PRIORITY]),1) AS 'PRIORITY' FROM [PREP].[PCO_OUTBOUNDQUEUE] WHERE SERVICEID = {ServiceID} AND LOADID = {LoadID}";

         int priority = 0;
         try
         {
            using (var conn = new SqlConnection(ConnString))
            {
               await conn.OpenAsync();
               using (SqlCommand cmd = conn.CreateCommand())
               {
                  cmd.CommandTimeout = 0;
                  cmd.CommandText = query;
                  SqlDataReader reader = cmd.ExecuteReader(CommandBehavior.CloseConnection);
                  TempData.Load(reader);
               }
               conn.Close();
            }


            for (int i = 0; i < TempData.Rows.Count; i++)
            {
               string strPriority = TempData.Rows[i]["PRIORITY"].ToString();
               priority = int.Parse(strPriority);
            }

            return priority;
         }
         catch (Exception ex)
         {
            return priority;
         }
      }
      #endregion
   }
}