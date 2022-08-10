namespace CIMWorker.Models
{
   public class TracePreTrace
   {
      public long SourceID { get; set; }
      public int DaysITC { get; set; }
      public int IsPreTrace { get; set; }
      public int IsDiallerTrace { get; set; }
      public int IsEmailTrace { get; set; }
      public int EmailResponseDays { get; set; }
      public int ValidContactExists { get; set; }
      public int ValidEmailExists { get; set; }
      public int NumberSoftResponsesSMS { get; set; }
      public int HardResponsesSMS { get; set; }
      public int NumberSoftResponsesEmail { get; set; }
      public int EmailSent { get; set; }
      public int SMSSent { get; set; }
      public int NextPriorityNumberExists { get; set; }
      public int NextPriorityEmailExists { get; set; }
      public int HardResponsesEmail { get; set; }
      public int PositiveResponse2DaysEmail { get; set; }
      public int PTPCaptured { get; set; }
      public int LastQCode { get; set; }
      public int QueuedToEmailService { get; set; }
      public int PositiveResponse2DaysSMS { get; set; }
      public string newPriorityPhone { get; set; }
      public string newPriorityEmail { get; set; }
      public string OutcomeTracePreTrace { get; set; }
      public string OutcomeTracePreTraceEmail { get; set; }
   }
}