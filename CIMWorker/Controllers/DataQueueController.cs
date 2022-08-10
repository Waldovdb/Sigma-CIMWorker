#region [ using ]
using CIMWorker.Data.Entities;
using CIMWorker.Helpers;
using CIMWorker.Models;
using CIMWorker.Services;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
#endregion

namespace CIMWorker.Controllers
{
    public class DataQueueController
    {
        #region [ Variables ]
        private readonly IDataService _dataService;
        private readonly IDiallerService _diallerService;
        private readonly ILogService _logService;
        private readonly DateTime _startTime;
        private readonly string _presenceDatabase;
        private Dictionary<int, string> LoadStatusList;
        private Dictionary<int, Models.OutboundQueue> QueueSourceList;
        private readonly AppSettings _appSettings;
        #endregion

        #region [ Default Constructor ]
        public DataQueueController(IOptions<AppSettings> appSettings, IDataService dataService, IDiallerService diallerService, ILogService logService)
        {
            _appSettings = appSettings.Value;
            _dataService = dataService;
            _diallerService = diallerService;
            _logService = logService;
            _startTime = DateTime.Now;
            _presenceDatabase = _dataService.GetConnectionString("Presence");
        }
        #endregion

        //-----------------------------//

        #region [ Master Async ]
        public async Task<bool> MasterAsync()
        {
            await _logService.InfoAsync("Data Queue Controller Started");
            int Pass = 0, Fail = 0;

            List<PhoneQueue> collection = await _dataService.SelectMany<PhoneQueue, dynamic>("EXECUTE [dbo].[spGetPhoneQueue]", new { });
            if (collection != null)
            {
                if (collection.Count() > 0)
                {
                    LoadStatusList = await _diallerService.GetLoadStatusList(_presenceDatabase);
                    QueueSourceList = await _diallerService.GetQueueSourceList(_presenceDatabase);

                    for (int i = 0; i < collection.Count(); i++)
                    {
                        try
                        {
                            await ProcessRecord(collection[i], LoadStatusList, QueueSourceList);
                            Pass++;
                        }
                        catch (Exception ex)
                        {
                            await _logService.ErrorAsync(GetType().Name, MethodBase.GetCurrentMethod(), ex);
                            Fail++;
                            continue;
                        }
                    }
                }
            }

            TimeSpan runSpan = DateTime.Now.Subtract(_startTime);
            double RecordsPerSecond = ((Pass + Fail) / runSpan.TotalSeconds);
            await _logService.InfoAsync($"Data Queue Controller Ended : Runtime -> Records per Second: {RecordsPerSecond}");
            return true;
        }
        #endregion

        //----------------------------------------//

        #region [ Process Record ]
        public async Task ProcessRecord(PhoneQueue record, Dictionary<int, string> LoadStatusList, Dictionary<int, Models.OutboundQueue> QueueSourceList)
        {
            try
            {
                if (record.Command == "callback")
                {
                    bool IsEnabled = LoadStatusList.ContainsValue(record.ServiceID + "," + record.LoadID + ",E");
                    int LoadStatus = (IsEnabled) ? 2 : 42;
                    bool InPresence = QueueSourceList.ContainsValue(new OutboundQueue() { SOURCEID = record.SourceID, SERVICEID = record.ServiceID, LOADID = record.LoadID});
                    if (InPresence)
                    {
                        await ProcessCallback(record, LoadStatus);
                        await SendToPhoneQueueDone(record, "Updated Presence");
                    }
                }
                else
                {
                    bool IsEnabled = LoadStatusList.ContainsValue(record.ServiceID + "," + record.LoadID + ",E");
                    int LoadStatus = (IsEnabled) ? 1 : 41;
                    LoadStatus = (record.ScheduleDate == null) ? LoadStatus : LoadStatus + 1;

                    bool HasValidPhone = _diallerService.IsPhoneNumbersValid(record);
                    //bool HasValidPhone = true;
                        if (HasValidPhone)
                    {
                        bool InPresence = QueueSourceList.ContainsValue(new OutboundQueue() { LOADID = record.LoadID, SERVICEID = record.ServiceID, SOURCEID = record.SourceID});
                        //bool InPresence = false;
                        if (InPresence)
                        {
                            bool isCallBack = (record.CapturingAgent != 0 && record.ScheduleDate != null);
                            if (InPresence && !isCallBack)
                            {
                                await SendToPhoneQueueDone(record, "No Change");
                            }
                            else if (isCallBack && InPresence)
                            {
                                await UpdateInPresence(record, LoadStatus);
                                await SendToPhoneQueueDone(record, "Updated Presence");
                            }
                            else if (isCallBack && !InPresence)
                            {
                                await SendToPresence(record, LoadStatus);
                                await SendToPhoneQueueDone(record, "Added to Presence");
                            }
                        }
                        else
                        {
                            await SendToPresence(record, LoadStatus);
                            await SendToPhoneQueueDone(record, "Added to Presence");
                        }
                    }
                    else
                    {
                        await RemoveFromPresence(record);
                        await SendToPhoneQueueDone(record, "No Valid Phone Numbers");
                    }
                }
            }
            catch (Exception ex)
            {
                await _logService.ErrorAsync(GetType().Name, MethodBase.GetCurrentMethod(), ex);
            }
        }
        #endregion

