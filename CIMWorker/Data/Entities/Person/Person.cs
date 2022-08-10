using System;
using System.Collections.Generic;
using System.Text;

namespace CIMWorker.Data.Entities 
{
   public class Person
   {
      public int PersonID { get; set; }
      public int Title { get; set; }
      public string Name { get; set; }
      public string Surname { get; set; }
      public string IDNumber { get; set; }
      public string ExternalID { get; set; }
      public DateTime Updated { get; set; }

      public ICollection<PersonAccount> PersonAccounts { get; set; }
      public ICollection<PersonAddress> PersonAddresses { get; set; }
      public ICollection<PersonContact> PersonContacts { get; set; }
      public ICollection<PersonHistory> PersonHistories { get; set; }
      public ICollection<PersonNote> PersonNotes { get; set; }

      public Person() { }
      public Person(int Title, string Name, string Surname, string IDNumber, string ExternalID = "")
      {
         this.Title = Title;
         this.Name = Name;
         this.Surname = Surname;
         this.IDNumber = IDNumber;
         this.ExternalID = ExternalID;
         this.Updated = DateTime.Now;
      }

      public Person(string Name, string IDNumber)
      {
         this.Title = 1;
         this.Name = Name;
         this.Surname = "";
         this.IDNumber = IDNumber;
         this.ExternalID = IDNumber;
         this.Updated = DateTime.Now;
      }

   }
}
