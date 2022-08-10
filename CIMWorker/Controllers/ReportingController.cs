#region [ using ]
using CIMWorker.Data.Entities;
using CIMWorker.Data.Entities.Custom;
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
using System.Runtime.InteropServices;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
#endregion

namespace CIMWorker.Controllers
{
    public class ReportingController
    {
        #region [ Variables ]
        private readonly IDataService _dataService;
        private readonly IDiallerService _diallerService;
        private readonly ILogService _logService;
        private readonly IEmailService _emailService;
        private readonly IRuleService _ruleService;
        private readonly DateTime _startTime;
        private readonly string _presenceDatabase;
        private readonly string _reportDatabase;
        private readonly string _inovoCIMDatabase;
        private readonly AppSettings _appSettings;
        #endregion

        #region [ Default Constructor ]
        public ReportingController(IOptions<AppSettings> appSettings, IDataService dataService, IDiallerService diallerService, ILogService logService, IEmailService emailService, IRuleService ruleService)
        {
            _appSettings = appSettings.Value;
            _dataService = dataService;
            _diallerService = diallerService;
            _logService = logService;
            _emailService = emailService;
            _ruleService = ruleService;
            _startTime = DateTime.Now;
            _presenceDatabase = _dataService.GetConnectionString("Presence");
            _reportDatabase = _dataService.GetConnectionString("Reporting");
            _inovoCIMDatabase = _dataService.GetConnectionString("InovoCIM");
        }
        #endregion

        //-----------------------------//

        #region [ Master Async ]
        public async Task<bool> MasterAsync()
        {
            try
            {
                await _logService.InfoAsync("Reporting Controller Started");
                await RunRespin();
                await MailReport();
                await ReplicateData();
                await LiveData();
            }
            catch (Exception ex)
            {
                await _logService.ErrorAsync(GetType().Name, MethodBase.GetCurrentMethod(), ex);
            }

            TimeSpan runSpan = DateTime.Now.Subtract(_startTime);
            await _logService.InfoAsync($"Reporting Controller Ended : Runtime -> {runSpan.TotalSeconds}");

            return true;
        }
        #endregion

        #region [ Master Async ]
        public async Task<bool> TestingAsync()
        {
            try
            {
                await _logService.InfoAsync("Reporting Controller Started");
                //await RunRespin();
                //await ReplicateData();
                //await LiveData();
                //await MailReport();
                await ReplicateTesting();
            }
            catch (Exception ex)
            {
                await _logService.ErrorAsync(GetType().Name, MethodBase.GetCurrentMethod(), ex);
            }

            TimeSpan runSpan = DateTime.Now.Subtract(_startTime);
            await _logService.InfoAsync($"Reporting Controller Ended : Runtime -> {runSpan.TotalSeconds}");

            return true;
        }
        #endregion

        //-----------------//

        #region [ Replicate Data ]
        public async Task ReplicateData()
        {
            int year = DateTime.Now.Year;
            int month = DateTime.Now.Month;
            int day = DateTime.Now.Day;

            try
            {
                string[] start = _appSettings.Reporting.Start.Split(':').ToArray();
                string[] end = _appSettings.Reporting.End.Split(':').ToArray();

                DateTime StartTime = new DateTime(year, month, day, int.Parse(start[0]), int.Parse(start[1]), 0);
                DateTime EndTime = new DateTime(year, month, day, int.Parse(end[0]), int.Parse(end[1]), 0);

                if (DateTime.Now >= StartTime && DateTime.Now <= EndTime)
                {
                    List<string> columns = new List<string>();

                    columns = await _dataService.MapTableColumns(_inovoCIMDatabase, "vwCIMSnapShot");
                    await _dataService.DeleteCustom("DELETE FROM [dbo].[CIMSnapShot] WHERE CONVERT(DATE,[CIMDATE]) = CONVERT(DATE,GETDATE())", _reportDatabase);
                    _dataService.CopyTable(_inovoCIMDatabase, _reportDatabase, "SELECT * FROM dbo.vwCIMSnapShot", "dbo.CIMSnapShot", columns.ToArray());

                    columns = await _dataService.MapTableColumns(_inovoCIMDatabase, "vwAPT");
                    _dataService.CopyTable(_inovoCIMDatabase, _reportDatabase, "SELECT * FROM dbo.vwAPT", "dbo.APT", columns.ToArray());

                    columns = await _dataService.MapTableColumns(_inovoCIMDatabase, "vwAPTDetail");
                    _dataService.CopyTable(_inovoCIMDatabase, _reportDatabase, "SELECT * FROM dbo.vwAPTDetail", "dbo.APTDetail", columns.ToArray());

                    columns = await _dataService.MapTableColumns(_inovoCIMDatabase, "vwAPTOutput");
                    _dataService.CopyTable(_inovoCIMDatabase, _reportDatabase, "SELECT * FROM dbo.vwAPTOutput", "dbo.APTOutput", columns.ToArray());

                    columns = await _dataService.MapTableColumns(_inovoCIMDatabase, "vwAPTPhone");
                    _dataService.CopyTable(_inovoCIMDatabase, _reportDatabase, "SELECT * FROM dbo.vwAPTPhone", "dbo.APTPhone", columns.ToArray());

                    columns = await _dataService.MapTableColumns(_inovoCIMDatabase, "vwAPTSCI");
                    _dataService.CopyTable(_inovoCIMDatabase, _reportDatabase, "SELECT * FROM dbo.vwAPTSCI", "dbo.APTSCI", columns.ToArray());

                    columns = await _dataService.MapTableColumns(_inovoCIMDatabase, "vwBaseFileLeads");
                    _dataService.CopyTable(_inovoCIMDatabase, _reportDatabase, "SELECT * FROM dbo.vwBaseFileLeads", "dbo.BaseFileLeads", columns.ToArray());

                    columns = await _dataService.MapTableColumns(_inovoCIMDatabase, "vwLogError");
                    _dataService.CopyTable(_inovoCIMDatabase, _reportDatabase, "SELECT * FROM dbo.vwLogError", "dbo.LogError", columns.ToArray());
                }
            }
            catch (Exception ex)
            {
                await _logService.ErrorAsync(GetType().Name, MethodBase.GetCurrentMethod(), ex);
            }
        }
        #endregion

