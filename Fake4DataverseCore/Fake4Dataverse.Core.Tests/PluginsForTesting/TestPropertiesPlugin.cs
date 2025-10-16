using Microsoft.Xrm.Sdk;
using System;

namespace Fake4Dataverse.Tests.PluginsForTesting
{
    public class TestPropertiesPlugin : IPlugin
    {
        public string Property { get; set; }

        public void Execute(IServiceProvider serviceProvider)
        {
            var context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            Property = "Property Updated";
            
            // Add depth to the target entity for testing
            if (context.InputParameters.Contains("Target") && context.InputParameters["Target"] is Entity target)
            {
                target["depth"] = context.Depth;
            }
        }
    }
}