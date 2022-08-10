#region [ using ]
using CIMWorker.Data.Entities.Custom;
using CIMWorker.Helpers;
using CIMWorker.Models;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
#endregion

namespace CIMWorker.Services
{
   #region [ Interface ]
   public interface IEmailService
   {
      Task<bool> SendAsync(string Body, string Subject, string MailTo);
      Task<bool> MailReport(List<PresenceSummary> presenceSummary, List<PresenceLog> presenceLog, List<SMSSent> smsList, List<SMSSent> emailList, DateTime TriadLoadedDate, List<EmailList> sendList);
   }
   #endregion

   //--------------------------------------------//

   public class EmailService : IEmailService
   {
      private readonly AppSettings _appSettings;

      #region [ Default Constructor ]
      public EmailService(IOptions<AppSettings> appSettings)
      {
         _appSettings = appSettings.Value;
      }
      #endregion

      //-----------------------------//

      #region [ Mail Error ]
      public async Task<bool> MailError()
      {
         try
         {
            string TempClient = File.ReadAllText(Directory.GetCurrentDirectory() + @"\HtmlTemplates\Error.html");

            TempClient = TempClient.Replace("#FILENAME#", "");
            TempClient = TempClient.Replace("#DATE#", DateTime.Now.ToString("yyyy-MM-dd hh:mm tt", CultureInfo.InvariantCulture));

            return await SendAsync(TempClient, "Mail Error", "wvandenberg@inovo.co.za");

         }
         catch (Exception ex)
         {
            return false;
         }
      }
      #endregion

      #region [ Mail Notify ]
      public async Task<bool> MailNotify()
      {
         try
         {
            string TempClient = File.ReadAllText(Directory.GetCurrentDirectory() + @"\HtmlTemplates\Error.html");

            TempClient = TempClient.Replace("#FILENAME#", "");
            TempClient = TempClient.Replace("#DATE#", DateTime.Now.ToString("yyyy-MM-dd hh:mm tt", CultureInfo.InvariantCulture));

            return await SendAsync(TempClient, "Mail Error", "wvandenberg@inovo.co.za");

         }
         catch (Exception ex)
         {
            return false;
         }
      }
      #endregion

