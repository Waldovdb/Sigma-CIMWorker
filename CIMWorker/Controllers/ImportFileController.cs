#region [ using ]
using CIMWorker.Data.Entities;
using CIMWorker.Models;
using CIMWorker.Services;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
#endregion

namespace CIMWorker.Controllers
{
    public class ImportFileController
    {
        private readonly IDataService _dataService;
        private readonly IFileService _fileService;
        private readonly ILogService _logService;
        private readonly IEmailService _emailService;
        private readonly IDiallerService _dialerService;
        private readonly DateTime _startTime;

        #region [ Default Constructor ]
        public ImportFileController(IDataService dataService, IFileService fileService, ILogService logService, IEmailService emailService, IDiallerService dialerService)
        {
            _dataService = dataService;
            _fileService = fileService;
            _logService = logService;
            _emailService = emailService;
            _dialerService = dialerService;
            _startTime = DateTime.Now;
        }
        #endregion

        //-----------------------------//

        #region [ Master Async ]
        public async Task<bool> MasterAsync()
        {
            int Pass = 0, Fail = 0;
            int PassTotal = 0, FailTotal = 0;
            bool hasFiles = false;
            DateTime start = DateTime.Now;
            await _logService.InfoAsync("Import File Controller Started");
            try
            {
                var fileConfigs = await _fileService.GetFileConfigurations();
                fileConfigs = fileConfigs.Where(o => o.IsActive == true).ToList();
                for (int i = 0; i < fileConfigs.Count(); i++)
                {
                    var files = await _fileService.GetFileList(fileConfigs[i].Location, fileConfigs[i].Partial);
                    //var files = await _fileService.GetFileList("C:\\0.InovoCIM\\Busy", fileConfigs[i].Partial);

                    for (int x = 0; x < files.Count(); x++)
                    {
                        FileInfo file = files[x];
                        bool IsHeaderValidate = await _fileService.ValidateHeader(file, fileConfigs[i]);
                        if (IsHeaderValidate)
                        {
                            DataTable FileTable = new DataTable();

                            FileTable = await _fileService.SetDataTableSchema(fileConfigs[i].TableName);
                            FileTable = await ReadTextFile(FileTable, fileConfigs[i], file);

                            if (FileTable.Rows.Count >= 1)
                            {
                                hasFiles = true;
                                bool IsFileLogged = _fileService.LogFileDataTable(FileTable, fileConfigs[i].TableName);
                                if(IsFileLogged)
                                {
                                    await _fileService.CloseTextFile(file);
                                }

                                List<PhoneQueue> phoneQueue = new List<PhoneQueue>();
                                int LoadID = 0;
                                LoadID = GetLoadID(fileConfigs[i]);
                                foreach (DataRow row in FileTable.Rows)
                                {
                                    var person = new Person();
                                    var personContact = new PersonContact();
                                    int SourceID = 0;
                                    int ServiceID = 0;
                                    int Priority = 0;
                                    if (fileConfigs[i].Partial != "Scheduled Callback")
                                    {
                                        person = new Person(1, row[fileConfigs[i].PersonName].ToString(), row[fileConfigs[i].PersonSurname].ToString(), row[fileConfigs[i].PersonIDNumber].ToString(), row[fileConfigs[i].PersonExternalID].ToString());
                                        personContact = new PersonContact(row[fileConfigs[i].PersonPhone1].ToString());
                                        SourceID = await GetSourceID(person, personContact);
                                        ServiceID = fileConfigs[i].ServiceID;
                                        Priority = await GetPriority(fileConfigs[i], row);
                                    }
                                    else
                                    {
                                        
                                    }

                                    PhoneQueue singleQueue = new PhoneQueue();

                                    try
                                    {
                                        if(fileConfigs[i].Partial != "Scheduled Callback")
                                        {
                                            singleQueue = await MapFileToPhoneQueue(SourceID, LoadID, Priority, row, file, fileConfigs[i]);
                                        }
                                        else
                                        {
                                            singleQueue = MapScheduleToPhoneQueue(row, file);
                                        }
                                        if(singleQueue.Command != null)
                                        {
                                            List<PhoneQueue> CurrentQueue = new List<PhoneQueue>();
                                            CurrentQueue.Add(singleQueue);
                                            await _fileService.AddToQueue(CurrentQueue);
                                            phoneQueue.Add(singleQueue);
                                        }
                                    }
                                    catch (Exception ex)
                                    {

                                    }
                                }
                                //bool IsLoaded = await _fileService.AddToQueue(phoneQueue);
                                PassTotal += Pass;
                                FailTotal += Fail;
                            }
                        }
                    }
                }
                //await _dataService.InsertSingle<DBLeads, DBLeads>("INSERT INTO [dbo].[DBLeads] ([Pass],[Fail],[TimeTaken]) VALUES (@Pass, @Fail, @TimeTaken)", new DBLeads(PassTotal, FailTotal, DateTime.Now.Subtract(start).Seconds));
                return true;
            }
            catch (Exception ex)
            {
                await _logService.ErrorAsync(GetType().Name, MethodBase.GetCurrentMethod(), ex);
            }

            TimeSpan runSpan = DateTime.Now.Subtract(_startTime);
            double RecordsPerSecond = ((Pass + Fail) / runSpan.TotalSeconds);
            await _logService.InfoAsync($"Import File Controller Ended : Runtime -> Records per Second: {RecordsPerSecond}");

            return true;
        }
        #endregion


