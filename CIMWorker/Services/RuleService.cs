#region [ using ]
using CIMWorker.Data.Entities;
using CIMWorker.Helpers;
using CIMWorker.Models;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#endregion

namespace CIMWorker.Services
{
    #region [ Interface ]
    public interface IRuleService
    {
        Task<MasterDataModel> GetLeadOutcome(MasterDataModel record, List<PhoneRoute> collection, List<SMSRoute> smsRoutes, int identity, int Step, List<int> RPCList);
        Task<LegalPreLegal> AssignLegalPreLegal(MasterDataModel record, IDataService dataService);
        Task<TracePreTrace> AssignTracePreTrace(MasterDataModel record);
    }
    #endregion

    //--------------------------------------------//

    public class RuleService : IRuleService
    {
        private readonly AppSettings _appSettings;

        #region [ Default Constructor ]
        public RuleService(IOptions<AppSettings> appSettings)
        {
            _appSettings = appSettings.Value;
        }
        #endregion

        //-----------------------------//

        private bool IsHighRisk(MasterDataModel record)
        {
            try
            {
                if (!String.IsNullOrEmpty(record.Email) && record.Risk >= 7 && record.Payment_Method == "Debit_Order")
                {
                    return false;
                }
                else if ((String.IsNullOrEmpty(record.Email)) || (record.Payment_Method == "Cash") || (record.Risk == 5 || record.Risk == 6))
                {
                    return true;
                }
                else return false;
            }
            catch(Exception)
            {
                throw;
            }
        }

