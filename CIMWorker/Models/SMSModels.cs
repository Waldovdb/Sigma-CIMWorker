using System.Collections.Generic;

namespace CIMWorker.Models
{
   public class SMSTemplateCollection
   {
      public List<SMSTemplate> TemplateCollection;
      public SMSTemplateCollection()
      {
         TemplateCollection = new List<SMSTemplate>();
      }
   }

   public class SMSTemplate
   {
      public int SMSTemplateID { get; set; }
      public string Message { get; set; }
      public string Subject { get; set; }
      public string EmailAddress { get; set; }
      public List<SMSVariable> SMSVariables { get; set; }
      public SMSTemplate()
      {
         SMSVariables = new List<SMSVariable>();
      }
   }

   public class SMSVariable
   {
      public string PlaceHolder { get; set; }
      public string TableName { get; set; }
      public string TableField { get; set; }
   }
}
