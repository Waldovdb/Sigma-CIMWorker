using System;
using System.Collections.Generic;
using System.Text;

namespace CIMWorker.Data.Entities 
{
   public class PersonHistory
   {
      public int PersonHistoryID { get; set; }
      public int PersonID { get; set; }
      public int Type { get; set; }
      public string Activity { get; set; }
      public DateTime Created { get; set; }
      public Person Person { get; set; }
   }
}
