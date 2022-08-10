using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace CIMWorker.Models
{
   public class CIMPersonData
   {
      public long PersonID { get; set; }
      public long ExternalID { get; set; }
      public string Name { get; set; }
      public string Surname { get; set; }
      public int Title { get; set; }
      public string Product { get; set; }
      public Decimal Balance { get; set; }
      public Decimal CurrentDue { get; set; }
      public Decimal TotalDue { get; set; }
      public string Currency { get; set; }
      public DateTime DateDue { get; set; }

      public CIMPersonData(DataRow row)
      {
         this.PersonID = Convert.ToInt64(row["PersonID"]);
         this.ExternalID = Convert.ToInt64(row["ExternalID"]);
         if (row["Name"] != DBNull.Value)
         {
            this.Name = row["Name"].ToString();
         }
         if (row["Surname"] != DBNull.Value)
         {
            this.Surname = row["Surname"].ToString();
         }
         if (row["Title"] != DBNull.Value)
         {
            this.Title = Convert.ToInt32(row["Title"]);
         }
         if (row["Product"] != DBNull.Value)
         {
            this.Product = row["Product"].ToString();
         }
         if (row["Balance"] != DBNull.Value)
         {
            this.Balance = Convert.ToDecimal(row["Balance"]);
         }
         if (row["CurrentDue"] != DBNull.Value)
         {
            this.CurrentDue = Convert.ToDecimal(row["CurrentDue"]);
         }
         if (row["TotalDue"] != DBNull.Value)
         {
            this.TotalDue = Convert.ToDecimal(row["TotalDue"]);
         }
         if (row["Currency"] != DBNull.Value)
         {
            this.Currency = row["Currency"].ToString();
         }
         if (row["DateDue"] != DBNull.Value)
         {
            this.DateDue = Convert.ToDateTime(row["DateDue"]);
         }
      }
   }
}
