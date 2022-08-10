using System;
using System.Collections.Generic;
using System.Text;

namespace CIMWorker.Data.Entities 
{
   public class ImportDataFile
   {
      public int ImportDataFileID { get; set; }
      public bool IsActive { get; set; }
      public int Priority { get; set; }
      public string Location { get; set; }
      public string Partial { get; set; }
      public bool HasHeader { get; set; }
      public string HeaderText { get; set; }
      public int HeaderChars { get; set; }
      public char Delimiter { get; set; }
      public string TableName { get; set; }
      public string TableCreateQuery { get; set; }
      public string OnLoadedQuery { get; set; }
      public string PersonTitle { get; set; }
      public string PersonName { get; set; }
      public string PersonSurname { get; set; }
      public string PersonIDNumber { get; set; }
      public string PersonExternalID { get; set; }
      public string PersonPhone1 { get; set; }
      public string PersonPhone2 { get; set; }
      public string PersonPhone3 { get; set; }
      public string PersonEmail { get; set; }
      public int ServiceID { get; set; }
        public string PersonPhone4 { get; set; }
        public string PersonPhone5 { get; set; }
        public string CustomData1 { get; set; }
        public string CustomData2 { get; set; }
        public string CustomData3 { get; set; }
        public string Comments { get; set; }

        public ICollection<ImportDataFileDetail> ImportDataFileDetails { get; set; }
      public ICollection<ImportDataFileRule> ImportDataFileRules { get; set; }
   }
}
