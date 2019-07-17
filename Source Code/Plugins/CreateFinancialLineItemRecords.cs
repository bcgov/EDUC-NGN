using Microsoft.Xrm.Sdk;
using NGN.DataContext;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace Plugins
{
    /// <summary>
    /// Call on:
    ///     Create of Site Service
    ///     Update of Service on Site Service
    ///     Update of Status on Site Service
    ///     
    ///     PreImage: edu_service, edu_startdate, edu_currentprice, edu_recoverablemonthly, edu_vendor
    ///     PostImage: edu_OneTimeChargeTotal, edu_OneTimeChargeRecoverable, edu_service, edu_vendor, edu_currentprice, edu_startdate, statecode
    /// </summary>
    public class CreateFinancialLineItemRecords : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            IPluginExecutionContext context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            IOrganizationServiceFactory serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);
            ITracingService tracingService = (ITracingService)serviceProvider.GetService(typeof(ITracingService));
            tracingService.Trace("{0}", "CreateFinancialLineItemRecords Plug-in");

            

            // The InputParameters collection contains all the data passed in the message request.
            if (context.InputParameters.Contains("Target") && context.InputParameters["Target"] is Entity)
            {
                Entity entity = (Entity)context.InputParameters["Target"];

                if (entity.LogicalName != "edu_siteservice")
                    return;

                edu_siteservice preImage = new edu_siteservice();
                if (context.PreEntityImages.Contains("preImage"))
                {
                    preImage = (context.PreEntityImages["preImage"]).ToEntity<edu_siteservice>();
                }

                edu_siteservice postImage = (context.PostEntityImages["postImage"]).ToEntity<edu_siteservice>();

                var serviceReference = postImage.edu_Service;

                try
                {
                    var eventName = context.MessageName;
                    tracingService.Trace("Event Name: {0}", eventName);

                    //check if installation date is in current month, if it's not exit

                    //if new or updated service
                    if (eventName == "Create" || preImage.edu_Service != postImage.edu_Service)
                    {
                        tracingService.Trace("{0}", "Line 51");
                        //get OTC, add to FLI
                        //need to check these fields exist (maybe get post image?)
                        var OTC = postImage.edu_OneTimeChargeTotal;

                        if (OTC.HasValue)
                        {
                            tracingService.Trace("{0}", "Line 58");
                            var recoverableOTC = (postImage.edu_OneTimeChargeRecoverable.HasValue) ? postImage.edu_OneTimeChargeRecoverable.Value : 0;

                            CreateFinancialLineItem(service,
                                                    "One Time Charge",
                                                    new OptionSetValue(100000000),
                                                    GetFinancialCategoryFromService(service, serviceReference),
                                                    new EntityReference(entity.LogicalName, entity.Id),
                                                    postImage.edu_Vendor,
                                                    OTC.Value,
                                                    recoverableOTC);
                        }
                    }


                    //if updated or inactive service
                    if (eventName == "Update" && postImage.edu_CurrentPrice.HasValue)
                    {
                        tracingService.Trace("{0}", "Line 76");
                        int effectiveDay;
                        
                        if (preImage.edu_Service != preImage.edu_Service && preImage.edu_StartDate.HasValue)
                        {
                            effectiveDay = postImage.edu_StartDate.Value.Day - 1;
                        }
                        else if (postImage.statecode == edu_siteserviceState.Inactive)
                        {
                            effectiveDay = DateTime.Today.Day - 1;
                        }
                        else { return; }

                        tracingService.Trace("Effective Day: {0}", effectiveDay);
                        //it's the first of the month, so will be billed for full month as part of on-going billing
                        if (effectiveDay == 0) { return; }

                        //calculate % for first part of month (effective date/#of days in month)
                        int daysInMonth = DateTime.DaysInMonth(DateTime.Today.Year, DateTime.Today.Month);
                        decimal adjustedCostPercentage = ((decimal)effectiveDay / daysInMonth) * 100;

                        tracingService.Trace("Effective Day: {0}", effectiveDay);

                        tracingService.Trace("{0}", "Line 97");
                        //get ongoing charge, multiply by %, add to FLI
                        var charge = preImage.edu_CurrentPrice.Value * adjustedCostPercentage;
                        var recoverableCharge = (preImage.edu_RecoverableMonthly.HasValue) ? preImage.edu_RecoverableMonthly.Value * adjustedCostPercentage : 0;

                        CreateFinancialLineItem(service,
                                                "Adjusted Charges for Previous or Removed Service",
                                                new OptionSetValue(100000001),
                                                GetFinancialCategoryFromService(service, serviceReference),
                                                new EntityReference(entity.LogicalName, entity.Id),
                                                preImage.edu_Vendor,
                                                charge,
                                                recoverableCharge);

                    }



                }
                catch (FaultException<OrganizationServiceFault> ex)
                {
                    throw new InvalidPluginExecutionException("An error occurred in the FollowupPlugin plug-in.", ex);
                }
                catch (Exception ex)
                {
                    tracingService.Trace("FollowupPlugin: {0}", ex.ToString());
                    throw;
                }
            }
        }

        internal void CreateFinancialLineItem(IOrganizationService service, string description, OptionSetValue type, EntityReference financialCategory, EntityReference siteService, EntityReference vendor, decimal cost, decimal? recoverableCost)
        {

            edu_financiallineitem recordToCreate = new edu_financiallineitem();
            recordToCreate.edu_Cost = cost;
            recordToCreate.edu_Description = description;
            recordToCreate.edu_FinancialCategory = financialCategory;
            recordToCreate.edu_SiteService = siteService;
            recordToCreate.edu_Vendor = vendor;
            recordToCreate.edu_RecoverableCost = recoverableCost;
            recordToCreate.edu_Type = type;

            service.Create(recordToCreate);
        }

        internal EntityReference GetFinancialCategoryFromService(IOrganizationService service, EntityReference serviceReference)
        {
            //get referenced record
            Microsoft.Xrm.Sdk.Query.ColumnSet columns = new Microsoft.Xrm.Sdk.Query.ColumnSet("edu_financialcategory");
            var serviceRecord = service.Retrieve(serviceReference.LogicalName, serviceReference.Id, columns) as edu_service;

            return serviceRecord.edu_FinancialCategory;
        }
    }
}
