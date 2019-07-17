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
    public class CreateFinancialMonthlySummariesForVendor : CodeActivity
    {
        //Define the properties
        [Input("Vendor")]
        [ReferenceTarget("account")]
        public InArgument<EntityReference> vendor { get; set; }

        protected override void Execute(CodeActivityContext executionContext)
        {
            ITracingService tracingService = executionContext.GetExtension<ITracingService>();

            IWorkflowContext context = executionContext.GetExtension<IWorkflowContext>();
            IOrganizationServiceFactory serviceFactory = executionContext.GetExtension<IOrganizationServiceFactory>();
            IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);

            tracingService.Trace("{0}{1}", "Start Custom Workflow Activity: CreateFinancialMonthlySummariesForVendor", DateTime.Now.ToLongTimeString());

            //Check to see if the EntityReference has been set
            EntityReference vendorReference = this.vendor.Get(executionContext);
            if (vendorReference == null)
            {
                throw new InvalidOperationException("Vendor has not been specified", new ArgumentNullException("Vendor"));
            }
            else if (vendorReference.LogicalName != NGN.DataContext.Account.EntityLogicalName)
            {
                throw new InvalidOperationException("Input must reference an account record",
                    new ArgumentException("Input must be of type account", "Vendor"));
            }

            tracingService.Trace("{0}", "Line 43");
            //get referenced record
            Microsoft.Xrm.Sdk.Query.ColumnSet columns = new Microsoft.Xrm.Sdk.Query.ColumnSet("edu_nextinvoicedate");
            var vendorRecord = service.Retrieve(vendorReference.LogicalName, vendorReference.Id, columns) as Account;

            if (!vendorRecord.edu_NextInvoiceDate.HasValue) return;
            var nextInoviceDate = vendorRecord.edu_NextInvoiceDate.Value;

            DateTime temp = new DateTime(nextInoviceDate.Year, nextInoviceDate.Month, 1);
            DateTime startPeriod = temp.AddMonths(-1);
            DateTime endPeriod = temp.AddDays(-1);

            //get Financial Categories for the Vendor
            QueryExpression query = new QueryExpression(edu_financialcategory.EntityLogicalName);
            //query.ColumnSet = new ColumnSet(true);
            LinkEntity linkEntity1 = new LinkEntity(edu_financialcategory.EntityLogicalName, "edu_account_edu_financialcategory", "edu_financialcategoryid", "edu_financialcategoryid", JoinOperator.Inner);
            ConditionExpression condition = new ConditionExpression("accountid", ConditionOperator.Equal, vendorReference.Id);
            linkEntity1.LinkCriteria = new FilterExpression(LogicalOperator.And);
            linkEntity1.LinkCriteria.AddCondition(condition);

            query.LinkEntities.Add(linkEntity1);
            var records = service.RetrieveMultiple(query);

            //For each category, create a Financial Monthly Summary record
            foreach(var record in records.Entities)
            {
                tracingService.Trace("{0}", "Line 62");
                edu_financialmonthlysummary recordToCreate = new edu_financialmonthlysummary();
                recordToCreate.edu_Vendor = vendorReference;
                recordToCreate.edu_FinancialCategory = new EntityReference(record.LogicalName, record.Id);
                //create in draft state
                recordToCreate.statuscode = new OptionSetValue(1);
                //set start and end dates
                recordToCreate.edu_PeriodStartDate = startPeriod;
                recordToCreate.edu_PeriodEndDate = endPeriod;
                service.Create(recordToCreate);
            }
            tracingService.Trace("{0}", "Line 69");
            //Set the Next Invoice Date to 1 month in the future
            //vendorRecord.edu_NextInvoiceDate = vendorRecord.edu_NextInvoiceDate.Value.AddMonths(1);
            //service.Update(vendorRecord);

            tracingService.Trace("{0}{1}", "Custom Workflow Activity: End CreateFinancialMonthlySummariesForVendor", DateTime.Now.ToLongTimeString());
        }
    }
}