        #region [ Is Same Service ]
        public async Task<bool> IsSameService(PhoneQueue data)
        {
            OutboundQueue dataOut = new OutboundQueue();
            try
            {
                string query = @"SELECT TOP 1 [SERVICEID], [LOADID] FROM [PREP].[PCO_OUTBOUNDQUEUE] WHERE [SOURCEID] = @SOURCEID ORDER BY ID DESC";

                using IDbConnection connection = new SqlConnection(_dataService.GetConnectionString("Presence"));
                {
                    var dataOutElement = await connection.QueryAsync<OutboundQueue>(query, new { SOURCEID = data.SourceID });
                    dataOut = dataOutElement.FirstOrDefault();
                }

                if (dataOut.SERVICEID == data.ServiceID && dataOut.LOADID == data.LoadID)
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                await _logService.ErrorAsync(GetType().Name, MethodBase.GetCurrentMethod(), ex);
            }
            return false;
        }
        #endregion

        #region [ Update In Presence ]
        public async Task UpdateInPresence(PhoneQueue data, int LoadStatus)
        {
            try
            {
                int priority = await _diallerService.GetMaxPriority(_presenceDatabase, data.ServiceID, data.LoadID);

                string query = @"IF (SELECT COUNT(*) FROM PREP.PCO_OUTBOUNDQUEUE WHERE SOURCEID = @SOURCEID) > 1
                                BEGIN
                                UPDATE [PREP].[PCO_OUTBOUNDQUEUE] 
                                SET [NAME] = @NAME,
                                [SERVICEID] = @SERVICEID,
                                [LOADID] = @LOADID,
                                [STATUS] = @STATUS,
                                [PHONE] = @PHONE,
                                [PHONE1] = @PHONE1,
                                [PHONE2] = @PHONE2,
                                [PHONE3] = @PHONE3,
                                [PHONE4] = @PHONE4,
                                [PHONE5] = @PHONE5,
                                [PHONE6] = @PHONE6,
                                [PHONE7] = @PHONE7,
                                [PHONE8] = @PHONE8,
                                [PHONE9] = @PHONE9,
                                [PHONE10] = @PHONE10,
                                [CUSTOMDATA1] = @CUSTOMDATA1,
                                [CUSTOMDATA2] = @CUSTOMDATA2,
                                [CUSTOMDATA3] = @CUSTOMDATA3,
                                [PRIORITY] = @PRIORITY,
                                [CallerID] = @CALLERID,
                                [RDATE] = GETDATE(),
                                [CAPTURINGAGENT] = @CAPTURINGAGENT,
                                [SCHEDULEDATE] = @SCHEDULEDATE
                                WHERE [SOURCEID] = @SOURCEID
                                AND SERVICEID = @SERVICEID
                                END
                                ELSE
                                BEGIN 
                                UPDATE [PREP].[PCO_OUTBOUNDQUEUE] 
                                SET [NAME] = @NAME,
                                [SERVICEID] = @SERVICEID,
                                [LOADID] = @LOADID,
                                [STATUS] = @STATUS,
                                [PHONE] = @PHONE,
                                [PHONE1] = @PHONE1,
                                [PHONE2] = @PHONE2,
                                [PHONE3] = @PHONE3,
                                [PHONE4] = @PHONE4,
                                [PHONE5] = @PHONE5,
                                [PHONE6] = @PHONE6,
                                [PHONE7] = @PHONE7,
                                [PHONE8] = @PHONE8,
                                [PHONE9] = @PHONE9,
                                [PHONE10] = @PHONE10,
                                [CUSTOMDATA1] = @CUSTOMDATA1,
                                [CUSTOMDATA2] = @CUSTOMDATA2,
                                [CUSTOMDATA3] = @CUSTOMDATA3,
                                [PRIORITY] = @PRIORITY,
                                [CallerID] = @CALLERID,
                                [RDATE] = GETDATE(),
                                [CAPTURINGAGENT] = @CAPTURINGAGENT,
                                [SCHEDULEDATE] = @SCHEDULEDATE
                                WHERE [SOURCEID] = @SOURCEID
                                END";

                using IDbConnection connection = new SqlConnection(_dataService.GetConnectionString("Presence"));
                {
                    var affectedRows = connection.Execute(query, new
                    {
                        NAME = data.Name,
                        STATUS = LoadStatus,
                        PHONE = data.Phone,
                        PHONE1 = data.Phone1,
                        PHONE2 = data.Phone2,
                        PHONE3 = data.Phone3,
                        PHONE4 = data.Phone4,
                        PHONE5 = data.Phone5,
                        PHONE6 = data.Phone6,
                        PHONE7 = data.Phone7,
                        PHONE8 = data.Phone8,
                        PHONE9 = data.Phone9,
                        PHONE10 = data.Phone10,
                        SOURCEID = data.SourceID,
                        CUSTOMDATA1 = data.CustomData1,
                        CUSTOMDATA2 = data.CustomData2,
                        CUSTOMDATA3 = data.CustomData3,
                        PRIORITY = priority,
                        SERVICEID = data.ServiceID,
                        LOADID = data.LoadID,
                        CALLERID = data.CallerID,
                        CAPTURINGAGENT = data.CapturingAgent,
                        SCHEDULEDATE = data.ScheduleDate
                    });
                }
            }
            catch (Exception ex)
            {
                await _logService.ErrorAsync(GetType().Name, MethodBase.GetCurrentMethod(), ex);
            }
        }
        #endregion

