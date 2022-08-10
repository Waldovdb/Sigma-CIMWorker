namespace CIMWorker.Helpers
{
   public class AppSettings
   {
      public WorkerLogs WorkerLogs { get; set; }
      public FileDirectory FileDirectory { get; set; }
      public DataSettings DataSettings { get; set; }
      public DiallerSettings DiallerSettings { get; set; }
      public ReportSettings ReportSettings { get; set; }
      public Scheduler Scheduler { get; set; }
      public EmailAccount EmailAccount { get; set; }
      public Reporting Reporting { get; set; }
      public RunRespin RunRespin { get; set; }
      public MailReport MailReport { get; set; }
      public TreeLogger TreeLogger { get; set; }
   }

   //-------------------------------------//

   public class WorkerLogs
   {
      public string Process { get; set; }
      public string Error { get; set; }
   }

   //---------------//

   public class FileDirectory
   {
      public string Busy { get; set; }
      public string Done { get; set; }
      public string Failed { get; set; }
   }

   //---------------//

   public class DataSettings
   {
      public string Server { get; set; }
      public string Database { get; set; }
      public string User { get; set; }
      public string Password { get; set; }
   }

   //---------------//

   public class DiallerSettings
   {
      public string Server { get; set; }
      public string Database { get; set; }
      public string User { get; set; }
      public string Password { get; set; }
   }

   //---------------//

   public class ReportSettings
   {
      public string Server { get; set; }
      public string Database { get; set; }
      public string User { get; set; }
      public string Password { get; set; }
   }

   //---------------//

   public class Scheduler
   {
      public int Default { get; set; }
      public Monday Monday { get; set; }
      public Tuesday Tuesday { get; set; }
      public Wednesday Wednesday { get; set; }
      public Thursday Thursday { get; set; }
      public Friday Friday { get; set; }
      public Saturday Saturday { get; set; }
      public Sunday Sunday { get; set; }
   }

   public class Monday
   {
      public bool Active { get; set; }
      public int Interval { get; set; }
      public string Start { get; set; }
      public string End { get; set; }
   }

   public class Tuesday
   {
      public bool Active { get; set; }
      public int Interval { get; set; }
      public string Start { get; set; }
      public string End { get; set; }
   }

   public class Wednesday
   {
      public bool Active { get; set; }
      public int Interval { get; set; }
      public string Start { get; set; }
      public string End { get; set; }
   }

   public class Thursday
   {
      public bool Active { get; set; }
      public int Interval { get; set; }
      public string Start { get; set; }
      public string End { get; set; }
   }

   public class Friday
   {
      public bool Active { get; set; }
      public int Interval { get; set; }
      public string Start { get; set; }
      public string End { get; set; }
   }

   public class Saturday
   {
      public bool Active { get; set; }
      public int Interval { get; set; }
      public string Start { get; set; }
      public string End { get; set; }
   }

   public class Sunday
   {
      public bool Active { get; set; }
      public int Interval { get; set; }
      public string Start { get; set; }
      public string End { get; set; }
   }

   //---------------//

   public class EmailAccount
   {
      public string Display { get; set; }
      public string From { get; set; }
      public string Server { get; set; }
      public int Port { get; set; }
      public string SSL { get; set; }
      public string Username { get; set; }
      public string Password { get; set; }
      public string Logo { get; set; }
   }

   //---------------//

   public class Reporting
   {
      public string Start { get; set; }
      public string End { get; set; }
   }

   //---------------//

   public class RunRespin
   {
      public string StartRun01 { get; set; }
      public string EndRun01 { get; set; }
      public string StartRun02 { get; set; }
      public string EndRun02 { get; set; }
      public string StartRun03 { get; set; }
      public string EndRun03 { get; set; }
      public string StartRun04 { get; set; }
      public string EndRun04 { get; set; }
   }

   //---------------//

   public class MailReport
   {
      public string StartRun01 { get; set; }
      public string EndRun01 { get; set; }
      public string StartRun02 { get; set; }
      public string EndRun02 { get; set; }
      public string StartRun03 { get; set; }
      public string EndRun03 { get; set; }
      public string StartRun04 { get; set; }
      public string EndRun04 { get; set; }
   }

   //---------------//

   public class TreeLogger
   {
      public string Execute { get; set; }
   }

   //-------------------------------------//
}