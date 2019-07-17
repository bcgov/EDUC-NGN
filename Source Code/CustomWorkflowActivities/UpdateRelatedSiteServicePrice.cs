using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using System;
using System.Activities;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NGN.DataContext;

namespace NGN
{
    public class UpdateRelatedSiteServicePrice : CodeActivity
    {
        //Define the properties
        [Input("Service")]
        [ReferenceTarget("edu_service")]
        public InArgument<EntityReference> service{ get; set; }

        protected override void Execute(CodeActivityContext executionContext)
        {
            ITracingService tracingService = executionContext.GetExtension<ITracingService>();

            IWorkflowContext context = executionContext.GetExtension<IWorkflowContext>();
            IOrganizationServiceFactory serviceFactory = executionContext.GetExtension<IOrganizationServiceFactory>();
            IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);

            tracingService.Trace("{0}", "Custom Workflow Activity: UpdateRelatedSiteServicePrice");

            //Check to see if the EntityReference has been set
            EntityReference serviceReference = this.service.Get(executionContext);
            if (serviceReference == null)
            {
                throw new InvalidOperationException("Service has not been specified", new ArgumentNullException("Service"));
            }
            else if (serviceReference.LogicalName != edu_service.EntityLogicalName)
            {
                throw new InvalidOperationException("Input must reference an service entity record",
                    new ArgumentException("Input must be of type service", "Service"));

            }

            //get referenced record
            Microsoft.Xrm.Sdk.Query.ColumnSet columns = new Microsoft.Xrm.Sdk.Query.ColumnSet("edu_cost");
            var serviceRecord = service.Retrieve(serviceReference.LogicalName, serviceReference.Id, columns) as edu_service;

            //Get Telus as Vendor


            //find any site service records for the service and update their price
            using (var crmContext = new CRMContext(service))
            {
                var telus = crmContext.AccountSet.Where(r=>r.Name == "Telus" && r.StateCode == AccountState.Active).FirstOrDefault();

                //if can't find Telus, throw error
                if (telus == null)
                {
                    throw new Exception("Unable to identify Telus record.  Please make sure the company record is named Telus and is active.");
                }

                /*UPDATE SITE SERVICES*/
                var siteServiceRecords = crmContext.edu_siteserviceSet.Where(r => r.edu_Service.Id == serviceReference.Id
                                                                            && r.edu_Vendor == new EntityReference(Account.EntityLogicalName, telus.Id)
                                                                            && r.statuscode.Value == 1);
                foreach (var record in siteServiceRecords)
                {
                    var recordToUpdate = new edu_siteservice();
                    recordToUpdate.Id = record.Id;
                    recordToUpdate.edu_CurrentPrice = serviceRecord.edu_Cost;

                    service.Update(recordToUpdate);
                }

                /*UPDATE ORDERS*/
                var orderRecords = crmContext.edu_orderSet.Where(r => r.edu_RequestedService.Id == serviceReference.Id
                                                                            && r.edu_Vendor == new EntityReference(Account.EntityLogicalName, telus.Id)
                                                                            && r.statecode == edu_orderState.Active);

                foreach (var record in orderRecords)
                {
                    var recordToUpdate = new edu_order();
                    recordToUpdate.Id = record.Id;
                    recordToUpdate.edu_TotalMonthlyCost = serviceRecord.edu_Cost;

                    service.Update(recordToUpdate);
                }

                /*UPDATE CASES*/
                var caseRecords = crmContext.IncidentSet.Where(r => r.edu_Service.Id == serviceReference.Id
                                                                            && r.edu_Vendor == new EntityReference(Account.EntityLogicalName, telus.Id)
                                                                            && r.StateCode == IncidentState.Active);

                foreach (var record in caseRecords)
                {
                    var recordToUpdate = new Incident();
                    recordToUpdate.Id = record.Id;
                    recordToUpdate.edu_TotalMonthlyCost = serviceRecord.edu_Cost;

                    service.Update(recordToUpdate);
                }
            }
        }
    }
}
