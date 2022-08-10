using System;
using System.Collections.Generic;
using System.Text;

namespace CIMWorker
{
    public class InMaxStatus
    {
        public int ID { get; set; }
        public string LeadGuid { get; set; }

        public OutMaxLead GetOutModel()
        {
            return new OutMaxLead() { LeadGuid = this.LeadGuid };
        }
    }

    public class OutMaxLead
    {
        public string LeadGuid { get; set; }
    }

    public class InPhoneCallActivity
    {
        public int ID { get; set; }
        public string LeadOwner { get; set; }
        public string LeadGuid { get; set; }
        public string subject { get; set; }
        public int dc_cho_leadcalloutcome { get; set; }
        public bool dc_bit_capturing { get; set; }
        public string phonenumber { get; set; }
        public string OutcomeType { get; set; }
        public int SubOutcome { get; set; }
        public int RecordingID { get; set; }
        public DateTime ScheduleDateTime { get; set; }

        public OutPhoneCallActivity GetOutCall()
        {
            OutPhoneCallActivity output = new OutPhoneCallActivity();
            output.LeadGuid = this.LeadGuid;
            output.LeadOwner = this.LeadOwner;
            output.subject = "Inovo - " + this.OutcomeType;
            output.dc_bit_capturing = true;
            output.dc_cho_leadcalloutcome = this.dc_cho_leadcalloutcome;
            output.phonenumber = this.phonenumber;
            output.SubOutcome = this.SubOutcome;
            output.OutcomeType = this.OutcomeType;
            output.RecordingID = this.RecordingID;
            return output;
        }

        public OutPhoneCallSchedule GetOutSchedule()
        {
            OutPhoneCallSchedule output = new OutPhoneCallSchedule();
            output.LeadGuid = this.LeadGuid;
            output.LeadOwner = this.LeadOwner;
            output.subject = "Inovo - " + this.OutcomeType;
            output.dc_bit_capturing = true;
            output.dc_cho_leadcalloutcome = this.dc_cho_leadcalloutcome;
            output.phonenumber = this.phonenumber;
            output.SubOutcome = this.SubOutcome;
            output.OutcomeType = this.OutcomeType;
            output.RecordingID = this.RecordingID;
            output.ScheduleDateTime = this.ScheduleDateTime;
            return output;
        }
    }

    public class OutPhoneCallActivity
    {
        public string LeadOwner { get; set; }
        public string LeadGuid { get; set; }
        public string subject { get; set; }
        public int dc_cho_leadcalloutcome { get; set; }
        public bool dc_bit_capturing { get; set; }
        public string phonenumber { get; set; }
        public int SubOutcome { get; set; }
        public string OutcomeType { get; set; }
        public int RecordingID { get; set; }

    }

    public class OutPhoneCallSchedule
    {
        public string LeadOwner { get; set; }
        public string LeadGuid { get; set; }
        public string subject { get; set; }
        public int dc_cho_leadcalloutcome { get; set; }
        public bool dc_bit_capturing { get; set; }
        public string phonenumber { get; set; }
        public int SubOutcome { get; set; }
        public string OutcomeType { get; set; }
        public int RecordingID { get; set; }
        public DateTime ScheduleDateTime { get; set; }
    }
}
