#region [ using ]
using System;
using System.Collections.Generic;
using System.Text;
#endregion

namespace CIMWorker.Models
{
   public class PhoneRoute
   {
      public int ID { get; set; }
      public int CDStatus { get; set; }
      public string Description { get; set; }
      public int ServiceID { get; set; }
      public int LoadID { get; set; }
   }
}