        #region [ Send To Presence ]
        public async Task SendToPresence(PhoneQueue data, int LoadStatus)
        {
            try
            {
                if (data.ScheduleDate != null)
                {
                    LoadStatus = LoadStatus + 1;
                }
                int priority = await _diallerService.GetMaxPriority(_presenceDatabase, data.ServiceID, data.LoadID);

                string query = @"INSERT INTO [PREP].[PCO_OUTBOUNDQUEUE]
                           (
	                           [SERVICEID],[NAME],[PHONE],[SOURCEID],[STATUS],[SCHEDULETYPE],[SCHEDULEDATE],[LOADID],[PRIORITY],[CAPTURINGAGENT],
	                           [PHONE1],[PHONE2],[PHONE3],[PHONE4],[PHONE5],[PHONE6],[PHONE7],[PHONE8],[PHONE9],[PHONE10],
                              [PHONEDESC1],[PHONEDESC2],[PHONEDESC3],[PHONEDESC4],[PHONEDESC5],[PHONEDESC6],[PHONEDESC7],[PHONEDESC8],[PHONEDESC9],[PHONEDESC10],
                              [COMMENTS],[CUSTOMDATA1],[CUSTOMDATA2],[CUSTOMDATA3],[RDATE]
                           )
                           VALUES
                           (
                              @SERVICEID,LEFT(@NAME,40),@PHONE,@SOURCEID,@STATUS,@SCHEDULETYPE,@SCHEDULEDATE,@LOADID,@PRIORITY,NULL,
	                           @PHONE1,@PHONE2,@PHONE3,@PHONE4,@PHONE5,@PHONE6,@PHONE7,@PHONE8,@PHONE9,@PHONE10,
                              @PHONEDESC1,@PHONEDESC2,@PHONEDESC3,@PHONEDESC4,@PHONEDESC5,@PHONEDESC6,@PHONEDESC7,@PHONEDESC8,@PHONEDESC9,@PHONEDESC10,
                              @COMMENTS,@CUSTOMDATA1,@CUSTOMDATA2,@CUSTOMDATA3,GETDATE()
                           )";

                using IDbConnection connection = new SqlConnection(_dataService.GetConnectionString("Presence"));
                {
                    var affectedRows = connection.Execute(query, new
                    {
                        SERVICEID = data.ServiceID,
                        NAME = data.Name,
                        PHONE = data.Phone,
                        SOURCEID = data.SourceID,
                        STATUS = LoadStatus,
                        LOADID = data.LoadID,
                        PRIORITY = priority,
                        SCHEDULETYPE = 2,
                        SCHEDULEDATE = data.ScheduleDate,
                        PHONE1 = data.Phone1,
                        PHONE2 = data.Phone2,
                        PHONE3 = data.Phone3,
                        PHONE4 = data.Phone4,
                        PHONE5 = data.Phone5,
                        PHONE6 = data.Phone6,
                        PHONE7 = data.Phone7,
                        PHONE8 = data.Phone8,
                        PHONE9 = data.Phone9,
                        PHONE10 = data.Phone10,
                        PHONEDESC1 = 1,
                        PHONEDESC2 = 2,
                        PHONEDESC3 = 3,
                        PHONEDESC4 = 4,
                        PHONEDESC5 = 5,
                        PHONEDESC6 = 6,
                        PHONEDESC7 = 7,
                        PHONEDESC8 = 8,
                        PHONEDESC9 = 9,
                        PHONEDESC10 = 10,
                        COMMENTS = data.Comments,
                        CUSTOMDATA1 = data.CustomData1,
                        CUSTOMDATA2 = data.CustomData2,
                        CUSTOMDATA3 = data.CustomData3
                    });
                }
            }
            catch (Exception ex)
            {
                await _logService.ErrorAsync(GetType().Name, MethodBase.GetCurrentMethod(), ex);
            }
        }
        #endregion

