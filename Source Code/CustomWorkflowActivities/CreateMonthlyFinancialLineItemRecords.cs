using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Workflow;
using NGN.DataContext;
using System;
using System.Activities;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NGN
{
    public class CreateMonthlyFinancialLineItemRecords : CodeActivity
    {
        //Define the properties
        [Input("Monthly Summary")]
        [ReferenceTarget("edu_financialmonthlysummary")]
        public InArgument<EntityReference> monthlySummary { get; set; }

        protected override void Execute(CodeActivityContext executionContext)
        {
            ITracingService tracingService = executionContext.GetExtension<ITracingService>();

            IWorkflowContext context = executionContext.GetExtension<IWorkflowContext>();
            IOrganizationServiceFactory serviceFactory = executionContext.GetExtension<IOrganizationServiceFactory>();
            IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);

            tracingService.Trace("{0}{1}", "Start Custom Workflow Activity: CreateMonthlyFinancialLineItemRecords", DateTime.Now.ToLongTimeString());

            //Check to see if the EntityReference has been set
            EntityReference monthlySummaryReference = this.monthlySummary.Get(executionContext);
            if (monthlySummaryReference == null)
            {
                throw new InvalidOperationException("Financial Monthly Summary has not been specified", new ArgumentNullException("Monthly Summary"));
            }
            else if (monthlySummaryReference.LogicalName != NGN.DataContext.edu_financialmonthlysummary.EntityLogicalName)
            {
                throw new InvalidOperationException("Input must reference a financial monthly summary record",
                    new ArgumentException("Input must be of type edu_FinancialMonthlySummary", "Monthly Summary"));
            }

            //get referenced record
            Microsoft.Xrm.Sdk.Query.ColumnSet columns = new Microsoft.Xrm.Sdk.Query.ColumnSet("edu_vendor", "edu_financialcategory", "edu_periodstartdate", "edu_periodenddate");
            var financialSummaryRecord = service.Retrieve(monthlySummaryReference.LogicalName, monthlySummaryReference.Id, columns) as edu_financialmonthlysummary;

            var financialCategory = financialSummaryRecord.edu_FinancialCategory;
            var vendor = financialSummaryRecord.edu_Vendor;
            var startDate = financialSummaryRecord.edu_PeriodStartDate.Value;
            var endDate = financialSummaryRecord.edu_PeriodEndDate.Value;
            var financialSummary = new EntityReference(financialSummaryRecord.LogicalName, financialSummaryRecord.Id);

            tracingService.Trace("{0}", "Line 52");
            //Get all Site Services for that vendor and category
            GetOngoingTransactions(tracingService, service, financialSummary, financialCategory, vendor, startDate, endDate);

            //Get all prorated and OTC financial line items without parents
            using (var crmContext = new CRMContext(service))
            {
                var records = crmContext.edu_financiallineitemSet.Where(r => r.edu_FinancialMonthlySummary == null 
                && r.CreatedOn >= startDate 
                && r.CreatedOn < endDate.AddDays(1)
                && r.edu_Vendor.Id == vendor.Id
                && r.statecode == edu_financiallineitemState.Active);
                
                foreach (var record in records)
                {
                    //add financial monthly summary
                    edu_financiallineitem recordToUpdate = new edu_financiallineitem();
                    recordToUpdate.Id = record.Id;
                    recordToUpdate.edu_FinancialMonthlySummary = financialSummary;

                    service.Update(recordToUpdate);

                }
            }

            tracingService.Trace("{0}{1}", "Custom Workflow Activity: End CreateMonthlyFinancialLineItemRecords", DateTime.Now.ToLongTimeString());
        }

        private void GetOngoingTransactions(ITracingService tracingService, IOrganizationService service, EntityReference financialSummary, EntityReference financialCategory, EntityReference vendor, DateTime startDate, DateTime endDate)
        {
            QueryExpression query = new QueryExpression(edu_siteservice.EntityLogicalName);
            query.ColumnSet = new ColumnSet(true);
            LinkEntity linkEntityService = new LinkEntity(edu_siteservice.EntityLogicalName, edu_service.EntityLogicalName, "edu_service", "edu_serviceid", JoinOperator.Inner);
            ConditionExpression serviceCategoryCondition = new ConditionExpression("edu_financialcategory", ConditionOperator.Equal, financialCategory.Id);
            linkEntityService.LinkCriteria.AddCondition(serviceCategoryCondition);

            ConditionExpression vendorCondition = new ConditionExpression("edu_vendor", ConditionOperator.Equal, vendor.Id);
            ConditionExpression stateCondition = new ConditionExpression("statecode", ConditionOperator.Equal, (int)edu_siteserviceState.Active);
            query.Criteria.AddCondition(vendorCondition);
            query.Criteria.AddCondition(stateCondition);

            query.LinkEntities.Add(linkEntityService);
            var records = service.RetrieveMultiple(query);

            //foreach create financial line item
            foreach (var record in records.Entities)
            {
                tracingService.Trace("{0}", "Line 70");

                edu_siteservice siteServiceRecord = record.ToEntity<edu_siteservice>();
                tracingService.Trace("Name: {0}", siteServiceRecord.edu_name);

                if (!siteServiceRecord.edu_CurrentPrice.HasValue) continue;

                tracingService.Trace("{0}", "Line 77");

                //get effective date
                if (siteServiceRecord.edu_StartDate == null || siteServiceRecord.edu_StartDate <= startDate)
                {
                    var charge = siteServiceRecord.edu_CurrentPrice.Value;
                    var recoverableCharge = siteServiceRecord.edu_RecoverableMonthly;

                    tracingService.Trace("{0}", "Line 79");
                    //charge full month
                    CreateFinancialLineItem(service,
                        "Monthly Charge for Service",
                        new OptionSetValue(100000001),
                        financialCategory,
                        financialSummary,
                        new EntityReference(siteServiceRecord.LogicalName, siteServiceRecord.Id),
                        vendor,
                        charge,
                        recoverableCharge);
                }
                else if(siteServiceRecord.edu_StartDate != null 
                    &&siteServiceRecord.edu_StartDate > startDate 
                    && siteServiceRecord.edu_StartDate <= endDate)
                {
                    //prorate
                    int effectivateDate = siteServiceRecord.edu_StartDate.Value.Day; //16
                    tracingService.Trace("effectivedate {0}", effectivateDate);
                    int daysInMonth = endDate.Day - startDate.Day + 1; //31
                    tracingService.Trace("Days in Month {0}", daysInMonth);
                    decimal adjustedCostPercentage = (((decimal)daysInMonth - effectivateDate + 1) / daysInMonth);
                    tracingService.Trace("Adjusted Percentage {0}", adjustedCostPercentage);

                    var charge = siteServiceRecord.edu_CurrentPrice.Value * adjustedCostPercentage;
                    var recoverableCharge = (siteServiceRecord.edu_RecoverableMonthly.HasValue) ? siteServiceRecord.edu_RecoverableMonthly.Value * adjustedCostPercentage : 0;

                    CreateFinancialLineItem(service,
                        "Prorated Monthly Charge for Service",
                        new OptionSetValue(100000001),
                        financialCategory,
                        financialSummary,
                        new EntityReference(siteServiceRecord.LogicalName, siteServiceRecord.Id),
                        vendor,
                        charge,
                        recoverableCharge);
                }
            }
        }

        internal void CreateFinancialLineItem(IOrganizationService service, string description, OptionSetValue type, EntityReference financialCategory, EntityReference financialSummary, EntityReference siteService, EntityReference vendor, decimal cost, decimal? recoverableCost)
        {

            edu_financiallineitem recordToCreate = new edu_financiallineitem();
            recordToCreate.edu_Cost = cost;
            recordToCreate.edu_Description = description;
            recordToCreate.edu_FinancialCategory = financialCategory;
            recordToCreate.edu_SiteService = siteService;
            recordToCreate.edu_Vendor = vendor;
            recordToCreate.edu_RecoverableCost = recoverableCost;
            recordToCreate.edu_FinancialMonthlySummary = financialSummary;
            recordToCreate.edu_Type = type;

            service.Create(recordToCreate);
        }
    }
}
