using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using System;
using System.Activities;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NGN.Helpers;
using Microsoft.Xrm.Sdk.Query;

namespace NGN
{
    public class CalculateNumberOfBusinessDays : CodeActivity
    {
        //Define the properties
        [Input("Start Date")]
        public InArgument<DateTime> input_startDate { get; set; }

        [Input("End Date")]
        public InArgument<DateTime> input_endDate { get; set; }

        [Input("Holiday/Closure Calendar")]
        [ReferenceTarget("calendar")]
        public InArgument<EntityReference> holidayClosureCalendar { get; set; }

        [Output("Number of Days")]
        public OutArgument<int> numberOfDays { get; set; }

        protected override void Execute(CodeActivityContext executionContext)
        {
            ITracingService tracingService = executionContext.GetExtension<ITracingService>();

            IWorkflowContext context = executionContext.GetExtension<IWorkflowContext>();
            IOrganizationServiceFactory serviceFactory = executionContext.GetExtension<IOrganizationServiceFactory>();
            IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);

            tracingService.Trace("{0}", "Custom Workflow Activity: CalculateNumberOfBusinessDays");

            //input arguments
            DateTime startDate = this.input_startDate.Get(executionContext);
            DateTime endDate = this.input_endDate.Get(executionContext);

            tracingService.Trace("End Date: {0}", endDate.ToShortDateString());

            if (startDate >= endDate)
            {
                throw new ArgumentException("Start Date must be before End Date");
            }

            //Retrive Calendar Rules Collection
            EntityReference holidaySchedule = holidayClosureCalendar.Get(executionContext);
            Entity calendar = service.Retrieve("calendar", holidaySchedule.Id, new ColumnSet(true));
            var calendarRules = calendar.GetAttributeValue<EntityCollection>("calendarrules");

            int businessDayCounter = 0;
            DateTime dateToReview = startDate;
            
            do
            {
                tracingService.Trace("Date To Check:{0}", dateToReview.ToShortDateString());
                //if this is a weekend, skip
                if (!dateToReview.IsWeekend())
                {
                    tracingService.Trace("{0}", "Not a weekend");
                    //otherwise, check if date in holiday calendar
                    if (!dateToReview.IsHoliday(calendarRules))
                    {
                        tracingService.Trace("{0}", "Not a holiday");
                        businessDayCounter++;
                    }
                }

                dateToReview = dateToReview.AddDays(1);
            }
            while (dateToReview < endDate);

            //remove the first day from the counter
            businessDayCounter--;

            tracingService.Trace("Business Day Count: {0}", businessDayCounter.ToString());
            //set output to dateToReview
            this.numberOfDays.Set(executionContext, businessDayCounter);
        }
    }
}