        #region [ APT Output File ]
        public async Task CreateAPTFile()
        {
            try
            {
                bool canWrite = false;
                string fileName = "APT_Triad_" + DateTime.Now.Year.ToString();
                fileName += (DateTime.Now.Month.ToString().Length == 1) ? "0" + DateTime.Now.Month.ToString() : DateTime.Now.Month.ToString();
                fileName += (DateTime.Now.Day.ToString().Length == 1) ? "0" + DateTime.Now.Day.ToString() + "_Day.txt" : DateTime.Now.Day.ToString() + "_Day.txt";
                using (new NetworkConnection(@"\\11.19.1.130\ftproot\APTData\Upload\Triad", new System.Net.NetworkCredential("jdg.co.za\\inovoadmin", "9GX89UEhjgp%")))
                {
                    canWrite = !File.Exists(@"\\11.19.1.130\ftproot\APTData\Upload\Triad\" + fileName.Replace(".txt", ".imp"));
                }
                if (canWrite)
                {
                    string file = String.Empty;
                    file += "HDR|7777|" + DateTime.Now.Year.ToString();
                    file += (DateTime.Now.Month.ToString().Length == 1) ? "0" + DateTime.Now.Month.ToString() : DateTime.Now.Month.ToString();
                    file += (DateTime.Now.Day.ToString().Length == 1) ? "0" + DateTime.Now.Day.ToString() + "|" : DateTime.Now.Day.ToString() + "|";
                    file += "TriadT\n";

                    List<MasterDataModel> phoneQueue = await _dataService.SelectMany<MasterDataModel, dynamic>("SELECT * FROM [dbo].[CIMSnapShot] WHERE APT_Account_Number IN (SELECT SourceID FROM [dbo].[PhoneQueue])", new { });
                    if (phoneQueue.Count > 0)
                    {
                        foreach (MasterDataModel record in phoneQueue)
                        {
                            string QueueID = await GetQueueID(record);
                            if (QueueID != String.Empty )
                            {
                                file += "TCI|";
                                file += "0000000000" + record.BP_Number.ToString() + "|";
                                file += "00" + record.CML_Unique_Key.ToString() + "|1|";
                                file += QueueID + "|";
                                file += "|";
                                file += "|";
                                file += "|";
                                file += QueueID + "|";
                                file += "100\n";
                            }

                        }
                        string countRecords = "000000000" + phoneQueue.Count.ToString();
                        int countRecordActual = countRecords.Length;


                        file += "FTR|" + countRecords.Substring((countRecordActual - 9), 9);

                        //StreamWriter sw = File.CreateText("C:\\x.SMSFiles\\" + fileName);
                        //sw.Write(file);
                        using (new NetworkConnection(@"\\11.19.1.130\ftproot\APTData\Upload\Triad", new System.Net.NetworkCredential("jdg.co.za\\inovoadmin", "9GX89UEhjgp%")))
                        {
                            using (StreamWriter sw = File.CreateText(@"\\11.19.1.130\ftproot\APTData\Upload\Triad\" + fileName))
                            {
                                await sw.WriteAsync(file);
                            }
                        }
                        //using (StreamWriter sw = File.CreateText(@"C:\Users\Waldo\Desktop\Work\JD\APT_Triad\" + fileName))
                        //{
                        //    await sw.WriteAsync(file);
                        //}
                    }
                }
                else return;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        #endregion

        #region [ Calculate Queue ID ]
        public async Task<string> GetQueueID(MasterDataModel recordIn)
        {
            try
            {
                string outString = String.Empty;
                switch (recordIn.Country)
                {
                    case "SA":
                        outString += "6";
                        break;
                    case "Nam":
                        outString += "7";
                        break;
                    case "Bot":
                        outString += "8";
                        break;
                    case "Swa":
                        outString += "9";
                        break;
                }
                if (outString == "6")
                {
                    if (recordIn.ServiceIDOut == 142)
                    {
                        outString += "1";
                    }
                    else if (recordIn.OutcomeTemporary == "92. Pre-Legal High-Risk")
                    {
                        outString += "6";
                    }
                    else
                    {
                        if (recordIn.Current_CD >= 0 && recordIn.Current_CD < 2)
                        {
                            outString += "2";
                        }
                        else if (recordIn.Current_CD >= 2 && recordIn.Current_CD <= 3)
                        {
                            outString += "3";
                        }
                        else if (recordIn.Current_CD >= 4)
                        {
                            outString += "4";
                        }
                    }
                }
                else if (outString == "7")
                {
                    if (recordIn.ServiceIDOut == 154)
                    {
                        outString += "1";
                    }
                    else if (recordIn.OutcomeTemporary == "92. Pre-Legal High-Risk")
                    {
                        outString += "6";
                    }
                    else
                    {
                        if (recordIn.Current_CD >= 0 && recordIn.Current_CD < 2)
                        {
                            outString += "2";
                        }
                        else if (recordIn.Current_CD >= 2 && recordIn.Current_CD <= 3)
                        {
                            outString += "3";
                        }
                        else if (recordIn.Current_CD >= 4)
                        {
                            outString += "4";
                        }
                    }
                }
                else if (outString == "8")
                {
                    if (recordIn.ServiceIDOut == 155)
                    {
                        outString += "1";
                    }
                    else if (recordIn.OutcomeTemporary == "92. Pre-Legal High-Risk")
                    {
                        outString += "6";
                    }
                    else
                    {
                        if (recordIn.Current_CD >= 0 && recordIn.Current_CD < 2)
                        {
                            outString += "2";
                        }
                        else if (recordIn.Current_CD >= 2 && recordIn.Current_CD <= 3)
                        {
                            outString += "3";
                        }
                        else if (recordIn.Current_CD >= 4)
                        {
                            outString += "4";
                        }
                    }
                }
                else if (outString == "9")
                {
                    if (recordIn.ServiceIDOut == 156)
                    {
                        outString += "1";
                    }
                    else if (recordIn.OutcomeTemporary == "92. Pre-Legal High-Risk")
                    {
                        outString += "6";
                    }
                    else
                    {
                        if (recordIn.Current_CD >= 0 && recordIn.Current_CD < 2)
                        {
                            outString += "2";
                        }
                        else if (recordIn.Current_CD >= 2 && recordIn.Current_CD <= 3)
                        {
                            outString += "3";
                        }
                        else if (recordIn.Current_CD >= 4)
                        {
                            outString += "4";
                        }
                    }
                }

                switch (recordIn.OutcomeTemporary)
                {
                    case "3. PTP Reminder":
                        outString += "23";
                        break;
                    case "4. Broken PTP":
                        outString += "01";
                        break;
                    case "7. Tracking":
                        outString += "03";
                        break;
                    case "8. RD Failure":
                        outString += "04";
                        break;
                    case "10. Dispute Failure":
                        outString += "05";
                        break;
                    case "11. PTP Arrears":
                        switch (recordIn.P_Collection_G)
                        {
                            case 3:
                                outString += "12";
                                break;
                            case 2:
                                outString += "13";
                                break;
                            case 1:
                                outString += "14";
                                break;
                            default:
                                outString += "13";
                                break;
                        }
                        break;
                    case "5. Partial Payment Broken PTP":
                        outString += "37";
                        break;
                    case "9. Admin Failure":
                        outString += "06";
                        break;
                    case "12. Partial Payment":
                        switch (recordIn.P_Collection_G)
                        {
                            case 3:
                                outString += "12";
                                break;
                            case 2:
                                outString += "13";
                                break;
                            case 1:
                                outString += "14";
                                break;
                            default:
                                outString += "12";
                                break;
                        }
                        break;
                    case "13. Call to revise PTP amount":
                        outString += "37";
                        break;
                    case "92. Pre-Legal High-Risk":
                        outString += "14";
                        //switch (recordIn.P_Collection_G)
                        //{
                        //    case 3:
                        //        outString += "12";
                        //        break;
                        //    case 2:
                        //        outString += "13";
                        //        break;
                        //    case 1:
                        //        outString += "14";
                        //        break;
                        //    default:
                        //        outString += "12";
                        //        break;
                        //}
                        break;
                    case "88. Pre-Trace SA":
                        outString += "14";
                        break;
                    case "89. Pre-Trace Non-SA":
                        outString += "14";
                        break;
                    case "90. Pre-Legal FPD":
                        outString += "16";
                        break;
                    case "91. Pre-Legal SPD":
                        outString += "17";
                        break;
                    case "6. PTP Call":
                        outString += (recordIn.Days_To_PTP_Due < -1 * (DateTime.Now.Day)) ? "18" : "19";
                        break;
                    case "2. PTP Call Internal":
                        outString += (recordIn.Days_To_PTP_Due < -1 * (DateTime.Now.Day)) ? "18" : "19";
                        break;
                    case "1. PTP Call By CRC":
                        outString += (recordIn.Days_To_PTP_Due < -1 * (DateTime.Now.Day)) ? "18" : "19";
                        break;
                    default:
                        string temp = recordIn.OutcomeTemporary;
                        return String.Empty;
                }
                if (outString.Length < 4)
                {
                    //
                }

                return outString;
            }
            catch (Exception ex)
            {
                return string.Empty;
            }
        }
        #endregion

        #region [Testing Replication]
        public async Task ReplicateTesting()
        {
            try
            {
                List<string> columns = new List<string>();
                columns = await _dataService.MapTableColumns(_inovoCIMDatabase, "vwCIMSnapShotTest");
                _dataService.Truncate("CIMSnapShotTest", _reportDatabase);
                _dataService.CopyTable(_inovoCIMDatabase, _reportDatabase, "SELECT * FROM dbo.vwCIMSnapShotTest", "dbo.CIMSnapShotTest", columns.ToArray());

                string ViewTable = "vwLiveMailQueueTest";
                string ReportTable = "LiveMailQueueTest";

                columns = await _dataService.MapTableColumns(_inovoCIMDatabase, ViewTable);
                _dataService.Truncate(ReportTable, _reportDatabase);
                _dataService.CopyTable(_inovoCIMDatabase, _reportDatabase, $"SELECT * FROM dbo.{ViewTable}", $"dbo.{ReportTable}", columns.ToArray());

                ViewTable = "vwLiveSMSQueueTest";
                ReportTable = "LiveSMSQueueTest";

                columns = await _dataService.MapTableColumns(_inovoCIMDatabase, ViewTable);
                _dataService.Truncate(ReportTable, _reportDatabase);
                _dataService.CopyTable(_inovoCIMDatabase, _reportDatabase, $"SELECT * FROM dbo.{ViewTable}", $"dbo.{ReportTable}", columns.ToArray());

            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        #endregion

        #region [ Live Data ]
        public async Task LiveData()
        {
            List<string> columns = new List<string>();

            try
            {
                string ViewTable = "vwLiveAPT";
                string ReportTable = "LiveAPT";

                columns = await _dataService.MapTableColumns(_inovoCIMDatabase, ViewTable);
                _dataService.Truncate(ReportTable, _reportDatabase);
                _dataService.CopyTable(_inovoCIMDatabase, _reportDatabase, $"SELECT * FROM dbo.{ViewTable}", $"dbo.{ReportTable}", columns.ToArray());

                //----------------------------------------//

                ViewTable = "vwLiveSMSReply";
                ReportTable = "LiveSMSReply";

                columns = await _dataService.MapTableColumns(_inovoCIMDatabase, ViewTable);
                _dataService.Truncate(ReportTable, _reportDatabase);
                _dataService.CopyTable(_inovoCIMDatabase, _reportDatabase, $"SELECT * FROM dbo.{ViewTable}", $"dbo.{ReportTable}", columns.ToArray());

                //----------------------------------------//

                ViewTable = "vwLiveAPTDetail";
                ReportTable = "LiveAPTDetail";

                columns = await _dataService.MapTableColumns(_inovoCIMDatabase, ViewTable);
                _dataService.Truncate(ReportTable, _reportDatabase);
                _dataService.CopyTable(_inovoCIMDatabase, _reportDatabase, $"SELECT * FROM dbo.{ViewTable}", $"dbo.{ReportTable}", columns.ToArray());

                //----------------------------------------//

                ViewTable = "vwLiveAPTOutput";
                ReportTable = "LiveAPTOutput";

                columns = await _dataService.MapTableColumns(_inovoCIMDatabase, ViewTable);
                _dataService.Truncate(ReportTable, _reportDatabase);
                _dataService.CopyTable(_inovoCIMDatabase, _reportDatabase, $"SELECT * FROM dbo.{ViewTable}", $"dbo.{ReportTable}", columns.ToArray());

                //----------------------------------------//

                ViewTable = "vwLiveAPTPhone";
                ReportTable = "LiveAPTPhone";

                columns = await _dataService.MapTableColumns(_inovoCIMDatabase, ViewTable);
                _dataService.Truncate(ReportTable, _reportDatabase);
                _dataService.CopyTable(_inovoCIMDatabase, _reportDatabase, $"SELECT * FROM dbo.{ViewTable}", $"dbo.{ReportTable}", columns.ToArray());

                //----------------------------------------//

                ViewTable = "vwLiveAPTSCI";
                ReportTable = "LiveAPTSCI";

                columns = await _dataService.MapTableColumns(_inovoCIMDatabase, ViewTable);
                _dataService.Truncate(ReportTable, _reportDatabase);
                _dataService.CopyTable(_inovoCIMDatabase, _reportDatabase, $"SELECT * FROM dbo.{ViewTable}", $"dbo.{ReportTable}", columns.ToArray());

                //----------------------------------------//

                ViewTable = "vwLiveBaseFileLeads";
                ReportTable = "LiveBaseFileLeads";

                columns = await _dataService.MapTableColumns(_inovoCIMDatabase, ViewTable);
                _dataService.Truncate(ReportTable, _reportDatabase);
                _dataService.CopyTable(_inovoCIMDatabase, _reportDatabase, $"SELECT * FROM dbo.{ViewTable}", $"dbo.{ReportTable}", columns.ToArray());

                //----------------------------------------//

                ViewTable = "vwLiveBureau";
                ReportTable = "LiveBureau";

                columns = await _dataService.MapTableColumns(_inovoCIMDatabase, ViewTable);
                _dataService.Truncate(ReportTable, _reportDatabase);
                _dataService.CopyTable(_inovoCIMDatabase, _reportDatabase, $"SELECT * FROM dbo.{ViewTable}", $"dbo.{ReportTable}", columns.ToArray());

                //----------------------------------------//

                ViewTable = "vwLiveCIMSnapShot";
                ReportTable = "LiveCIMSnapShot";

                columns = await _dataService.MapTableColumns(_inovoCIMDatabase, ViewTable);
                _dataService.Truncate(ReportTable, _reportDatabase);
                _dataService.CopyTable(_inovoCIMDatabase, _reportDatabase, $"SELECT * FROM dbo.{ViewTable}", $"dbo.{ReportTable}", columns.ToArray());

                //----------------------------------------//

                ViewTable = "vwLiveHyphen";
                ReportTable = "LiveHyphen";

                columns = await _dataService.MapTableColumns(_inovoCIMDatabase, ViewTable);
                _dataService.Truncate(ReportTable, _reportDatabase);
                _dataService.CopyTable(_inovoCIMDatabase, _reportDatabase, $"SELECT * FROM dbo.{ViewTable}", $"dbo.{ReportTable}", columns.ToArray());

                //----------------------------------------//

                ViewTable = "vwLiveLastPayment";
                ReportTable = "LiveLastPayment";

                columns = await _dataService.MapTableColumns(_inovoCIMDatabase, ViewTable);
                _dataService.Truncate(ReportTable, _reportDatabase);
                _dataService.CopyTable(_inovoCIMDatabase, _reportDatabase, $"SELECT * FROM dbo.{ViewTable}", $"dbo.{ReportTable}", columns.ToArray());

                //----------------------------------------//

                ViewTable = "vwLiveLegalPreLegal";
                ReportTable = "LiveLegalPreLegal";

                columns = await _dataService.MapTableColumns(_inovoCIMDatabase, ViewTable);
                _dataService.Truncate(ReportTable, _reportDatabase);
                _dataService.CopyTable(_inovoCIMDatabase, _reportDatabase, $"SELECT * FROM dbo.{ViewTable}", $"dbo.{ReportTable}", columns.ToArray());

                //----------------------------------------//

                ViewTable = "vwLiveLogError";
                ReportTable = "LiveLogError";

                columns = await _dataService.MapTableColumns(_inovoCIMDatabase, ViewTable);
                _dataService.Truncate(ReportTable, _reportDatabase);
                _dataService.CopyTable(_inovoCIMDatabase, _reportDatabase, $"SELECT * FROM dbo.{ViewTable}", $"dbo.{ReportTable}", columns.ToArray());

                //----------------------------------------//

                ViewTable = "vwLiveMailQueue";
                ReportTable = "LiveMailQueue";

                columns = await _dataService.MapTableColumns(_inovoCIMDatabase, ViewTable);
                _dataService.Truncate(ReportTable, _reportDatabase);
                _dataService.CopyTable(_inovoCIMDatabase, _reportDatabase, $"SELECT * FROM dbo.{ViewTable}", $"dbo.{ReportTable}", columns.ToArray());

                //----------------------------------------//

                ViewTable = "vwLiveMailSent";
                ReportTable = "LiveMailSent";

                columns = await _dataService.MapTableColumns(_inovoCIMDatabase, ViewTable);
                _dataService.Truncate(ReportTable, _reportDatabase);
                _dataService.CopyTable(_inovoCIMDatabase, _reportDatabase, $"SELECT * FROM dbo.{ViewTable}", $"dbo.{ReportTable}", columns.ToArray());

                //----------------------------------------//

                ViewTable = "vwLivePhoneQueue";
                ReportTable = "LivePhoneQueue";

                columns = await _dataService.MapTableColumns(_inovoCIMDatabase, ViewTable);
                _dataService.Truncate(ReportTable, _reportDatabase);
                _dataService.CopyTable(_inovoCIMDatabase, _reportDatabase, $"SELECT * FROM dbo.{ViewTable}", $"dbo.{ReportTable}", columns.ToArray());

                //----------------------------------------//

                ViewTable = "vwLivePhoneQueueDone";
                ReportTable = "LivePhoneQueueDone";

                columns = await _dataService.MapTableColumns(_inovoCIMDatabase, ViewTable);
                _dataService.Truncate(ReportTable, _reportDatabase);
                _dataService.CopyTable(_inovoCIMDatabase, _reportDatabase, $"SELECT * FROM dbo.{ViewTable}", $"dbo.{ReportTable}", columns.ToArray());

                //----------------------------------------//

                ViewTable = "vwLivePhoneRoute";
                ReportTable = "LivePhoneRoute";

                columns = await _dataService.MapTableColumns(_inovoCIMDatabase, ViewTable);
                _dataService.Truncate(ReportTable, _reportDatabase);
                _dataService.CopyTable(_inovoCIMDatabase, _reportDatabase, $"SELECT * FROM dbo.{ViewTable}", $"dbo.{ReportTable}", columns.ToArray());

                //----------------------------------------//

                ViewTable = "vwLivePreTrace";
                ReportTable = "LivePreTrace";

                columns = await _dataService.MapTableColumns(_inovoCIMDatabase, ViewTable);
                _dataService.Truncate(ReportTable, _reportDatabase);
                _dataService.CopyTable(_inovoCIMDatabase, _reportDatabase, $"SELECT * FROM dbo.{ViewTable}", $"dbo.{ReportTable}", columns.ToArray());

                //----------------------------------------//

                ViewTable = "vwLivePreTraceQCodes";
                ReportTable = "LivePreTraceQCodes";

                columns = await _dataService.MapTableColumns(_inovoCIMDatabase, ViewTable);
                _dataService.Truncate(ReportTable, _reportDatabase);
                _dataService.CopyTable(_inovoCIMDatabase, _reportDatabase, $"SELECT * FROM dbo.{ViewTable}", $"dbo.{ReportTable}", columns.ToArray());

                //----------------------------------------//

                ViewTable = "vwLiveSAP1";
                ReportTable = "LiveSAP1";

                columns = await _dataService.MapTableColumns(_inovoCIMDatabase, ViewTable);
                _dataService.Truncate(ReportTable, _reportDatabase);
                _dataService.CopyTable(_inovoCIMDatabase, _reportDatabase, $"SELECT * FROM dbo.{ViewTable}", $"dbo.{ReportTable}", columns.ToArray());

                //----------------------------------------//

                ViewTable = "vwLiveSMSQueue";
                ReportTable = "LiveSMSQueue";

                columns = await _dataService.MapTableColumns(_inovoCIMDatabase, ViewTable);
                _dataService.Truncate(ReportTable, _reportDatabase);
                _dataService.CopyTable(_inovoCIMDatabase, _reportDatabase, $"SELECT * FROM dbo.{ViewTable}", $"dbo.{ReportTable}", columns.ToArray());

                //----------------------------------------//

                ViewTable = "vwLiveSMSSent";
                ReportTable = "LiveSMSSent";

                columns = await _dataService.MapTableColumns(_inovoCIMDatabase, ViewTable);
                _dataService.Truncate(ReportTable, _reportDatabase);
                _dataService.CopyTable(_inovoCIMDatabase, _reportDatabase, $"SELECT * FROM dbo.{ViewTable}", $"dbo.{ReportTable}", columns.ToArray());

                //----------------------------------------//

                ViewTable = "vwLiveTracePreTrace";
                ReportTable = "LiveTracePreTrace";

                columns = await _dataService.MapTableColumns(_inovoCIMDatabase, ViewTable);
                _dataService.Truncate(ReportTable, _reportDatabase);
                _dataService.CopyTable(_inovoCIMDatabase, _reportDatabase, $"SELECT * FROM dbo.{ViewTable}", $"dbo.{ReportTable}", columns.ToArray());

                //----------------------------------------//

                ViewTable = "vwLiveTracking";
                ReportTable = "LiveTracking";

                columns = await _dataService.MapTableColumns(_inovoCIMDatabase, ViewTable);
                _dataService.Truncate(ReportTable, _reportDatabase);
                _dataService.CopyTable(_inovoCIMDatabase, _reportDatabase, $"SELECT * FROM dbo.{ViewTable}", $"dbo.{ReportTable}", columns.ToArray());

                //----------------------------------------//

                ViewTable = "vwLiveTransactionSummary";
                ReportTable = "LiveTransactionSummary";

                columns = await _dataService.MapTableColumns(_inovoCIMDatabase, ViewTable);
                _dataService.Truncate(ReportTable, _reportDatabase);
                _dataService.CopyTable(_inovoCIMDatabase, _reportDatabase, $"SELECT * FROM dbo.{ViewTable}", $"dbo.{ReportTable}", columns.ToArray());

                //----------------------------------------//

                ViewTable = "vwLiveZCMLData";
                ReportTable = "LiveZCMLData";

                columns = await _dataService.MapTableColumns(_inovoCIMDatabase, ViewTable);
                _dataService.Truncate(ReportTable, _reportDatabase);
                _dataService.CopyTable(_inovoCIMDatabase, _reportDatabase, $"SELECT * FROM dbo.{ViewTable}", $"dbo.{ReportTable}", columns.ToArray());

                //----------------------------------------//

                ViewTable = "vwLiveZCMLPaymentQueue";
                ReportTable = "LiveZCMLPaymentQueue";

                columns = await _dataService.MapTableColumns(_inovoCIMDatabase, ViewTable);
                _dataService.Truncate(ReportTable, _reportDatabase);
                _dataService.CopyTable(_inovoCIMDatabase, _reportDatabase, $"SELECT * FROM dbo.{ViewTable}", $"dbo.{ReportTable}", columns.ToArray());
            }
            catch (Exception ex)
            {
                await _logService.ErrorAsync(GetType().Name, MethodBase.GetCurrentMethod(), ex);
            }
        }
        #endregion

        #region [ Run Respin ]
        public async Task RunRespin()
        {
            try
            {
                await _dataService.SelectMany<dynamic, dynamic>("EXECUTE [dbo].[spRunRespin]", new { });
            }
            catch (Exception ex)
            {
                await _logService.ErrorAsync(GetType().Name, MethodBase.GetCurrentMethod(), ex);
            }
        }
        #endregion

        #region [ Mail Report ]
        public async Task MailReport()
        {
            int year = DateTime.Now.Year;
            int month = DateTime.Now.Month;
            int day = DateTime.Now.Day;

            try
            {
                string[] start01 = _appSettings.MailReport.StartRun01.Split(':').ToArray();
                string[] end01 = _appSettings.MailReport.EndRun01.Split(':').ToArray();
                DateTime StartTime01 = new DateTime(year, month, day, int.Parse(start01[0]), int.Parse(start01[1]), 0);
                DateTime EndTime01 = new DateTime(year, month, day, int.Parse(end01[0]), int.Parse(end01[1]), 0);

                string[] start02 = _appSettings.MailReport.StartRun02.Split(':').ToArray();
                string[] end02 = _appSettings.MailReport.EndRun02.Split(':').ToArray();
                DateTime StartTime02 = new DateTime(year, month, day, int.Parse(start02[0]), int.Parse(start02[1]), 0);
                DateTime EndTime02 = new DateTime(year, month, day, int.Parse(end02[0]), int.Parse(end02[1]), 0);

                string[] start03 = _appSettings.MailReport.StartRun03.Split(':').ToArray();
                string[] end03 = _appSettings.MailReport.EndRun03.Split(':').ToArray();
                DateTime StartTime03 = new DateTime(year, month, day, int.Parse(start03[0]), int.Parse(start03[1]), 0);
                DateTime EndTime03 = new DateTime(year, month, day, int.Parse(end03[0]), int.Parse(end03[1]), 0);

                string[] start04 = _appSettings.MailReport.StartRun04.Split(':').ToArray();
                string[] end04 = _appSettings.MailReport.EndRun04.Split(':').ToArray();
                DateTime StartTime04 = new DateTime(year, month, day, int.Parse(start04[0]), int.Parse(start04[1]), 0);
                DateTime EndTime04 = new DateTime(year, month, day, int.Parse(end04[0]), int.Parse(end04[1]), 0);

                if (DateTime.Now >= StartTime01 && DateTime.Now <= EndTime01)
                {
                    await RunMailReport();
                }

                if (DateTime.Now >= StartTime02 && DateTime.Now <= EndTime02)
                {
                    await RunMailReport();
                }

                if (DateTime.Now >= StartTime03 && DateTime.Now <= EndTime03)
                {
                    await RunMailReport();
                }

                if (DateTime.Now >= StartTime04 && DateTime.Now <= EndTime04)
                {
                    await RunMailReport();
                }
            }
            catch (Exception ex)
            {
                await _logService.ErrorAsync(GetType().Name, MethodBase.GetCurrentMethod(), ex);
            }
        }
        #endregion

        //-----------------//

        #region [ Run Mail Report ]
        private async Task RunMailReport()
        {
            try
            {
                List<PresenceSummary> presenceSummary = await _dataService.SelectMany<PresenceSummary, dynamic>("EXECUTE [dbo].[spReportPresenceSummary]", new { });
                List<PresenceLog> presenceLog = await _dataService.SelectMany<PresenceLog, dynamic>("EXECUTE [dbo].[spReportPresenceLog]", new { });


                List<EmailList> collection = await _dataService.SelectMany<EmailList, dynamic>("SELECT * FROM [EmailList]", new { });
                //List<EmailList> collection = new List<EmailList>() { new EmailList() { Email = "wvandenberg@inovo.co.za" } };
                List<SMSSent> sents = await _dataService.SelectMany<SMSSent, dynamic>("EXECUTE [dbo].[spGetSMSByCountry]", new { });
                List<SMSSent> mails = await _dataService.SelectMany<SMSSent, dynamic>("EXECUTE [dbo].[spGetMailVolume]", new { });
                DateTime TriadCreatedDate = await GetTriadLoaded();
                await _emailService.MailReport(presenceSummary, presenceLog, sents, mails, TriadCreatedDate, collection);
            }
            catch (Exception ex)
            {
                await _logService.ErrorAsync(GetType().Name, MethodBase.GetCurrentMethod(), ex);
            }
        }
        #endregion

        #region [ Check Triad File ]
        public async Task<DateTime> GetTriadLoaded()
        {
            DateTime modDate;

            string fileName = "APT_Triad_" + DateTime.Now.Year.ToString();
            fileName += (DateTime.Now.Month.ToString().Length == 1) ? "0" + DateTime.Now.Month.ToString() : DateTime.Now.Month.ToString();
            fileName += (DateTime.Now.Day.ToString().Length == 1) ? "0" + DateTime.Now.Day.ToString() + "_Day.txt" : DateTime.Now.Day.ToString() + "_Day.txt";

            using (new NetworkConnection(@"\\11.19.1.130\ftproot\APTData\Upload\Triad", new System.Net.NetworkCredential("jdg.co.za\\inovoadmin", "9GX89UEhjgp%")))
            {
                string dirCreated = @"\\11.19.1.130\ftproot\APTData\Upload\Triad\" + fileName;
                string dirRead = dirCreated.Replace(".txt", ".imp");
                try
                {
                    modDate = System.IO.File.GetLastWriteTime(dirCreated);
                    if (modDate.Year < 2019)
                    {
                        modDate = System.IO.File.GetLastWriteTime(dirRead);
                    }
                }
                catch (Exception ex)
                {
                    throw;
                }
            }
            return modDate;
        }

        #endregion
    }
}