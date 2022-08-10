using System;
using System.Collections.Generic;
using System.Text;

namespace CIMWorker.Data.Entities 
{
   public class PhoneQueueDone
   {
      public int PhoneQueueDoneID { get; set; }
      public string Command { get; set; }
      public string Input { get; set; }
      public string InputName { get; set; }
      public string Status { get; set; }
      public DateTime Received { get; set; }
      public DateTime NextExecute { get; set; }
      public DateTime? Actioned { get; set; }
      public int RetryCount { get; set; }
      public DateTime? RetryDate { get; set; }
      public int PersonID { get; set; }
      public string ExternalID { get; set; }
      public int SourceID { get; set; }
      public int ServiceID { get; set; }
      public int LoadID { get; set; }
      public string Name { get; set; }
      public string Phone { get; set; }
      public DateTime? ScheduleDate { get; set; }
      public int Priority { get; set; }
      public int CapturingAgent { get; set; }
      public string Phone1 { get; set; }
      public string Phone02 { get; set; }
      public string Phone03 { get; set; }
      public string Phone04 { get; set; }
      public string Phone05 { get; set; }
      public string Phone06 { get; set; }
      public string Phone07 { get; set; }
      public string Phone08 { get; set; }
      public string Phone09 { get; set; }
      public string Phone10 { get; set; }
      public string Comments { get; set; }
      public string CustomData1 { get; set; }
      public string CustomData2 { get; set; }
      public string CustomData3 { get; set; }
      public string CallerID { get; set; }
      public string CallerName { get; set; }
   }
}
