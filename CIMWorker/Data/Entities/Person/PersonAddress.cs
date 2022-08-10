using System;
using System.Collections.Generic;
using System.Text;

namespace CIMWorker.Data.Entities 
{
   public class PersonAddress
   {
      public int PersonAddressID { get; set; }
      public int PersonID { get; set; }
      public int Type { get; set; }
      public string Line1 { get; set; }
      public string Line2 { get; set; }
      public int Province { get; set; }
      public string City { get; set; }
      public string Code { get; set; }
      public Person Person { get; set; }
   }
}
