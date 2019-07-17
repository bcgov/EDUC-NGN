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
    public class GetRecommendedServiceFromModel : CodeActivity
    {
        //Define the properties
        [Input("Site")]
        [ReferenceTarget("edu_site")]
        public InArgument<EntityReference> site { get; set; }

        
        [Output("Recommended Service")]
        [ReferenceTarget("edu_service")]
        public OutArgument<EntityReference> recommendedService { get; set; }

        protected override void Execute(CodeActivityContext executionContext)
        {
            ITracingService tracingService = executionContext.GetExtension<ITracingService>();

            IWorkflowContext context = executionContext.GetExtension<IWorkflowContext>();
            IOrganizationServiceFactory serviceFactory = executionContext.GetExtension<IOrganizationServiceFactory>();
            IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);

            tracingService.Trace("{0}", "Custom Workflow Activity: GetRecommendedServiceFromModel");

            //Check to see if the EntityReference has been set
            EntityReference siteReference = this.site.Get(executionContext);
            if (siteReference == null)
            {
                throw new InvalidOperationException("Site has not been specified", new ArgumentNullException("Site"));
            }
            else if (siteReference.LogicalName != edu_site.EntityLogicalName)
            {
                throw new InvalidOperationException("Input must reference an service entity record",
                    new ArgumentException("Input must be of type service", "Service"));

            }

            //get referenced record
            Microsoft.Xrm.Sdk.Query.ColumnSet columns = new Microsoft.Xrm.Sdk.Query.ColumnSet("edu_sitetype", "edu_adjustedenrolment", "edu_schoolprofileenrolment");
            var siteRecord = service.Retrieve(siteReference.LogicalName, siteReference.Id, columns) as edu_site;

            //check site type and enrolment are set, otherwise exit
            if (siteRecord.edu_SiteType == null 
                || (siteRecord.edu_AdjustedEnrolment == null && siteRecord.edu_SchoolProfileEnrolment == null)) return;

            //get enrolment number
            var enrolmentCount = (siteRecord.edu_AdjustedEnrolment != null) ? siteRecord.edu_AdjustedEnrolment : siteRecord.edu_SchoolProfileEnrolment;

            //find all active model records for the site type
            using (var crmContext = new CRMContext(service))
            {
                var modelRecords = crmContext.edu_servicemodelSet.Where(r => r.edu_SiteType == siteRecord.edu_SiteType
                                                                            && r.statuscode.Value == 1
                                                                            && r.edu_MaximumEnrolment >= enrolmentCount).AsEnumerable();

                if (modelRecords.Count() <= 0) return;

                //Get model record
                var matchingModel = modelRecords.OrderBy(r => r.edu_MaximumEnrolment).FirstOrDefault();

                if (matchingModel != null)
                {
                    recommendedService.Set(executionContext, matchingModel.edu_Service);
                }

            }
        }
    }
}
