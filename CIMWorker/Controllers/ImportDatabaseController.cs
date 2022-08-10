#region [ using ]
using CIMWorker.Data.Entities;
using CIMWorker.Helpers;
using CIMWorker.Models;
using CIMWorker.Services;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text;
using System.Net;
using System.Security.Authentication;
using Newtonsoft.Json;
#endregion

namespace CIMWorker.Controllers
{
    public class IntClass
    {
        public int SOURCEID { get; set; }
    }
    public class ImportDatabaseController
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
        private readonly string _baseURL;
        private readonly AppSettings _appSettings;
        #endregion

        #region [ Default Constructor ]
        public ImportDatabaseController(IOptions<AppSettings> appSettings, IDataService dataService, IDiallerService diallerService, ILogService logService, IEmailService emailService, IRuleService ruleService)
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
            //_baseURL = "https://localhost:5001/api/";
            _baseURL = "https://vwmompcmprd101.metmom.mmih.biz/api/";
        }
        #endregion

        //-----------------------------//

        #region [ Master Async ]
        public async Task<bool> MasterAsync()
        {
            await _logService.InfoAsync("Import Database Controller Started");
            int Pass = 0, Fail = 0;

            List<InPhoneCallActivity> collection = await _dataService.SelectMany<InPhoneCallActivity, dynamic>("EXECUTE [dbo].[spGetNewPhoneCallsv2]", new { });
            List<InMaxStatus> inMaxStatuses = await _dataService.SelectMany<InMaxStatus, dynamic>("EXECUTE [dbo].[spGetNewMaximums]", new { });

            await _logService.InfoAsync($"Import Database : Phone Calls Query Returned {collection.Count()}");
            if (collection != null)
            {
                if (collection.Count() > 0)
                {
                    foreach (var item in collection)
                    {
                        if(item.OutcomeType == "No Application")
                        {
                            var output = item.GetOutCall();
                            var data = new StringContent(JsonConvert.SerializeObject(output), Encoding.UTF8, "application/json");
                            using var client = await GetHttpClient();
                            try
                            {
                                System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
                                //var check = await client.PostAsync("https://vwmompcmprd101.metmom.mmih.biz/api/createphonecallnoapp", data);
                                var check = await client.PostAsync(_baseURL + "createphonecallnoapp", data);
                                Console.WriteLine(await check.Content.ReadAsStringAsync());
                                if (check.IsSuccessStatusCode)
                                {
                                    await InsertOBLSent(item.ID);
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex.Message);
                                Console.WriteLine(ex.InnerException.Message);
                            }
                        }
                        else if (item.OutcomeType == "No Review")
                        {
                            var output = item.GetOutCall();
                            var data = new StringContent(JsonConvert.SerializeObject(output), Encoding.UTF8, "application/json");
                            using var client = await GetHttpClient();
                            try
                            {
                                System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
                                //var check = await client.PostAsync("https://vwmompcmprd101.metmom.mmih.biz/api/createphonecallnoreview", data);
                                var check = await client.PostAsync(_baseURL + "createphonecallnoreview", data);
                                Console.WriteLine(await check.Content.ReadAsStringAsync());
                                if (check.IsSuccessStatusCode)
                                {
                                    await InsertOBLSent(item.ID);
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex.Message);
                                Console.WriteLine(ex.InnerException.Message);
                            }
                        }
                        else if(item.OutcomeType == "Won")
                        {
                            var output = item.GetOutCall();
                            var data = new StringContent(JsonConvert.SerializeObject(output), Encoding.UTF8, "application/json");
                            using var client = await GetHttpClient();
                            try
                            {
                                System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
                                //var check = await client.PostAsync("https://vwmompcmprd101.metmom.mmih.biz/api/createphonecallwon", data);
                                var check = await client.PostAsync(_baseURL + "createphonecallwon", data);
                                Console.WriteLine(await check.Content.ReadAsStringAsync());
                                if (check.IsSuccessStatusCode)
                                {
                                    await InsertOBLSent(item.ID);
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex.Message);
                                Console.WriteLine(ex.InnerException.Message);
                            }
                        }
                        else if (item.OutcomeType == "Lost" || item.OutcomeType == "Uncontactable")
                        {
                            var output = item.GetOutCall();
                            var data = new StringContent(JsonConvert.SerializeObject(output), Encoding.UTF8, "application/json");
                            using var client = await GetHttpClient();
                            try
                            {
                                System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
                                //var check = await client.PostAsync("https://vwmompcmprd101.metmom.mmih.biz/api/createphonecalllost", data);
                                var check = await client.PostAsync(_baseURL + "createphonecalllost", data);
                                Console.WriteLine(await check.Content.ReadAsStringAsync());
                                if (check.IsSuccessStatusCode)
                                {
                                    await InsertOBLSent(item.ID);
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex.Message);
                                Console.WriteLine(ex.InnerException.Message);
                            }
                        }
                        else if (item.OutcomeType == "No Contact")
                        {
                            var output = item.GetOutCall();
                            var data = new StringContent(JsonConvert.SerializeObject(output), Encoding.UTF8, "application/json");
                            using var client = await GetHttpClient();
                            try
                            {
                                System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
                                //var check = await client.PostAsync("https://vwmompcmprd101.metmom.mmih.biz/api/createphonecallnocontact", data);
                                var check = await client.PostAsync(_baseURL + "createphonecallnocontact", data);
                                Console.WriteLine(await check.Content.ReadAsStringAsync());
                                if (check.IsSuccessStatusCode)
                                {
                                    await InsertOBLSent(item.ID);
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex.Message);
                                Console.WriteLine(ex.InnerException.Message);
                            }
                        }
                        else if (item.OutcomeType == "Hot Transfer")
                        {
                            var output = item.GetOutCall();
                            var data = new StringContent(JsonConvert.SerializeObject(output), Encoding.UTF8, "application/json");
                            using var client = await GetHttpClient();
                            try
                            {
                                System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
                                //var check = await client.PostAsync("https://vwmompcmprd101.metmom.mmih.biz/api/createphonecallhottransfer", data);
                                var check = await client.PostAsync(_baseURL + "createphonecallhottransfer", data);
                                Console.WriteLine(await check.Content.ReadAsStringAsync());
                                if (check.IsSuccessStatusCode)
                                {
                                    await InsertOBLSent(item.ID);
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex.Message);
                                Console.WriteLine(ex.InnerException.Message);
                            }
                        }
                        else if (item.OutcomeType == "Route to Digital Adviser")
                        {
                            var output = item.GetOutCall();
                            var data = new StringContent(JsonConvert.SerializeObject(output), Encoding.UTF8, "application/json");
                            using var client = await GetHttpClient();
                            try
                            {
                                System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
                                //var check = await client.PostAsync("https://vwmompcmprd101.metmom.mmih.biz/api/createphonecall", data);
                                var check = await client.PostAsync(_baseURL + "createphonecall", data);
                                Console.WriteLine(await check.Content.ReadAsStringAsync());
                                if (check.IsSuccessStatusCode)
                                {
                                    await InsertOBLSent(item.ID);
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex.Message);
                                Console.WriteLine(ex.InnerException.Message);
                            }
                        }
                        else if (item.OutcomeType == "Schedule")
                        {
                            var output = item.GetOutSchedule();
                            var data = new StringContent(JsonConvert.SerializeObject(output), Encoding.UTF8, "application/json");
                            using var client = await GetHttpClient();
                            try
                            {
                                System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
                                //var check = await client.PostAsync("https://vwmompcmprd101.metmom.mmih.biz/api/createphonecallschedule", data);
                                var check = await client.PostAsync(_baseURL + "createphonecallschedule", data);
                                Console.WriteLine(await check.Content.ReadAsStringAsync());
                                if (check.IsSuccessStatusCode)
                                {
                                    await InsertOBLSent(item.ID);
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex.Message);
                                Console.WriteLine(ex.InnerException.Message);
                            }
                        }
                        else
                        {
                            var output = item.GetOutCall();
                            var data = new StringContent(JsonConvert.SerializeObject(output), Encoding.UTF8, "application/json");
                            using var client = await GetHttpClient();
                            try
                            {
                                System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
                                //var check = await client.PostAsync("https://vwmompcmprd101.metmom.mmih.biz/api/createphonecallschedule", data);
                                var check = await client.PostAsync(_baseURL + "createphonecallother", data);
                                Console.WriteLine(await check.Content.ReadAsStringAsync());
                                if (check.IsSuccessStatusCode)
                                {
                                    await InsertOBLSent(item.ID);
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex.Message);
                                Console.WriteLine(ex.InnerException.Message);
                            }
                        }
                    }
                }
            }

            await _logService.InfoAsync($"Import Database : Maximum Status Query Returned {inMaxStatuses.Count()}");

            if (inMaxStatuses != null)
            {
                if(inMaxStatuses.Count > 0)
                {
                    foreach(var item in inMaxStatuses)
                    {
                        var output = item.GetOutModel();
                        var data = new StringContent(JsonConvert.SerializeObject(output), Encoding.UTF8, "application/json");
                        using var client = await GetHttpClient();
                        try
                        {
                            System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
                            var check = await client.PostAsync(_baseURL + "disqualifylead", data);
                            Console.WriteLine(await check.Content.ReadAsStringAsync());
                            if(check.IsSuccessStatusCode)
                            {
                                await InsertOBQSent(item.ID);
                            }
                        }
                        catch(Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                            Console.WriteLine(ex.InnerException.Message);
                        }
                    }
                }
            }

            //TimeSpan runSpan = DateTime.Now.Subtract(_startTime);
            //double RecordsPerSecond = ((inMaxStatuses.Count + collection.Count) / runSpan.TotalSeconds);
            //await _logService.InfoAsync($"Import Database Controller Ended : Runtime -> Records per Second: {RecordsPerSecond}");
            return true;
        }
        #endregion

        #region [ Insert into OBLSent ]
        private async Task InsertOBLSent(int IDin)
        {
            try
            {
                await _dataService.InsertSingle<dynamic, dynamic>(@"INSERT INTO [dbo].[OBLSent]
                                                                                       ([OBLID])
                                                                                 VALUES
                                                                                       (@OBLID)", new { OBLID = IDin });
            }
            catch(Exception)
            {
                throw;
            }
        }
        #endregion

        #region [ Insert into OBLSent ]
        private async Task InsertOBQSent(int IDin)
        {
            try
            {
                await _dataService.InsertSingle<dynamic, dynamic>(@"INSERT INTO [dbo].[OBQSent]
                                                                                       ([OBQID])
                                                                                 VALUES
                                                                                       (@OBQID)", new { OBQID = IDin });
            }
            catch (Exception)
            {
                throw;
            }
        }
        #endregion

        #region [ Get HTTP Client ]
        private async Task<HttpClient> GetHttpClient()
        {
            try
            {
                var httpClientHandler = new HttpClientHandler() {
                    SslProtocols = SslProtocols.Tls12 | SslProtocols.Tls11 | SslProtocols.Tls
                };
                httpClientHandler.ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) =>
                {
                    return true;
                };
                HttpClient client = new HttpClient(httpClientHandler);
                client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
                var authenticationString = "inovotest:ABLeORYgElde";
                var base64EncodedAuthenticationString = Convert.ToBase64String(System.Text.ASCIIEncoding.UTF8.GetBytes(authenticationString));
                client.DefaultRequestHeaders.Add("Authorization", "Basic " + base64EncodedAuthenticationString);
                return client;
            }
            catch (Exception)
            {
                throw;
            }
        }
        #endregion
        //----------------------------------------//
    }
}