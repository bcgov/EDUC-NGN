using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Workflow;
using System.Activities;

// Author: LT
// Date: 2019-04-13
// Version: 0.1

// Details:
// Simple workflow to run on the 'Fujitsu Import' Entity
// It looks for a case with a 'ngn_heatid' which matches the import
// If it finds a case, it outputs that case's entity reference
// This is then used to populate a lookup on the import entity

namespace NGN
{
    public class FindAndReturnFujitsuCase : CodeActivity
    {
        //Define the properties
        [Input("Fujitsu HeatID")]
        public InArgument<string> Input_HeatId { get; set; }

        [Output("Case Found?")]
        public OutArgument<bool> Output_CaseFoundBool { get; set; }

        [Output("Fujitsu Case Ref")]
        [ReferenceTarget("incident")]
        public OutArgument<EntityReference> Output_CaseRef { get; set; }

        protected override void Execute(CodeActivityContext executionContext)
        {
            ITracingService tracingService = executionContext.GetExtension<ITracingService>();
            IWorkflowContext context = executionContext.GetExtension<IWorkflowContext>();
            IOrganizationServiceFactory serviceFactory = executionContext.GetExtension<IOrganizationServiceFactory>();
            IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);

            //Get the headID passed to the CWA from the workflow engine
            string heatId = Input_HeatId.Get<string>(executionContext);

            //Check to ensure that there is a value entered
            if (heatId == null) return;

            //Create and Execute a query to find the incident
            QueryExpression caseQuery = new QueryExpression("incident");
            caseQuery.ColumnSet.AddColumns("ngn_heatid");
            caseQuery.Criteria.AddCondition("ngn_heatid", ConditionOperator.Equal, heatId);
            EntityCollection caseCollection = service.RetrieveMultiple(caseQuery);

            //Check to see that there are entities returned, if not, set the case found flag to false
            if (caseCollection.Entities.Count == 0)
            {
                Output_CaseFoundBool.Set(executionContext, false);
            }

            //otherwise, there is an entityreference to return, and set the case found flag to true
            else
            {
                Output_CaseRef.Set(executionContext, caseCollection.Entities[0].ToEntityReference());
                Output_CaseFoundBool.Set(executionContext, true);
            }
        }
    }
}