        #region [ Remove From Presence ]
        public async Task RemoveFromPresence(PhoneQueue data)
        {
            try
            {
                string query = @"DELETE FROM [PREP].[PCO_OUTBOUNDQUEUE] WHERE [SOURCEID] = @SourceID";
                using IDbConnection connection = new SqlConnection(_dataService.GetConnectionString("Presence"));
                {
                    var affectedRows = connection.Execute(query, new { SourceID = data.SourceID });
                }
            }
            catch (Exception ex)
            {
                await _logService.ErrorAsync(GetType().Name, MethodBase.GetCurrentMethod(), ex);
            }
        }
        #endregion

        #region [ Process Callback ]
        public async Task ProcessCallback(PhoneQueue data, int LoadStatus)
        {
            try
            {
                string query = "";
                if(data.CapturingAgent != 0)
                {
                    query = @"UPDATE [PREP].[PCO_OUTBOUNDQUEUE]
                                SET CAPTURINGAGENT = @CAPTURINGAGENT, SCHEDULEDATE = @SCHEDULEDATE, STATUS = @STATUS, COMMENTS = @COMMENTS
                                WHERE SOURCEID = @SOURCEID";
                }
                else
                {
                    query = @"UPDATE [PREP].[PCO_OUTBOUNDQUEUE]
                                SET SCHEDULEDATE = @SCHEDULEDATE, STATUS = @STATUS, LOADID = @LOADID, COMMENTS = @COMMENTS
                                WHERE SOURCEID = @SOURCEID";
                }
                using IDbConnection connection = new SqlConnection(_dataService.GetConnectionString("Presence"));
                {
                    var affectedRows = connection.Execute(query, new { CAPTURINGAGENT = data.CapturingAgent, SCHEDULEDATE = data.ScheduleDate, SERVICEID = data.ServiceID, LOADID = data.LoadID, SOURCEID = data.SourceID, STATUS = LoadStatus });
                }
            }
            catch (Exception ex)
            {
                await _logService.ErrorAsync(GetType().Name, MethodBase.GetCurrentMethod(), ex);
            }
        }
        #endregion

        #region [ Send To Phone Queue Done ]
        public async Task SendToPhoneQueueDone(PhoneQueue data, string status)
        {
            try
            {
                string query = @"INSERT INTO [dbo].[PhoneQueueDone]
                           (
	                           [Command],[Input],[InputName],[Status],[Received],[NextExecute],[Actioned],[RetryCount],[RetryDate],
	                           [PersonID],[ExternalID],[SourceID],[ServiceID],[LoadID],[Name],[Phone],[ScheduleDate],[Priority],[CapturingAgent],
                              [Phone01],[Phone02],[Phone03],[Phone04],[Phone05],[Phone06],[Phone07],[Phone08],[Phone09],[Phone10],
	                           [Comments],[CustomData1],[CustomData2],[CustomData3],[CallerID],[CallerName]
                           )
                           SELECT [Command],[Input],[InputName],@Status,[Received],[NextExecute],GETDATE(),[RetryCount],[RetryDate],
	                              [PersonID],[ExternalID],[SourceID],[ServiceID],[LoadID],[Name],[Phone],[ScheduleDate],[Priority],[CapturingAgent],
                                  [Phone01],[Phone02],[Phone03],[Phone04],[Phone05],[Phone06],[Phone07],[Phone08],[Phone09],[Phone10],
	                              [Comments],[CustomData1],[CustomData2],[CustomData3],[CallerID],[CallerName]
                           FROM [dbo].[PhoneQueue]
                           WHERE [PhoneQueueID] = @PhoneQueueID

                           DELETE FROM [dbo].[PhoneQueue] WHERE [PhoneQueueID] = @PhoneQueueID";

                using IDbConnection connection = new SqlConnection(_dataService.GetConnectionString("InovoCIM"));
                {
                    var affectedRows = connection.Execute(query, new
                    {
                        Status = status,
                        PhoneQueueID = data.PhoneQueueID
                    });
                }
            }
            catch (Exception ex)
            {
                await _logService.ErrorAsync(GetType().Name, MethodBase.GetCurrentMethod(), ex);
            }
        }
        #endregion
    }
}