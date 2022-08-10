using System;
using System.Collections.Generic;
using System.Text;

namespace CIMWorker.Data.Entities
{
   public class ImportDataDatabase
   {
      public int ImportDataDatabaseID { get; set; }
      public int ImportDataDatabaseServerID { get; set; }
      public bool IsActive { get; set; }
      public int Priority { get; set; }
      public string TableName { get; set; }
      public string SelectQuery { get; set; }
      public string TableCreateQuery { get; set; }
      public string OnLoadedQuery { get; set; }
      public string PersonTitle { get; set; }
      public string PersonName { get; set; }
      public string PersonSurname { get; set; }
      public string PersonIDNumber { get; set; }
      public string PersonExternalID { get; set; }

      public ICollection<ImportDataDatabaseRule> ImportDataDatabaseRules { get; set; }

      public ImportDataDatabase() { }
      public ImportDataDatabase(int ServerID, bool IsActive, int Priority, string TableName, string SelectQuery, string TableCreateQuery, string OnLoadedQuery, string PersonTitle, string PersonName, string PersonSurname, string PersonIDNumber, string PersonExternalID)
      {
         this.ImportDataDatabaseServerID = ServerID;
         this.IsActive = IsActive;
         this.Priority = Priority;
         this.TableName = TableName;
         this.SelectQuery = SelectQuery;
         this.TableCreateQuery = TableCreateQuery;
         this.OnLoadedQuery = OnLoadedQuery;
         this.PersonTitle = PersonTitle;
         this.PersonName = PersonName;
         this.PersonSurname = PersonSurname;
         this.PersonIDNumber = PersonIDNumber;
         this.PersonExternalID = PersonExternalID;
      }
   }
}
