using System;
using System.Collections.Generic;
using System.Text;

namespace CIMWorker.Data.Entities 
{
   public class PhoneQueueScript
   {
      public int PhoneQueueScriptID { get; set; }
      public int PhoneQueueID { get; set; }
      public string ScriptTable { get; set; }
      public string ScriptColumn { get; set; }
      public string Value { get; set; }
      public PhoneQueue PhoneQueue { get; set; }
   }
}
