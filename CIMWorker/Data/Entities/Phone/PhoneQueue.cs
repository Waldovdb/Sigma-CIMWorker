using System;
using System.Collections.Generic;
using System.Text;

namespace CIMWorker.Data.Entities
{
   public class PhoneQueue
   {
      public int PhoneQueueID { get; set; }
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
      public string Phone2 { get; set; }
      public string Phone3 { get; set; }
      public string Phone4 { get; set; }
      public string Phone5 { get; set; }
      public string Phone6 { get; set; }
      public string Phone7 { get; set; }
      public string Phone8 { get; set; }
      public string Phone9 { get; set; }
      public string Phone10 { get; set; }
      public string Comments { get; set; }
      public string CustomData1 { get; set; }
      public string CustomData2 { get; set; }
      public string CustomData3 { get; set; }
      public string CallerID { get; set; }
      public string CallerName { get; set; }
      public ICollection<PhoneQueueScript> PhoneQueueScripts { get; set; }

      public PhoneQueue() { }
      public PhoneQueue(int personID, string externalID, int sourceID, int serviceID, int loadID)
      {
         Command = "";
         Input = "";
         InputName = "";
         Status = "";
         Received = DateTime.Now;
         NextExecute = DateTime.Now;
         Actioned = DateTime.Now;
         RetryCount = 0;
         RetryDate = DateTime.Now;
         PersonID = personID;
         ExternalID = externalID;
         SourceID = sourceID;
         ServiceID = serviceID;
         LoadID = loadID;
         Name = externalID;
         Phone = externalID;
         ScheduleDate = DateTime.Now;
         Priority = 1;
         CapturingAgent = 0;
         Phone1 = externalID;
         Phone2 = "";
         Phone3 = "";
         Phone4 = "";
         Phone5 = "";
         Phone6 = "";
         Phone7 = "";
         Phone8 = "";
         Phone9 = "";
         Phone10 = "";
         Comments = "";
         CustomData1 = "";
         CustomData2 = "";
         CustomData3 = "";
         CallerID = "";
         CallerName = "";
      }
   }
}
