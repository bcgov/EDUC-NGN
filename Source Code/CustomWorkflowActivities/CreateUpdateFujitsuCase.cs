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
    public class CreateUpdateFujitsuCase : CodeActivity
    {
        [Input("Fujitsu Import")]
        [ReferenceTarget("edu_fujitsucaseimport")]
        public InArgument<EntityReference> Input_FujitsuImport { get; set; }

        [Input("Fujitsu Company")]
        [ReferenceTarget("account")]
        public InArgument<EntityReference> Input_FujitsuCompany { get; set; }

        protected override void Execute(CodeActivityContext executionContext)
        {
            ITracingService tracingService = executionContext.GetExtension<ITracingService>();
            IWorkflowContext context = executionContext.GetExtension<IWorkflowContext>();
            IOrganizationServiceFactory serviceFactory = executionContext.GetExtension<IOrganizationServiceFactory>();
            IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);

            //Get the headID passed to the CWA from the workflow engine
            EntityReference fujitsuImport = Input_FujitsuImport.Get<EntityReference>(executionContext);
            EntityReference fujitsuCompany = Input_FujitsuCompany.Get<EntityReference>(executionContext);

            if (fujitsuImport.LogicalName != edu_fujitsucaseimport.EntityLogicalName || fujitsuCompany.LogicalName != Account.EntityLogicalName)
            {
                throw new ArgumentException("The parameters are of the wrong type.  Fujitsu Import should be of type Fujitsu Case Import and Fujitsu Company should be of type Account.");
            }

            tracingService.Trace("{0}", "Getting Fujitsu Case Import Record");
            edu_fujitsucaseimport fujitsuRecord = service.Retrieve(edu_fujitsucaseimport.EntityLogicalName, fujitsuImport.Id, new ColumnSet(true)) as edu_fujitsucaseimport;

            tracingService.Trace("{0}", "Check for existing Case");
            var existingCase = GetExistingCase(service, fujitsuRecord.edu_Incident);

            if (existingCase == null)
            {
                tracingService.Trace("{0}", "Case not found");
                //Create new Case
                var recordToCreate = CreateRecordToUpsert(fujitsuRecord, fujitsuCompany);
                //This is only set on create
                recordToCreate.StatusCode = new OptionSetValue(1); // In Progress
                recordToCreate.OverriddenCreatedOn = fujitsuRecord.edu_CreatedOn;
                var newCase = service.Create(recordToCreate);
                tracingService.Trace("{0}", "New case record created.");

                //Update fujitsu case import record with case reference
                edu_fujitsucaseimport recordToUpdate = new edu_fujitsucaseimport();
                recordToUpdate.Id = fujitsuRecord.Id;
                recordToUpdate.edu_case = new EntityReference(Incident.EntityLogicalName, newCase);
                if (fujitsuRecord.edu_ClosedDate.HasValue) {
                    recordToUpdate.statuscode = new OptionSetValue(100000003);//Case Created Closed
                }
                else
                {
                    recordToUpdate.statuscode = new OptionSetValue(100000000);//Case Created Open
                }

                service.Update(recordToUpdate);
                tracingService.Trace("{0}", "Fujitsu Case Import Record updated with new state and link to case.");
            }
            else
            {
                tracingService.Trace("{0}", "Case record found.");
                //Update existing case
                //Get case
                var relatedCase = service.Retrieve(Incident.EntityLogicalName, existingCase.Id, new ColumnSet("statecode")) as Incident;

                if (relatedCase.StateCode == IncidentState.Active)
                {
                    tracingService.Trace("{0}", "Case is Active");
                    var caseRecordToUpdate = CreateRecordToUpsert(fujitsuRecord, fujitsuCompany);
                    caseRecordToUpdate.Id = relatedCase.Id;

                    service.Update(caseRecordToUpdate);

                    //Update fujitsu case import record with case reference
                    edu_fujitsucaseimport recordToUpdate = new edu_fujitsucaseimport();
                    recordToUpdate.Id = fujitsuRecord.Id;
                    recordToUpdate.edu_case = new EntityReference(Incident.EntityLogicalName, relatedCase.Id);
                    if (fujitsuRecord.edu_ClosedDate.HasValue)
                    {
                        recordToUpdate.statuscode = new OptionSetValue(100000002);//Case Updated Closed
                    }
                    else
                    {
                        recordToUpdate.statuscode = new OptionSetValue(100000001);//Case Updated Open
                    }

                    service.Update(recordToUpdate);
                    tracingService.Trace("{0}", "Fujitsu Case Import Record updated with new state and link to case.");
                }
                else if (relatedCase.StateCode == IncidentState.Cancelled || relatedCase.StateCode == IncidentState.Resolved)
                {
                    tracingService.Trace("{0}", "Case is Cancelled or Resolved");
                    //Set reference to case on fujitsu case import record
                    //Set FJ status to No Action Required - Case Inactive
                    edu_fujitsucaseimport recordToUpdate = new edu_fujitsucaseimport();
                    recordToUpdate.Id = fujitsuRecord.Id;
                    recordToUpdate.edu_case = existingCase;
                    recordToUpdate.statuscode = new OptionSetValue(100000004);

                    service.Update(recordToUpdate);
                }
            }
        }

        internal Incident CreateRecordToUpsert(edu_fujitsucaseimport fujitsuRecord, EntityReference fujitsuCompany)
        {
            Incident recordToCreate = new Incident();
            recordToCreate.Title = "Fujitsu Incident - " + fujitsuRecord.edu_Incident;
            recordToCreate.edu_CaseType = new OptionSetValue(100000002); //Fujitsu
            recordToCreate.edu_Category = fujitsuRecord.edu_Category;
            recordToCreate.edu_Subcategory = fujitsuRecord.edu_Subcategory;
            recordToCreate.PriorityCode = new OptionSetValue(2); //Normal
            recordToCreate.CaseOriginCode = new OptionSetValue(100000001); //Fujitsu
            recordToCreate.edu_HEATID = fujitsuRecord.edu_Incident;
            recordToCreate.edu_Raisedby = new OptionSetValue(100000002); //School District
            recordToCreate.edu_SchoolDistrict = fujitsuRecord.edu_Location;
            recordToCreate.CustomerId = fujitsuCompany;
            recordToCreate.edu_CaseCompletedDate = fujitsuRecord.edu_ClosedDate;

            return recordToCreate;
        }

        internal EntityReference GetExistingCase(IOrganizationService service, string heatId)
        {
            //Create and Execute a query to find the incident
            QueryExpression caseQuery = new QueryExpression("incident");
            caseQuery.ColumnSet.AddColumns("edu_heatid");
            caseQuery.Criteria.AddCondition("edu_heatid", ConditionOperator.Equal, heatId);
            EntityCollection caseCollection = service.RetrieveMultiple(caseQuery);

            //Check to see that there are entities returned, if not, set the case found flag to false
            if (caseCollection.Entities.Count == 0)
            {
                return null;
            }

            //otherwise, there is an entityreference to return, and set the case found flag to true
            else
            {
                return caseCollection.Entities[0].ToEntityReference();
            }
        }
    }
}
