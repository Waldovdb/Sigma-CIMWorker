using System;
using System.Collections.Generic;
using System.Text;

namespace CIMWorker.Data.Entities 
{
   public class PersonContact
   {
      public int PersonContactID { get; set; }
      public int PersonID { get; set; }
      public int Type { get; set; }
      public string Contact { get; set; }
      public DateTime Created { get; set; }
      public Person Person { get; set; }

      public PersonContact() { }
      public PersonContact(string contact)
      {
         PersonID = -1;
         Type = 1;
         Contact = contact;
         Created = DateTime.Now;
      }
   }
}