        #region [ Read Text File ]
        public async Task<DataTable> ReadTextFile(DataTable FileTable, ImportDataFile model, FileInfo file)
        {
            DateTime StartFile = DateTime.Now;
            try
            {
                int LineID = 0, Pass = 0, Fail = 0;
                string FileLine;
                using (var fileStream = File.OpenRead(file.FullName))
                using (var reader = new StreamReader(fileStream, Encoding.UTF8, true, 1024))
                {
                    while ((FileLine = await reader.ReadLineAsync()) != null)
                    {

                        try
                        {
                            string[] InData = null;
                            if (model.HasHeader == false)
                            {
                                InData = await _fileService.GetDelimiterLine(FileLine, model, LineID);
                            }
                            else
                            {
                                model.HasHeader = false;
                                continue;
                            }

                            if (InData.Count() >= 1)
                            {
                                LineID++;
                                FileTable = await AddToFileTable(FileTable, InData, LineID);
                                Pass++;
                            }
                            else
                            {
                                Fail++;
                            }
                        }
                        catch (Exception ex)
                        {
                            //await _logService.ErrorAsync($"");
                            continue;
                        }
                    }
                }

                TimeSpan runtime = DateTime.Now.Subtract(StartFile);
                await _logService.InfoAsync($"Read Text File - Lines: { LineID} || Pass: { Pass} || Fail: { Fail} || Time: { runtime.ToString()}");
            }
            catch (Exception ex)
            {
                await _logService.ErrorAsync(GetType().Name, MethodBase.GetCurrentMethod(), ex);
            }
            return FileTable;
        }
        #endregion

        //#region [ Insert Runtime ]
        //public async Task InsertRuntime(DBRuntime runtime)
        //{
        //    try
        //    {
        //        await _dataService.InsertSingle<DBRuntime, dynamic>("INSERT INTO [dbo].[DBRuntime] ([Start],[End],[DBLeadsID]) VALUES (@StartTime, @EndTime, (SELECT MAX(ID) FROM [dbo].[DBLeads]))", new { StartTime = runtime.RuntimeStart, EndTime = runtime.RuntimeEnd });
        //    }
        //    catch (Exception ex)
        //    {
        //        await _logService.ErrorAsync(GetType().Name, MethodBase.GetCurrentMethod(), ex);
        //    }
        //}
        //#endregion

