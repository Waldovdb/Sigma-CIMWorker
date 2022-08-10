using System;
using System.Collections.Generic;
using System.Text;

namespace CIMWorker.Data.Entities 
{
   public class PersonAccount
   {
      public int PersonAccountID { get; set; }
      public int PersonID { get; set; }
      public string Account { get; set; }
      public string Product { get; set; }
      public string Category { get; set; }
      public decimal Balance { get; set; }
      public decimal CurrentDue { get; set; }
      public decimal PastDue { get; set; }
      public decimal TotalDue { get; set; }
      public string Currency { get; set; }
      public int CDStatus { get; set; }
      public int RCStatus { get; set; }
      public Person Person { get; set; }
   }
}