      #region [ Mail Report ]
      public async Task<bool> MailReport(List<PresenceSummary> presenceSummary, List<PresenceLog> presenceLog, List<SMSSent> smsList, List<SMSSent> emailList, DateTime TriadLoadedDate, List<EmailList> sendList)
      {
         try
         {
            string TempClient = File.ReadAllText(Directory.GetCurrentDirectory() + @"\HtmlTemplates\Report.html");

            StringBuilder strSummary = new StringBuilder();
            foreach (var summary in presenceSummary)
            {
               strSummary.Append("<tr>");
               strSummary.Append($"<td><small>{summary.SERVICENAME}</small></td>");
               strSummary.Append($"<td style=\"text-align:center\"><small>{summary.SERVICEID}</small></td>");
               strSummary.Append($"<td style=\"text-align:center\"><small>{summary.TOTAL}</small></td>");
               strSummary.Append($"<td style=\"text-align:center\"><small>{summary.INITIAL}</small></td>");
               strSummary.Append($"<td style=\"text-align:center\"><small>{summary.SCHEDULE}</small></td>");
               strSummary.Append($"<td style=\"text-align:center\"><small>{summary.INVALID}</small></td>");
               strSummary.Append($"<td style=\"text-align:center\"><small>{summary.COMPLETE}</small></td>");
               strSummary.Append("</tr>");
            }
            TempClient = TempClient.Replace("#PRESENCESUMMARY#", strSummary.ToString());

            StringBuilder strLog = new StringBuilder();
            foreach (var log in presenceLog)
            {
               strLog.Append("<tr>");
               strLog.Append($"<td><small>{log.SERVICENAME}</small></td>");
               strLog.Append($"<td style=\"text-align:center\"><small>{log.SERVICEID}</small></td>");
               strLog.Append($"<td style=\"text-align:center\"><small>{log.TOTAL}</small></td>");
               strLog.Append($"<td style=\"text-align:center\"><small>{log.NONUSEFUL}</small></td>");
               strLog.Append($"<td style=\"text-align:center\"><small>{log.NEGATIVE}</small></td>");
               strLog.Append($"<td style=\"text-align:center\"><small>{log.POSITIVE}</small></td>");
               strLog.Append("</tr>");
            }
            TempClient = TempClient.Replace("#PRESENCELOGS#", strLog.ToString());

            StringBuilder strSMS = new StringBuilder();
            foreach (var sms in smsList)
            {
                strSMS.Append("<tr>");
                strSMS.Append($"<td><small>{sms.COUNTRY}</small></td>");
                strSMS.Append($"<td style=\"text-align:center\"><small>{sms.SENT}</small></td>");
                strSMS.Append("</tr>");
            }
            TempClient = TempClient.Replace("#SMSLOGS#", strSMS.ToString());

            StringBuilder strEmail = new StringBuilder();
            foreach (var email in emailList)
            {
                strEmail.Append("<tr>");
                strEmail.Append($"<td><small>{email.COUNTRY}</small></td>");
                strEmail.Append($"<td style=\"text-align:center\"><small>{email.SENT}</small></td>");
                strEmail.Append("</tr>");
            }
            TempClient = TempClient.Replace("#MAILLOGS#", strEmail.ToString());

            string triadLoaded = "No";

            if (TriadLoadedDate.Date == DateTime.Now.Date)
            {
                triadLoaded = "Yes";
            }

            StringBuilder strTriad = new StringBuilder();
            strTriad.Append("<tr>");
            strTriad.Append($"<td><small>APT_Triad File</small></td>");
            strTriad.Append($"<td style=\"text-align:center\"><small>{triadLoaded}</small></td>");
            strTriad.Append($"<td style=\"text-align:center\"><small>{TriadLoadedDate}</small></td>");
            strTriad.Append("</tr>");

            TempClient = TempClient.Replace("#TRIADLOGS#", strTriad.ToString());

            TempClient = TempClient.Replace("#DATE#", DateTime.Now.ToString("yyyy-MM-dd hh:mm tt", CultureInfo.InvariantCulture));

            foreach (var user in sendList)
            {
               await SendAsync(TempClient, "InovoCIM - JD Group Report", user.Email);
            }

            return true;
         }
         catch (Exception ex)
         {
            return false;
         }
      }
      #endregion

      //-----------------------------//

      #region [ Send Async ]
      public async Task<bool> SendAsync(string Body, string Subject, string MailTo)
      {
         try
         {
            var emailMsg = new MailMessage { From = new MailAddress(_appSettings.EmailAccount.From, _appSettings.EmailAccount.Display) };
            emailMsg.To.Add(MailTo);

            emailMsg.Subject = Subject;
            emailMsg.Body = Body;
            emailMsg.IsBodyHtml = true;

            LinkedResource img = new LinkedResource(Directory.GetCurrentDirectory() + _appSettings.EmailAccount.Logo, MediaTypeNames.Image.Jpeg) { ContentId = "CompanyLogo" };
            AlternateView av = AlternateView.CreateAlternateViewFromString(Body, null, MediaTypeNames.Text.Html);
            av.LinkedResources.Add(img);
            emailMsg.AlternateViews.Add(av);

            using (var smtpClient = new SmtpClient(_appSettings.EmailAccount.Server))
            {
               smtpClient.Port = _appSettings.EmailAccount.Port;
               smtpClient.EnableSsl = (_appSettings.EmailAccount.SSL == "Yes") ? true : false;
               smtpClient.UseDefaultCredentials = true;
               smtpClient.Credentials = new NetworkCredential(_appSettings.EmailAccount.Username, _appSettings.EmailAccount.Password);
               await smtpClient.SendMailAsync(emailMsg);
            }

            return true;
         }
         catch (Exception ex)
         {
            Console.WriteLine(ex.Message);
            return false;
         }
      }
      #endregion
   }
}