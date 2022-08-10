using System;

namespace CIMWorker.Models
{
    public class MasterDataModel
    {
        public int CML_Unique_Key { get; set; }
        public string BASRISKGRADE { get; set; }
        public int? MOB { get; set; }
        public int? correct_recency { get; set; }
        public int? P_Collection_G { get; set; }
        public decimal? BALANCE { get; set; }
        public int? Current_CD { get; set; }
        public string Payment_Method { get; set; }
        public decimal? ARREARS { get; set; }
        public string CO_code { get; set; }
        public int? SAP_Strike_Date { get; set; }
        public int? SAP_Strike_Date_In { get; set; }
        public string Potential_Strike_Date_In { get; set; }
        public int? BP_Number { get; set; }
        public decimal? Original_Instalment { get; set; }
        public string SAP_Block_Code { get; set; }
        public string Country { get; set; }
        public string Disbursement_Date { get; set; }
        public decimal? TransactionAmountIncl { get; set; }
        public int? CountConsec { get; set; }
        public string Description { get; set; }
        public int? Days_To_PTP_Due { get; set; }
        public int? Days_Since_PTP_Capture { get; set; }
        public int? Days_Since_Evaluation { get; set; }
        public DateTime? acp_original_date_captured { get; set; }
        public DateTime? acp_date_capture { get; set; }
        public int? acc_digit_group { get; set; }
        public int? acp_terms { get; set; }
        public decimal? acp_amount { get; set; }
        public int? STATUS { get; set; }
        public string Failure_Reason { get; set; }
        public int? Days_since_failure { get; set; }
        public int? Days_to_salary_date { get; set; }
        public DateTime? Potential_Strike_Date { get; set; }
        public int? Days_to_Potential_Strike_Date { get; set; }
        public int? Tracking { get; set; }
        public int? Days_Since_Tracking { get; set; }
        public int? SCI { get; set; }
        public decimal? Net_Payments { get; set; }
        public decimal? Credit_Payments { get; set; }
        public string Region { get; set; }
        public string Phone1 { get; set; }
        public string Phone2 { get; set; }
        public string Phone3 { get; set; }
        public string Phone4 { get; set; }
        public string Phone5 { get; set; }
        public string Phone6 { get; set; }
        public string Phone7 { get; set; }
        public string Phone8 { get; set; }
        public string Phone9 { get; set; }
        public string Phone10 { get; set; }
        public string Email { get; set; }
        public string Name { get; set; }
        public int Title { get; set; }
        public string ID_Number { get; set; }
        public long Account { get; set; }
        public int NumberPayments { get; set; }
        public DateTime PTPDateCapture { get; set; }
        public DateTime PTPOriginalDateCapture { get; set; }
        public int? Days_Since_Disbursement { get; set; }
        public string OutcomeTemporary { get; set; }
        public string CurrentOutcomeNonPresence { get; set; }
        public int ServiceIDOut { get; set; }
        public int LoadIDOut { get; set; }
        public string SendSMSEmail { get; set; }
        public int EmailID { get; set; }
        public int SMSID { get; set; }
        public int SMSFrequency { get; set; }
        public int? Risk { get; set; }
        public bool PreLegal { get; set; }
        public bool Legal { get; set; }
        public int MonthsInPreLegal { get; set; }
        public int Delay { get; set; }
        public long APT_Account_Number { get; set; }
        public bool NoPayment { get; set; }
        public string NoSMSReason { get; set; }
    }
}