        #region [ Add To File Table ]
        public async Task<DataTable> AddToFileTable(DataTable Table, string[] InData, int LineID)
        {
            DataRow Row = Table.NewRow();

            int ColumnIndex = -1;
            bool IsLineError = false;
            foreach (DataColumn column in Table.Columns)
            {
                IsLineError = false;
                try
                {
                    ColumnIndex++;
                    
                    if (ColumnIndex >= 1)
                    {
                        string item = column.DataType.ToString();
                        if (item == "System.Decimal")
                        {
                            var value = string.IsNullOrEmpty(InData[ColumnIndex - 1].ToString()) ? "0" : InData[ColumnIndex - 1];
                            value = (value.Contains('.')) ? value.Substring(0, value.IndexOf('.')) : value;
                            Row[ColumnIndex] = Convert.ToDecimal(value);
                        }
                        else
                        {
                            if (string.IsNullOrEmpty(InData[ColumnIndex - 1].ToString().Trim()))
                            {
                                Row[ColumnIndex] = DBNull.Value;
                            }
                            else
                            {
                                Row[ColumnIndex] = InData[ColumnIndex - 1].ToString().Trim();
                            }
                        }
                    }
                    else
                    {
                        Row[ColumnIndex] = DBNull.Value;
                    }
                }
                catch (Exception ex)
                {
                    IsLineError = true;
                    continue;
                }
            }

            if (IsLineError == false)
            {
                Table.Rows.Add(Row);
            }


            return Table;
        }
        #endregion

        public PhoneQueue MapScheduleToPhoneQueue(DataRow row, FileInfo file)
        {
            int SourceID = 0;
            string MSISDN = row["MSISDN"].ToString();
            var dbdip = _dataService.SelectSinglePresence<dynamic,dynamic>("SELECT SOURCEID, NAME FROM PREP.PCO_OUTBOUNDQUEUE WHERE RIGHT(PHONE1,9) = @PHONE OR RIGHT(PHONE2,9) = @PHONE OR RIGHT(PHONE3,9) = @PHONE", new { PHONE = MSISDN }).GetAwaiter().GetResult();
            if(dbdip.SOURCEID != null)
            {
                PhoneQueue phoneQueue = new PhoneQueue();
                phoneQueue.Command = "callback";
                phoneQueue.Input = "File";
                phoneQueue.InputName = file.Name;
                phoneQueue.Status = "Received";
                phoneQueue.Received = DateTime.Now;
                phoneQueue.NextExecute = DateTime.Now;
                phoneQueue.RetryCount = 0;
                phoneQueue.PersonID = (int)dbdip.SOURCEID;
                phoneQueue.ServiceID = 0;
                phoneQueue.LoadID = 0;
                phoneQueue.SourceID = (int)dbdip.SOURCEID;
                phoneQueue.Phone = MSISDN;
                phoneQueue.Name = dbdip.NAME;
                phoneQueue.Priority = 1;
                //if(row["Presence_Agent_Login"] !=)
                phoneQueue.CapturingAgent = Convert.ToInt32(row["Presence_Agent_Login"]);
                phoneQueue.Phone1 = MSISDN;
                phoneQueue.Comments = row["Comments"].ToString();
                string DateString = row["Schedule_Date"].ToString();
                var splitdate = DateString.Split('/');
                int day = Convert.ToInt32(splitdate[0]);
                int month = Convert.ToInt32(splitdate[1]);
                int year = Convert.ToInt32(splitdate[2]);
                string TimeString = row["ScheduleTme"].ToString();
                var splittime = TimeString.Split(':');
                int hour = Convert.ToInt32(splittime[0]);
                int minute = 0;
                var splitminsec = splittime[1].ToString().Split(':');
                if (splitminsec.Count() > 1)
                {
                    minute = Convert.ToInt32(splitminsec[0]);
                }
                phoneQueue.ScheduleDate = new DateTime(year, month, day, hour, minute, 0);
                return phoneQueue;
            }
            else
            {
                return new PhoneQueue();
            }
        }

