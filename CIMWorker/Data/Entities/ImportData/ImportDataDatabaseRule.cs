using System;
using System.Collections.Generic;
using System.Text;

namespace CIMWorker.Data.Entities
{
   public class ImportDataDatabaseRule
   {
      public int ImportDataDatabaseRuleID { get; set; }
      public int ImportDataDatabaseID { get; set; }
      public int WorkflowProcessID { get; set; }
      public string Name { get; set; }
      public string Clause { get; set; }

      public ImportDataDatabase ImportDataDatabase { get; set; }

      public ImportDataDatabaseRule() { }
      public ImportDataDatabaseRule(string Name, string Clause)
      {
         this.Name = Name;
         this.Clause = Clause;
      }
   }
}