        #region [ Get Lead Outcome ]
        public async Task<MasterDataModel> GetLeadOutcome(MasterDataModel record, List<PhoneRoute> collection, List<SMSRoute> smsRoutes, int identity, int Step, List<int> RPCList)
        {
            try
            {
                if (record.CML_Unique_Key == 21490295)
                {
                    //
                }
                bool HighRiskNew = IsHighRisk(record);

                decimal PositivePayments = (record.Net_Payments > 0) ? record.Net_Payments ?? (decimal)0 : (decimal)0;
                decimal MinArrearsInstalmentBalance = ((record.ARREARS + record.Original_Instalment) < record.BALANCE) ? (record.ARREARS + record.Original_Instalment) ?? (decimal)0 : record.BALANCE ?? (decimal)0;
                decimal PaymentRemainder = (record.acp_amount == null) ? (MinArrearsInstalmentBalance - PositivePayments) : (record.acp_amount - PositivePayments) ?? (decimal)0;
                if (record.STATUS == 60)
                {
                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (STATUS == 60)", $"{record.STATUS.ToString()}", "true");
                    if (string.IsNullOrEmpty(record.SAP_Block_Code))
                    {
                        _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (string.IsNullOrEmpty(SAP_Block_Code))", "", "true");
                        if (record.BALANCE == 0)
                        {
                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (BALANCE == 0)", $"{record.BALANCE.ToString()}", "true");
                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "97. Zero Balances", "Update Phone Variable");
                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "99. No SMS", "Update SMS Variable");
                            record = await AssignLeadOutcome(record, collection, smsRoutes, "97. Zero Balances", identity, Step, "99. No SMS", 0, 0, "Balance = 0");
                        }
                        else if (record.BALANCE > 0 && record.BALANCE <= 50)
                        {
                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "else if (BALANCE > 0 && BALANCE <= 50)", $"{record.BALANCE.ToString()}", "true");
                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "96. Finance Small Balance Queue", "Update Phone Variable");
                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "99. No SMS", "Update SMS Variable");
                            record = await AssignLeadOutcome(record, collection, smsRoutes, "96. Finance Small Balance Queue", identity, Step, "99. No SMS", 0, 0, "Balance < 50");
                        }
                        else if (record.BALANCE < 0)
                        {
                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "else if (BALANCE < 0)", $"{record.BALANCE.ToString()}", "true");
                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "95. Credit Balances", "Update Phone Variable");
                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "99. No SMS", "Update SMS Variable");
                            record = await AssignLeadOutcome(record, collection, smsRoutes, "95. Credit Balances", identity, Step, "99. No SMS", 0, 0, "Account Balance < 0 (Credit Balance)");
                        }
                        else if (record.BALANCE > 50)
                        {
                            //if ((record.Current_CD > 1) || ((record.Current_CD == 1 || record.Current_CD == 0) && record.MOB < 0 && (record.Payment_Method == "Cash" || (record.Payment_Method == "Debit_Order" && (record.P_Collection_G == 2 || record.P_Collection_G == 3 || record.P_Collection_G == null)))))
                            if ((record.Current_CD > 1) || ((record.Current_CD == 1 || record.Current_CD == 0) && record.MOB < 0) || ((record.Current_CD == 0 || record.Current_CD == 1) && (record.Payment_Method == "Cash" || (record.Payment_Method == "Debit_Order" && record.P_Collection_G == 1))))
                            {
                                //if (record.Payment_Method == "Cash" || (record.Payment_Method == "Debit_Order" && (record.P_Collection_G == 1)))
                                //{
                                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "else if (BALANCE > 50)", $"{record.BALANCE.ToString()}", "true");
                                if (record.SCI == 0 || record.SCI == null)
                                {
                                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (SCI == 0 || SCI == null)", $"{record.SCI.ToString()}", "true");


                                    #region [ CD STATUS 0 ]
                                    if (record.Current_CD == 0)
                                    {
                                        _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Current_CD == 0)", $"{record.Current_CD.ToString()}", "true");

                                        #region [ MOB -2 ]
                                        if (record.MOB == -2)
                                        {
                                            if (record.Payment_Method == "Debit_Order")
                                            {
                                                if (!HighRiskNew)
                                                {
                                                    if (record.Description == "In Effect")
                                                    {
                                                        if (record.Days_Since_Disbursement == 1)
                                                        {
                                                            //25. Welcome Pack
                                                            record = await AssignLeadOutcome(record, collection, smsRoutes, "No Call", identity, Step, "25. Welcome Pack");
                                                        }
                                                        else if (record.Days_Since_Disbursement >= 2)
                                                        {
                                                            record = await AssignLeadOutcome(record, collection, smsRoutes, "31. Park", identity, Step, "99. No SMS", 0, 0, "CD = 0 And MOB = -2 And Low Risk and PTP In Effect and Days Since Disbursement > 1");
                                                        }
                                                        else
                                                        {
                                                            record = await AssignLeadOutcome(record, collection, smsRoutes, "31. Park", identity, Step, "99. No SMS", 0, 0, "CD = 0 And MOB = -2 And Low Risk and PTP In Effect and no Days Since Disbursement");
                                                        }
                                                    }
                                                    else
                                                    {
                                                        record = await AssignLeadOutcome(record, collection, smsRoutes, "31. Park", identity, Step, "99. No SMS", 0, 0, "CD = 0 And MOB = -2 And Low Risk and PTP Not In Effect");
                                                    }
                                                }
                                                else
                                                {
                                                    if (record.Description == "In Effect")
                                                    {
                                                        if (record.Days_Since_Disbursement == 1)
                                                        {
                                                            record = await AssignLeadOutcome(record, collection, smsRoutes, "2. PTP Call Internal", identity, Step, "25. Welcome Pack");
                                                        }
                                                        else if (record.Days_Since_Disbursement > 1 && record.Days_Since_Disbursement < 8)
                                                        {
                                                            if (RPCList.Contains((Int32)record.APT_Account_Number))
                                                            {
                                                                record = await AssignLeadOutcome(record, collection, smsRoutes, "31. Park", identity, Step, "99. No SMS", 0, 0, "CD = 0 And MOB = -2 And High Risk and PTP In Effect and RPC Made");
                                                            }
                                                            else
                                                            {
                                                                record = await AssignLeadOutcome(record, collection, smsRoutes, "2. PTP Call Internal", identity, Step, "99. No SMS", 0, 0, "CD = 0 And MOB = -2 And High Risk and Between 2 and 7 days since disbursement and PTP In Effect and RPC Not Made");
                                                            }
                                                        }
                                                        else
                                                        {
                                                            record = await AssignLeadOutcome(record, collection, smsRoutes, "31. Park", identity, Step, "99. No SMS", 0, 0, "CD = 0 And MOB = -2 And High Risk and PTP In Effect and more than 8 Days Since Disbursement");
                                                        }
                                                    }
                                                    else
                                                    {
                                                        record = await AssignLeadOutcome(record, collection, smsRoutes, "31. Park", identity, Step, "99. No SMS", 0, 0, "CD = 0 And MOB = -2 And High Risk and PTP Not In Effect");
                                                    }
                                                }
                                            }
                                            else if (record.Payment_Method == "Cash")
                                            {
                                                if (record.Description == "No PTP")
                                                {
                                                    if (record.Days_Since_Disbursement == 1)
                                                    {
                                                        record = await AssignLeadOutcome(record, collection, smsRoutes, "2. PTP Call Internal", identity, Step, "25. Welcome Pack");
                                                    }
                                                    else if (record.Days_Since_Disbursement > 1 && record.Days_Since_Disbursement < 8)
                                                    {
                                                        if (RPCList.Contains((Int32)record.APT_Account_Number))
                                                        {
                                                            record = await AssignLeadOutcome(record, collection, smsRoutes, "31. Park", identity, Step, "99. No SMS", 0, 0, "CD = 0 And MOB = -2 And High Risk and PTP In Effect and RPC Made");
                                                        }
                                                        else
                                                        {
                                                            record = await AssignLeadOutcome(record, collection, smsRoutes, "2. PTP Call Internal", identity, Step, "99. No SMS", 0, 0, "CD = 0 And MOB = -2 And High Risk and Between 2 and 7 days since disbursement and PTP In Effect and RPC Not Made");
                                                        }
                                                    }
                                                    else
                                                    {
                                                        record = await AssignLeadOutcome(record, collection, smsRoutes, "31. Park", identity, Step, "99. No SMS", 0, 0, "CD = 0 And MOB = -2 And High Risk and PTP In Effect and more than 8 Days Since Disbursement");
                                                    }
                                                }
                                                else if (record.Description == "In Effect")
                                                {
                                                    record = await AssignLeadOutcome(record, collection, smsRoutes, "31. Park", identity, Step, "99. No SMS", 0, 0, "CD = 0 And MOB = -2 And High Risk and Cash Account and PTP In Effect");
                                                }
                                            }
                                        }
                                        #endregion

                                        #region [ MOB -1 ]
                                        else if (record.MOB == -1)
                                        {
                                            if (record.Description == "In Effect")
                                            {
                                                record = await AssignLeadOutcome(record, collection, smsRoutes, "31. Park", identity, Step, "99. No SMS", 0, 0, "CD = 0 And MOB = -1 and PTP In Effect");
                                            }
                                            else
                                            {
                                                record = await AssignLeadOutcome(record, collection, smsRoutes, "31. Park", identity, Step, "99. No SMS", 0, 0, "CD = 0 And MOB = -1 and PTP Not In Effect");
                                            }
                                        }
                                        #endregion

                                        //#region [ MOB -2 ]
                                        //if (record.MOB == -2)
                                        //{
                                        //    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (MOB == -2)", $"{record.MOB.ToString()}", "true");
                                        //    if (record.Description == "In Effect")
                                        //    {
                                        //        _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Description == \"In Effect\")", $"{record.Description.ToString()}", "true");
                                        //        if (record.acp_amount < Math.Min(record.Original_Instalment ?? 0, record.BALANCE ?? 0))
                                        //        {
                                        //            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (acp_amount < Math.Min(Original_Instalment ?? 0, BALANCE ?? 0))", $"{record.acp_amount.ToString()}", "true");
                                        //            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "93. QA", "Update Phone Variable");
                                        //            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "21. Check Disbursement", "Update SMS Variable");
                                        //            record = await AssignLeadOutcome(record, collection, smsRoutes, "93. QA", identity, Step, "21. Check Disbursement");
                                        //        }
                                        //        else if (record.acp_amount >= Math.Min(record.Original_Instalment ?? 0, record.BALANCE ?? 0))
                                        //        {
                                        //            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (acp_amount >= Math.Min(Original_Instalment ?? 0, BALANCE ?? 0))", $"{record.acp_amount.ToString()}", "true");
                                        //            if (record.Days_To_PTP_Due > 1)
                                        //            {
                                        //                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Days_To_PTP_Due > 1)", $"{record.Days_To_PTP_Due.ToString()}", "true");
                                        //                if (record.Days_Since_PTP_Capture >= 1)
                                        //                {
                                        //                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Days_Since_PTP_Capture >= 1)", $"{record.Days_Since_PTP_Capture.ToString()}", "true");
                                        //                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "30. Await PTP Fulfilment", "Update Phone Variable");
                                        //                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "21. Check Disbursement", "Update SMS Variable");
                                        //                    record = await AssignLeadOutcome(record, collection, smsRoutes, "30. Await PTP Fulfilment", identity, Step, "21. Check Disbursement");
                                        //                }
                                        //                else if (record.Days_Since_PTP_Capture == 0)
                                        //                {
                                        //                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Days_Since_PTP_Capture == 0)", $"{record.Days_Since_PTP_Capture.ToString()}", "true");
                                        //                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "No Call", "Update Phone Variable");
                                        //                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "13.2 PTP Confirmation + Disbursement", "Update SMS Variable");
                                        //                    record = await AssignLeadOutcome(record, collection, smsRoutes, "No Call", identity, Step, "13.2 PTP Confirmation + Disbursement");
                                        //                }
                                        //            }
                                        //            else if (record.Days_To_PTP_Due == 1 || record.Days_To_PTP_Due == 0)
                                        //            {
                                        //                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Days_To_PTP_Due == 1 || Days_To_PTP_Due == 0)", $"{record.Days_To_PTP_Due.ToString()}", "true");
                                        //                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "93. QA", "Update Phone Variable");
                                        //                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "21. Check Disbursement", "Update SMS Variable");
                                        //                record = await AssignLeadOutcome(record, collection, smsRoutes, "93. QA", identity, Step, "21. Check Disbursement");
                                        //            }
                                        //        }
                                        //    }
                                        //    else if (record.Description == "No PTP")
                                        //    {
                                        //        _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Description == \"No PTP\")", $"{record.Description.ToString()}", "true");
                                        //        if (record.Days_Since_Disbursement <= 12 && record.Days_Since_Disbursement > 0)
                                        //        {
                                        //            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Days_Since_Disbursement <= 12 && Days_Since_Disbursement > 0)", $"{record.Days_Since_Disbursement.ToString()}", "true");
                                        //            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "1. PTP Call By CRC", "Update Phone Variable");
                                        //            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "21. Check Disbursement", "Update SMS Variable");
                                        //            record = await AssignLeadOutcome(record, collection, smsRoutes, "1. PTP Call By CRC", identity, Step, "21. Check Disbursement");
                                        //        }
                                        //        else if (record.Days_Since_Disbursement > 12)
                                        //        {
                                        //            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Days_Since_Disbursement > 12)", $"{record.Days_Since_Disbursement.ToString()}", "true");
                                        //            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "2. PTP Call Internal", "Update Phone Variable");
                                        //            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "21. Check Disbursement", "Update SMS Variable");
                                        //            record = await AssignLeadOutcome(record, collection, smsRoutes, "2. PTP Call Internal", identity, Step, "21. Check Disbursement");
                                        //        }
                                        //    }
                                        //    else
                                        //    {
                                        //        _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "93. QA", "Update Phone Variable");
                                        //        _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "99. No SMS", "Update SMS Variable");
                                        //        record = await AssignLeadOutcome(record, collection, smsRoutes, "93. QA", identity, Step, "99. No SMS", 0, 0, "No PTP Description");
                                        //    }
                                        //}
                                        //#endregion

                                        //#region [ MOB -1 ]
                                        //else if (record.MOB == -1)
                                        //{
                                        //    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (MOB == -1)", $"{record.MOB.ToString()}", "true");
                                        //    if (record.Description == "In Effect")
                                        //    {
                                        //        _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Description == \"In Effect\")", $"{record.Description.ToString()}", "true");
                                        //        if (record.acp_amount < Math.Min(record.Original_Instalment ?? 0, record.BALANCE ?? 0))
                                        //        {
                                        //            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (acp_amount < Math.Min(Original_Instalment ?? 0, BALANCE ?? 0))", $"{record.acp_amount.ToString()}", "true");
                                        //            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "93. QA", "Update Phone Variable");
                                        //            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "21. Check Disbursement", "Update SMS Variable");
                                        //            record = await AssignLeadOutcome(record, collection, smsRoutes, "93. QA", identity, Step, "21. Check Disbursement");
                                        //        }
                                        //        else if (record.acp_amount >= Math.Min(record.Original_Instalment ?? 0, record.BALANCE ?? 0))
                                        //        {
                                        //            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (acp_amount >= Math.Min(Original_Instalment ?? 0, BALANCE ?? 0))", $"{record.acp_amount.ToString()}", "true");
                                        //            if (record.Days_To_PTP_Due > 1)
                                        //            {
                                        //                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Days_To_PTP_Due > 1)", $"{record.Days_To_PTP_Due.ToString()}", "true");
                                        //                if (record.Days_Since_PTP_Capture >= 1)
                                        //                {
                                        //                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Days_Since_PTP_Capture >= 1)", $"{record.Days_Since_PTP_Capture.ToString()}", "true");
                                        //                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "30. Await PTP Fulfilment", "Update Phone Variable");
                                        //                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "21. Check Disbursement", "Update SMS Variable");
                                        //                    record = await AssignLeadOutcome(record, collection, smsRoutes, "30. Await PTP Fulfilment", identity, Step, "21. Check Disbursement");
                                        //                }
                                        //                else if (record.Days_Since_PTP_Capture == 0)
                                        //                {
                                        //                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Days_Since_PTP_Capture == 0)", $"{record.Days_Since_PTP_Capture.ToString()}", "true");
                                        //                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "No Call", "Update Phone Variable");
                                        //                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "13.2 PTP Confirmation + Disbursement", "Update SMS Variable");
                                        //                    record = await AssignLeadOutcome(record, collection, smsRoutes, "No Call", identity, Step, "13.2 PTP Confirmation + Disbursement");
                                        //                }
                                        //            }
                                        //            else if (record.Days_To_PTP_Due == 1 || record.Days_To_PTP_Due == 0)
                                        //            {
                                        //                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Days_To_PTP_Due == 1 || Days_To_PTP_Due == 0)", $"{record.Days_To_PTP_Due.ToString()}", "true");
                                        //                if (record.Risk >= 7)
                                        //                {
                                        //                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Risk >= 7)", $"{record.Risk.ToString()}", "true");
                                        //                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "No Call", "Update Phone Variable");
                                        //                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "19.2 Reminder + Check Disbursement", "Update SMS Variable");
                                        //                    record = await AssignLeadOutcome(record, collection, smsRoutes, "No Call", identity, Step, "19.2 Reminder + Check Disbursement");
                                        //                }
                                        //                else if (record.Risk < 7)
                                        //                {
                                        //                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Risk >= 7)", $"{record.Risk.ToString()}", "false");
                                        //                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "3. PTP Reminder", "Update Phone Variable");
                                        //                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "13.2 PTP Confirmation + Disbursement", "Update SMS Variable");
                                        //                    record = await AssignLeadOutcome(record, collection, smsRoutes, "3. PTP Reminder", identity, Step, "13.2 PTP Confirmation + Disbursement");
                                        //                }
                                        //            }
                                        //        }
                                        //    }
                                        //    else if (record.Description == "No PTP")
                                        //    {
                                        //        _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Description == \"No PTP\")", $"{record.Description.ToString()}", "true");
                                        //        if (record.Days_Since_Disbursement >= 1 && record.Days_Since_Disbursement <= 12)
                                        //        {
                                        //            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Days_Since_Disbursement >= 1 && Days_Since_Disbursement <= 12)", $"{record.Days_Since_Disbursement.ToString()}", "true");
                                        //            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "1. PTP Call by CRC", "Update Phone Variable");
                                        //            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "21. Check Disbursement", "Update SMS Variable");

                                        //            record = await AssignLeadOutcome(record, collection, smsRoutes, "1. PTP Call by CRC", identity, Step, "21. Check Disbursement");
                                        //        }
                                        //        else
                                        //        {
                                        //            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Days_Since_Disbursement >= 1 && Days_Since_Disbursement <= 12)", $"{record.Days_Since_Disbursement.ToString()}", "false");
                                        //            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "2. PTP Call Internal", "Update Phone Variable");
                                        //            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "21. Check Disbursement", "Update SMS Variable");
                                        //            record = await AssignLeadOutcome(record, collection, smsRoutes, "2. PTP Call Internal", identity, Step, "21. Check Disbursement");
                                        //        }
                                        //    }
                                        //    else if (record.Description == "Broken" || record.Description == "Short Paid" || record.Description == "Short Paid 3 Days After")
                                        //    {
                                        //        _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Description == \"Broken\" || Description == \"Short Paid\" || Description == \"Short Paid 3 Days After\")", $"{record.Description.ToString()}", "true");
                                        //        if (record.Net_Payments > record.acp_amount || record.NoPayment || record.Description == "Broken")
                                        //        {
                                        //            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Net_Payments > acp_amount || NoPayment || Description == \"Broken\")", $"{record.Net_Payments.ToString()}", "true");
                                        //            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "4. Broken PTP", "Update Phone Variable");
                                        //            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "3. Broken PTP", "Update SMS Variable");
                                        //            record = await AssignLeadOutcome(record, collection, smsRoutes, "4. Broken PTP", identity, Step, "3. Broken PTP", 0, 4);
                                        //        }
                                        //        else if ((record.Net_Payments < record.acp_amount && record.Net_Payments > 0) || record.Description == "Short Paid" || record.Description == "Short Paid 3 Days After")
                                        //        {
                                        //            if (PaymentRemainder >= 50)
                                        //            {
                                        //                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (((Net_Payments > 0 && Net_Payments < acp_amount && !record.NoPayment) || Description == \"Short Paid\" || Description == \"Short Paid 3 Days After\")) && PaymentRemainder > 50", $"{record.Description.ToString()}", "true");
                                        //                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "5. Partial Payment Broken PTP", "Update Phone Variable");
                                        //                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "23. Partial PTP", "Update SMS Variable");
                                        //                record = await AssignLeadOutcome(record, collection, smsRoutes, "5. Partial Payment Broken PTP", identity, Step, "23. Partial PTP", 0, 4);
                                        //            }
                                        //            else
                                        //            {
                                        //                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (((Net_Payments > 0 && Net_Payments < acp_amount && !record.NoPayment) || Description == \"Short Paid\" || Description == \"Short Paid 3 Days After\")) && PaymentRemainder <= 50", $"{record.Description.ToString()}", "true");
                                        //                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "31. Park", "Update Phone Variable");
                                        //                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "99. No SMS", "Update SMS Variable");
                                        //                record = await AssignLeadOutcome(record, collection, smsRoutes, "31. Park", identity, Step, "99. No SMS", 0, 0, "Payment Remainder < 50");
                                        //            }
                                        //        }
                                        //        else if (record.Net_Payments <= 0)
                                        //        {
                                        //            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Net_Payments <= 0)", $"{record.Net_Payments.ToString()}", "true");
                                        //            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "No Call", "Update Phone Variable");
                                        //            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "22. Thank You", "Update SMS Variable");
                                        //            record = await AssignLeadOutcome(record, collection, smsRoutes, "No Call", identity, Step, "22. Thank You");
                                        //        }
                                        //    }
                                        //    else
                                        //    {
                                        //        _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "4. Broken PTP", "Update Phone Variable");
                                        //        _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "3. Broken PTP", "Update SMS Variable");
                                        //        record = await AssignLeadOutcome(record, collection, smsRoutes, "4. Broken PTP", identity, Step, "3. Broken PTP", 0, 4);
                                        //    }
                                        //}

                                        //#endregion

                                        #region [ MOB 0+ ]
                                        else if (record.MOB >= 0)
                                        {
                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (MOB >= 0)", $"{record.MOB.ToString()}", "true");
                                            if (record.Description == "In Effect")
                                            {
                                                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Description == \"In Effect\")", $"{record.Description.ToString()}", "true");
                                                if (record.Days_To_PTP_Due == 1 || record.Days_To_PTP_Due == 0)
                                                {
                                                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Days_To_PTP_Due == 1 || Days_To_PTP_Due == 0)", $"{record.Days_To_PTP_Due.ToString()}", "true");
                                                    if (record.BASRISKGRADE == "NA" || record.BASRISKGRADE == String.Empty)
                                                    {
                                                        _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (BASRISKGRADE == \"NA\" || BASRISKGRADE == String.Empty)", $"{record.BASRISKGRADE.ToString()}", "true");
                                                        _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "No Call", "Update Phone Variable");
                                                        _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "19.2 Reminder + Check Disbursement", "Update SMS Variable");
                                                        record = await AssignLeadOutcome(record, collection, smsRoutes, "No Call", identity, Step, "19.2 Reminder + Check Disbursement");
                                                    }
                                                    else
                                                    {
                                                        if (record.P_Collection_G <= 2)
                                                        {
                                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (P_Collection_G <= 2)", $"{record.P_Collection_G.ToString()}", "true");
                                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "No Call", "Update Phone Variable");
                                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "19.1 PTP Reminder", "Update SMS Variable");
                                                            record = await AssignLeadOutcome(record, collection, smsRoutes, "No Call", identity, Step, "19.1 PTP Reminder");
                                                        }
                                                        else if (record.P_Collection_G > 2)
                                                        {
                                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (P_Collection_G > 2)", $"{record.P_Collection_G.ToString()}", "true");
                                                            if (record.correct_recency == 0)
                                                            {
                                                                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (correct_recency == 0)", $"{record.correct_recency.ToString()}", "true");
                                                                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "30. Await PTP Fulfilment", "Update Phone Variable");
                                                                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "99. No SMS", "Update SMS Variable");
                                                                record = await AssignLeadOutcome(record, collection, smsRoutes, "30. Await PTP Fulfilment", identity, Step, "99. No SMS", 0, 0, "Low Risk and Recency = 0");
                                                            }
                                                            else
                                                            {
                                                                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (correct_recency == 0)", $"{record.correct_recency.ToString()}", "false");
                                                                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "3. PTP Reminder", "Update Phone Variable");
                                                                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "19.1 PTP Reminder", "Update SMS Variable");
                                                                record = await AssignLeadOutcome(record, collection, smsRoutes, "3. PTP Reminder", identity, Step, "19.1 PTP Reminder");
                                                            }
                                                        }
                                                    }
                                                }
                                                else if (record.Days_To_PTP_Due > 1)
                                                {
                                                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Days_To_PTP_Due > 1)", $"{record.Days_To_PTP_Due.ToString()}", "true");
                                                    if (record.Days_Since_PTP_Capture == 0)
                                                    {
                                                        _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Days_Since_PTP_Capture == 0)", $"{record.Days_Since_PTP_Capture.ToString()}", "true");
                                                        if (record.PTPOriginalDateCapture.Date < record.PTPDateCapture.Date)
                                                        {
                                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (PTPOriginalDateCapture.Date < PTPDateCapture.Date)", $"{record.PTPOriginalDateCapture.Date.ToString()}", "true");
                                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "30. Await PTP Fulfilment", "Update Phone Variable");
                                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "99. No SMS", "Update SMS Variable");
                                                            record = await AssignLeadOutcome(record, collection, smsRoutes, "30. Await PTP Fulfilment", identity, Step);
                                                        }
                                                        else
                                                        {
                                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (PTPOriginalDateCapture.Date < PTPDateCapture.Date)", $"{record.PTPOriginalDateCapture.Date.ToString()}", "false");
                                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "No Call", "Update Phone Variable");
                                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "13.1 PTP Confirmation", "Update SMS Variable");
                                                            record = await AssignLeadOutcome(record, collection, smsRoutes, "No Call", identity, Step, "13.1 PTP Confirmation");
                                                        }
                                                    }
                                                    else if (record.Days_Since_PTP_Capture >= 1)
                                                    {
                                                        _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Days_Since_PTP_Capture >= 1)", $"{record.Days_Since_PTP_Capture.ToString()}", "true");
                                                        _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "30. Await PTP Fulfilment", "Update Phone Variable");
                                                        _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "99. No SMS", "Update SMS Variable");
                                                        record = await AssignLeadOutcome(record, collection, smsRoutes, "30. Await PTP Fulfilment", identity, Step);
                                                    }
                                                }
                                            }
                                            else if (record.Description == "No PTP")
                                            {
                                                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Description == \"No PTP\")", $"{record.Description.ToString()}", "true");
                                                if (record.Net_Payments <= 0 || record.NoPayment)
                                                {
                                                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Net_Payments <= 0 || NoPayment)", $"{record.Net_Payments.ToString()}", "true");
                                                    if (record.Payment_Method == "Cash")
                                                    {
                                                        _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Payment_Method == \"Cash\")", $"{record.Payment_Method.ToString()}", "true");
                                                        if (record.Days_to_salary_date == 0)
                                                        {
                                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Days_to_salary_date == 0)", $"{record.Days_to_salary_date.ToString()}", "true");
                                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "6. PTP Call", "Update Phone Variable");
                                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "4. Cash Reminder", "Update SMS Variable");
                                                            record = await AssignLeadOutcome(record, collection, smsRoutes, "6. PTP Call", identity, Step, "4. Cash Reminder");
                                                        }
                                                        else if (record.Days_to_salary_date < 0)
                                                        {
                                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Days_to_salary_date < 0)", $"{record.Days_to_salary_date.ToString()}", "true");
                                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "6. PTP Call", "Update Phone Variable");
                                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "99. No SMS", "Update SMS Variable");
                                                            record = await AssignLeadOutcome(record, collection, smsRoutes, "6. PTP Call", identity, Step, "99. No SMS", 0, 0, "Cash Account no payments past salary date");
                                                        }
                                                        else if (record.Days_to_salary_date > 0)
                                                        {
                                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Days_to_salary_date > 0)", $"{record.Days_to_salary_date.ToString()}", "true");
                                                            if (record.P_Collection_G <= 2)
                                                            {
                                                                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (P_Collection_G <= 2)", $"{record.P_Collection_G.ToString()}", "true");
                                                                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "6. PTP Call", "Update Phone Variable");
                                                                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "99. No SMS", "Update SMS Variable");
                                                                record = await AssignLeadOutcome(record, collection, smsRoutes, "6. PTP Call", identity, Step, "99. No SMS", 0, 0, "Cash Account no payments before salary date High Risk");
                                                            }
                                                            else if (record.P_Collection_G > 2)
                                                            {
                                                                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (P_Collection_G > 2)", $"{record.P_Collection_G.ToString()}", "true");
                                                                if (record.correct_recency == 0)
                                                                {
                                                                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (correct_recency == 0)", $"{record.correct_recency.ToString()}", "true");
                                                                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "31. Park", "Update Phone Variable");
                                                                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "99. No SMS", "Update SMS Variable");
                                                                    record = await AssignLeadOutcome(record, collection, smsRoutes, "31. Park", identity, Step, "99. No SMS", 0, 0, "Cash Account no payments before salary date Low Risk with Recency = 0");
                                                                }
                                                                else
                                                                {
                                                                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (correct_recency == 0)", $"{record.correct_recency.ToString()}", "false");
                                                                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "6. PTP Call", "Update Phone Variable");
                                                                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "99. No SMS", "Update SMS Variable");
                                                                    record = await AssignLeadOutcome(record, collection, smsRoutes, "6. PTP Call", identity, Step, "99. No SMS", 0, 0, "Cash Account no payments before salary date Low Risk with Recency != 0");
                                                                }
                                                            }
                                                        }
                                                        else
                                                        {
                                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "6. PTP Call", "Update Phone Variable");
                                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "99. No SMS", "Update SMS Variable");
                                                            record = await AssignLeadOutcome(record, collection, smsRoutes, "6. PTP Call", identity, Step, "99. No SMS", 0, 0, "Cash Account no payments with no salary date");
                                                        }
                                                    }
                                                    else if (record.Payment_Method == "Debit_Order")
                                                    {
                                                        _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Payment_Method == \"Debit_Order\")", $"{record.Payment_Method.ToString()}", "true");
                                                        if (record.Days_to_Potential_Strike_Date == 1)
                                                        {
                                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Days_to_Potential_Strike_Date == 1)", $"{record.Days_to_Potential_Strike_Date.ToString()}", "true");
                                                            if (record.P_Collection_G <= 2)
                                                            {
                                                                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (P_Collection_G <= 2)", $"{record.P_Collection_G.ToString()}", "true");
                                                                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "6. PTP Call", "Update Phone Variable");
                                                                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "5. Debit Order Reminder", "Update SMS Variable");
                                                                record = await AssignLeadOutcome(record, collection, smsRoutes, "6. PTP Call", identity, Step, "5. Debit Order Reminder");
                                                            }
                                                            else if (record.P_Collection_G > 2)
                                                            {
                                                                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (P_Collection_G > 2)", $"{record.P_Collection_G.ToString()}", "true");
                                                                if (record.correct_recency == 0)
                                                                {
                                                                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (correct_recency == 0)", $"{record.correct_recency.ToString()}", "true");
                                                                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "31. Park", "Update Phone Variable");
                                                                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "99. No SMS", "Update SMS Variable");
                                                                    record = await AssignLeadOutcome(record, collection, smsRoutes, "31. Park", identity, Step, "99. No SMS", 0, 0, "Debit Order Account no payments day before strike date with Recency = 0");
                                                                }
                                                                else
                                                                {
                                                                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (correct_recency == 0)", $"{record.correct_recency.ToString()}", "false");
                                                                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "6. PTP Call", "Update Phone Variable");
                                                                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "99. No SMS", "Update SMS Variable");
                                                                    record = await AssignLeadOutcome(record, collection, smsRoutes, "6. PTP Call", identity, Step, "99. No SMS", 0, 0, "Debit Order Account no payments day before strike date with Recency != 0");
                                                                }
                                                            }
                                                        }
                                                        else if (record.Days_to_Potential_Strike_Date == 0 || record.Days_to_Potential_Strike_Date > 1)
                                                        {
                                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Days_to_Potential_Strike_Date == 0 || Days_to_Potential_Strike_Date > 1)", $"{record.Days_to_Potential_Strike_Date.ToString()}", "true");
                                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "32. Await Strike Date Outcome", "Update Phone Variable");
                                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "99. No SMS", "Update SMS Variable");
                                                            record = await AssignLeadOutcome(record, collection, smsRoutes, "32. Await Strike Date Outcome", identity, Step, "99. No SMS", 0, 0, "Debit Order Account no payments on or more than 1 day before potential strike date");
                                                        }
                                                        else
                                                        {
                                                            if (record.Tracking == 0 || record.Tracking == null)
                                                            {
                                                                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Tracking == 0 || Tracking == null)", $"{record.Tracking.ToString()}", "true");
                                                                if (record.Days_since_failure >= 7)
                                                                {
                                                                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Days_since_failure >= 7)", $"{record.Days_since_failure.ToString()}", "true");
                                                                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "11. PTP Arrears", "Update Phone Variable");
                                                                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "12.2 Daily Arrears Low", "Update SMS Variable");
                                                                    record = await AssignLeadOutcome(record, collection, smsRoutes, "11. PTP Arrears", identity, Step, "12.2 Daily Arrears Low", 24);
                                                                }
                                                                else if (record.Days_since_failure < 7)
                                                                {
                                                                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Days_since_failure < 7)", $"{record.Days_since_failure.ToString()}", "true");

                                                                    switch (record.Failure_Reason)
                                                                    {
                                                                        case "RD":
                                                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Failure_Reason = \"RD\")", $"{record.Failure_Reason.ToString()}", "true");
                                                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "8. RD Failure", "Update Phone Variable");
                                                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "8.1 RD Failure Low", "Update SMS Variable");
                                                                            record = await AssignLeadOutcome(record, collection, smsRoutes, "8. RD Failure", identity, Step, "8.1 RD Failure Low");
                                                                            break;
                                                                        case "Dispute":
                                                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Failure_Reason = \"Dispute\")", $"{record.Failure_Reason.ToString()}", "true");
                                                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "10. Dispute Failure", "Update Phone Variable");
                                                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "10.1 Dispute Failure Low", "Update SMS Variable");
                                                                            record = await AssignLeadOutcome(record, collection, smsRoutes, "10. Dispute Failure", identity, Step, "10.1 Dispute Failure Low");
                                                                            break;
                                                                        case "Admin":
                                                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Failure_Reason = \"Admin\")", $"{record.Failure_Reason.ToString()}", "true");
                                                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "9. Admin Failure", "Update Phone Variable");
                                                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "11.1 Admin Failure Low", "Update SMS Variable");
                                                                            record = await AssignLeadOutcome(record, collection, smsRoutes, "9. Admin Failure", identity, Step, "11.1 Admin Failure Low");
                                                                            break;
                                                                        default:
                                                                            record = await AssignLeadOutcome(record, collection, smsRoutes, "Invalid Outcome at Debit Order Failure", identity, Step);
                                                                            break;
                                                                    }
                                                                }
                                                                else if (record.Days_since_failure == null)
                                                                {
                                                                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Days_since_failure == null)", $"", "true");
                                                                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "32. Await Strike Date Outcome", "Update Phone Variable");
                                                                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "99. No SMS", "Update SMS Variable");
                                                                    record = await AssignLeadOutcome(record, collection, smsRoutes, "32. Await Strike Date Outcome", identity, Step, "99. No SMS", 0, 0, "Debit Order Account no payments past strike date or null strike date with null or zero tracking and null days since failure");
                                                                }
                                                            }
                                                            else if (record.Tracking == 1)
                                                            {
                                                                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Tracking == 1)", $"{record.Tracking.ToString()}", "true");
                                                                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "7. Tracking", "Update Phone Variable");
                                                                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "7. Tracking", "Update SMS Variable");
                                                                record = await AssignLeadOutcome(record, collection, smsRoutes, "7. Tracking", identity, Step, "7. Tracking", 0, 3);
                                                            }
                                                        }
                                                    }
                                                }
                                                else if (record.Net_Payments >= Math.Min(record.Original_Instalment ?? 0, record.BALANCE ?? 0))
                                                {
                                                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Net_Payments >= Math.Min(Original_Instalment ?? 0, BALANCE ?? 0))", $"{record.Net_Payments.ToString()}", "true");
                                                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "No Call", "Update Phone Variable");
                                                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "22. Thank You", "Update SMS Variable");
                                                    record = await AssignLeadOutcome(record, collection, smsRoutes, "No Call", identity, Step, "22. Thank You");
                                                }
                                                else if (record.Net_Payments > 0 && record.Net_Payments < Math.Min(record.Original_Instalment ?? 0, record.BALANCE ?? 0) && record.Net_Payments != 0)
                                                {
                                                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (((Net_Payments > 0 && Net_Payments < acp_amount && !record.NoPayment) || Description == \"Short Paid\" || Description == \"Short Paid 3 Days After\")) && PaymentRemainder > 50", $"{record.Description.ToString()}", "true");
                                                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "5. Partial Payment Broken PTP", "Update Phone Variable");
                                                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "23. Partial PTP", "Update SMS Variable");
                                                    record = await AssignLeadOutcome(record, collection, smsRoutes, "12. Partial Payment", identity, Step, "23.1 Partial Instalment");
                                                }
                                            }
                                            else if (record.Description == "Broken" || record.Description == "Short Paid" || record.Description == "Short Paid 3 Days After")
                                            {
                                                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Description == \"Broken\" || Description == \"Short Paid\" || Description == \"Short Paid 3 Days After\")", $"{record.Description.ToString()}", "true");
                                                if (record.Net_Payments == null || record.Description == "Broken")
                                                {
                                                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Net_Payments == null || Description == \"Broken\")", $"{record.Description.ToString()}", "true");
                                                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "4. Broken PTP", "Update Phone Variable");
                                                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "3. Broken PTP", "Update SMS Variable");
                                                    record = await AssignLeadOutcome(record, collection, smsRoutes, "4. Broken PTP", identity, Step, "3. Broken PTP", 0, 4);
                                                }
                                                else if (record.Net_Payments > 0 && record.Net_Payments < record.acp_amount && record.Net_Payments >= 0 || record.Description == "Short Paid" || record.Description == "Short Paid 3 Days After")
                                                {
                                                    if (PaymentRemainder >= 50)
                                                    {
                                                        _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (((Net_Payments > 0 && Net_Payments < acp_amount && !record.NoPayment) || Description == \"Short Paid\" || Description == \"Short Paid 3 Days After\")) && PaymentRemainder > 50", $"{record.Description.ToString()}", "true");
                                                        _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "5. Partial Payment Broken PTP", "Update Phone Variable");
                                                        _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "23. Partial PTP", "Update SMS Variable");
                                                        record = await AssignLeadOutcome(record, collection, smsRoutes, "6. PTP Call", identity, Step, "23. Partial PTP", 0, 4);
                                                    }
                                                    else
                                                    {
                                                        _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (((Net_Payments > 0 && Net_Payments < acp_amount && !record.NoPayment) || Description == \"Short Paid\" || Description == \"Short Paid 3 Days After\")) && PaymentRemainder <= 50", $"{record.Description.ToString()}", "true");
                                                        _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "31. Park", "Update Phone Variable");
                                                        _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "99. No SMS", "Update SMS Variable");
                                                        record = await AssignLeadOutcome(record, collection, smsRoutes, "31. Park", identity, Step, "99. No SMS", 0, 0, "Account with payments and Payment Remainder < 50");
                                                    }
                                                }
                                                else
                                                {
                                                    if (PaymentRemainder >= 50)
                                                    {
                                                        _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (((Net_Payments > 0 && Net_Payments < acp_amount && !record.NoPayment) || Description == \"Short Paid\" || Description == \"Short Paid 3 Days After\")) && PaymentRemainder > 50", $"{record.Description.ToString()}", "true");
                                                        _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "5. Partial Payment Broken PTP", "Update Phone Variable");
                                                        _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "23. Partial PTP", "Update SMS Variable");
                                                        record = await AssignLeadOutcome(record, collection, smsRoutes, "6. PTP Call", identity, Step, "23. Partial PTP", 0, 4);
                                                    }
                                                    else
                                                    {
                                                        _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (((Net_Payments > 0 && Net_Payments < acp_amount && !record.NoPayment) || Description == \"Short Paid\" || Description == \"Short Paid 3 Days After\")) && PaymentRemainder <= 50", $"{record.Description.ToString()}", "true");
                                                        _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "31. Park", "Update Phone Variable");
                                                        _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "99. No SMS", "Update SMS Variable");
                                                        record = await AssignLeadOutcome(record, collection, smsRoutes, "31. Park", identity, Step, "99. No SMS", 0, 0, "Account with payments and Payment Remainder < 50");
                                                    }
                                                }
                                            }
                                            else if (record.Description == "Paid")
                                            {
                                                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Description == \"Paid\")", $"{record.Description.ToString()}", "true");
                                                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "PTP Paid", "Update Phone Variable");
                                                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "99. No SMS", "Update SMS Variable");
                                                record = await AssignLeadOutcome(record, collection, smsRoutes, "PTP Paid", identity, Step, "99. No SMS", 0, 0, "PTP Paid");
                                            }
                                            else
                                            {
                                                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "4. Broken PTP", "Update Phone Variable");
                                                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "3. Broken PTP", "Update SMS Variable");
                                                record = await AssignLeadOutcome(record, collection, smsRoutes, "4. Broken PTP", identity, Step, "3. Broken PTP", 0, 4);
                                            }
                                        }
                                        #endregion

                                    }
                                    #endregion


                                    #region [ CD STATUS 1 ]
                                    else if (record.Current_CD == 1)
                                    {
                                        _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Current_CD == 1)", $"{record.Current_CD.ToString()}", "true");

                                        #region [ MOB -2 ]
                                        if (record.MOB == -2)
                                        {
                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (MOB == -2)", $"{record.MOB.ToString()}", "true");
                                            if (record.Description == "In Effect")
                                            {
                                                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Description == \"In Effect\")", $"{record.Description.ToString()}", "true");
                                                if (record.acp_amount < Math.Min(record.Original_Instalment ?? 0, record.BALANCE ?? 0))
                                                {
                                                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (acp_amount < Math.Min(Original_Instalment ?? 0, BALANCE ?? 0))", $"{record.acp_amount.ToString()}", "true");
                                                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "93. QA", "Update Phone Variable");
                                                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "21. Check Disbursement", "Update SMS Variable");
                                                    record = await AssignLeadOutcome(record, collection, smsRoutes, "93. QA", identity, Step, "21. Check Disbursement");
                                                }
                                                else if (record.acp_amount >= Math.Min(record.Original_Instalment ?? 0, record.BALANCE ?? 0))
                                                {
                                                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (acp_amount >= Math.Min(Original_Instalment ?? 0, BALANCE ?? 0))", $"{record.acp_amount.ToString()}", "true");
                                                    if (record.Days_To_PTP_Due > 1)
                                                    {
                                                        _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Days_To_PTP_Due > 1)", $"{record.Days_To_PTP_Due.ToString()}", "true");
                                                        if (record.Days_Since_PTP_Capture >= 1)
                                                        {
                                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Days_Since_PTP_Capture >= 1)", $"{record.Days_Since_PTP_Capture.ToString()}", "true");
                                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "30. Await PTP Fulfilment", "Update Phone Variable");
                                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "21. Check Disbursement", "Update SMS Variable");
                                                            record = await AssignLeadOutcome(record, collection, smsRoutes, "30. Await PTP Fulfilment", identity, Step, "21. Check Disbursement");
                                                        }
                                                        else if (record.Days_Since_PTP_Capture == 0)
                                                        {
                                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Days_Since_PTP_Capture == 0)", $"{record.Days_Since_PTP_Capture.ToString()}", "true");
                                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "No Call", "Update Phone Variable");
                                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "13.2 PTP Confirmation + Disbursement", "Update SMS Variable");
                                                            record = await AssignLeadOutcome(record, collection, smsRoutes, "No Call", identity, Step, "13.2 PTP Confirmation + Disbursement");
                                                        }
                                                    }
                                                    else if (record.Days_To_PTP_Due == 1 || record.Days_To_PTP_Due == 0)
                                                    {
                                                        _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Days_To_PTP_Due == 1 || Days_To_PTP_Due == 0)", $"{record.Days_To_PTP_Due.ToString()}", "true");
                                                        _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "93. QA", "Update Phone Variable");
                                                        _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "21. Check Disbursement", "Update SMS Variable");
                                                        record = await AssignLeadOutcome(record, collection, smsRoutes, "93. QA", identity, Step, "21. Check Disbursement");
                                                    }
                                                }
                                            }
                                            else if (record.Description == "Paid")
                                            {
                                                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Description == \"Paid\")", $"{record.Description.ToString()}", "true");
                                                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "PTP Paid", "Update Phone Variable");
                                                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "99. No SMS", "Update SMS Variable");
                                                record = await AssignLeadOutcome(record, collection, smsRoutes, "PTP Paid", identity, Step, "99. No SMS", 0, 0, "PTP Paid");
                                            }
                                            else if (record.Description == "No PTP")
                                            {
                                                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Description == \"No PTP\")", $"{record.Description.ToString()}", "true");
                                                if (record.Days_Since_Disbursement <= 12 && record.Days_Since_Disbursement > 0)
                                                {
                                                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Days_Since_Disbursement <= 12 && Days_Since_Disbursement > 0)", $"{record.Days_Since_Disbursement.ToString()}", "true");
                                                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "1. PTP Call by CRC", "Update Phone Variable");
                                                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "21. Check Disbursement", "Update SMS Variable");
                                                    record = await AssignLeadOutcome(record, collection, smsRoutes, "1. PTP Call by CRC", identity, Step, "21. Check Disbursement");
                                                }
                                                else if (record.Days_Since_Disbursement > 12)
                                                {
                                                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Days_Since_Disbursement > 12)", $"{record.Days_Since_Disbursement.ToString()}", "true");
                                                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "2. PTP Call Internal", "Update Phone Variable");
                                                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "21. Check Disbursement", "Update SMS Variable");
                                                    record = await AssignLeadOutcome(record, collection, smsRoutes, "2. PTP Call Internal", identity, Step, "21. Check Disbursement");
                                                }
                                            }
                                            else
                                            {
                                                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "93. QA", "Update Phone Variable");
                                                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "99. No SMS", "Update SMS Variable");
                                                record = await AssignLeadOutcome(record, collection, smsRoutes, "93. QA", identity, Step, "99. No SMS", 0, 0, "No PTP Description");
                                            }
                                        }
                                        #endregion

                                        #region [ MOB -1 ]
                                        else if (record.MOB == -1)
                                        {
                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (MOB == -1)", $"{record.MOB.ToString()}", "true");

                                            if (record.Payment_Method == "Debit_Order")
                                            {
                                                if (record.Description == "In Effect")
                                                {
                                                    if (record.Days_To_PTP_Due == 1)
                                                    {
                                                        if (record.Risk == 5 || record.Risk == 6)
                                                        {
                                                            record = await AssignLeadOutcome(record, collection, smsRoutes, "3. PTP Reminder", identity, Step, "19.1 PTP Reminder");
                                                        }
                                                        else if (record.Risk > 6)
                                                        {
                                                            record = await AssignLeadOutcome(record, collection, smsRoutes, "No Call", identity, Step, "19.1 PTP Reminder");
                                                        }
                                                    }
                                                    else
                                                    {
                                                        record = await AssignLeadOutcome(record, collection, smsRoutes, "31. Park", identity, Step, "99. No SMS", 0, 0, "CD = 1 And MOB = -1 and PTP In Effect and more than 1 day to PTP Due Date");
                                                    }
                                                }
                                                else if (record.Description == "Broken" || record.Description == "Short Paid" || record.Description == "Short Paid 3 Days After")
                                                {
                                                    if (record.Tracking == 1)
                                                    {
                                                        if (RPCList.Contains((Int32)record.APT_Account_Number))
                                                        {
                                                            record = await AssignLeadOutcome(record, collection, smsRoutes, "31. Park", identity, Step, "99. No SMS", 0, 0, "CD = 1 And MOB = -1 And PTP Broken and RPC Made");
                                                        }
                                                        else
                                                        {
                                                            record = await AssignLeadOutcome(record, collection, smsRoutes, "7. Tracking", identity, Step, "99. No SMS", 0, 3, "CD = 1 And MOB = -1 And PTP Broken and RPC Not Made");
                                                        }
                                                    }
                                                    else
                                                    {
                                                        switch (record.Failure_Reason)
                                                        {
                                                            case "RD":
                                                                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Failure_Reason = \"RD\")", $"{record.Days_since_failure.ToString()}", "true");
                                                                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "8. RD Failure", "Update Phone Variable");
                                                                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "8.1 RD Failure Low", "Update SMS Variable");
                                                                record = await AssignLeadOutcome(record, collection, smsRoutes, "8. RD Failure", identity, Step, "99. No SMS", 0, 3, "CD = 1 And MOB = -1 And PTP Broken and Not Tracking");
                                                                break;
                                                            case "Dispute":
                                                                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Failure_Reason = \"Dispute\")", $"{record.Days_since_failure.ToString()}", "true");
                                                                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "10. Dispute Failure", "Update Phone Variable");
                                                                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "10.1 Dispute Failure Low", "Update SMS Variable");
                                                                record = await AssignLeadOutcome(record, collection, smsRoutes, "10. Dispute Failure", identity, Step, "99. No SMS", 0, 3, "CD = 1 And MOB = -1 And PTP Broken and Not Tracking");
                                                                break;
                                                            case "Admin":
                                                                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Failure_Reason = \"Admin\")", $"{record.Days_since_failure.ToString()}", "true");
                                                                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "9. Admin Failure", "Update Phone Variable");
                                                                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "11.1 Admin Failure Low", "Update SMS Variable");
                                                                record = await AssignLeadOutcome(record, collection, smsRoutes, "9. Admin Failure", identity, Step, "99. No SMS", 0, 3, "CD = 1 And MOB = -1 And PTP Broken and Not Tracking");
                                                                break;
                                                            default:
                                                                record = await AssignLeadOutcome(record, collection, smsRoutes, "Invalid Outcome at Debit Order Failure", identity, Step);
                                                                break;
                                                        }
                                                    }
                                                }
                                                else if (record.Description == "No PTP")
                                                {
                                                    record = await AssignLeadOutcome(record, collection, smsRoutes, "6. PTP Call", identity, Step, "99. No SMS", 0, 0, "CD = 1 And MOB = -1 and Debit Order Account and No PTP");
                                                }
                                            }
                                            else if (record.Payment_Method == "Cash")
                                            {
                                                if (record.Description == "No PTP")
                                                {
                                                    if (record.Days_to_salary_date == 2)
                                                    {
                                                        record = await AssignLeadOutcome(record, collection, smsRoutes, "31. Park", identity, Step, "99. No SMS", 0, 0, "CD = 1 And MOB = -1 and Cash Account and No PTP and 2 days to Salary Date");
                                                    }
                                                    else if (record.Days_to_salary_date == 1)
                                                    {
                                                        record = await AssignLeadOutcome(record, collection, smsRoutes, "No Call", identity, Step, "19.1 PTP Reminder");
                                                    }
                                                    else if (record.Days_to_salary_date == 0)
                                                    {
                                                        record = await AssignLeadOutcome(record, collection, smsRoutes, "31. Park", identity, Step, "99. No SMS", 0, 0, "CD = 1 And MOB = -1 and Cash Account and No PTP and on Salary Date");
                                                    }
                                                    else if (record.Days_to_salary_date == -1)
                                                    {
                                                        record = await AssignLeadOutcome(record, collection, smsRoutes, "No Call", identity, Step, "19.1 PTP Reminder");
                                                    }
                                                    else if (record.Days_to_salary_date < -1)
                                                    {
                                                        record = await AssignLeadOutcome(record, collection, smsRoutes, "31. Park", identity, Step, "99. No SMS", 0, 0, "CD = 1 And MOB = -1 and Cash Account and No PTP and 2 days since Salary Date");
                                                    }
                                                    else
                                                    {
                                                        int DaysToMonthEnd = (DateTime.DaysInMonth(DateTime.Now.Year, DateTime.Now.Month)) - DateTime.Now.Day;
                                                        if (DaysToMonthEnd > 1)
                                                        {
                                                            record = await AssignLeadOutcome(record, collection, smsRoutes, "31. Park", identity, Step, "99. No SMS", 0, 0, "CD = 1 And MOB = -1 and Cash Account and PTP Broken and more than 1 day to Month End");
                                                        }
                                                        else
                                                        {
                                                            record = await AssignLeadOutcome(record, collection, smsRoutes, "3. PTP Reminder", identity, Step, "19.1 PTP Reminder");
                                                        }
                                                    }
                                                }
                                                else if (record.Description == "In Effect")
                                                {
                                                    if (record.Days_To_PTP_Due == 1)
                                                    {
                                                        record = await AssignLeadOutcome(record, collection, smsRoutes, "3. PTP Reminder", identity, Step, "19.1 PTP Reminder");
                                                    }
                                                    else if (record.Days_To_PTP_Due > 1)
                                                    {
                                                        record = await AssignLeadOutcome(record, collection, smsRoutes, "31. Park", identity, Step, "99. No SMS", 0, 0, "CD = 1 And MOB = -1 and Cash Account and PTP In Effect and more than 1 day to PTP Due Date");
                                                    }
                                                }
                                                else if (record.Description == "Broken" || record.Description == "Short Paid" || record.Description == "Short Paid 3 Days After")
                                                {
                                                    if (record.Days_to_salary_date == -1)
                                                    {
                                                        record = await AssignLeadOutcome(record, collection, smsRoutes, "No Call", identity, Step, "19.1 PTP Reminder");
                                                    }
                                                    else if (record.Days_to_salary_date < -1)
                                                    {
                                                        record = await AssignLeadOutcome(record, collection, smsRoutes, "31. Park", identity, Step, "99. No SMS", 0, 0, "CD = 1 And MOB = -1 and Cash Account and PTP Broken and more than 1 day since Salary Date");
                                                    }
                                                    else
                                                    {
                                                        int DaysToMonthEnd = (DateTime.DaysInMonth(DateTime.Now.Year, DateTime.Now.Month)) - DateTime.Now.Day;
                                                        if (DaysToMonthEnd > 1)
                                                        {
                                                            record = await AssignLeadOutcome(record, collection, smsRoutes, "31. Park", identity, Step, "99. No SMS", 0, 0, "CD = 1 And MOB = -1 and Cash Account and PTP Broken and more than 1 day to Month End");
                                                        }
                                                        else
                                                        {
                                                            record = await AssignLeadOutcome(record, collection, smsRoutes, "3. PTP Reminder", identity, Step, "19.1 PTP Reminder");
                                                        }
                                                    }
                                                }
                                            }
                                            //    if (record.Description == "In Effect")
                                            //    {
                                            //        _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Description == \"In Effect\")", $"{record.Description.ToString()}", "true");

                                            //        if (record.acp_amount < Math.Min(record.Original_Instalment ?? 0, record.BALANCE ?? 0))
                                            //        {
                                            //            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (acp_amount < Math.Min(Original_Instalment ?? 0, BALANCE ?? 0))", $"{record.acp_amount.ToString()}", "true");
                                            //            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "93. QA", "Update Phone Variable");
                                            //            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "21. Check Disbursement", "Update SMS Variable");
                                            //            record = await AssignLeadOutcome(record, collection, smsRoutes, "93. QA", identity, Step, "21. Check Disbursement");
                                            //        }
                                            //        else if (record.acp_amount >= Math.Min(record.Original_Instalment ?? 0, record.BALANCE ?? 0))
                                            //        {
                                            //            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (acp_amount >= Math.Min(Original_Instalment ?? 0, BALANCE ?? 0))", $"{record.acp_amount.ToString()}", "true");

                                            //            if (record.Days_To_PTP_Due > 1)
                                            //            {
                                            //                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Days_To_PTP_Due > 1)", $"{record.Days_To_PTP_Due.ToString()}", "true");

                                            //                if (record.Days_Since_PTP_Capture >= 1)
                                            //                {
                                            //                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Days_Since_PTP_Capture >= 1)", $"{record.Days_Since_PTP_Capture.ToString()}", "true");
                                            //                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "30. Await PTP Fulfilment", "Update Phone Variable");
                                            //                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "21. Check Disbursement", "Update SMS Variable");
                                            //                    record = await AssignLeadOutcome(record, collection, smsRoutes, "30. Await PTP Fulfilment", identity, Step, "21. Check Disbursement");
                                            //                }
                                            //                else if (record.Days_Since_PTP_Capture == 0)
                                            //                {
                                            //                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Days_Since_PTP_Capture == 0)", $"{record.Days_Since_PTP_Capture.ToString()}", "true");
                                            //                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "No Call", "Update Phone Variable");
                                            //                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "13.2 PTP Confirmation + Disbursement", "Update SMS Variable");
                                            //                    record = await AssignLeadOutcome(record, collection, smsRoutes, "No Call", identity, Step, "13.2 PTP Confirmation + Disbursement");
                                            //                }
                                            //            }
                                            //            else if (record.Days_To_PTP_Due == 1 || record.Days_To_PTP_Due == 0)
                                            //            {
                                            //                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Days_To_PTP_Due == 1 || Days_To_PTP_Due == 0)", $"{record.Days_To_PTP_Due.ToString()}", "true");
                                            //                if (record.Risk == null)
                                            //                {
                                            //                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Risk == null)", $"", "true");
                                            //                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "No Call", "Update Phone Variable");
                                            //                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "19.2 Reminder + Check Disbursement", "Update SMS Variable");
                                            //                    record = await AssignLeadOutcome(record, collection, smsRoutes, "No Call", identity, Step, "19.2 Reminder + Disbursement");
                                            //                }
                                            //                else
                                            //                {
                                            //                    if (record.Risk >= 7)
                                            //                    {
                                            //                        _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Risk >= 7)", $"{record.Risk.ToString()}", "true");
                                            //                        _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "No Call", "Update Phone Variable");
                                            //                        _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "19.2 Reminder + Check Disbursement", "Update SMS Variable");
                                            //                        record = await AssignLeadOutcome(record, collection, smsRoutes, "No Call", identity, Step, "19.2 Reminder + Disbursement");
                                            //                    }
                                            //                    else if (record.Risk < 7)
                                            //                    {
                                            //                        _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Risk < 7)", $"{record.Risk.ToString()}", "true");
                                            //                        _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "3. PTP Reminder", "Update Phone Variable");
                                            //                        _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "13.2 PTP Confirmation + Disbursement", "Update SMS Variable");
                                            //                        record = await AssignLeadOutcome(record, collection, smsRoutes, "3. PTP Reminder", identity, Step, "19.2 Reminder + Disbursement");
                                            //                    }
                                            //                }
                                            //            }
                                            //        }
                                            //    }
                                            //    else if (record.Description == "Paid")
                                            //    {
                                            //        _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Description == \"Paid\")", $"{record.Description.ToString()}", "true");
                                            //        _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "PTP Paid", "Update Phone Variable");
                                            //        _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "99. No SMS", "Update SMS Variable");
                                            //        record = await AssignLeadOutcome(record, collection, smsRoutes, "PTP Paid", identity, Step, "99. No SMS", 0, 0, "PTP Paid");
                                            //    }
                                            //    else if (record.Description == "No PTP")
                                            //    {
                                            //        _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Description == \"No PTP\")", $"{record.Description.ToString()}", "true");
                                            //        if (record.Days_Since_Disbursement >= 1 && record.Days_Since_Disbursement <= 12)
                                            //        {
                                            //            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Days_Since_Disbursement >= 1 && Days_Since_Disbursement <= 12)", $"{record.Days_Since_Disbursement.ToString()}", "true");
                                            //            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "1. PTP Call by CRC", "Update Phone Variable");
                                            //            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "21. Check Disbursement", "Update SMS Variable");
                                            //            record = await AssignLeadOutcome(record, collection, smsRoutes, "1. PTP Call by CRC", identity, Step, "21. Check Disbursement");
                                            //        }
                                            //        else
                                            //        {
                                            //            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Days_Since_Disbursement >= 1 && Days_Since_Disbursement <= 12)", $"{record.Days_Since_Disbursement.ToString()}", "false");
                                            //            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "2. PTP Call Internal", "Update Phone Variable");
                                            //            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "21. Check Disbursement", "Update SMS Variable");
                                            //            record = await AssignLeadOutcome(record, collection, smsRoutes, "2. PTP Call Internal", identity, Step, "21. Check Disbursement");
                                            //        }
                                            //    }
                                            //    else if (record.Description == "Broken" || record.Description == "Short Paid" || record.Description == "Short Paid 3 Days After")
                                            //    {
                                            //        _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Description == \"Broken\" || Description == \"Short Paid\" || Description == \"Short Paid 3 Days After\")", $"{record.Description.ToString()}", "true");

                                            //        if (record.Net_Payments == 0 || record.Net_Payments == null || record.NoPayment || record.Description == "Broken") // No Payment Made
                                            //        {
                                            //            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Net_Payments == 0 || Net_Payments == null || NoPayment || Description == \"Broken\")", $"{record.Description.ToString()}", "true");
                                            //            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "4. Broken PTP", "Update Phone Variable");
                                            //            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "3. Broken PTP", "Update SMS Variable");
                                            //            record = await AssignLeadOutcome(record, collection, smsRoutes, "4. Broken PTP", identity, Step, "3. Broken PTP", 0, 4);
                                            //        }
                                            //        else if ((record.Net_Payments > 0 && record.Net_Payments < record.acp_amount && !record.NoPayment) || record.Description == "Short Paid" || record.Description == "Short Paid 3 Days After") // Partial Payment Made
                                            //        {
                                            //            if (PaymentRemainder >= 50)
                                            //            {
                                            //                if (PaymentRemainder >= 50)
                                            //                {
                                            //                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (((Net_Payments > 0 && Net_Payments < acp_amount && !record.NoPayment) || Description == \"Short Paid\" || Description == \"Short Paid 3 Days After\")) && PaymentRemainder > 50", $"{record.Description.ToString()}", "true");
                                            //                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "5. Partial Payment Broken PTP", "Update Phone Variable");
                                            //                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "23. Partial PTP", "Update SMS Variable");
                                            //                    record = await AssignLeadOutcome(record, collection, smsRoutes, "5. Partial Payment Broken PTP", identity, Step, "23. Partial PTP", 0, 4);
                                            //                }
                                            //                else
                                            //                {
                                            //                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (((Net_Payments > 0 && Net_Payments < acp_amount && !record.NoPayment) || Description == \"Short Paid\" || Description == \"Short Paid 3 Days After\")) && PaymentRemainder <= 50", $"{record.Description.ToString()}", "true");
                                            //                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "31. Park", "Update Phone Variable");
                                            //                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "99. No SMS", "Update SMS Variable");
                                            //                    record = await AssignLeadOutcome(record, collection, smsRoutes, "31. Park", identity, Step, "99. No SMS", 0, 0, "Account with payments and Payment Remainder < 50");
                                            //                }
                                            //            }
                                            //            else
                                            //            {
                                            //                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (((Net_Payments > 0 && Net_Payments < acp_amount && !record.NoPayment) || Description == \"Short Paid\" || Description == \"Short Paid 3 Days After\")) && PaymentRemainder <= 50", $"{record.Description.ToString()}", "true");
                                            //                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "31. Park", "Update Phone Variable");
                                            //                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "99. No SMS", "Update SMS Variable");
                                            //                record = await AssignLeadOutcome(record, collection, smsRoutes, "31. Park", identity, Step, "99. No SMS", 0, 0, "Account with payments and Payment Remainder < 50");
                                            //            }
                                            //        }
                                            //        else
                                            //        {
                                            //            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if ((Net_Payments > 0 && Net_Payments < acp_amount && !record.NoPayment) || Description == \"Short Paid\" || Description == \"Short Paid 3 Days After\")", $"{record.Description.ToString()}", "false");
                                            //            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "4. Broken PTP", "Update Phone Variable");
                                            //            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "3. Broken PTP", "Update SMS Variable");
                                            //            record = await AssignLeadOutcome(record, collection, smsRoutes, "4. Broken PTP", identity, Step, "3. Broken PTP", 0, 4);
                                            //        }
                                            //    }
                                            }

                                            #endregion

                                            #region [ MOB 0+ ]
                                            else if (record.MOB >= 0)
                                        {
                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (MOB >= 0)", $"{record.MOB.ToString()}", "true");

                                            if (record.Description == "In Effect")
                                            {
                                                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Description == \"In Effect\")", $"{record.Description.ToString()}", "true");

                                                if (record.Days_To_PTP_Due == 1 || record.Days_To_PTP_Due == 0)
                                                {
                                                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Days_To_PTP_Due == 1 || Days_To_PTP_Due == 0)", $"{record.Days_To_PTP_Due.ToString()}", "true");

                                                    if (record.P_Collection_G == null)
                                                    {
                                                        _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (P_Collection_G == null)", $"", "true");
                                                        _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "3. PTP Reminder", "Update Phone Variable");
                                                        _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "19.1 PTP Reminder", "Update SMS Variable");
                                                        record = await AssignLeadOutcome(record, collection, smsRoutes, "3. PTP Reminder", identity, Step, "19.1 PTP Reminder");
                                                    }
                                                    else
                                                    {
                                                        if (record.P_Collection_G > 2)
                                                        {
                                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (P_Collection_G > 2)", $"{record.P_Collection_G.ToString()}", "true");

                                                            if (record.Risk < 7)
                                                            {
                                                                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Risk < 7)", $"{record.Risk.ToString()}", "true");
                                                                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "3. PTP Reminder", "Update Phone Variable");
                                                                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "19.1 PTP Reminder", "Update SMS Variable");
                                                                record = await AssignLeadOutcome(record, collection, smsRoutes, "3. PTP Reminder", identity, Step, "19.1 PTP Reminder");
                                                            }
                                                            else if (record.Risk >= 7)
                                                            {
                                                                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Risk >= 7)", $"{record.Risk.ToString()}", "true");
                                                                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "30. Await PTP Fulfilment", "Update Phone Variable");
                                                                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "99. No SMS", "Update SMS Variable");
                                                                record = await AssignLeadOutcome(record, collection, smsRoutes, "30. Await PTP Fulfilment", identity, Step, "99. No SMS", 0, 0, "Account on or before PTP due date with P_Collection_G > 2 and Low Risk");
                                                            }
                                                        }
                                                        else if (record.P_Collection_G <= 2)
                                                        {
                                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (P_Collection_G <= 2)", $"{record.Risk.ToString()}", "true");
                                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "3. PTP Reminder", "Update Phone Variable");
                                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "19.1 PTP Reminder", "Update SMS Variable");
                                                            record = await AssignLeadOutcome(record, collection, smsRoutes, "3. PTP Reminder", identity, Step, "19.1 PTP Reminder");
                                                        }
                                                    }
                                                }
                                                else if (record.Days_To_PTP_Due > 1)
                                                {
                                                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Days_To_PTP_Due > 1)", $"{record.Days_To_PTP_Due.ToString()}", "true");
                                                    if (record.Days_Since_PTP_Capture == 0)
                                                    {
                                                        _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Days_Since_PTP_Capture == 0)", $"{record.Days_Since_PTP_Capture.ToString()}", "true");
                                                        if (record.PTPOriginalDateCapture.Date < record.PTPDateCapture.Date)
                                                        {
                                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (PTPOriginalDateCapture.Date < PTPDateCapture.Date)", $"{record.PTPOriginalDateCapture.Date.ToString()}", "true");
                                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "30. Await PTP Fulfilment", "Update Phone Variable");
                                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "99. No SMS", "Update SMS Variable");
                                                            record = await AssignLeadOutcome(record, collection, smsRoutes, "30. Await PTP Fulfilment", identity, Step, "99. No SMS", 0, 0, "Day of PTP Capture");
                                                        }
                                                        else
                                                        {
                                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (PTPOriginalDateCapture.Date < PTPDateCapture.Date)", $"{record.PTPOriginalDateCapture.Date.ToString()}", "false");
                                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "No Call", "Update Phone Variable");
                                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "13.1 PTP Confirmation", "Update SMS Variable");
                                                            record = await AssignLeadOutcome(record, collection, smsRoutes, "No Call", identity, Step, "13.1 PTP Confirmation");
                                                        }
                                                    }
                                                    else if (record.Days_Since_PTP_Capture >= 1)
                                                    {
                                                        _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Days_Since_PTP_Capture >= 1)", $"{record.Days_Since_PTP_Capture.ToString()}", "true");
                                                        _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "30. Await PTP Fulfilment", "Update Phone Variable");
                                                        _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "99. No SMS", "Update SMS Variable");
                                                        record = await AssignLeadOutcome(record, collection, smsRoutes, "30. Await PTP Fulfilment", identity, Step, "99. No SMS", 0, 0, "More than one day before PTP Due Date and past PTP Capture Date");
                                                    }
                                                }
                                            }
                                            else if (record.Description == "Paid")
                                            {
                                                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Description == \"Paid\")", $"{record.Description.ToString()}", "true");
                                                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "PTP Paid", "Update Phone Variable");
                                                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "99. No SMS", "Update SMS Variable");
                                                record = await AssignLeadOutcome(record, collection, smsRoutes, "PTP Paid", identity, Step);
                                            }
                                            else if (record.Description == "No PTP")
                                            {
                                                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Description == \"No PTP\")", $"{record.Description.ToString()}", "true");
                                                if (record.Net_Payments <= 0 || record.Net_Payments == null || record.NoPayment)
                                                {
                                                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Net_Payments <= 0 || Net_Payments == null || NoPayment)", $"{record.Net_Payments.ToString()}", "true");
                                                    if (record.Payment_Method == "Cash")
                                                    {
                                                        _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Payment_Method == \"Cash\")", $"{record.Payment_Method.ToString()}", "true");
                                                        if (record.Days_to_salary_date == 0)
                                                        {
                                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Days_to_salary_date == 0)", $"{record.Days_to_salary_date.ToString()}", "true");
                                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "6. PTP Call", "Update Phone Variable");
                                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "4. Cash Reminder", "Update SMS Variable");
                                                            record = await AssignLeadOutcome(record, collection, smsRoutes, "6. PTP Call", identity, Step, "4. Cash Reminder");
                                                        }
                                                        else if (record.Days_to_salary_date < 0)
                                                        {
                                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Days_to_salary_date < 0)", $"{record.Days_to_salary_date.ToString()}", "true");
                                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "6. PTP Call", "Update Phone Variable");
                                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "99. No SMS", "Update SMS Variable");
                                                            record = await AssignLeadOutcome(record, collection, smsRoutes, "6. PTP Call", identity, Step, "99. No SMS", 0, 0, "Cash account after salary date");
                                                        }
                                                        else if (record.Days_to_salary_date > 0)
                                                        {
                                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Days_to_salary_date > 0)", $"{record.Days_to_salary_date.ToString()}", "true");
                                                            if (record.P_Collection_G > 2)
                                                            {
                                                                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (P_Collection_G > 2)", $"{record.P_Collection_G.ToString()}", "true");
                                                                if (record.Risk >= 7)
                                                                {
                                                                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Risk >= 7)", $"{record.Risk.ToString()}", "true");
                                                                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "31. Park", "Update Phone Variable");
                                                                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "99. No SMS", "Update SMS Variable");
                                                                    record = await AssignLeadOutcome(record, collection, smsRoutes, "31. Park", identity, Step, "99. No SMS", 0, 0, "Cash account before salary date with P_Collection_G > 2 and Low Risk");
                                                                }
                                                                else if (record.Risk < 7)
                                                                {
                                                                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Risk < 7)", $"{record.Risk.ToString()}", "true");
                                                                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "6. PTP Call", "Update Phone Variable");
                                                                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "99. No SMS", "Update SMS Variable");
                                                                    record = await AssignLeadOutcome(record, collection, smsRoutes, "6. PTP Call", identity, Step, "99. No SMS", 0, 0, "Cash account before salary date with P_Collection_G > 2 and High Risk");
                                                                }
                                                            }
                                                            else if (record.P_Collection_G <= 2)
                                                            {
                                                                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (P_Collection_G <= 2)", $"{record.P_Collection_G.ToString()}", "true");
                                                                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "6. PTP Call", "Update Phone Variable");
                                                                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "99. No SMS", "Update SMS Variable");
                                                                record = await AssignLeadOutcome(record, collection, smsRoutes, "6. PTP Call", identity, Step, "99. No SMS", 0, 0, "Cash account before salary date with P_Collection_G <= 2");
                                                            }
                                                        }
                                                        else
                                                        {
                                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Days_to_salary_date > 0)", $"{record.P_Collection_G.ToString()}", "false");
                                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "6. PTP Call", "Update Phone Variable");
                                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "99. No SMS", "Update SMS Variable");
                                                            record = await AssignLeadOutcome(record, collection, smsRoutes, "6. PTP Call", identity, Step, "99. No SMS", 0, 0, "Cash account with no salary date");
                                                        }
                                                    }
                                                    else if (record.Payment_Method == "Debit_Order")
                                                    {
                                                        _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Payment_Method == \"Debit_Order\")", $"{record.P_Collection_G.ToString()}", "true");
                                                        if (record.Days_to_Potential_Strike_Date == 1)
                                                        {
                                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Days_to_Potential_Strike_Date == 1)", $"{record.Days_to_Potential_Strike_Date.ToString()}", "true");
                                                            if (record.P_Collection_G <= 2 || record.P_Collection_G == null)
                                                            {
                                                                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (P_Collection_G <= 2 || P_Collection_G == null)", $"{record.P_Collection_G.ToString()}", "true");
                                                                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "6. PTP Call", "Update Phone Variable");
                                                                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "5. Debit Order Reminder", "Update SMS Variable");
                                                                record = await AssignLeadOutcome(record, collection, smsRoutes, "6. PTP Call", identity, Step, "5. Debit Order Reminder");
                                                            }
                                                            else if (record.P_Collection_G > 2)
                                                            {
                                                                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (P_Collection_G > 2)", $"{record.P_Collection_G.ToString()}", "true");
                                                                if (record.Risk >= 7)
                                                                {
                                                                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Risk >= 7)", $"{record.Risk.ToString()}", "true");
                                                                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "31. Park", "Update Phone Variable");
                                                                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "99. No SMS", "Update SMS Variable");
                                                                    record = await AssignLeadOutcome(record, collection, smsRoutes, "31. Park", identity, Step, "99. No SMS", 0, 0, "Debit Order Account day before strike date with P_Collection_G > 2");
                                                                }
                                                                else if (record.Risk < 7)
                                                                {
                                                                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Risk < 7)", $"{record.Risk.ToString()}", "true");
                                                                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "6. PTP Call", "Update Phone Variable");
                                                                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "99. No SMS", "Update SMS Variable");
                                                                    record = await AssignLeadOutcome(record, collection, smsRoutes, "6. PTP Call", identity, Step, "99. No SMS", 0, 0, "Debit Order Account day before strike date with P_Collection_G > 2");
                                                                }
                                                            }
                                                        }
                                                        else if (record.Days_to_Potential_Strike_Date == 0 || record.Days_to_Potential_Strike_Date > 1)
                                                        {
                                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Days_to_Potential_Strike_Date == 0 || Days_to_Potential_Strike_Date > 1)", $"{record.Days_to_Potential_Strike_Date.ToString()}", "true");
                                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "32. Await Strike Date Outcome", "Update Phone Variable");
                                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "99. No SMS", "Update SMS Variable");
                                                            record = await AssignLeadOutcome(record, collection, smsRoutes, "32. Await Strike Date Outcome", identity, Step, "99. No SMS", 0, 0, "Debit Order Account on or more than one day before strike date");
                                                        }
                                                        else
                                                        {
                                                            if (record.Tracking == 0 || record.Tracking == null)
                                                            {
                                                                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Tracking == 0 || Tracking == null)", $"{record.Tracking.ToString()}", "true");
                                                                if (record.Days_since_failure >= 7)
                                                                {
                                                                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Days_since_failure >= 7)", $"{record.Days_since_failure.ToString()}", "true");
                                                                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "11. PTP Arrears", "Update Phone Variable");
                                                                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "12.2 Daily Arrears Low", "Update SMS Variable");
                                                                    record = await AssignLeadOutcome(record, collection, smsRoutes, "11. PTP Arrears", identity, Step, "12.2 Daily Arrears Low", 24);
                                                                }
                                                                else if (record.Days_since_failure < 7)
                                                                {
                                                                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Days_since_failure < 7)", $"{record.Days_since_failure.ToString()}", "true");
                                                                    switch (record.Failure_Reason)
                                                                    {
                                                                        case "RD":
                                                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Failure_Reason = \"RD\")", $"{record.Days_since_failure.ToString()}", "true");
                                                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "8. RD Failure", "Update Phone Variable");
                                                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "8.1 RD Failure Low", "Update SMS Variable");
                                                                            record = await AssignLeadOutcome(record, collection, smsRoutes, "8. RD Failure", identity, Step, "8.1 RD Failure Low");
                                                                            break;
                                                                        case "Dispute":
                                                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Failure_Reason = \"Dispute\")", $"{record.Days_since_failure.ToString()}", "true");
                                                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "10. Dispute Failure", "Update Phone Variable");
                                                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "10.1 Dispute Failure Low", "Update SMS Variable");
                                                                            record = await AssignLeadOutcome(record, collection, smsRoutes, "10. Dispute Failure", identity, Step, "10.1 Dispute Failure Low");
                                                                            break;
                                                                        case "Admin":
                                                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Failure_Reason = \"Admin\")", $"{record.Days_since_failure.ToString()}", "true");
                                                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "9. Admin Failure", "Update Phone Variable");
                                                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "11.1 Admin Failure Low", "Update SMS Variable");
                                                                            record = await AssignLeadOutcome(record, collection, smsRoutes, "9. Admin Failure", identity, Step, "11.1 Admin Failure Low");
                                                                            break;
                                                                        default:
                                                                            record = await AssignLeadOutcome(record, collection, smsRoutes, "Invalid Outcome at Debit Order Failure", identity, Step);
                                                                            break;
                                                                    }
                                                                }
                                                                else if (record.Days_since_failure == null)
                                                                {
                                                                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Days_since_failure == null)", $"", "true");
                                                                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "32. Await Strike Date Outcome", "Update Phone Variable");
                                                                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "99. No SMS", "Update SMS Variable");
                                                                    record = await AssignLeadOutcome(record, collection, smsRoutes, "32. Await Strike Date Outcome", identity, Step, "99. No SMS", 0, 0, "Debit order account without days since failure");
                                                                }
                                                            }
                                                            else if (record.Tracking == 1)
                                                            {
                                                                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Tracking == 1)", $"{record.Tracking.ToString()}", "true");
                                                                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "7. Tracking", "Update Phone Variable");
                                                                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "7. Tracking", "Update SMS Variable");
                                                                record = await AssignLeadOutcome(record, collection, smsRoutes, "7. Tracking", identity, Step, "7. Tracking", 0, 3);
                                                            }
                                                        }
                                                    }
                                                }
                                                else if (record.Net_Payments < Math.Min(record.Original_Instalment ?? 0, record.BALANCE ?? 0) && record.Net_Payments > 0 && !record.NoPayment)
                                                {
                                                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (((Net_Payments > 0 && Net_Payments < acp_amount && !record.NoPayment) || Description == \"Short Paid\" || Description == \"Short Paid 3 Days After\")) && PaymentRemainder > 50", $"{record.Description.ToString()}", "true");
                                                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "5. Partial Payment Broken PTP", "Update Phone Variable");
                                                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "23. Partial PTP", "Update SMS Variable");
                                                    record = await AssignLeadOutcome(record, collection, smsRoutes, "12. Partial Payment", identity, Step, "23.1 Partial Instalment");
                                                }
                                                else if (record.Net_Payments >= Math.Min(record.Original_Instalment ?? 0, record.BALANCE ?? 0))
                                                {
                                                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Net_Payments >= Math.Min(Original_Instalment ?? 0, BALANCE ?? 0))", $"{record.Net_Payments.ToString()}", "true");
                                                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "No Call", "Update Phone Variable");
                                                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "22. Thank You", "Update SMS Variable");
                                                    record = await AssignLeadOutcome(record, collection, smsRoutes, "No Call", identity, Step, "22. Thank You");
                                                }
                                            }
                                            else if (record.Description == "Broken" || record.Description == "Short Paid" || record.Description == "Short Paid 3 Days After")
                                            {
                                                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Description == \"Broken\" || Description == \"Short Paid\" || Description == \"Short Paid 3 Days After\")", $"{record.Description.ToString()}", "true");
                                                if ((record.NoPayment) || record.Description == "Broken")
                                                {
                                                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if ((NoPayment) || Description == \"Broken\")", $"{record.Description.ToString()}", "true");
                                                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "4. Broken PTP", "Update Phone Variable");
                                                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "3. Broken PTP", "Update SMS Variable");
                                                    record = await AssignLeadOutcome(record, collection, smsRoutes, "4. Broken PTP", identity, Step, "3. Broken PTP", 0, 4);
                                                }
                                                else if ((record.Net_Payments < record.acp_amount && record.Net_Payments > 0 && !record.NoPayment) || record.Description == "Short Paid" || record.Description == "Short Paid 3 Days After") // Partial Payment Made
                                                {
                                                    if (PaymentRemainder >= 50)
                                                    {
                                                        _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (((Net_Payments > 0 && Net_Payments < acp_amount && !record.NoPayment) || Description == \"Short Paid\" || Description == \"Short Paid 3 Days After\")) && PaymentRemainder > 50", $"{record.Description.ToString()}", "true");
                                                        _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "5. Partial Payment Broken PTP", "Update Phone Variable");
                                                        _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "23. Partial PTP", "Update SMS Variable");
                                                        record = await AssignLeadOutcome(record, collection, smsRoutes, "6. PTP Call", identity, Step, "23. Partial PTP", 0, 2);
                                                    }
                                                    else
                                                    {
                                                        _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (((Net_Payments > 0 && Net_Payments < acp_amount && !record.NoPayment) || Description == \"Short Paid\" || Description == \"Short Paid 3 Days After\")) && PaymentRemainder <= 50", $"{record.Description.ToString()}", "true");
                                                        _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "31. Park", "Update Phone Variable");
                                                        _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "99. No SMS", "Update SMS Variable");
                                                        record = await AssignLeadOutcome(record, collection, smsRoutes, "31. Park", identity, Step, "99. No SMS", 0, 0, "Account with payments and Payment Remainder < 50");
                                                    }
                                                }
                                                else
                                                {
                                                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Description == \"Broken\" || Description == \"Short Paid\" || Description == \"Short Paid 3 Days After\")", $"{record.Description.ToString()}", "false");
                                                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "4. Broken PTP", "Update Phone Variable");
                                                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "3. Broken PTP", "Update SMS Variable");
                                                    record = await AssignLeadOutcome(record, collection, smsRoutes, "4. Broken PTP", identity, Step, "3. Broken PTP", 0, 4);
                                                }
                                            }
                                        }
                                    }
                                    #endregion

                                    #endregion

                                    #region [ CD STATUS 2 ]
                                    else if (record.Current_CD == 2 && record.ARREARS >= 50M)
                                    {
                                        _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Current_CD == 2)", $"{record.Current_CD.ToString()}", "true");

                                        #region [ MOB 0 ]
                                        if (record.MOB <= 0)
                                        {
                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (MOB <= 0)", $"{record.MOB.ToString()}", "true");
                                            //if (record.ARREARS > 20)
                                            //{
                                            //   _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (ARREARS > 20)", $"{record.ARREARS.ToString()}", "true");
                                            //   _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "90. Pre-Legal FPD", "Update Phone Variable");
                                            //   _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "99. No SMS", "Update SMS Variable");
                                            //   record = await AssignLeadOutcome(record, collection, smsRoutes, "90. Pre-Legal FPD", identity, Step);
                                            //}
                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (ARREARS > 20)", $"{record.ARREARS.ToString()}", "true");
                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "90. Pre-Legal FPD", "Update Phone Variable");
                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "99. No SMS", "Update SMS Variable");
                                            record = await AssignLeadOutcome(record, collection, smsRoutes, "90. Pre-Legal FPD", identity, Step, "99. No SMS", 0, 0, "Pre-Legal Account");
                                        }
                                        #endregion

                                        #region [ MOB 0+ ]
                                        else if (record.MOB >= 1)
                                        {
                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (MOB >= 1)", $"{record.MOB.ToString()}", "true");
                                            if (record.Description == "In Effect" || record.Description == "Pending")
                                            {
                                                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Description == \"In Effect\" || Description == \"Pending\")", $"{record.Description.ToString()}", "true");
                                                if (record.acp_amount < Math.Min(record.Original_Instalment ?? 0, record.BALANCE ?? 0))
                                                {
                                                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (acp_amount < Math.Min(Original_Instalment ?? 0, BALANCE ?? 0))", $"{record.acp_amount.ToString()}", "true");
                                                    if (record.CountConsec >= 3)
                                                    {
                                                        _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (CountConsec >= 3)", $"{record.CountConsec.ToString()}", "true");
                                                        _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "13. Call to revise PTP amount", "Update Phone Variable");
                                                        _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "99. No SMS", "Update SMS Variable");
                                                        record = await AssignLeadOutcome(record, collection, smsRoutes, "13. Call to revise PTP amount", identity, Step, "99. No SMS", 0, 0, "Account with PTP Amount less than instalment and more than 3 honored PTPs");
                                                    }
                                                    else if (record.CountConsec < 3 || record.CountConsec == null)
                                                    {
                                                        _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (CountConsec < 3 || CountConsec == null)", $"{record.CountConsec.ToString()}", "true");
                                                        if (record.Days_To_PTP_Due == 1 || record.Days_To_PTP_Due == 0)
                                                        {
                                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Days_To_PTP_Due == 1 || Days_To_PTP_Due == 0)", $"{record.Days_To_PTP_Due.ToString()}", "true");
                                                            if (record.P_Collection_G > 2)
                                                            {
                                                                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (P_Collection_G > 2)", $"{record.P_Collection_G.ToString()}", "true");
                                                                if (record.Risk >= 7)
                                                                {
                                                                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Risk >= 7)", $"{record.Risk.ToString()}", "true");
                                                                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "30. Await PTP Fulfilment", "Update Phone Variable");
                                                                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "99. No SMS", "Update SMS Variable");
                                                                    record = await AssignLeadOutcome(record, collection, smsRoutes, "30. Await PTP Fulfilment", identity, Step, "99. No SMS", 0, 0, "Account on or day before PTP Due with < 3 honored PTPs and P_Collection_G > 2 and Low Risk");
                                                                }
                                                                else
                                                                {
                                                                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Risk >= 7)", $"{record.Risk.ToString()}", "false");
                                                                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "3. PTP Reminder", "Update Phone Variable");
                                                                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "19.1 PTP Reminder", "Update SMS Variable");
                                                                    record = await AssignLeadOutcome(record, collection, smsRoutes, "3. PTP Reminder", identity, Step, "19.1 PTP Reminder");
                                                                }
                                                            }
                                                            else if (record.P_Collection_G <= 2)
                                                            {
                                                                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (P_Collection_G <= 2)", $"{record.P_Collection_G.ToString()}", "true");
                                                                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "3. PTP Reminder", "Update Phone Variable");
                                                                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "19.1 PTP Reminder", "Update SMS Variable");
                                                                record = await AssignLeadOutcome(record, collection, smsRoutes, "3. PTP Reminder", identity, Step, "19.1 PTP Reminder");
                                                            }
                                                        }
                                                        else if (record.Days_To_PTP_Due > 1)
                                                        {
                                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Days_To_PTP_Due > 1)", $"{record.Days_To_PTP_Due.ToString()}", "true");
                                                            if (record.Days_Since_PTP_Capture == 0)
                                                            {
                                                                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Days_Since_PTP_Capture == 0)", $"{record.Days_Since_PTP_Capture.ToString()}", "true");
                                                                if (record.PTPOriginalDateCapture.Date < record.PTPDateCapture.Date)
                                                                {
                                                                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (PTPOriginalDateCapture.Date < PTPDateCapture.Date)", $"{record.PTPOriginalDateCapture.Date.ToString()}", "true");
                                                                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "30. Await PTP Fulfilment", "Update Phone Variable");
                                                                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "99. No SMS", "Update SMS Variable");
                                                                    record = await AssignLeadOutcome(record, collection, smsRoutes, "30. Await PTP Fulfilment", identity, Step, "99. No SMS", 0, 0, "Account with chain PTP on day of PTP capture with more than 1 day before PTP Due");
                                                                }
                                                                else
                                                                {
                                                                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (PTPOriginalDateCapture.Date < PTPDateCapture.Date)", $"{record.PTPOriginalDateCapture.Date.ToString()}", "false");
                                                                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "No Call", "Update Phone Variable");
                                                                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "13.1 PTP Confirmation", "Update SMS Variable");
                                                                    record = await AssignLeadOutcome(record, collection, smsRoutes, "No Call", identity, Step, "13.1 PTP Confirmation");
                                                                }
                                                            }
                                                            else if (record.Days_Since_PTP_Capture > 0)
                                                            {
                                                                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Days_Since_PTP_Capture > 0)", $"{record.Days_Since_PTP_Capture.ToString()}", "true");
                                                                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "30. Await PTP Fulfilment", "Update Phone Variable");
                                                                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "99. No SMS", "Update SMS Variable");
                                                                record = await AssignLeadOutcome(record, collection, smsRoutes, "30. Await PTP Fulfilment", identity, Step, "99. No SMS", 0, 0, "Account after day of PTP capture with more than 1 day before PTP Due");
                                                            }
                                                        }
                                                    }
                                                }
                                                else if (record.acp_amount >= Math.Min(record.Original_Instalment ?? 0, record.BALANCE ?? 0))
                                                {
                                                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (acp_amount >= Math.Min(Original_Instalment ?? 0, BALANCE ?? 0))", $"{record.acp_amount.ToString()}", "true");
                                                    if (record.Days_To_PTP_Due > 1)
                                                    {
                                                        _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Days_To_PTP_Due > 1)", $"{record.Days_To_PTP_Due.ToString()}", "true");
                                                        if (record.Days_Since_PTP_Capture >= 1)
                                                        {
                                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Days_Since_PTP_Capture >= 1)", $"{record.Days_Since_PTP_Capture.ToString()}", "true");
                                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "30. Await PTP Fulfilment", "Update Phone Variable");
                                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "99. No SMS", "Update SMS Variable");
                                                            record = await AssignLeadOutcome(record, collection, smsRoutes, "30. Await PTP Fulfilment", identity, Step, "99. No SMS", 0, 0, "Account after day of PTP capture with more than 1 day before PTP Due");
                                                        }
                                                        else if (record.Days_Since_PTP_Capture == 0)
                                                        {
                                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Days_Since_PTP_Capture == 0)", $"{record.Days_Since_PTP_Capture.ToString()}", "true");
                                                            if (record.PTPOriginalDateCapture.Date < record.PTPDateCapture.Date)
                                                            {
                                                                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (PTPOriginalDateCapture.Date < PTPDateCapture.Date)", $"{record.PTPOriginalDateCapture.Date.ToString()}", "true");
                                                                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "30. Await PTP Fulfilment", "Update Phone Variable");
                                                                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "99. No SMS", "Update SMS Variable");
                                                                record = await AssignLeadOutcome(record, collection, smsRoutes, "30. Await PTP Fulfilment", identity, Step, "99. No SMS", 0, 0, "Account with chain PTP on day of PTP capture with more than 1 day before PTP Due");
                                                            }
                                                            else
                                                            {
                                                                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (PTPOriginalDateCapture.Date < PTPDateCapture.Date)", $"{record.PTPOriginalDateCapture.Date.ToString()}", "false");
                                                                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "No Call", "Update Phone Variable");
                                                                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "13.1 PTP Confirmation", "Update SMS Variable");
                                                                record = await AssignLeadOutcome(record, collection, smsRoutes, "No Call", identity, Step, "13.1 PTP Confirmation");
                                                            }
                                                        }
                                                    }
                                                    else if (record.Days_To_PTP_Due == 1 || record.Days_To_PTP_Due == 0)
                                                    {
                                                        _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Days_To_PTP_Due == 1 || Days_To_PTP_Due == 0)", $"{record.Days_To_PTP_Due.ToString()}", "true");
                                                        if (record.P_Collection_G == null)
                                                        {
                                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (P_Collection_G == null)", $"", "true");
                                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "No Call", "Update Phone Variable");
                                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "19.1 PTP Reminder", "Update SMS Variable");
                                                            record = await AssignLeadOutcome(record, collection, smsRoutes, "No Call", identity, Step, "19.1 PTP Reminder");
                                                        }
                                                        else
                                                        {
                                                            if (record.P_Collection_G > 2)
                                                            {
                                                                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (P_Collection_G > 2)", $"{record.P_Collection_G.ToString()}", "true");
                                                                if (record.Risk >= 7)
                                                                {
                                                                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Risk >= 7)", $"{record.Risk.ToString()}", "true");
                                                                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "30. Await PTP Fulfilment", "Update Phone Variable");
                                                                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "99. No SMS", "Update SMS Variable");
                                                                    record = await AssignLeadOutcome(record, collection, smsRoutes, "30. Await PTP Fulfilment", identity, Step, "99. No SMS", 0, 0, "Account on or day before PTP Due with P_Collection_G > 2 and Low Risk");
                                                                }
                                                                else if (record.Risk < 7)
                                                                {
                                                                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Risk < 7)", $"{record.Risk.ToString()}", "true");
                                                                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "3. PTP Reminder", "Update Phone Variable");
                                                                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "19.1 PTP Reminder", "Update SMS Variable");
                                                                    record = await AssignLeadOutcome(record, collection, smsRoutes, "3. PTP Reminder", identity, Step, "19.1 PTP Reminder");
                                                                }
                                                            }
                                                            else if (record.P_Collection_G <= 2)
                                                            {
                                                                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (P_Collection_G <= 2)", $"{record.Risk.ToString()}", "true");
                                                                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "3. PTP Reminder", "Update Phone Variable");
                                                                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "19.1 PTP Reminder", "Update SMS Variable");
                                                                record = await AssignLeadOutcome(record, collection, smsRoutes, "3. PTP Reminder", identity, Step, "19.1 PTP Reminder");
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                            else if (record.Description == "Paid")
                                            {
                                                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Description == \"Paid\")", $"{record.Description.ToString()}", "true");
                                                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "PTP Paid", "Update Phone Variable");
                                                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "99. No SMS", "Update SMS Variable");
                                                record = await AssignLeadOutcome(record, collection, smsRoutes, "PTP Paid", identity, Step);
                                            }
                                            else if (record.Description == "No PTP")
                                            {
                                                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Description == \"No PTP\")", $"{record.Description.ToString()}", "true");
                                                if (record.Net_Payments <= 0 || record.Net_Payments == null || record.NoPayment)
                                                {
                                                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Net_Payments <= 0 || Net_Payments == null || NoPayment)", $"{record.Net_Payments.ToString()}", "true");
                                                    if (record.Payment_Method == "Cash")
                                                    {
                                                        _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Payment_Method == \"Cash\")", $"{record.Payment_Method.ToString()}", "true");
                                                        if (record.Days_to_salary_date == 0 || record.Days_to_salary_date == 1)
                                                        {
                                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Days_to_salary_date == 0)", $"{record.Days_to_salary_date.ToString()}", "true");
                                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "6. PTP Call", "Update Phone Variable");
                                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "4. Cash Reminder", "Update SMS Variable");
                                                            record = await AssignLeadOutcome(record, collection, smsRoutes, "6. PTP Call", identity, Step, "6. Arrears (3 Daily)");
                                                        }
                                                        //if (record.Days_to_salary_date == 0)
                                                        //{
                                                        //    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Days_to_salary_date == 0)", $"{record.Days_to_salary_date.ToString()}", "true");
                                                        //    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "6. PTP Call", "Update Phone Variable");
                                                        //    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "4. Cash Reminder", "Update SMS Variable");
                                                        //    record = await AssignLeadOutcome(record, collection, smsRoutes, "6. PTP Call", identity, Step, "4. Cash Reminder");
                                                        //}
                                                        else if (record.Days_to_salary_date < 0 || record.Days_to_salary_date >= 2)
                                                        {
                                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Days_to_salary_date < 0)", $"{record.Days_to_salary_date.ToString()}", "true");
                                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "6. PTP Call", "Update Phone Variable");
                                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "4. Cash Reminder", "Update SMS Variable");
                                                            record = await AssignLeadOutcome(record, collection, smsRoutes, "6. PTP Call", identity, Step, "6. Arrears (3 Daily)", 24);
                                                        }
                                                        //else if (record.Days_to_salary_date < 0)
                                                        //{
                                                        //    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Days_to_salary_date < 0)", $"{record.Days_to_salary_date.ToString()}", "true");
                                                        //    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "6. PTP Call", "Update Phone Variable");
                                                        //    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "4. Cash Reminder", "Update SMS Variable");
                                                        //    record = await AssignLeadOutcome(record, collection, smsRoutes, "6. PTP Call", identity, Step, "4. Cash Reminder", 24);
                                                        //}
                                                        //else if (record.Days_to_salary_date > 0)
                                                        //{
                                                        //    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Days_to_salary_date > 0)", $"{record.Days_to_salary_date.ToString()}", "true");
                                                        //    if (record.P_Collection_G > 2)
                                                        //    {
                                                        //        _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (P_Collection_G > 2)", $"{record.P_Collection_G.ToString()}", "true");
                                                        //        _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "No Call", "Update Phone Variable");
                                                        //        _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "4. Cash Reminder", "Update SMS Variable");
                                                        //        record = await AssignLeadOutcome(record, collection, smsRoutes, "No Call", identity, Step, "4. Cash Reminder", 120);
                                                        //    }
                                                        //    else if (record.P_Collection_G <= 2)
                                                        //    {
                                                        //        _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (P_Collection_G <= 2)", $"{record.P_Collection_G.ToString()}", "true");
                                                        //        _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "6. PTP Call", "Update Phone Variable");
                                                        //        _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "4. Cash Reminder", "Update SMS Variable");
                                                        //        record = await AssignLeadOutcome(record, collection, smsRoutes, "6. PTP Call", identity, Step, "4. Cash Reminder", 120);
                                                        //    }
                                                        //}
                                                        else
                                                        {
                                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Days_to_salary_date > 0)", $"{record.Days_to_salary_date.ToString()}", "false");
                                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "6. PTP Call", "Update Phone Variable");
                                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "4. Cash Reminder", "Update SMS Variable");
                                                            record = await AssignLeadOutcome(record, collection, smsRoutes, "6. PTP Call", identity, Step, "6. Arrears (3 Daily)", 120);
                                                        }
                                                    }
                                                    else if (record.Payment_Method == "Debit_Order")
                                                    {
                                                        _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Payment_Method == \"Debit_Order\")", $"{record.Payment_Method.ToString()}", "true");
                                                        if (record.Days_to_Potential_Strike_Date == 1 || record.Days_to_Potential_Strike_Date == 0)
                                                        {
                                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (P_Collection_G <= 2)", $"{record.P_Collection_G.ToString()}", "true");
                                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "6. PTP Call", "Update Phone Variable");
                                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "5. Debit Order Reminder", "Update SMS Variable");
                                                            record = await AssignLeadOutcome(record, collection, smsRoutes, "6. PTP Call", identity, Step, "5. Debit Order Reminder");
                                                        }
                                                        else if (record.Days_to_Potential_Strike_Date >= 2)
                                                        {
                                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (P_Collection_G <= 2)", $"{record.P_Collection_G.ToString()}", "true");
                                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "6. PTP Call", "Update Phone Variable");
                                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "5. Debit Order Reminder", "Update SMS Variable");
                                                            record = await AssignLeadOutcome(record, collection, smsRoutes, "6. PTP Call", identity, Step, "6. Arrears (3 Daily)");
                                                        }
                                                        else
                                                        {
                                                            if (record.Tracking == 0 || record.Tracking == null)
                                                            {
                                                                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Tracking == 0 || Tracking == null)", $"{record.Tracking.ToString()}", "true");
                                                                if (record.Days_since_failure >= 7)
                                                                {
                                                                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Days_since_failure >= 7)", $"{record.Days_since_failure.ToString()}", "true");
                                                                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "11. PTP Arrears", "Update Phone Variable");
                                                                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "12.1 Daily Arrears", "Update SMS Variable");
                                                                    record = await AssignLeadOutcome(record, collection, smsRoutes, "11. PTP Arrears", identity, Step, "12.1 Daily Arrears", 24);
                                                                }
                                                                else if (record.Days_since_failure < 7)
                                                                {
                                                                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Days_since_failure < 7)", $"{record.Days_since_failure.ToString()}", "true");
                                                                    switch (record.Failure_Reason)
                                                                    {
                                                                        case "RD":
                                                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Failure_Reason = \"RD\")", $"{record.Failure_Reason.ToString()}", "true");
                                                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "8. RD Failure", "Update Phone Variable");
                                                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "8.2 RD Failure", "Update SMS Variable");
                                                                            record = await AssignLeadOutcome(record, collection, smsRoutes, "8. RD Failure", identity, Step, "8.2 RD Failure");
                                                                            break;
                                                                        case "Dispute":
                                                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Failure_Reason = \"Dispute\")", $"{record.Failure_Reason.ToString()}", "true");
                                                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "10. Dispute Failure", "Update Phone Variable");
                                                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "10.2 Dispute Failure", "Update SMS Variable");
                                                                            record = await AssignLeadOutcome(record, collection, smsRoutes, "10. Dispute Failure", identity, Step, "10.2 Dispute Failure");
                                                                            break;
                                                                        case "Admin":
                                                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Failure_Reason = \"Admin\")", $"{record.Failure_Reason.ToString()}", "true");
                                                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "9. Admin Failure", "Update Phone Variable");
                                                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "11.2 Admin Failure", "Update SMS Variable");
                                                                            record = await AssignLeadOutcome(record, collection, smsRoutes, "9. Admin Failure", identity, Step, "11.2 Admin Failure");
                                                                            break;
                                                                        default:
                                                                            record = await AssignLeadOutcome(record, collection, smsRoutes, "Invalid Outcome at Debit Order Failure", identity, Step);
                                                                            break;
                                                                    }
                                                                }
                                                                else if (record.Days_since_failure == null)
                                                                {
                                                                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Days_since_failure == null)", $"", "true");
                                                                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "11. PTP Arrears", "Update Phone Variable");
                                                                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "99. No SMS", "Update SMS Variable");
                                                                    record = await AssignLeadOutcome(record, collection, smsRoutes, "11. PTP Arrears", identity, Step, "99. No SMS", 0, 0, "Debit Order Account without days since failure");
                                                                }
                                                            }
                                                            else if (record.Tracking == 1)
                                                            {
                                                                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Tracking == 1)", $"{record.Tracking.ToString()}", "true");
                                                                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "7. Tracking", "Update Phone Variable");
                                                                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "7. Tracking", "Update SMS Variable");
                                                                record = await AssignLeadOutcome(record, collection, smsRoutes, "7. Tracking", identity, Step, "7. Tracking", 0, 4);
                                                            }
                                                        }
                                                    }
                                                }
                                                else if (record.Net_Payments > 0 && !record.NoPayment)
                                                {
                                                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Net_Payments > 0 && !NoPayment)", $"{record.Net_Payments.ToString()}", "true");
                                                    if (record.Net_Payments < Math.Min(record.Original_Instalment ?? 0, record.BALANCE ?? 0)) //Paymnet Less than instalment
                                                    {
                                                        _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (((Net_Payments > 0 && Net_Payments < acp_amount && !record.NoPayment) || Description == \"Short Paid\" || Description == \"Short Paid 3 Days After\")) && PaymentRemainder > 50", $"{record.Description.ToString()}", "true");
                                                        _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "5. Partial Payment Broken PTP", "Update Phone Variable");
                                                        _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "23. Partial PTP", "Update SMS Variable");
                                                        record = await AssignLeadOutcome(record, collection, smsRoutes, "12. Partial Payment", identity, Step, "23.1 Partial Instalment");
                                                    }
                                                    else if (record.Net_Payments >= Math.Min(record.Original_Instalment ?? 0, record.BALANCE ?? 0)) // Payment Equal or more than instalment
                                                    {
                                                        _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Net_Payments >= Math.Min(Original_Instalment ?? 0, BALANCE ?? 0))", $"{record.Net_Payments.ToString()}", "true");
                                                        _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "No Call", "Update Phone Variable");
                                                        _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "22. Thank You", "Update SMS Variable");
                                                        record = await AssignLeadOutcome(record, collection, smsRoutes, "No Call", identity, Step, "22. Thank You");
                                                    }
                                                }
                                            }
                                            else if (record.Description == "Broken" || record.Description == "Short Paid" || record.Description == "Short Paid 3 Days After")
                                            {
                                                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Description == \"Broken\" || Description == \"Short Paid\" || Description == \"Short Paid 3 Days After\")", $"{record.Description.ToString()}", "true");
                                                if (record.Net_Payments == 0 || record.Net_Payments == null || record.Description == "Broken")
                                                {
                                                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Net_Payments == 0 || Net_Payments == null || Description == \"Broken\")", $"{record.Net_Payments.ToString()}", "true");
                                                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "4. Broken PTP", "Update Phone Variable");
                                                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "3. Broken PTP", "Update SMS Variable");
                                                    record = await AssignLeadOutcome(record, collection, smsRoutes, "4. Broken PTP", identity, Step, "3. Broken PTP");
                                                }
                                                else if ((record.Net_Payments > 0 && !record.NoPayment) || record.Description == "Short Paid" || record.Description == "Short Paid 3 Days After")
                                                {
                                                    if (PaymentRemainder >= 50)
                                                    {
                                                        _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (((Net_Payments > 0 && Net_Payments < acp_amount && !record.NoPayment) || Description == \"Short Paid\" || Description == \"Short Paid 3 Days After\")) && PaymentRemainder > 50", $"{record.Description.ToString()}", "true");
                                                        _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "5. Partial Payment Broken PTP", "Update Phone Variable");
                                                        _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "23. Partial PTP", "Update SMS Variable");
                                                        record = await AssignLeadOutcome(record, collection, smsRoutes, "5. Partial Payment Broken PTP", identity, Step, "23. Partial PTP");
                                                    }
                                                    else
                                                    {
                                                        _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (((Net_Payments > 0 && Net_Payments < acp_amount && !record.NoPayment) || Description == \"Short Paid\" || Description == \"Short Paid 3 Days After\")) && PaymentRemainder <= 50", $"{record.Description.ToString()}", "true");
                                                        _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "31. Park", "Update Phone Variable");
                                                        _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "99. No SMS", "Update SMS Variable");
                                                        record = await AssignLeadOutcome(record, collection, smsRoutes, "31. Park", identity, Step, "99. No SMS", 0, 0, "Account with payments and Payment Remainder < 50");
                                                    }
                                                }
                                                else
                                                {
                                                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Description == \"Broken\" || Description == \"Short Paid\" || Description == \"Short Paid 3 Days After\")", $"{record.Description.ToString()}", "false");
                                                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "4. Broken PTP", "Update Phone Variable");
                                                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "3. Broken PTP", "Update SMS Variable");
                                                    record = await AssignLeadOutcome(record, collection, smsRoutes, "4. Broken PTP", identity, Step, "3. Broken PTP");
                                                }
                                            }
                                        }
                                        #endregion
                                    }
                                    else if (record.Current_CD == 2 && record.ARREARS < 50M)
                                    {
                                        record = await AssignLeadOutcome(record, collection, smsRoutes, "CD 2 Small Arrears", identity, Step, "99. No SMS", 0, 0, "Adhoc");
                                    }
                                    #endregion

                                    #region [ CD STATUS 3 ]
                                    else if (record.Current_CD == 3)
                                    {
                                        _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Current_CD == 3)", $"{record.Current_CD.ToString()}", "true");

                                        #region [ MOB 1 ]
                                        if (record.MOB <= 1)
                                        {
                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (MOB <= 1)", $"{record.MOB.ToString()}", "true");
                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "91. Pre-Legal SPD", "Update Phone Variable");
                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "99. No SMS", "Update SMS Variable");
                                            record = await AssignLeadOutcome(record, collection, smsRoutes, "91. Pre-Legal SPD", identity, Step, "99. No SMS", 0, 0, "Pre-Legal Account");
                                        }
                                        #endregion

                                        #region [ MOB 1+ ]
                                        else if (record.MOB > 1)
                                        {
                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (MOB > 1)", $"{record.MOB.ToString()}", "true");
                                            if (record.Description == "In Effect" || record.Description == "Pending")
                                            {
                                                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Description == \"In Effect\" || Description == \"Pending\")", $"{record.Description.ToString()}", "true");
                                                if (record.acp_amount < Math.Min(record.Original_Instalment ?? 0, record.BALANCE ?? 0))
                                                {
                                                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (acp_amount < Math.Min(Original_Instalment ?? 0, BALANCE ?? 0))", $"{record.acp_amount.ToString()}", "true");
                                                    if (record.CountConsec >= 3)
                                                    {
                                                        _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (CountConsec >= 3)", $"{record.CountConsec.ToString()}", "true");
                                                        _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "13. Call to revise PTP amount", "Update Phone Variable");
                                                        _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "99. No SMS", "Update SMS Variable");
                                                        record = await AssignLeadOutcome(record, collection, smsRoutes, "13. Call to revise PTP amount", identity, Step, "99. No SMS", 0, 0, "Account with PTP Amount less than instalment and more than 3 honored PTPs");
                                                    }
                                                    else if (record.CountConsec < 3 || record.CountConsec == null)
                                                    {
                                                        _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (CountConsec < 3 || CountConsec == null)", $"{record.CountConsec.ToString()}", "true");
                                                        if (record.Days_To_PTP_Due == 0 || record.Days_To_PTP_Due == 1 || record.Days_To_PTP_Due == 2)
                                                        {
                                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Days_To_PTP_Due == 0 || Days_To_PTP_Due == 1 || Days_To_PTP_Due == 2)", $"{record.Days_To_PTP_Due.ToString()}", "true");
                                                            if (record.P_Collection_G > 2)
                                                            {
                                                                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (P_Collection_G > 2)", $"{record.P_Collection_G.ToString()}", "true");
                                                                if (record.Risk >= 7)
                                                                {
                                                                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Risk >= 7)", $"{record.Risk.ToString()}", "true");
                                                                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "30. Await PTP Fulfilment", "Update Phone Variable");
                                                                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "99. No SMS", "Update SMS Variable");
                                                                    record = await AssignLeadOutcome(record, collection, smsRoutes, "30. Await PTP Fulfilment", identity, Step, "99. No SMS", 0, 0, "Account on or within 2 days with PTP Amount < Instalment and before PTP Due with < 3 honored PTPs and P_Collection_G > 2 and Low Risk");
                                                                }
                                                                else
                                                                {
                                                                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Risk >= 7)", $"{record.Risk.ToString()}", "false");
                                                                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "3. PTP Reminder", "Update Phone Variable");
                                                                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "19.1 PTP Reminder", "Update SMS Variable");
                                                                    record = await AssignLeadOutcome(record, collection, smsRoutes, "3. PTP Reminder", identity, Step, "19.1 PTP Reminder");
                                                                }
                                                            }
                                                            else if (record.P_Collection_G <= 2)
                                                            {
                                                                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (P_Collection_G <= 2)", $"{record.P_Collection_G.ToString()}", "false");
                                                                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "3. PTP Reminder", "Update Phone Variable");
                                                                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "19.1 PTP Reminder", "Update SMS Variable");
                                                                record = await AssignLeadOutcome(record, collection, smsRoutes, "3. PTP Reminder", identity, Step, "19.1 PTP Reminder");
                                                            }
                                                        }
                                                        else if (record.Days_To_PTP_Due > 2)
                                                        {
                                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Days_To_PTP_Due > 2)", $"{record.Days_To_PTP_Due.ToString()}", "true");
                                                            if (record.Days_Since_PTP_Capture == 0)
                                                            {
                                                                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Days_Since_PTP_Capture == 0)", $"{record.Days_Since_PTP_Capture.ToString()}", "true");
                                                                if (record.PTPOriginalDateCapture.Date < record.PTPDateCapture.Date)
                                                                {
                                                                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (PTPOriginalDateCapture.Date < PTPDateCapture.Date)", $"{record.PTPOriginalDateCapture.Date.ToString()}", "true");
                                                                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "30. Await PTP Fulfilment", "Update Phone Variable");
                                                                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "99. No SMS", "Update SMS Variable");
                                                                    record = await AssignLeadOutcome(record, collection, smsRoutes, "30. Await PTP Fulfilment", identity, Step, "99. No SMS", 0, 0, "Account with PTP Amount < Instalment and more than 2 days before PTP Due and on day of PTP Capture with < 3 honored PTPs");
                                                                }
                                                                else
                                                                {
                                                                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (PTPOriginalDateCapture.Date < PTPDateCapture.Date)", $"{record.PTPOriginalDateCapture.Date.ToString()}", "false");
                                                                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "No Call", "Update Phone Variable");
                                                                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "13.1 PTP Confirmation", "Update SMS Variable");
                                                                    record = await AssignLeadOutcome(record, collection, smsRoutes, "No Call", identity, Step, "13.1 PTP Confirmation");
                                                                }
                                                            }
                                                            else if (record.Days_Since_PTP_Capture > 0)
                                                            {
                                                                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Days_Since_PTP_Capture > 0)", $"{record.Days_Since_PTP_Capture.ToString()}", "true");
                                                                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "30. Await PTP Fulfilment", "Update Phone Variable");
                                                                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "99. No SMS", "Update SMS Variable");
                                                                record = await AssignLeadOutcome(record, collection, smsRoutes, "30. Await PTP Fulfilment", identity, Step, "99. No SMS", 0, 0, "Account with PTP Amount < Instalment and more than 2 days before PTP Due and past day of PTP Capture with < 3 honored PTPs");
                                                            }
                                                        }
                                                    }
                                                }
                                                else if (record.acp_amount >= Math.Min(record.Original_Instalment ?? 0, record.BALANCE ?? 0))
                                                {
                                                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (acp_amount >= Math.Min(Original_Instalment ?? 0, BALANCE ?? 0))", $"{record.acp_amount.ToString()}", "true");
                                                    if (record.Days_To_PTP_Due > 2)
                                                    {
                                                        _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Days_To_PTP_Due > 2)", $"{record.Days_To_PTP_Due.ToString()}", "true");
                                                        if (record.Days_Since_PTP_Capture >= 1)
                                                        {
                                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Days_Since_PTP_Capture >= 1)", $"{record.Days_Since_PTP_Capture.ToString()}", "true");
                                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "30. Await PTP Fulfilment", "Update Phone Variable");
                                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "99. No SMS", "Update SMS Variable");
                                                            record = await AssignLeadOutcome(record, collection, smsRoutes, "30. Await PTP Fulfilment", identity, Step, "99. No SMS", 0, 0, "Account with PTP Amount > Instalment and past day of PTP Capture");
                                                        }
                                                        else if (record.Days_Since_PTP_Capture == 0)
                                                        {
                                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Days_Since_PTP_Capture == 0)", $"{record.Days_Since_PTP_Capture.ToString()}", "true");
                                                            if (record.PTPOriginalDateCapture.Date < record.PTPDateCapture.Date)
                                                            {
                                                                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (PTPOriginalDateCapture.Date < PTPDateCapture.Date)", $"{record.PTPOriginalDateCapture.Date.ToString()}", "true");
                                                                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "30. Await PTP Fulfilment", "Update Phone Variable");
                                                                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "99. No SMS", "Update SMS Variable");
                                                                record = await AssignLeadOutcome(record, collection, smsRoutes, "30. Await PTP Fulfilment", identity, Step, "99. No SMS", 0, 0, "Account with PTP Amount > Instalment for chain PTP and on day of PTP Capture");
                                                            }
                                                            else
                                                            {
                                                                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (PTPOriginalDateCapture.Date < PTPDateCapture.Date)", $"{record.PTPOriginalDateCapture.Date.ToString()}", "false");
                                                                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "No Call", "Update Phone Variable");
                                                                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "13.1 PTP Confirmation", "Update SMS Variable");
                                                                record = await AssignLeadOutcome(record, collection, smsRoutes, "No Call", identity, Step, "13.1 PTP Confirmation");
                                                            }
                                                        }
                                                    }
                                                    else if (record.Days_To_PTP_Due == 1 || record.Days_To_PTP_Due == 2 || record.Days_To_PTP_Due == 0)
                                                    {
                                                        _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Days_To_PTP_Due == 1 || Days_To_PTP_Due == 2 || Days_To_PTP_Due == 0)", $"{record.Days_To_PTP_Due.ToString()}", "true");
                                                        if (record.P_Collection_G == null)
                                                        {
                                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (BASRISKGRADE == String.Empty)", $"{record.BASRISKGRADE.ToString()}", "true");
                                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "3. PTP Reminder", "Update Phone Variable");
                                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "19.1 PTP Reminder", "Update SMS Variable");
                                                            record = await AssignLeadOutcome(record, collection, smsRoutes, "3. PTP Reminder", identity, Step, "19.1 PTP Reminder");
                                                        }
                                                        else
                                                        {
                                                            if (record.P_Collection_G > 2)
                                                            {
                                                                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (P_Collection_G > 2)", $"{record.P_Collection_G.ToString()}", "true");
                                                                if (record.Risk >= 7)
                                                                {
                                                                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Risk >= 7)", $"{record.Risk.ToString()}", "true");
                                                                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "30. Await PTP Fulfilment", "Update Phone Variable");
                                                                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "99. No SMS", "Update SMS Variable");
                                                                    record = await AssignLeadOutcome(record, collection, smsRoutes, "30. Await PTP Fulfilment", identity, Step, "99. No SMS", 0, 0, "Account with P_Collection_G > 2 and Low Risk");
                                                                }
                                                                else if (record.Risk < 7)
                                                                {
                                                                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Risk < 7)", $"{record.Risk.ToString()}", "true");
                                                                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "3. PTP Reminder", "Update Phone Variable");
                                                                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "19.1 PTP Reminder", "Update SMS Variable");
                                                                    record = await AssignLeadOutcome(record, collection, smsRoutes, "3. PTP Reminder", identity, Step, "19.1 PTP Reminder");
                                                                }
                                                            }
                                                            else if (record.P_Collection_G <= 2)
                                                            {
                                                                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (P_Collection_G <= 2)", $"{record.P_Collection_G.ToString()}", "true");
                                                                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "3. PTP Reminder", "Update Phone Variable");
                                                                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "19.1 PTP Reminder", "Update SMS Variable");
                                                                record = await AssignLeadOutcome(record, collection, smsRoutes, "3. PTP Reminder", identity, Step, "19.1 PTP Reminder");
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                            else if (record.Description == "Paid")
                                            {
                                                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Description == \"Paid\")", $"{record.Description.ToString()}", "true");
                                                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "PTP Paid", "Update Phone Variable");
                                                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "99. No SMS", "Update SMS Variable");
                                                record = await AssignLeadOutcome(record, collection, smsRoutes, "PTP Paid", identity, Step);
                                            }
                                            else if (record.Description == "No PTP")
                                            {
                                                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Description == \"No PTP\")", $"{record.Description.ToString()}", "true");
                                                if (record.Net_Payments <= 0 || record.Net_Payments == null || record.NoPayment)
                                                {
                                                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Net_Payments <= 0 || Net_Payments == null || NoPayment)", $"{record.Net_Payments.ToString()}", "true");
                                                    if (record.Payment_Method == "Cash")
                                                    {
                                                        _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Payment_Method == \"Cash\")", $"{record.Payment_Method.ToString()}", "true");
                                                        if (record.Days_to_salary_date == 0 || record.Days_to_salary_date == 1)
                                                        {
                                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Days_to_salary_date == 0)", $"{record.Days_to_salary_date.ToString()}", "true");
                                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "6. PTP Call", "Update Phone Variable");
                                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "4. Cash Reminder", "Update SMS Variable");
                                                            record = await AssignLeadOutcome(record, collection, smsRoutes, "6. PTP Call", identity, Step, "4. Cash Reminder");
                                                        }
                                                        //if (record.Days_to_salary_date == 0)
                                                        //{
                                                        //    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Days_to_salary_date == 0)", $"{record.Days_to_salary_date.ToString()}", "true");
                                                        //    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "6. PTP Call", "Update Phone Variable");
                                                        //    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "4. Cash Reminder", "Update SMS Variable");
                                                        //    record = await AssignLeadOutcome(record, collection, smsRoutes, "6. PTP Call", identity, Step, "4. Cash Reminder");
                                                        //}
                                                        else if (record.Days_to_salary_date < 0 || record.Days_to_salary_date >= 2)
                                                        {
                                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Days_to_salary_date < 0)", $"{record.Days_to_salary_date.ToString()}", "true");
                                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "6. PTP Call", "Update Phone Variable");
                                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "4. Cash Reminder", "Update SMS Variable");
                                                            record = await AssignLeadOutcome(record, collection, smsRoutes, "6. PTP Call", identity, Step, "6. Arrears (3 Daily)", 24);
                                                        }
                                                        //else if (record.Days_to_salary_date < 0)
                                                        //{
                                                        //    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Days_to_salary_date < 0)", $"{record.Days_to_salary_date.ToString()}", "true");
                                                        //    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "6. PTP Call", "Update Phone Variable");
                                                        //    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "4. Cash Reminder", "Update SMS Variable");
                                                        //    record = await AssignLeadOutcome(record, collection, smsRoutes, "6. PTP Call", identity, Step, "4. Cash Reminder", 24);
                                                        //}
                                                        //else if (record.Days_to_salary_date > 0)
                                                        //{
                                                        //    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Days_to_salary_date > 0)", $"{record.Days_to_salary_date.ToString()}", "true");
                                                        //    if (record.P_Collection_G > 2)
                                                        //    {
                                                        //        _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (P_Collection_G > 2)", $"{record.P_Collection_G.ToString()}", "true");
                                                        //        _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "No Call", "Update Phone Variable");
                                                        //        _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "4. Cash Reminder", "Update SMS Variable");
                                                        //        record = await AssignLeadOutcome(record, collection, smsRoutes, "No Call", identity, Step, "4. Cash Reminder", 120);
                                                        //    }
                                                        //    else if (record.P_Collection_G <= 2)
                                                        //    {
                                                        //        _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (P_Collection_G <= 2)", $"{record.P_Collection_G.ToString()}", "true");
                                                        //        _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "6. PTP Call", "Update Phone Variable");
                                                        //        _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "4. Cash Reminder", "Update SMS Variable");
                                                        //        record = await AssignLeadOutcome(record, collection, smsRoutes, "6. PTP Call", identity, Step, "4. Cash Reminder", 120);
                                                        //    }
                                                        //}
                                                        else
                                                        {
                                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Days_to_salary_date > 0)", $"{record.Days_to_salary_date.ToString()}", "false");
                                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "6. PTP Call", "Update Phone Variable");
                                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "4. Cash Reminder", "Update SMS Variable");
                                                            record = await AssignLeadOutcome(record, collection, smsRoutes, "6. PTP Call", identity, Step, "6. Arrears (3 Daily)", 120);
                                                        }
                                                    }
                                                    else if (record.Payment_Method == "Debit_Order")
                                                    {
                                                        _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Payment_Method == \"Debit_Order\")", $"{record.Payment_Method.ToString()}", "true");
                                                        if (record.Days_to_Potential_Strike_Date == 1 || record.Days_to_Potential_Strike_Date == 0)
                                                        {
                                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (P_Collection_G <= 2)", $"{record.P_Collection_G.ToString()}", "true");
                                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "6. PTP Call", "Update Phone Variable");
                                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "5. Debit Order Reminder", "Update SMS Variable");
                                                            record = await AssignLeadOutcome(record, collection, smsRoutes, "6. PTP Call", identity, Step, "5. Debit Order Reminder");
                                                        }
                                                        else if (record.Days_to_Potential_Strike_Date >= 2)
                                                        {
                                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (P_Collection_G <= 2)", $"{record.P_Collection_G.ToString()}", "true");
                                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "6. PTP Call", "Update Phone Variable");
                                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "5. Debit Order Reminder", "Update SMS Variable");
                                                            record = await AssignLeadOutcome(record, collection, smsRoutes, "6. PTP Call", identity, Step, "6. Arrears (3 Daily)");
                                                        }
                                                        else
                                                        {
                                                            if (record.Tracking == 0 || record.Tracking == null)
                                                            {
                                                                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Tracking == 0 || Tracking == null)", $"{record.Tracking.ToString()}", "true");
                                                                if (record.Days_since_failure >= 7)
                                                                {
                                                                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Days_since_failure >= 7)", $"{record.Days_since_failure.ToString()}", "true");
                                                                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "11. PTP Arrears", "Update Phone Variable");
                                                                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "12.1 Daily Arrears", "Update SMS Variable");
                                                                    record = await AssignLeadOutcome(record, collection, smsRoutes, "11. PTP Arrears", identity, Step, "12.1 Daily Arrears", 24);
                                                                }
                                                                else if (record.Days_since_failure < 7)
                                                                {
                                                                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Days_since_failure < 7)", $"{record.Days_since_failure.ToString()}", "true");
                                                                    switch (record.Failure_Reason)
                                                                    {
                                                                        case "RD":
                                                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Failure_Reason = \"RD\")", $"{record.Failure_Reason.ToString()}", "true");
                                                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "8. RD Failure", "Update Phone Variable");
                                                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "8.2 RD Failure", "Update SMS Variable");
                                                                            record = await AssignLeadOutcome(record, collection, smsRoutes, "8. RD Failure", identity, Step, "8.2 RD Failure");
                                                                            break;
                                                                        case "Dispute":
                                                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Failure_Reason = \"Dispute\")", $"{record.Failure_Reason.ToString()}", "true");
                                                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "10. Dispute Failure", "Update Phone Variable");
                                                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "10.2 Dispute Failure", "Update SMS Variable");
                                                                            record = await AssignLeadOutcome(record, collection, smsRoutes, "10. Dispute Failure", identity, Step, "10.2 Dispute Failure");
                                                                            break;
                                                                        case "Admin":
                                                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Failure_Reason = \"Admin\")", $"{record.Failure_Reason.ToString()}", "true");
                                                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "9. Admin Failure", "Update Phone Variable");
                                                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "11.2 Admin Failure", "Update SMS Variable");
                                                                            record = await AssignLeadOutcome(record, collection, smsRoutes, "9. Admin Failure", identity, Step, "11.2 Admin Failure");
                                                                            break;
                                                                        default:
                                                                            record = await AssignLeadOutcome(record, collection, smsRoutes, "Invalid Outcome at Debit Order Failure", identity, Step);
                                                                            break;
                                                                    }
                                                                }
                                                                else if (record.Days_since_failure == null)
                                                                {
                                                                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Days_since_failure == null)", $"", "true");
                                                                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "11. PTP Arrears", "Update Phone Variable");
                                                                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "99. No SMS", "Update SMS Variable");
                                                                    record = await AssignLeadOutcome(record, collection, smsRoutes, "11. PTP Arrears", identity, Step, "99. No SMS", 0, 0, "Debit Order account without Days Since Failure");
                                                                }
                                                            }
                                                            else if (record.Tracking == 1)
                                                            {
                                                                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Tracking == 1)", $"{record.Tracking.ToString()}", "true");
                                                                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "7. Tracking", "Update Phone Variable");
                                                                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "7. Tracking", "Update SMS Variable");
                                                                record = await AssignLeadOutcome(record, collection, smsRoutes, "7. Tracking", identity, Step, "7. Tracking", 0, 3);
                                                            }
                                                        }
                                                    }
                                                }
                                                else if (record.Net_Payments > 0 && !record.NoPayment)
                                                {
                                                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Net_Payments > 0 && !NoPayment)", $"{record.Net_Payments.ToString()}", "true");
                                                    if (record.Net_Payments < Math.Min(record.Original_Instalment ?? 0, record.BALANCE ?? 0))
                                                    {
                                                        _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (((Net_Payments > 0 && Net_Payments < acp_amount && !record.NoPayment) || Description == \"Short Paid\" || Description == \"Short Paid 3 Days After\")) && PaymentRemainder > 50", $"{record.Description.ToString()}", "true");
                                                        _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "5. Partial Payment Broken PTP", "Update Phone Variable");
                                                        _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "23. Partial PTP", "Update SMS Variable");
                                                        record = await AssignLeadOutcome(record, collection, smsRoutes, "12. Partial Payment", identity, Step, "23.1 Partial Instalment");
                                                    }
                                                    else if (record.Net_Payments >= Math.Min(record.Original_Instalment ?? 0, record.BALANCE ?? 0))
                                                    {
                                                        _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Net_Payments >= Math.Min(Original_Instalment ?? 0, BALANCE ?? 0))", $"{record.Net_Payments.ToString()}", "true");
                                                        _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "No Call", "Update Phone Variable");
                                                        _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "22. Thank You", "Update SMS Variable");
                                                        record = await AssignLeadOutcome(record, collection, smsRoutes, "No Call", identity, Step, "22. Thank You");
                                                    }
                                                }
                                            }
                                            else if (record.Description == "Broken" || record.Description == "Short Paid" || record.Description == "Short Paid 3 Days After")
                                            {
                                                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Description == \"Broken\" || Description == \"Short Paid\" || Description == \"Short Paid 3 Days After\")", $"{record.Description.ToString()}", "true");
                                                if ((record.NoPayment) || record.Description == "Broken")
                                                {
                                                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if ((NoPayment) || Description == \"Broken\")", $"{record.Description.ToString()}", "true");
                                                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "4. Broken PTP", "Update Phone Variable");
                                                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "3. Broken PTP", "Update SMS Variable");
                                                    record = await AssignLeadOutcome(record, collection, smsRoutes, "4. Broken PTP", identity, Step, "3. Broken PTP");
                                                }
                                                else if ((record.Net_Payments < record.acp_amount && record.Net_Payments > 0 && !record.NoPayment) || record.Description == "Short Paid" || record.Description == "Short Paid 3 Days After")
                                                {
                                                    if (PaymentRemainder >= 50)
                                                    {
                                                        _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (((Net_Payments > 0 && Net_Payments < acp_amount && !record.NoPayment) || Description == \"Short Paid\" || Description == \"Short Paid 3 Days After\")) && PaymentRemainder > 50", $"{record.Description.ToString()}", "true");
                                                        _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "5. Partial Payment Broken PTP", "Update Phone Variable");
                                                        _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "23. Partial PTP", "Update SMS Variable");
                                                        record = await AssignLeadOutcome(record, collection, smsRoutes, "5. Partial Payment Broken PTP", identity, Step, "23. Partial PTP");
                                                    }
                                                    else
                                                    {
                                                        _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (((Net_Payments > 0 && Net_Payments < acp_amount && !record.NoPayment) || Description == \"Short Paid\" || Description == \"Short Paid 3 Days After\")) && PaymentRemainder <= 50", $"{record.Description.ToString()}", "true");
                                                        _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "31. Park", "Update Phone Variable");
                                                        _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "99. No SMS", "Update SMS Variable");
                                                        record = await AssignLeadOutcome(record, collection, smsRoutes, "31. Park", identity, Step, "99. No SMS", 0, 0, "Account with payments and Payment Remainder < 50");
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Description == \"Broken\" || Description == \"Short Paid\" || Description == \"Short Paid 3 Days After\")", $"{record.Description.ToString()}", "false");
                                                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "4. Broken PTP", "Update Phone Variable");
                                                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "3. Broken PTP", "Update SMS Variable");
                                                record = await AssignLeadOutcome(record, collection, smsRoutes, "4. Broken PTP", identity, Step, "3. Broken PTP");
                                            }
                                        }
                                        #endregion
                                    }
                                    #endregion

                                    #region [ CD STATUS 4+ ]
                                    else if (record.Current_CD >= 4)
                                    {
                                        _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Current_CD >= 4)", $"{record.Current_CD.ToString()}", "true");
                                        if (record.NumberPayments == 0)
                                        {
                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (NumberPayments == 0)", $"{record.NumberPayments.ToString()}", "true");
                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "92. Pre-Legal High-Risk", "Update Phone Variable");
                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "99. No SMS", "Update SMS Variable");
                                            record = await AssignLeadOutcome(record, collection, smsRoutes, "92. Pre-Legal High-Risk", identity, Step, "99. No SMS", 0, 0, "Pre Legal Account");
                                        }
                                        else if (record.NumberPayments != 0)
                                        {
                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (NumberPayments != 0)", $"{record.NumberPayments.ToString()}", "true");
                                            if (record.Description == "In Effect")
                                            {
                                                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Description == \"In Effect\")", $"{record.Description.ToString()}", "true");
                                                if (record.acp_amount >= Math.Min(record.Original_Instalment ?? 0, record.BALANCE ?? 0))
                                                {
                                                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (acp_amount >= Math.Min(Original_Instalment ?? 0, BALANCE ?? 0))", $"{record.acp_amount.ToString()}", "true");
                                                    if (record.Days_To_PTP_Due == 0 || record.Days_To_PTP_Due == 1 || record.Days_To_PTP_Due == 2 || record.Days_To_PTP_Due == 3)
                                                    {
                                                        _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Days_To_PTP_Due == 0 || Days_To_PTP_Due == 1 || Days_To_PTP_Due == 2 || Days_To_PTP_Due == 3)", $"{record.Days_To_PTP_Due.ToString()}", "true");
                                                        if (record.P_Collection_G == null)
                                                        {
                                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (P_Collection_G == null)", $"", "true");
                                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "3. PTP Reminder", "Update Phone Variable");
                                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "19.1 PTP Reminder", "Update SMS Variable");
                                                            record = await AssignLeadOutcome(record, collection, smsRoutes, "3. PTP Reminder", identity, Step, "19.1 PTP Reminder");
                                                        }
                                                        else if (record.P_Collection_G > 2)
                                                        {
                                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (P_Collection_G > 2)", $"{record.P_Collection_G.ToString()}", "true");
                                                            if (record.Risk >= 7)
                                                            {
                                                                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Risk >= 7)", $"{record.Risk.ToString()}", "true");
                                                                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "30. Await PTP Fulfilment", "Update Phone Variable");
                                                                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "99. No SMS", "Update SMS Variable");
                                                                record = await AssignLeadOutcome(record, collection, smsRoutes, "30. Await PTP Fulfilment", identity, Step, "99. No SMS", 0, 0, "Account on or within 3 days of PTP Due with PTP Amount >= Instalment and before PTP Due and P_Collection_G > 2 and Low Risk");
                                                            }
                                                            else if (record.Risk < 7)
                                                            {
                                                                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Risk < 7)", $"{record.Risk.ToString()}", "true");
                                                                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "3. PTP Reminder", "Update Phone Variable");
                                                                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "19.1 PTP Reminder", "Update SMS Variable");
                                                                record = await AssignLeadOutcome(record, collection, smsRoutes, "3. PTP Reminder", identity, Step, "19.1 PTP Reminder");
                                                            }
                                                        }
                                                        else if (record.P_Collection_G <= 2)
                                                        {
                                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (P_Collection_G <= 2)", $"{record.P_Collection_G.ToString()}", "true");
                                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "3. PTP Reminder", "Update Phone Variable");
                                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "19.1 PTP Reminder", "Update SMS Variable");
                                                            record = await AssignLeadOutcome(record, collection, smsRoutes, "3. PTP Reminder", identity, Step, "19.1 PTP Reminder");
                                                        }
                                                    }
                                                    else if (record.Days_To_PTP_Due > 3)
                                                    {
                                                        _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Days_To_PTP_Due > 3)", $"{record.Days_To_PTP_Due.ToString()}", "true");
                                                        if (record.Days_Since_PTP_Capture == 0)
                                                        {
                                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Days_Since_PTP_Capture == 0)", $"{record.Days_Since_PTP_Capture.ToString()}", "true");
                                                            if (record.PTPOriginalDateCapture.Date < record.PTPDateCapture.Date)
                                                            {
                                                                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (PTPOriginalDateCapture.Date < PTPDateCapture.Date)", $"{record.PTPOriginalDateCapture.Date.ToString()}", "true");
                                                                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "30. Await PTP Fulfilment", "Update Phone Variable");
                                                                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "99. No SMS", "Update SMS Variable");
                                                                record = await AssignLeadOutcome(record, collection, smsRoutes, "30. Await PTP Fulfilment", identity, Step, "99. No SMS", 0, 0, "Account with more than 3 days to PTP Due on PTP Capture date with PTP Amount >= Instalmentwith chain PTP");
                                                            }
                                                            else
                                                            {
                                                                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (PTPOriginalDateCapture.Date < PTPDateCapture.Date)", $"{record.PTPOriginalDateCapture.Date.ToString()}", "false");
                                                                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "No Call", "Update Phone Variable");
                                                                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "13.1 PTP Confirmation", "Update SMS Variable");
                                                                record = await AssignLeadOutcome(record, collection, smsRoutes, "No Call", identity, Step, "13.1 PTP Confirmation");
                                                            }
                                                        }
                                                        else if (record.Days_Since_PTP_Capture >= 1)
                                                        {
                                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Days_Since_PTP_Capture >= 1)", $"{record.Days_Since_PTP_Capture.ToString()}", "false");
                                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "30. Await PTP Fulfilment", "Update Phone Variable");
                                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "99. No SMS", "Update SMS Variable");
                                                            record = await AssignLeadOutcome(record, collection, smsRoutes, "30. Await PTP Fulfilment", identity, Step, "99. No SMS", 0, 0, "Account with more than 3 days to PTP Due with PTP Amount < Instalment and after PTP Capture date");
                                                        }
                                                    }
                                                }
                                                else if (record.acp_amount < Math.Min(record.Original_Instalment ?? 0, record.BALANCE ?? 0))
                                                {
                                                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (acp_amount < Math.Min(Original_Instalment ?? 0, BALANCE ?? 0))", $"{record.acp_amount.ToString()}", "true");
                                                    if (record.CountConsec >= 3)
                                                    {
                                                        _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (CountConsec >= 3)", $"{record.CountConsec.ToString()}", "true");
                                                        _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "13. Call to revise PTP amount", "Update Phone Variable");
                                                        _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "99. No SMS", "Update SMS Variable");
                                                        record = await AssignLeadOutcome(record, collection, smsRoutes, "13. Call to revise PTP amount", identity, Step, "99. No SMS", 0, 0, "Account with PTP Amount less than Instalment amount and more than 3 PTPs honored");
                                                    }
                                                    else if (record.CountConsec < 3 || record.CountConsec == null)
                                                    {
                                                        _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (CountConsec < 3 || CountConsec == null)", $"{record.CountConsec.ToString()}", "true");
                                                        if (record.Days_To_PTP_Due > 3)
                                                        {
                                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Days_To_PTP_Due > 3)", $"{record.Days_To_PTP_Due.ToString()}", "true");
                                                            if (record.Days_Since_PTP_Capture >= 1)
                                                            {
                                                                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Days_Since_PTP_Capture >= 1)", $"{record.Days_Since_PTP_Capture.ToString()}", "true");
                                                                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "30. Await PTP Fulfilment", "Update Phone Variable");
                                                                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "99. No SMS", "Update SMS Variable");
                                                                record = await AssignLeadOutcome(record, collection, smsRoutes, "30. Await PTP Fulfilment", identity, Step, "99. No SMS", 0, 0, "Account with PTP Amount less than Instalment amount and less than 3 PTPs honored and more than 3 days to PTP due and past PTP Capture Date");
                                                            }
                                                            else if (record.Days_Since_PTP_Capture <= 0)
                                                            {
                                                                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Days_Since_PTP_Capture <= 0)", $"{record.Days_Since_PTP_Capture.ToString()}", "true");
                                                                if (record.PTPOriginalDateCapture.Date < record.PTPDateCapture.Date)
                                                                {
                                                                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (PTPOriginalDateCapture.Date < PTPDateCapture.Date)", $"{record.PTPOriginalDateCapture.Date.ToString()}", "true");
                                                                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "30. Await PTP Fulfilment", "Update Phone Variable");
                                                                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "99. No SMS", "Update SMS Variable");
                                                                    record = await AssignLeadOutcome(record, collection, smsRoutes, "30. Await PTP Fulfilment", identity, Step, "99. No SMS", 0, 0, "Account with PTP Amount less than Instalment amount and less than 3 PTPs honored and more than 3 days to PTP due and on or before PTP Capture Date");
                                                                }
                                                                else
                                                                {
                                                                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (PTPOriginalDateCapture.Date < PTPDateCapture.Date)", $"{record.PTPOriginalDateCapture.Date.ToString()}", "false");
                                                                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "No Call", "Update Phone Variable");
                                                                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "13.1 PTP Confirmation", "Update SMS Variable");
                                                                    record = await AssignLeadOutcome(record, collection, smsRoutes, "No Call", identity, Step, "13.1 PTP Confirmation");
                                                                }
                                                            }
                                                        }
                                                        else if (record.Days_To_PTP_Due == 0 || record.Days_To_PTP_Due == 1 || record.Days_To_PTP_Due == 2 || record.Days_To_PTP_Due == 3)
                                                        {
                                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Days_To_PTP_Due == 0 || Days_To_PTP_Due == 1 || Days_To_PTP_Due == 2 || Days_To_PTP_Due == 3)", $"{record.Days_To_PTP_Due.ToString()}", "true");
                                                            if (record.P_Collection_G == null)
                                                            {
                                                                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (P_Collection_G == null)", $"", "true");
                                                                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "3. PTP Reminder", "Update Phone Variable");
                                                                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "19.1 PTP Reminder", "Update SMS Variable");
                                                                record = await AssignLeadOutcome(record, collection, smsRoutes, "3. PTP Reminder", identity, Step, "19.1 PTP Reminder");
                                                            }
                                                            else if (record.P_Collection_G <= 2)
                                                            {
                                                                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (P_Collection_G <= 2)", $"{record.P_Collection_G.ToString()}", "true");
                                                                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "3. PTP Reminder", "Update Phone Variable");
                                                                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "19.1 PTP Reminder", "Update SMS Variable");
                                                                record = await AssignLeadOutcome(record, collection, smsRoutes, "3. PTP Reminder", identity, Step, "19.1 PTP Reminder");
                                                            }
                                                            else if (record.P_Collection_G > 2)
                                                            {
                                                                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (P_Collection_G > 2)", $"{record.P_Collection_G.ToString()}", "true");
                                                                if (record.Risk >= 7)
                                                                {
                                                                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Risk >= 7)", $"{record.Risk.ToString()}", "true");
                                                                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "30. Await PTP Fulfilment", "Update Phone Variable");
                                                                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "99. No SMS", "Update SMS Variable");
                                                                    record = await AssignLeadOutcome(record, collection, smsRoutes, "30. Await PTP Fulfilment", identity, Step, "99. No SMS", 0, 0, "Account on or within 3 days of PTP Due with PTP Amount < Instalment and before PTP Due with < 3 honored PTPs and P_Collection_G > 2 and Low Risk");
                                                                }
                                                                else if (record.Risk < 7)
                                                                {
                                                                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Risk < 7)", $"{record.Risk.ToString()}", "true");
                                                                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "3. PTP Reminder", "Update Phone Variable");
                                                                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "19.1 PTP Reminder", "Update SMS Variable");
                                                                    record = await AssignLeadOutcome(record, collection, smsRoutes, "3. PTP Reminder", identity, Step, "19.1 PTP Reminder");
                                                                }
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                            else if (record.Description == "Paid")
                                            {
                                                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Description == \"Paid\")", $"{record.Description.ToString()}", "true");
                                                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "PTP Paid", "Update Phone Variable");
                                                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "99. No SMS", "Update SMS Variable");
                                                record = await AssignLeadOutcome(record, collection, smsRoutes, "PTP Paid", identity, Step);
                                            }
                                            else if (record.Description == "Broken" || record.Description == "Short Paid" || record.Description == "Short Paid 3 Days After")
                                            {
                                                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Description == \"Broken\" || Description == \"Short Paid\" || Description == \"Short Paid 3 Days After\")", $"{record.Description.ToString()}", "true");
                                                if (record.NoPayment || record.Description == "Broken")
                                                {
                                                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (NoPayment || Description == \"Broken\")", $"{record.Description.ToString()}", "true");
                                                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "4. Broken PTP", "Update Phone Variable");
                                                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "3. Broken PTP", "Update SMS Variable");
                                                    record = await AssignLeadOutcome(record, collection, smsRoutes, "4. Broken PTP", identity, Step, "3. Broken PTP");
                                                }
                                                else if (record.Net_Payments < Math.Min(record.Original_Instalment ?? 0, record.BALANCE ?? 0) && record.Net_Payments > 0 && !record.NoPayment || record.Description == "Short Paid" || record.Description == "Short Paid 3 Days After") // PTP Partial Payment Made
                                                {
                                                    if (PaymentRemainder >= 50)
                                                    {
                                                        _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (((Net_Payments > 0 && Net_Payments < acp_amount && !record.NoPayment) || Description == \"Short Paid\" || Description == \"Short Paid 3 Days After\")) && PaymentRemainder > 50", $"{record.Description.ToString()}", "true");
                                                        _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "5. Partial Payment Broken PTP", "Update Phone Variable");
                                                        _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "23. Partial PTP", "Update SMS Variable");
                                                        record = await AssignLeadOutcome(record, collection, smsRoutes, "5. Partial Payment Broken PTP", identity, Step, "23. Partial PTP");
                                                    }
                                                    else
                                                    {
                                                        _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (((Net_Payments > 0 && Net_Payments < acp_amount && !record.NoPayment) || Description == \"Short Paid\" || Description == \"Short Paid 3 Days After\")) && PaymentRemainder <= 50", $"{record.Description.ToString()}", "true");
                                                        _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "31. Park", "Update Phone Variable");
                                                        _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "99. No SMS", "Update SMS Variable");
                                                        record = await AssignLeadOutcome(record, collection, smsRoutes, "31. Park", identity, Step, "99. No SMS", 0, 0, "Account with payments and Payment Remainder < 50");
                                                    }
                                                }
                                            }
                                            else if (record.Description == "No PTP")
                                            {
                                                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Description == \"No PTP\")", $"{record.Description.ToString()}", "true");
                                                if (record.Net_Payments == 0 || record.Net_Payments == null || record.NoPayment)
                                                {
                                                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Net_Payments == 0 || Net_Payments == null || NoPayment)", $"{record.Net_Payments.ToString()}", "true");
                                                    if (record.Payment_Method == "Cash")
                                                    {
                                                        _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Payment_Method == \"Cash\")", $"{record.Payment_Method.ToString()}", "true");
                                                        if (record.Days_to_salary_date == 0 || record.Days_to_salary_date == 1)
                                                        {
                                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Days_to_salary_date == 0)", $"{record.Days_to_salary_date.ToString()}", "true");
                                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "6. PTP Call", "Update Phone Variable");
                                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "4. Cash Reminder", "Update SMS Variable");
                                                            record = await AssignLeadOutcome(record, collection, smsRoutes, "6. PTP Call", identity, Step, "4. Cash Reminder");
                                                        }
                                                        //if (record.Days_to_salary_date == 0)
                                                        //{
                                                        //    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Days_to_salary_date == 0)", $"{record.Days_to_salary_date.ToString()}", "true");
                                                        //    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "6. PTP Call", "Update Phone Variable");
                                                        //    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "4. Cash Reminder", "Update SMS Variable");
                                                        //    record = await AssignLeadOutcome(record, collection, smsRoutes, "6. PTP Call", identity, Step, "4. Cash Reminder");
                                                        //}
                                                        else if (record.Days_to_salary_date < 0 || record.Days_to_salary_date >= 2)
                                                        {
                                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Days_to_salary_date < 0)", $"{record.Days_to_salary_date.ToString()}", "true");
                                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "6. PTP Call", "Update Phone Variable");
                                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "4. Cash Reminder", "Update SMS Variable");
                                                            record = await AssignLeadOutcome(record, collection, smsRoutes, "6. PTP Call", identity, Step, "6. Arrears (3 Daily)", 24);
                                                        }
                                                        //else if (record.Days_to_salary_date < 0)
                                                        //{
                                                        //    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Days_to_salary_date < 0)", $"{record.Days_to_salary_date.ToString()}", "true");
                                                        //    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "6. PTP Call", "Update Phone Variable");
                                                        //    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "4. Cash Reminder", "Update SMS Variable");
                                                        //    record = await AssignLeadOutcome(record, collection, smsRoutes, "6. PTP Call", identity, Step, "4. Cash Reminder", 24);
                                                        //}
                                                        //else if (record.Days_to_salary_date > 0)
                                                        //{
                                                        //    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Days_to_salary_date > 0)", $"{record.Days_to_salary_date.ToString()}", "true");
                                                        //    if (record.P_Collection_G > 2)
                                                        //    {
                                                        //        _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (P_Collection_G > 2)", $"{record.P_Collection_G.ToString()}", "true");
                                                        //        _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "No Call", "Update Phone Variable");
                                                        //        _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "4. Cash Reminder", "Update SMS Variable");
                                                        //        record = await AssignLeadOutcome(record, collection, smsRoutes, "No Call", identity, Step, "4. Cash Reminder", 120);
                                                        //    }
                                                        //    else if (record.P_Collection_G <= 2)
                                                        //    {
                                                        //        _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (P_Collection_G <= 2)", $"{record.P_Collection_G.ToString()}", "true");
                                                        //        _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "6. PTP Call", "Update Phone Variable");
                                                        //        _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "4. Cash Reminder", "Update SMS Variable");
                                                        //        record = await AssignLeadOutcome(record, collection, smsRoutes, "6. PTP Call", identity, Step, "4. Cash Reminder", 120);
                                                        //    }
                                                        //}
                                                        else
                                                        {
                                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Days_to_salary_date > 0)", $"{record.Days_to_salary_date.ToString()}", "false");
                                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "6. PTP Call", "Update Phone Variable");
                                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "4. Cash Reminder", "Update SMS Variable");
                                                            record = await AssignLeadOutcome(record, collection, smsRoutes, "6. PTP Call", identity, Step, "6. Arrears (3 Daily)", 120);
                                                        }
                                                    }
                                                    else if (record.Payment_Method == "Debit_Order")
                                                    {
                                                        _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Payment_Method == \"Debit_Order\")", $"{record.Payment_Method.ToString()}", "true");
                                                        if (record.Days_to_Potential_Strike_Date == 1 || record.Days_to_Potential_Strike_Date == 0)
                                                        {
                                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (P_Collection_G <= 2)", $"{record.P_Collection_G.ToString()}", "true");
                                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "6. PTP Call", "Update Phone Variable");
                                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "5. Debit Order Reminder", "Update SMS Variable");
                                                            record = await AssignLeadOutcome(record, collection, smsRoutes, "6. PTP Call", identity, Step, "5. Debit Order Reminder");
                                                        }
                                                        else if (record.Days_to_Potential_Strike_Date >= 2)
                                                        {
                                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (P_Collection_G <= 2)", $"{record.P_Collection_G.ToString()}", "true");
                                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "6. PTP Call", "Update Phone Variable");
                                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "5. Debit Order Reminder", "Update SMS Variable");
                                                            record = await AssignLeadOutcome(record, collection, smsRoutes, "6. PTP Call", identity, Step, "6. Arrears (3 Daily)");
                                                        }
                                                        else
                                                        {
                                                            if (record.Tracking == 0 || record.Tracking == null)
                                                            {
                                                                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Tracking == 0 || Tracking == null)", $"{record.Tracking.ToString()}", "true");
                                                                if (record.Days_since_failure >= 7)
                                                                {
                                                                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Days_since_failure >= 7)", $"{record.Days_since_failure.ToString()}", "true");
                                                                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "11. PTP Arrears", "Update Phone Variable");
                                                                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "12.1 Daily Arrears", "Update SMS Variable");
                                                                    record = await AssignLeadOutcome(record, collection, smsRoutes, "11. PTP Arrears", identity, Step, "12.1 Daily Arrears");
                                                                }
                                                                else if (record.Days_since_failure < 7)
                                                                {
                                                                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Days_since_failure < 7)", $"{record.Days_since_failure.ToString()}", "true");
                                                                    switch (record.Failure_Reason)
                                                                    {
                                                                        case "RD":
                                                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Failure_Reason = \"RD\")", $"{record.Failure_Reason.ToString()}", "true");
                                                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "8. RD Failure", "Update Phone Variable");
                                                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "8.2 RD Failure", "Update SMS Variable");
                                                                            record = await AssignLeadOutcome(record, collection, smsRoutes, "8. RD Failure", identity, Step, "8.2 RD Failure");
                                                                            break;
                                                                        case "Dispute":
                                                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Failure_Reason = \"Dispute\")", $"{record.Failure_Reason.ToString()}", "true");
                                                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "10. Dispute Failure", "Update Phone Variable");
                                                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "10.2 Dispute Failure", "Update SMS Variable");
                                                                            record = await AssignLeadOutcome(record, collection, smsRoutes, "10. Dispute Failure", identity, Step, "10.2 Dispute Failure");
                                                                            break;
                                                                        case "Admin":
                                                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Failure_Reason = \"Admin\")", $"{record.Failure_Reason.ToString()}", "true");
                                                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "9. Admin Failure", "Update Phone Variable");
                                                                            _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "11.2 Admin Failure", "Update SMS Variable");
                                                                            record = await AssignLeadOutcome(record, collection, smsRoutes, "9. Admin Failure", identity, Step, "11.2 Admin Failure");
                                                                            break;
                                                                        default:
                                                                            record = await AssignLeadOutcome(record, collection, smsRoutes, "Invalid Outcome at Debit Order Failure", identity, Step);
                                                                            break;
                                                                    }
                                                                }
                                                                else if (record.Days_since_failure == null)
                                                                {
                                                                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Days_since_failure == null)", $"", "true");
                                                                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "11. PTP Arrears", "Update Phone Variable");
                                                                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "99. No SMS", "Update SMS Variable");
                                                                    record = await AssignLeadOutcome(record, collection, smsRoutes, "11. PTP Arrears", identity, Step, "99. No SMS");
                                                                }
                                                            }
                                                            else if (record.Tracking == 1)
                                                            {
                                                                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Tracking == 1)", $"{record.Tracking.ToString()}", "true");
                                                                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "7. Tracking", "Update Phone Variable");
                                                                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "7. Tracking", "Update SMS Variable");
                                                                record = await AssignLeadOutcome(record, collection, smsRoutes, "7. Tracking", identity, Step, "7. Tracking", 0, 3);
                                                            }
                                                        }
                                                    }
                                                }
                                                else
                                                {
                                                    if (record.Net_Payments < Math.Min(record.Original_Instalment ?? 0, record.BALANCE ?? 0) && !record.NoPayment)
                                                    {
                                                        _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (((Net_Payments > 0 && Net_Payments < acp_amount && !record.NoPayment) || Description == \"Short Paid\" || Description == \"Short Paid 3 Days After\")) && PaymentRemainder > 50", $"{record.Description.ToString()}", "true");
                                                        _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "5. Partial Payment Broken PTP", "Update Phone Variable");
                                                        _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "23. Partial PTP", "Update SMS Variable");
                                                        record = await AssignLeadOutcome(record, collection, smsRoutes, "6. PTP Call", identity, Step, "23.1 Partial Instalment");
                                                    }
                                                    else if (record.Net_Payments >= Math.Min(record.Original_Instalment ?? 0, record.BALANCE ?? 0) && !record.NoPayment)
                                                    {
                                                        _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Net_Payments >= Math.Min(Original_Instalment ?? 0, BALANCE ?? 0) && !NoPayment)", $"{record.Net_Payments.ToString()}", "true");
                                                        _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "No Call", "Update Phone Variable");
                                                        _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "22. Thank You", "Update SMS Variable");
                                                        record = await AssignLeadOutcome(record, collection, smsRoutes, "No Call", identity, Step, "22. Thank You");
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (Description == \"No PTP\")", $"{record.Description.ToString()}", "false");
                                                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "4. Broken PTP", "Update Phone Variable");
                                                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "3. Broken PTP", "Update SMS Variable");
                                                record = await AssignLeadOutcome(record, collection, smsRoutes, "4. Broken PTP", identity, Step, "3. Broken PTP");
                                            }
                                        }
                                        #endregion
                                    }
                                }
                                else
                                {
                                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (SCI == 0 || SCI == null)", $"{record.SCI.ToString()}", "false");
                                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "94. SCI’s", "Update Phone Variable");
                                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "99. No SMS", "Update SMS Variable");
                                    record = await AssignLeadOutcome(record, collection, smsRoutes, "94. SCI’s", identity, Step, "99. No SMS", 0, 0, "SCI");
                                }
                                //}
                                //else
                                //{
                                //    record = await AssignLeadOutcome(record, collection, smsRoutes, "89. No Arrears", identity, Step, "99. No SMS", 0, 0, "Payment Method = Debit_Order and P_Collection_G = 2 or P_Collection_G = 3");
                                //}
                            }
                            else
                            {
                                record = await AssignLeadOutcome(record, collection, smsRoutes, "89. No Arrears", identity, Step, "99. No SMS", 0, 0, "CD = 0 or CD = 1 and MOB >= 0");
                            }
                        }
                        else
                        {
                            record = await AssignLeadOutcome(record, collection, smsRoutes, "96. Finance Small Balance Queue", identity, Step, "99. No SMS", 0, 0, "CD = 0 or CD = 1 and MOB >= 0");
                        }
                    }
                    else
                    {
                        _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (string.IsNullOrEmpty(SAP_Block_Code))", $"{record.SAP_Block_Code.ToString()}", "false");
                        _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "98. Block Codes", "Update Phone Variable");
                        _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "99. No SMS", "Update SMS Variable");
                        record = await AssignLeadOutcome(record, collection, smsRoutes, "98. Block Codes", identity, Step, "99. No SMS", 0, 0, "Block Codes");
                    }
                }
                else
                {
                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "if (STATUS == 60)", $"{record.STATUS.ToString()}", "false");
                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "99. Non Collections Status", "Update Phone Variable");
                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome", "Outcome", "99. No SMS", "Update SMS Variable");
                    record = await AssignLeadOutcome(record, collection, smsRoutes, "99. Non Collections Status", identity, Step, "99. No SMS", 0, 0, "Non-Collections Status");
                }
                if (record.OutcomeTemporary == null)
                {
                    //
                }
                return record;
            }
            catch (Exception ex)
            {
                return record;
            }
        }
        #endregion

        #region [ Assign Lead Outcome ]
        public async Task<MasterDataModel> AssignLeadOutcome(MasterDataModel record, List<PhoneRoute> collection, List<SMSRoute> smsRoutes, string outcome, int identity, int Step, string SMS = "99. No SMS", int Frequency = 0, int Delay = 0, string inNoSMSReason = null)
        {
            try
            {
                record.Delay = Delay;
                record.ServiceIDOut = 0;
                record.NoSMSReason = inNoSMSReason;

                #region [ SMS and Email ID ]
                if (SMS != "99. No SMS")
                {
                    record.SMSFrequency = Frequency;
                    switch (SMS)
                    {
                        case "21. Disbursement":
                            _ = new LogTreeLead(identity, Step++, "IMPData", "Assignment", "Outcome", "SMSID = 2", "Update SMS Template");
                            _ = new LogTreeLead(identity, Step++, "IMPData", "Assignment", "Outcome", "EmailID = 1", "Update Email Template");
                            record.SMSID = 2;
                            record.EmailID = 1;
                            break;
                        case "22. Thank You":
                            _ = new LogTreeLead(identity, Step++, "IMPData", "Assignment", "Outcome", "SMSID = 3", "Update SMS Template");
                            record.SMSID = 3;
                            break;
                        case "21. Check Disbursement":
                            _ = new LogTreeLead(identity, Step++, "IMPData", "Assignment", "Outcome", "SMSID = -2", "Update SMS Template");
                            _ = new LogTreeLead(identity, Step++, "IMPData", "Assignment", "Outcome", "EmailID = -1", "Update Email Template");
                            record.SMSID = -2;
                            record.EmailID = -1;
                            break;
                        case "13.2 PTP Confirmation + Disbursement":
                            _ = new LogTreeLead(identity, Step++, "IMPData", "Assignment", "Outcome", "SMSID = 8", "Update SMS Template");
                            record.SMSID = 8;
                            break;
                        case "3. Broken PTP":
                            _ = new LogTreeLead(identity, Step++, "IMPData", "Assignment", "Outcome", "SMSID = 5", "Update SMS Template");
                            record.SMSID = 5;
                            break;
                        case "23. Partial PTP":
                            _ = new LogTreeLead(identity, Step++, "IMPData", "Assignment", "Outcome", "SMSID = 6", "Update SMS Template");
                            record.SMSID = 6;
                            break;
                        case "19.1 PTP Reminder":
                            _ = new LogTreeLead(identity, Step++, "IMPData", "Assignment", "Outcome", "SMSID = 7", "Update SMS Template");
                            record.SMSID = 7;
                            break;
                        case "19.2 Reminder + Disbursement":
                            _ = new LogTreeLead(identity, Step++, "IMPData", "Assignment", "Outcome", "SMSID = 7", "Update SMS Template");
                            record.SMSID = 7;
                            break;
                        case "19.3 Reminder SMS + Email":
                            _ = new LogTreeLead(identity, Step++, "IMPData", "Assignment", "Outcome", "SMSID = 7", "Update SMS Template");
                            record.SMSID = 7;
                            break;
                        case "13.1 PTP Confirmation":
                            _ = new LogTreeLead(identity, Step++, "IMPData", "Assignment", "Outcome", "SMSID = 8", "Update SMS Template");
                            record.SMSID = 8;
                            break;
                        case "4. Cash Reminder":
                            _ = new LogTreeLead(identity, Step++, "IMPData", "Assignment", "Outcome", "SMSID = 9", "Update SMS Template");
                            record.SMSID = 9;
                            break;
                        case "6. Arrears (3 Daily)":
                            _ = new LogTreeLead(identity, Step++, "IMPData", "Assignment", "Outcome", "SMSID = 9", "Update SMS Template");
                            record.SMSID = 30;
                            break;
                        case "5. Debit Order Reminder":
                            _ = new LogTreeLead(identity, Step++, "IMPData", "Assignment", "Outcome", "SMSID = 10", "Update SMS Template");
                            record.SMSID = 10;
                            break;
                        case "12. Arrears Reminder":
                            _ = new LogTreeLead(identity, Step++, "IMPData", "Assignment", "Outcome", "SMSID = 11", "Update SMS Template");
                            record.SMSID = 11;
                            break;
                        case "8.2 RD Failure":
                            _ = new LogTreeLead(identity, Step++, "IMPData", "Assignment", "Outcome", "SMSID = 12", "Update SMS Template");
                            record.SMSID = 12;
                            break;
                        case "10.2 Dispute Failure":
                            _ = new LogTreeLead(identity, Step++, "IMPData", "Assignment", "Outcome", "SMSID = 13", "Update SMS Template");
                            record.SMSID = 13;
                            break;
                        case "11.2 Admin Failure":
                            _ = new LogTreeLead(identity, Step++, "IMPData", "Assignment", "Outcome", "SMSID = 14", "Update SMS Template");
                            record.SMSID = 14;
                            break;
                        case "7. Tracking":
                            _ = new LogTreeLead(identity, Step++, "IMPData", "Assignment", "Outcome", "SMSID = 15", "Update SMS Template");
                            record.SMSID = 15;
                            break;
                        case "12.1 Daily Arrears":
                            _ = new LogTreeLead(identity, Step++, "IMPData", "Assignment", "Outcome", "SMSID = 16", "Update SMS Template");
                            record.SMSID = 16;
                            break;
                        case "12.2 Daily Arrears Low":
                            _ = new LogTreeLead(identity, Step++, "IMPData", "Assignment", "Outcome", "SMSID = 18", "Update SMS Template");
                            record.SMSID = 18;
                            break;
                        case "8.1 RD Failure Low":
                            _ = new LogTreeLead(identity, Step++, "IMPData", "Assignment", "Outcome", "SMSID = 19", "Update SMS Template");
                            record.SMSID = 19;
                            break;
                        case "10.1 Dispute Failure Low":
                            _ = new LogTreeLead(identity, Step++, "IMPData", "Assignment", "Outcome", "SMSID = 20", "Update SMS Template");
                            record.SMSID = 20;
                            break;
                        case "11.1 Admin Failure Low":
                            _ = new LogTreeLead(identity, Step++, "IMPData", "Assignment", "Outcome", "SMSID = 21", "Update SMS Template");
                            record.SMSID = 21;
                            break;
                        case "25. Welcome Pack":
                            _ = new LogTreeLead(identity, Step++, "IMPData", "Assignment", "Outcome", "SMSID = 22", "Update SMS Template");
                            record.SMSID = 22;
                            break;
                        case "23.1 Partial Instalment":
                            _ = new LogTreeLead(identity, Step++, "IMPData", "Assignment", "Outcome", "SMSID = 24", "Update SMS Template");
                            record.SMSID = 24;
                            break;
                    }
                }
                #endregion

                if (outcome.Contains("Pre-Legal FPD") || outcome.Contains("Pre-Legal SPD") || outcome.Contains("Pre-Legal High-Risk") || outcome.Contains("Trace"))
                {
                    record.ServiceIDOut = collection.Where(x => x.CDStatus == -99 && x.Description.ToLower() == outcome.ToLower()).Select(x => x.ServiceID).FirstOrDefault();
                    record.LoadIDOut = collection.Where(x => x.CDStatus == -99 && x.Description.ToLower() == outcome.ToLower()).Select(x => x.LoadID).FirstOrDefault();

                    record.CurrentOutcomeNonPresence = SMS;
                    record.OutcomeTemporary = outcome;
                    record.SendSMSEmail = SMS;

                    #region [ Switch Country ]
                    if (record.LoadIDOut != 0)
                    {
                        switch (record.Country)
                        {
                            case "Nam":
                                if (record.ServiceIDOut == 141 || record.ServiceIDOut == 142 || record.ServiceIDOut == 143 || record.ServiceIDOut == 144 || record.ServiceIDOut == 145 || record.ServiceIDOut == 162)
                                {
                                    if (record.LoadIDOut == 1 || record.LoadIDOut == 2)
                                    {
                                        record.ServiceIDOut = 154;
                                        record.LoadIDOut = 1;
                                    }
                                    else
                                    {
                                        record.ServiceIDOut = 151;
                                    }
                                }
                                else if (record.ServiceIDOut == 146)
                                {
                                    record.ServiceIDOut = 157;
                                }
                                else if (record.ServiceIDOut == 149)
                                {
                                    record.ServiceIDOut = 158;
                                }
                                break;
                            case "Swa":
                                if (record.ServiceIDOut == 141 || record.ServiceIDOut == 142 || record.ServiceIDOut == 143 || record.ServiceIDOut == 144 || record.ServiceIDOut == 145 || record.ServiceIDOut == 162)
                                {
                                    if (record.LoadIDOut == 1 || record.LoadIDOut == 2)
                                    {
                                        record.ServiceIDOut = 155;
                                        record.LoadIDOut = 1;
                                    }
                                    else
                                    {
                                        record.ServiceIDOut = 152;
                                    }
                                }
                                else if (record.ServiceIDOut == 146)
                                {
                                    record.ServiceIDOut = 157;
                                }
                                else if (record.ServiceIDOut == 149)
                                {
                                    record.ServiceIDOut = 158;
                                }
                                break;
                            case "Bot":
                                if (record.ServiceIDOut == 141 || record.ServiceIDOut == 142 || record.ServiceIDOut == 143 || record.ServiceIDOut == 144 || record.ServiceIDOut == 145 || record.ServiceIDOut == 162)
                                {
                                    if (record.LoadIDOut == 1 || record.LoadIDOut == 2)
                                    {
                                        record.ServiceIDOut = 156;
                                        record.LoadIDOut = 1;
                                    }
                                    else
                                    {
                                        record.ServiceIDOut = 153;
                                    }
                                }
                                else if (record.ServiceIDOut == 146)
                                {
                                    record.ServiceIDOut = 153;
                                }
                                else if (record.ServiceIDOut == 149)
                                {
                                    record.ServiceIDOut = 158;
                                }
                                break;
                            default:
                                //
                                break;
                        }
                    }
                    #endregion

                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome Phone", "Final Outcome", $"{record.OutcomeTemporary.ToString()}", "");
                    _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome SMS", "Final Outcome", $"{record.SendSMSEmail.ToString()}", "");

                    return record;
                }

                if (record.Current_CD == 0 || record.Current_CD == 1 || record.Current_CD == 2 || record.Current_CD == 3)
                {
                    string strOutcome = outcome;

                    var list = collection.Where(x => x.CDStatus == record.Current_CD).ToList();
                    var serviceid = list.Find(x => x.Description.Contains(outcome));

                    record.ServiceIDOut = collection.Where(x => x.CDStatus == record.Current_CD && x.Description.ToLower() == outcome.ToLower()).Select(x => x.ServiceID).FirstOrDefault();
                    record.LoadIDOut = collection.Where(x => x.CDStatus == record.Current_CD && x.Description.ToLower() == outcome.ToLower()).Select(x => x.LoadID).FirstOrDefault();
                }

                if (record.Current_CD >= 4)
                {
                    record.ServiceIDOut = collection.Where(x => x.CDStatus >= record.Current_CD && x.Description.ToLower() == outcome.ToLower()).Select(x => x.ServiceID).FirstOrDefault();
                    record.LoadIDOut = collection.Where(x => x.CDStatus >= record.Current_CD && x.Description.ToLower() == outcome.ToLower()).Select(x => x.LoadID).FirstOrDefault();
                }

                if (record.ServiceIDOut == 0)
                {
                    record.CurrentOutcomeNonPresence = outcome;
                }

                record.CurrentOutcomeNonPresence = SMS;
                record.OutcomeTemporary = outcome;
                record.SendSMSEmail = SMS;

                #region [ Switch Country ]
                if (record.LoadIDOut != 0)
                {
                    switch (record.Country)
                    {
                        case "Nam":
                            if (record.ServiceIDOut == 141 || record.ServiceIDOut == 142 || record.ServiceIDOut == 143 || record.ServiceIDOut == 144 || record.ServiceIDOut == 145 || record.ServiceIDOut == 162)
                            {
                                if (record.LoadIDOut == 1 || record.LoadIDOut == 2)
                                {
                                    record.ServiceIDOut = 154;
                                    record.LoadIDOut = 1;
                                }
                                else
                                {
                                    record.ServiceIDOut = 151;
                                }
                            }
                            else if (record.ServiceIDOut == 146)
                            {
                                record.ServiceIDOut = 157;
                            }
                            else if (record.ServiceIDOut == 149)
                            {
                                record.ServiceIDOut = 158;
                            }
                            break;
                        case "Swa":
                            if (record.ServiceIDOut == 141 || record.ServiceIDOut == 142 || record.ServiceIDOut == 143 || record.ServiceIDOut == 144 || record.ServiceIDOut == 145 || record.ServiceIDOut == 162)
                            {
                                if (record.LoadIDOut == 1 || record.LoadIDOut == 2)
                                {
                                    record.ServiceIDOut = 155;
                                    record.LoadIDOut = 1;
                                }
                                else
                                {
                                    record.ServiceIDOut = 152;
                                }
                            }
                            else if (record.ServiceIDOut == 146)
                            {
                                record.ServiceIDOut = 157;
                            }
                            else if (record.ServiceIDOut == 149)
                            {
                                record.ServiceIDOut = 158;
                            }
                            break;
                        case "Bot":
                            if (record.ServiceIDOut == 141 || record.ServiceIDOut == 142 || record.ServiceIDOut == 143 || record.ServiceIDOut == 144 || record.ServiceIDOut == 145 || record.ServiceIDOut == 162)
                            {
                                if (record.LoadIDOut == 1 || record.LoadIDOut == 2)
                                {
                                    record.ServiceIDOut = 156;
                                    record.LoadIDOut = 1;
                                }
                                else
                                {
                                    record.ServiceIDOut = 153;
                                }
                            }
                            else if (record.ServiceIDOut == 146)
                            {
                                record.ServiceIDOut = 157;
                            }
                            else if (record.ServiceIDOut == 149)
                            {
                                record.ServiceIDOut = 158;
                            }
                            break;
                        default:
                            //
                            break;
                    }
                }
                #endregion

                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome Phone", "Final Outcome", $"{record.OutcomeTemporary.ToString()}", "");
                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome SMS", "Final Outcome", $"{record.SendSMSEmail.ToString()}", "");

                if (record.MOB <= -1 && record.ServiceIDOut != 0)
                { 
                    switch(record.Country)
                    {
                        case "SA":
                            record.ServiceIDOut = 162;
                            record.LoadIDOut = 1;
                            break;
                        case "Nam":
                            record.ServiceIDOut = 154;
                            record.LoadIDOut = 1;
                            break;
                        case "Swa":
                            record.ServiceIDOut = 155;
                            record.LoadIDOut = 1;
                            break;
                    }
                }

                return record;
            }
            catch (Exception ex)
            {
                record.OutcomeTemporary = String.Empty;
                record.SendSMSEmail = String.Empty;

                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome Phone", "Final Outcome", $"{record.OutcomeTemporary.ToString()}", "");
                _ = new LogTreeLead(identity, Step++, "IMPData", "Outcome SMS", "Final Outcome", $"{record.SendSMSEmail.ToString()}", "");

                return record;
            }
        }
        #endregion

        #region [ Assign Legal Pre Legal ]
        public async Task<LegalPreLegal> AssignLegalPreLegal(MasterDataModel record, IDataService dataService)
        {
            if (record.APT_Account_Number == 18700771)
            {
                //
            }
            var model = new LegalPreLegal();

            model.Payment33Days = 0;
            model.LegalLetterSent = 1;
            model.SecondCycleAccount = 0;
            model.WarningSMSEmail = 0;
            model.SMSEmailRead = 0;
            model.PreviousDiallerContact = 0;
            model.Outsourced = 0;
            model.OutsourcingType = (model.Outsourced == 1 || model.SecondCycleAccount == 1) ? 1 : 0;
            model.PTPCaptured = (record.Description == "In Effect" || record.Description == "Pending") ? 1 : 0;
            model.PTPInEffect = (record.Description == "In Effect" || record.Description == "Pending") ? 1 : 0;
            model.DaysPTPDue = record.Days_To_PTP_Due ?? 0;
            model.SourceID = (int)record.APT_Account_Number;

            if (model.LegalLetterSent == 1)
            {
                if (record.OutcomeTemporary == "90. Pre-Legal FPD")
                {
                    if (record.Description == "Broken" || record.Description == "Short Paid" || record.Description == "Short Paid 3 Days After" || record.Description == "No PTP")
                    {
                        model.LegalPreLegalOutcome = "Dial for PTP and send outsource warning SMS and email";
                        model.SecondCycleAccount = 999;
                        model.OutsourcingType = 999;
                    }
                    else if (record.Description == "In Effect")
                    {
                        if (record.Days_To_PTP_Due >= 0 && record.Days_To_PTP_Due <= 2)
                        {
                            model.LegalPreLegalOutcome = "Daily PTP reminder";
                            model.OutsourcingType = 998;
                        }
                        else
                        {
                            model.LegalPreLegalOutcome = "Park";
                            model.SecondCycleAccount = 0;
                        }
                    }
                }
                else if (record.OutcomeTemporary == "91. Pre-Legal SPD")
                {
                    if (record.Description == "Broken" || record.Description == "Short Paid" || record.Description == "Short Paid 3 Days After" || record.Description == "No PTP")
                    {
                        model.LegalPreLegalOutcome = "Dial for PTP and send outsource warning SMS and email";
                        model.SecondCycleAccount = 999;
                        model.OutsourcingType = 997;
                    }
                    else if (record.Description == "In Effect")
                    {
                        if (record.Days_To_PTP_Due >= 0 && record.Days_To_PTP_Due <= 2)
                        {
                            model.LegalPreLegalOutcome = "Daily PTP reminder";
                            model.OutsourcingType = 998;
                        }
                        else
                        {
                            model.LegalPreLegalOutcome = "Park";
                            model.SecondCycleAccount = 0;
                        }
                    }
                }
                else if (record.OutcomeTemporary == "92. Pre-Legal High-Risk")
                {
                    if (record.Description == "No PTP")
                    {
                        model.LegalPreLegalOutcome = "Dial for PTP and send outsource warning SMS and email";
                        model.SecondCycleAccount = 999;
                        model.OutsourcingType = 997;
                    }
                    else if (record.Description == "Broken" || record.Description == "Short Paid" || record.Description == "Short Paid 3 Days After")
                    {
                        try
                        {
                            IntClass intClass = new IntClass();
                            intClass = await dataService.SelectSingle<IntClass, dynamic>("WITH CTE AS(SELECT DATEDIFF(DAY, Received, GETDATE()) AS DAYSSINCE, ROW_NUMBER() OVER (PARTITION BY ExternalID ORDER BY Received DESC) AS RN FROM dbo.SMSSent WHERE ExternalID = @APT_ACC_NO AND SMSTemplateID IN (27)) SELECT DAYSSINCE FROM CTE WHERE RN = 1", new { APT_ACC_NO = record.APT_Account_Number });
                            if (intClass != null)
                            {
                                model.FinalWarningSMSEmail = 1;
                                if (intClass.DAYSSINCE <= 1)
                                {
                                    model.LegalPreLegalOutcome = "Park";
                                    model.SecondCycleAccount = 0;
                                }
                                else
                                {
                                    model.LegalPreLegalOutcome = "Outsource";
                                    model.SecondCycleAccount = 0;
                                    model.Outsourced = 999;
                                }
                            }
                            else
                            {
                                model.LegalPreLegalOutcome = "Send final outsource warning SMS and email";
                                model.SecondCycleAccount = 0;
                                model.OutsourcingType = 996;
                            }
                        }
                        catch (Exception ex)
                        {
                            // Do Nothing
                        }
                    }
                    else if (record.Description == "In Effect" || record.Description == "Pending")
                    {
                        if (record.Days_To_PTP_Due <= 2)
                        {
                            model.LegalPreLegalOutcome = "Daily PTP reminder";
                            model.OutsourcingType = 998;
                        }
                        else
                        {
                            model.LegalPreLegalOutcome = "Park";
                            model.SecondCycleAccount = 0;
                        }
                    }
                }
                else
                {
                    model.LegalPreLegalOutcome = "Not Pre-Legal";
                    model.SecondCycleAccount = 0;
                }
            }

            return model;
        }
        #endregion

        private class IntClass
        {
            public int DAYSSINCE { get; set; }
        }

        #region [ Assign Trace Per Trace ]
        public async Task<TracePreTrace> AssignTracePreTrace(MasterDataModel record)
        {
            var model = new TracePreTrace();

            model.IsPreTrace = 1;
            model.ValidContactExists = (record.Phone1 != null) ? 1 : 0;
            model.ValidEmailExists = (record.Email != String.Empty && record.Email != null) ? 1 : 0;
            model.NumberSoftResponsesSMS = 0;
            model.HardResponsesSMS = 0;
            model.NumberSoftResponsesEmail = 0;
            model.HardResponsesEmail = 0;
            model.SMSSent = 0;
            model.EmailSent = 0;
            model.NextPriorityEmailExists = 0;
            model.NextPriorityNumberExists = (record.Phone2 != null) ? 1 : 0;
            model.IsEmailTrace = (model.ValidEmailExists == 0) ? 1 : 0;
            model.IsDiallerTrace = (model.ValidContactExists == 0) ? 1 : 0;
            model.PositiveResponse2DaysEmail = 0;
            model.EmailResponseDays = 0;
            model.QueuedToEmailService = 0;

            if (model.IsDiallerTrace == 1)
            {
                model.OutcomeTracePreTrace = "Marked for Dialler Trace";
            }
            if (model.ValidContactExists == 1)
            {
                if (model.SMSSent == 0)
                {
                    model.OutcomeTracePreTrace += "Send No Contact SMS";
                }
                else
                {
                    if (model.NumberSoftResponsesSMS >= 0 && model.NumberSoftResponsesSMS <= 3)
                    {
                        if (model.HardResponsesSMS == 1)
                        {
                            if (record.Phone2 != null)
                            {
                                model.newPriorityPhone = record.Phone2;
                                record.Phone1 = record.Phone2;
                                model.OutcomeTracePreTrace = "Next Priority Cell set as Priority Email";
                            }
                            else
                            {
                                model.IsDiallerTrace = 1;
                                model.OutcomeTracePreTrace = "Marked for Dialler Trace";
                            }
                        }
                        else
                        {
                            if (model.PositiveResponse2DaysSMS == 1)
                            {
                                model.OutcomeTracePreTrace = "Queue to Dialler Agent as Priority";
                                if (model.PTPCaptured == 1)
                                {
                                    model.newPriorityPhone = record.Phone1;
                                    model.OutcomeTracePreTrace = "Set as Priority Cell";
                                }
                                else
                                {
                                    if (model.NextPriorityNumberExists == 1)
                                    {
                                        model.newPriorityPhone = record.Phone2;
                                        model.OutcomeTracePreTrace = "Next Priority Cell set as Priority";
                                    }
                                    else
                                    {
                                        model.IsDiallerTrace = 1;
                                        model.OutcomeTracePreTrace = "Marked for Dialler Trace";
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        model.OutcomeTracePreTrace = "Marked for Dialler Trace";
                        model.IsDiallerTrace = 1;
                    }
                }
            }
            if (model.ValidEmailExists == 1)
            {
                model.OutcomeTracePreTraceEmail += "Send No Contact Email";
                if (model.NumberSoftResponsesEmail > 3)
                {
                    model.IsEmailTrace = 1;
                }
                else if (model.NumberSoftResponsesEmail <= 3 && model.NumberSoftResponsesEmail >= 0)
                {
                    if (model.HardResponsesEmail == 1)
                    {
                        if (model.NextPriorityEmailExists == 1)
                        {
                            model.OutcomeTracePreTraceEmail = "Next Priority Email set as Priority Email";
                        }
                        else
                        {
                            //
                        }
                    }
                    else
                    {
                        record.EmailID = 3;
                        model.OutcomeTracePreTraceEmail = "Send No Contact Email";
                        if (record.Description == "In Effect" || record.Description == "Pending")
                        {
                            model.OutcomeTracePreTraceEmail = "Set as Priority Email";
                        }
                        else
                        {
                            model.OutcomeTracePreTraceEmail = "Marked for Email Trace";
                        }
                    }
                }
            }
            if (model.OutcomeTracePreTrace == String.Empty)
            {
                model.OutcomeTracePreTrace = "Is Pre-Trace";
            }
            if (model.IsEmailTrace == 1)
            {
                if (model.OutcomeTracePreTrace == String.Empty)
                {
                    model.OutcomeTracePreTraceEmail = "Marked for Email Trace";
                }
            }

            model.SourceID = record.APT_Account_Number;
            model.DaysITC = model.DaysITC;
            model.IsPreTrace = model.IsPreTrace;
            model.IsDiallerTrace = model.IsDiallerTrace;
            model.IsEmailTrace = model.IsEmailTrace;
            model.EmailResponseDays = model.EmailResponseDays;
            model.ValidContactExists = model.ValidContactExists;
            model.ValidEmailExists = model.ValidEmailExists;
            model.NumberSoftResponsesSMS = model.NumberSoftResponsesSMS;
            model.HardResponsesSMS = model.HardResponsesSMS;
            model.NumberSoftResponsesEmail = model.NumberSoftResponsesEmail;
            model.EmailSent = model.EmailSent;
            model.SMSSent = model.SMSSent;
            model.NextPriorityNumberExists = model.NextPriorityNumberExists;
            model.NextPriorityEmailExists = model.NextPriorityEmailExists;
            model.HardResponsesEmail = model.HardResponsesEmail;
            model.PositiveResponse2DaysEmail = model.PositiveResponse2DaysEmail;
            model.PTPCaptured = model.PTPCaptured;
            model.LastQCode = model.LastQCode;
            model.QueuedToEmailService = model.QueuedToEmailService;
            model.PositiveResponse2DaysSMS = model.PositiveResponse2DaysSMS;
            model.newPriorityPhone = model.newPriorityPhone;
            model.newPriorityEmail = String.Empty;
            model.OutcomeTracePreTrace = model.OutcomeTracePreTrace;

            return model;
        }
        #endregion
    }
}