        //#region [ Map File To Phone Queue ]
        //public PhoneQueue MapFileToPhoneQueue(int SourceID, int ServiceID, int LoadID, int Priority, DataRow row, FileInfo file)
        //{
        //    #region [ Old ]
        //    PhoneQueue phoneQueue = new PhoneQueue();
        //    phoneQueue.Command = "AddCall";
        //    phoneQueue.Input = "File";
        //    phoneQueue.InputName = file.Name;
        //    phoneQueue.Status = "Received";
        //    phoneQueue.Received = DateTime.Now;
        //    phoneQueue.NextExecute = DateTime.Now;
        //    phoneQueue.RetryCount = 0;
        //    phoneQueue.PersonID = SourceID;
        //    phoneQueue.ExternalID = row["IDNO"].ToString();
            
        //    //phoneQueue.ExternalID = (ServiceID == 1) ? row["IDNumber"].ToString() : row["ID_Number"].ToString();
        //    phoneQueue.SourceID = SourceID;
        //    phoneQueue.ServiceID = ServiceID;
        //    phoneQueue.LoadID = LoadID;
        //    phoneQueue.Name = row["Firstname"].ToString() + " " + row["Surname"].ToString();
        //    phoneQueue.Name = (phoneQueue.Name.Length > 40) ? phoneQueue.Name.Substring(0, 39) : phoneQueue.Name;
        //    //phoneQueue.Name = (ServiceID == 1) ? row["FullName"].ToString() : row["First_Name"].ToString() + " " + row["Last_Name"].ToString();
        //    phoneQueue.Phone = row["Cell1"].ToString();
        //    //phoneQueue.Phone = (ServiceID == 1) ? row["Phone"].ToString() : row["Phone_Cell"].ToString();
        //    phoneQueue.Priority = Priority;
        //    phoneQueue.CapturingAgent = 0;
        //    phoneQueue.Phone1 = row["Cell1"].ToString();
        //    //phoneQueue.Phone01 = (ServiceID == 1) ? row["Phone"].ToString() : row["Phone_Cell"].ToString();
        //    phoneQueue.Phone2 = row["Cell2"].ToString();
        //    phoneQueue.Phone3 = row["Cell3"].ToString();

        //    //phoneQueue.CustomData1 = (ServiceID == 1) ? row["IDNumber"].ToString() : row["ID_Number"].ToString();
        //    phoneQueue.CustomData1 = row["PhoneMakeModel"].ToString();
        //    phoneQueue.CustomData2 = row["DealType"].ToString();
        //    //phoneQueue.CustomData2 = (ServiceID == 1) ? row["LeadSource"].ToString() : row["Notes"].ToString().Split("<BR>")[1];

        //    phoneQueue.CustomData3 = row["AverageSpend/ContractEndDate"].ToString();
        //    phoneQueue.Comments = row["IDNO"].ToString();        
        //    //phoneQueue.CustomData3 = (ServiceID == 1) ? row["Reference"].ToString() : row["Notes"].ToString().Split("<BR>")[0];
        //    //phoneQueue.Comments = (ServiceID == 2) ? temp : null;
        //    return phoneQueue;
        //    #endregion
        //}
        //#endregion

