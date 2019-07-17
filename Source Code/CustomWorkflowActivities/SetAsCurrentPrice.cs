using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using NGN.DataContext;
using System;
using System.Activities;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NGN.CustomWorkflowActivities
{
    public class SetAsCurrentPrice : CodeActivity
    {
        //Define the properties
        [Input("ServicePrice")]
        [ReferenceTarget("edu_serviceprice")]
        public InArgument<EntityReference> servicePrice { get; set; }

        protected override void Execute(CodeActivityContext executionContext)
        {
            ITracingService tracingService = executionContext.GetExtension<ITracingService>();

            IWorkflowContext context = executionContext.GetExtension<IWorkflowContext>();
            IOrganizationServiceFactory serviceFactory = executionContext.GetExtension<IOrganizationServiceFactory>();
            IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);

            tracingService.Trace("{0}", "Custom Workflow Activity: SetAsCurrentPrice");

            //Check to see if the EntityReference has been set
            EntityReference servicePriceReference = this.servicePrice.Get(executionContext);
            if (servicePriceReference == null)
            {
                throw new InvalidOperationException("Service Price has not been specified", new ArgumentNullException("Service Price"));
            }
            else if (servicePriceReference.LogicalName !=  edu_serviceprice.EntityLogicalName)
            {
                throw new InvalidOperationException("Input must reference an service price entity record",
                    new ArgumentException("Input must be of type service price", "Service Price"));

            }

            //get referenced record
            Microsoft.Xrm.Sdk.Query.ColumnSet columns = new Microsoft.Xrm.Sdk.Query.ColumnSet("edu_service", "edu_vendor");
            var servicePriceRecord = service.Retrieve(servicePriceReference.LogicalName, servicePriceReference.Id, columns) as edu_serviceprice;

            //find any records currently active (statuscode = 1) and deactivate them
            using (var crmContext = new CRMContext(service))
            {
                var activeRecords = crmContext.edu_servicepriceSet.Where(r => r.edu_Service.Id == servicePriceRecord.edu_Service.Id
                                                                            && r.edu_Vendor.Id == servicePriceRecord.edu_Vendor.Id
                                                                            && r.statuscode.Value == 1);
                foreach(var record in activeRecords)
                {
                    var activeRecordToUpdate = new edu_serviceprice();
                    activeRecordToUpdate.Id = record.Id;
                    activeRecordToUpdate.statecode = edu_servicepriceState.Inactive;
                    activeRecordToUpdate.statuscode = new OptionSetValue(2);

                    service.Update(activeRecordToUpdate);
                }
            }

            //Set new service Price Record to active
            var recordToUpdate = new edu_serviceprice();
            recordToUpdate.Id = servicePriceRecord.Id;
            recordToUpdate.statecode = edu_servicepriceState.Active;
            recordToUpdate.statuscode = new OptionSetValue(1);

            service.Update(recordToUpdate);
        }
    }
}
