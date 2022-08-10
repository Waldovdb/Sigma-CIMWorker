using System;
using System.Collections.Generic;
using System.Text;

namespace CIMWorker.Data.Entities 
{
   public class ImportDataFileDetail
   {
      public int ImportDataFileDetailID { get; set; }
      public int ImportDataFileID { get; set; }
      public int FieldID { get; set; }
      public int Length { get; set; }
      public string Name { get; set; }
      public ImportDataFile ImportDataFile { get; set; }

      public ImportDataFileDetail() { }
      public ImportDataFileDetail(int fieldID, int length, string name)
      {
         this.FieldID = fieldID;
         this.Length = length;
         this.Name = name;
      }
   }
}
