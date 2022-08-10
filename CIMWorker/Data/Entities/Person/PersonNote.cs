using System;
using System.Collections.Generic;
using System.Text;

namespace CIMWorker.Data.Entities 
{
   public class PersonNote
   {
      public int PersonNoteID { get; set; }
      public int PersonID { get; set; }
      public int Type { get; set; }
      public string Note { get; set; }
      public int UserID { get; set; }
      public DateTime Updated { get; set; }
      public Person Person { get; set; }

      public PersonNote() { }
      public PersonNote(int Type, string Note, int UserID)
      {
         this.Type = Type;
         this.Note = Note;
         this.UserID = UserID;
         this.Updated = DateTime.Now;
      }
   }
}
