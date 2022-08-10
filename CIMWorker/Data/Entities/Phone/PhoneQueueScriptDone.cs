using System;
using System.Collections.Generic;
using System.Text;

namespace CIMWorker.Data.Entities 
{
   public class PhoneQueueScriptDone
   {
      public int PhoneQueueScriptDoneID { get; set; }
      public int PhoneQueueID { get; set; }
      public string ScriptTable { get; set; }
      public string ScriptColumn { get; set; }
      public string Value { get; set; }
   }
}
