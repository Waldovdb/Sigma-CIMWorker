#region [ using ]
using System;
using System.Collections.Generic;
using System.Text;
#endregion

namespace CIMWorker.Models
{
   public class SMSRoute
   {
      public int ID { get; set; }
      public string Description { get; set; }
      public int SMSID { get; set; }
      public int EmailID { get; set; }
   }
}