        #region [ Map File To Phone Queue ]
        public async Task<PhoneQueue> MapFileToPhoneQueue(int SourceID, int LoadID, int Priority, DataRow row, FileInfo file, ImportDataFile config)
        {
            #region [ Old ]
            PhoneQueue phoneQueue = new PhoneQueue();
            phoneQueue.Command = "addcall";
            phoneQueue.Input = "File";
            phoneQueue.InputName = file.Name;
            phoneQueue.Status = "Received";
            phoneQueue.Received = DateTime.Now;
            phoneQueue.NextExecute = DateTime.Now;
            phoneQueue.RetryCount = 0;
            phoneQueue.PersonID = SourceID;
            phoneQueue.ExternalID = row[config.PersonExternalID].ToString();
            phoneQueue.SourceID = SourceID;
            phoneQueue.ServiceID = config.ServiceID;
            phoneQueue.LoadID = LoadID;
            phoneQueue.Name = row[config.PersonName].ToString() + " " + row[config.PersonSurname].ToString();
            phoneQueue.Name = (phoneQueue.Name.Length > 40) ? phoneQueue.Name.Substring(0, 39) : phoneQueue.Name;
            phoneQueue.Phone = (String.IsNullOrEmpty(config.PersonPhone1)) ? null : row[config.PersonPhone1].ToString();
            phoneQueue.Priority = Priority;
            phoneQueue.CapturingAgent = 0;
            phoneQueue.Phone1 = (await CheckPhones(row, config.PersonPhone1)) ? null : row[config.PersonPhone1].ToString().Trim();
            phoneQueue.Phone2 = (await CheckPhones(row, config.PersonPhone2)) ? null : row[config.PersonPhone2].ToString();
            phoneQueue.Phone3 = (await CheckPhones(row, config.PersonPhone3)) ? null : row[config.PersonPhone3].ToString();
            phoneQueue.Phone4 = (await CheckPhones(row, config.PersonPhone4)) ? null : row[config.PersonPhone4].ToString();
            phoneQueue.Phone5 = (await CheckPhones(row, config.PersonPhone5)) ? null : row[config.PersonPhone5].ToString();
            phoneQueue.CustomData1 = (String.IsNullOrEmpty(config.CustomData1)) ? null : row[config.CustomData1].ToString();
            phoneQueue.CustomData2 = (String.IsNullOrEmpty(config.CustomData2)) ? null : row[config.CustomData2].ToString();
            phoneQueue.CustomData3 = (String.IsNullOrEmpty(config.CustomData3)) ? null : row[config.CustomData3].ToString();
            phoneQueue.Comments = (String.IsNullOrEmpty(config.Comments)) ? null : row[config.Comments].ToString();
            return phoneQueue;
            #endregion
        }
        #endregion

        #region [ Check Config for Empty and Data Row for Empty ]
        public async Task<bool> CheckPhones(DataRow row, string columnName)
        {
            try
            {
                bool IsValid = false;
                IsValid = !String.IsNullOrEmpty(columnName);
                if(IsValid)
                {
                    IsValid = !String.IsNullOrEmpty(row[columnName].ToString().Trim());
                }
                return !IsValid;
            }
            catch (Exception)
            {
                throw;
            }
        }
        #endregion

        //-----------------------------//

        #region [ Get ServiceID ]
        //private int GetServiceID(ImportDataFile model, DataRow row)
        //{
        //    switch (model.TableName)
        //    {
        //        case "DNC":
        //            return 2;
        //        case "ELI":
        //            return 2;
        //        case "ELI_Loyalty":
        //            return 2;
        //        case "OOC":
        //            return 2;
        //        case "OOC_Loyalty":
        //            return 2;
        //        case "MTN_New":
        //            return 1;
        //        case "MTN_FLTE":
        //            return 1;
        //        default:
        //            return 1;
        //    }
        //}
        #endregion

