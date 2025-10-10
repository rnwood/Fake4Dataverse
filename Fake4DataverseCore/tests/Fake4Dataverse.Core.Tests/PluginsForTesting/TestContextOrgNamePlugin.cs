using Microsoft.Xrm.Sdk;
using System;

namespace Fake4Dataverse.Tests.PluginsForTesting
{
    public class TestContextOrgNamePlugin : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            var context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));

            context.OutputParameters.Add("OrgName", context.OrganizationName);
        }
    }
}