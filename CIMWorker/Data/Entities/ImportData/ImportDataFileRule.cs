using System;
using System.Collections.Generic;
using System.Text;

namespace CIMWorker.Data.Entities 
{
   public class ImportDataFileRule
   {
      public int ImportDataFileRuleID { get; set; }
      public int ImportDataFileID { get; set; }
      public int WorkflowProcessID { get; set; }
      public string Name { get; set; }
      public string Clause { get; set; }
      public ImportDataFile ImportDataFile { get; set; }

      public ImportDataFileRule() { }
      public ImportDataFileRule(int WorkflowProcessID, string Name, string Clause)
      {
         this.WorkflowProcessID = WorkflowProcessID;
         this.Name = Name;
         this.Clause = Clause;
      }
   }
}
