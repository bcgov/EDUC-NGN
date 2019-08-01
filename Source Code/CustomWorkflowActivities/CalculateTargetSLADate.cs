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
using NGN.DataContext;

namespace NGN
{
    public class CalculateTargetSLADate : CodeActivity
    {
        //Define the properties
        [Input("Start Date")]
        public InArgument<DateTime> input_startDate { get; set; } 

        [Input("Number of Days")]
        public InArgument<int> input_numberOfDays { get; set; }

        [Input("Holiday/Closure Calendar")]
        [ReferenceTarget("calendar")]
        public InArgument<EntityReference> holidayClosureCalendar { get; set; }

        [Output("Target Date")]
        public OutArgument<DateTime> targetDate { get; set; }


        protected override void Execute(CodeActivityContext executionContext)
        {
            ITracingService tracingService = executionContext.GetExtension<ITracingService>();

            IWorkflowContext context = executionContext.GetExtension<IWorkflowContext>();
            IOrganizationServiceFactory serviceFactory = executionContext.GetExtension<IOrganizationServiceFactory>();
            IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);

            tracingService.Trace("{0}", "Custom Workflow Activity: CalculateTargetSLADate");

            //input arguments
            DateTime startDate = this.input_startDate.Get(executionContext);
            int numberOfDays = this.input_numberOfDays.Get(executionContext);

            //Retrive Calendar Rules Collection
            EntityReference holidaySchedule = holidayClosureCalendar.Get(executionContext);
            Entity calendar = service.Retrieve("calendar", holidaySchedule.Id, new ColumnSet(true));
            var calendarRules = calendar.GetAttributeValue<EntityCollection>("calendarrules");

            int businessDayCounter = 0;
            DateTime dateToReview = startDate.AddDays(-1);
            //this is to prevent infinite loops, it will look at the next 5+ years
            do
            {
                dateToReview = dateToReview.AddDays(1);

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
            }
            while (businessDayCounter <= numberOfDays);

            tracingService.Trace("TargetDate: {0}", dateToReview.ToShortDateString());
            //set output to dateToReview
            this.targetDate.Set(executionContext, dateToReview);
        }

    }
}
