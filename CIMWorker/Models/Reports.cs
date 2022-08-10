namespace CIMWorker.Models
{
   public class PresenceSummary
   {
      public string SERVICENAME { get; set; }
      public int SERVICEID { get; set; }
      public int TOTAL { get; set; }
      public int INITIAL { get; set; }
      public int SCHEDULE { get; set; }
      public int INVALID { get; set; }
      public int COMPLETE { get; set; }
   }

   public class PresenceLog
   {
      public string SERVICENAME { get; set; }
      public int SERVICEID { get; set; }
      public int TOTAL { get; set; }
      public int NONUSEFUL { get; set; }
      public int NEGATIVE { get; set; }
      public int POSITIVE { get; set; }
   }

    public class SMSSent
    {
        public string COUNTRY { get; set; }
        public int SENT { get; set; }
    }
}