        #region [ Get LoadID ]
        private int GetLoadID(ImportDataFile model) 
        {
            try
            {
                int outServiceID = 0;
                outServiceID = model.ServiceID;
                string LoadName = model.Partial + DateTime.Now.ToShortDateString();
                var resultLoad = _dataService.SelectSinglePresence<dynamic, dynamic>("SELECT COALESCE(LOADID,0) AS [LOADID] FROM [PREP].[PCO_LOAD] WHERE [SERVICEID] = @SERVICEID AND [DESCRIPTION] = @LOADNAME", new { LOADNAME = LoadName, SERVICEID = outServiceID }).GetAwaiter().GetResult();
                if (resultLoad == null)
                {
                    var newLoad = _dataService.SelectSinglePresence<dynamic, dynamic>(@"DECLARE @TEMPLOAD INT
                                                                                    SET @TEMPLOAD = (SELECT COALESCE((
                                                                                    SELECT TOP 1 LOADID FROM[PREP].[PCO_LOAD] WHERE[SERVICEID] = @SERVICEID ORDER BY LOADID DESC), 0))

                                                                                    SELECT @TEMPLOAD AS LOADID", new { SERVICEID = outServiceID }).GetAwaiter().GetResult();
                    int newLoadID = Convert.ToInt32(newLoad.LOADID) + 1;
                    string insertLoadQuery = @"INSERT INTO [PREP].[PCO_LOAD]
                                                   ([SERVICEID]
                                                   ,[LOADID]
                                                   ,[STATUS]
                                                   ,[DESCRIPTION]
                                                   ,[RDATE]
                                                   ,[RECORDCOUNT]
                                                   ,[PRIORITYTYPE]
                                                   ,[PRIORITYVALUE])
                                             VALUES
                                                   (@SERVICEID
                                                   ,@NEWLOADID
                                                   ,'D'
                                                   ,@LOADNAME
                                                   ,GETDATE()
                                                   ,0
                                                   ,0
                                                   ,0)";
                    _dataService.InsertSinglePresence<dynamic, dynamic>(insertLoadQuery, new { NEWLOADID = newLoadID, LOADNAME = LoadName, SERVICEID = outServiceID }).GetAwaiter().GetResult();
                    return newLoadID;
                }
                else
                {
                    return Convert.ToInt32(resultLoad.LOADID);
                }
            }
            catch (Exception)
            {

                throw;
            }
        }
        #endregion

        #region [ Get LoadID ]
        //private int GetLoadID(ImportDataFile model, DataRow row, int ServiceID) // Update this with logic for Telemarketing Load Creation
        //{
        //    return 1;
        //}
        #endregion

        #region [ Get Priority ]
        private async Task<int> GetPriority(ImportDataFile model, DataRow row)
        {
            try
            {
                int MaxPriority = (model.Priority == 1) ? await _dialerService.GetMaxPriority(_dataService.GetConnectionString("Presence"), 1, 1) : 0;
                return MaxPriority + 1;
            }
            catch (Exception)
            {
                return 1;
            }
        }
        #endregion

        #region [ Get SourceID ]
        private async Task<int> GetSourceID(Person data, PersonContact dataContact)
        {
            int PersonID = -1;
            try
            {
                string query = @"IF NOT EXISTS(SELECT TOP 1 [PersonID] FROM [dbo].[Person] WHERE [IDNumber] = @IDNumber AND [ExternalID] = @ExternalID)
                           BEGIN
	                           INSERT INTO [dbo].[Person] ([Title],[Name],[Surname],[IDNumber],[ExternalID],[Updated]) VALUES (1,@Name,@Surname,@IDNumber,@ExternalID,GETDATE())
                           END";

                await _dataService.InsertSingle<Person, Person>(query, data);
                var person = await _dataService.SelectSingle<Person, dynamic>("SELECT TOP 1 * FROM [Person] WHERE [IDNumber] = @IDNumber AND [ExternalID] = @ExternalID", new { data.ExternalID, data.IDNumber });
                if (person != null)
                {
                    PersonID = person.PersonID;

                    string phoneQuery = @"IF NOT EXISTS(SELECT TOP 1 [PersonID] FROM [dbo].[PersonContact] WHERE [Contact] = @Contact)
                                     BEGIN
	                                     INSERT INTO [dbo].[PersonContact] ([PersonID],[Type],[Contact],[Created]) VALUES (@PersonID,1,@Contact,GETDATE())
                                     END";

                    dataContact.PersonID = PersonID;
                    await _dataService.InsertSingle<PersonContact, PersonContact>(phoneQuery, dataContact);
                }
            }
            catch (Exception ex)
            {
                await _logService.ErrorAsync(GetType().Name, MethodBase.GetCurrentMethod(), ex);
            }

            return PersonID;
        }
        #endregion
    }
}