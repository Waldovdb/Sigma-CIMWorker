#region [ using ]
using System;
using System.Collections.Generic;
using System.Text;
#endregion

namespace CIMWorker.Models
{
   public class LegalPreLegal
   {
      public int SourceID { get; set; }
      public int? Payment33Days { get; set; }
      public int? LegalLetterSent { get; set; }
      public int? OutsourcingType { get; set; }
      public int? SecondCycleAccount { get; set; }
      public int? WarningSMSEmail { get; set; }
      public int? TimeSinceWarningSMSEmail { get; set; }
      public int? SMSEmailRead { get; set; }
      public int? Outsourced { get; set; }
      public int? PTPCaptured { get; set; }
      public int? PhoneAnswered { get; set; }
      public int? DaysSinceSMSSent { get; set; }
      public int? FinalWarningSMSEmail { get; set; }
      public int? SendFinalSMSEmail { get; set; }
      public int? DaysSinceFinalSMSEmail { get; set; }
      public int? PreviousDiallerContact { get; set; }
      public int? PTPInEffect { get; set; }
      public int? DaysPTPDue { get; set; }
      public string LegalPreLegalOutcome { get; set; }
   }
}