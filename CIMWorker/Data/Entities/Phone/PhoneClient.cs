using System;
using System.Collections.Generic;
using System.Text;

namespace CIMWorker.Data.Entities 
{
   public class PhoneClient
   {
      public int PhoneClientID { get; set; }
      public int Type { get; set; }
      public string Server { get; set; }
      public string Database { get; set; }
      public string User { get; set; }
      public string Password { get; set; }

      public PhoneClient() { }
      public PhoneClient(int Type, string Server, string Database, string User, string Password)
      {
         this.Type = Type;
         this.Server = Server;
         this.Database = Database;
         this.User = User;
         this.Password = Password;
      }
   }
}
