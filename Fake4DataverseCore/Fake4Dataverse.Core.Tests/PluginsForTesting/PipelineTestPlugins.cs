using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;

namespace Fake4Dataverse.Tests.PluginsForTesting
{
    /// <summary>
    /// A simple plugin for testing multiple plugin registration and execution order.
    /// Records the execution in a static list to verify execution order.
    /// </summary>
    public class TestExecutionOrderPlugin : IPlugin
    {
        public static List<string> ExecutionLog { get; set; } = new List<string>();

        private readonly string _pluginName;
        private readonly int _executionOrder;

        public TestExecutionOrderPlugin() : this("DefaultPlugin", "0")
        {
        }

        public TestExecutionOrderPlugin(string unsecureConfig, string secureConfig)
        {
            // Parse configuration to get plugin name and execution order
            _pluginName = string.IsNullOrEmpty(unsecureConfig) ? "DefaultPlugin" : unsecureConfig;
            _executionOrder = 0;
            if (!string.IsNullOrEmpty(secureConfig) && int.TryParse(secureConfig, out int order))
            {
                _executionOrder = order;
            }
        }

        public void Execute(IServiceProvider serviceProvider)
        {
            var context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            
            ExecutionLog.Add($"{_pluginName}|{_executionOrder}|Stage{context.Stage}");

            // Add a marker to the target entity to show this plugin executed
            if (context.InputParameters.Contains("Target") && context.InputParameters["Target"] is Entity target)
            {
                target[$"executed_{_pluginName}"] = true;
                target[$"order_{_executionOrder}"] = ExecutionLog.Count;
            }
        }
    }

    /// <summary>
    /// Plugin that modifies an attribute value
    /// </summary>
    public class ModifyAttributePlugin : IPlugin
    {
        private readonly string _attributeName;
        private readonly string _newValue;

        public ModifyAttributePlugin() : this("name", "DefaultValue")
        {
        }

        public ModifyAttributePlugin(string unsecureConfig, string secureConfig)
        {
            _attributeName = string.IsNullOrEmpty(unsecureConfig) ? "name" : unsecureConfig;
            _newValue = string.IsNullOrEmpty(secureConfig) ? "DefaultValue" : secureConfig;
        }

        public void Execute(IServiceProvider serviceProvider)
        {
            var context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            
            if (context.InputParameters.Contains("Target") && context.InputParameters["Target"] is Entity target)
            {
                target[_attributeName] = _newValue;
            }
        }
    }

    /// <summary>
    /// Plugin that throws an exception to test error handling
    /// </summary>
    public class ThrowExceptionPlugin : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            throw new InvalidPluginExecutionException("Test exception from plugin");
        }
    }

    /// <summary>
    /// Plugin that creates another record to test depth tracking
    /// </summary>
    public class RecursivePlugin : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            var context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            var factory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            var service = factory.CreateOrganizationService(context.UserId);

            // Only recurse if depth is less than 3 to avoid infinite loops in tests
            if (context.Depth < 3)
            {
                var newEntity = new Entity("account")
                {
                    ["name"] = $"Recursive Account (Depth {context.Depth + 1})"
                };
                service.Create(newEntity);
            }
        }
    }

    /// <summary>
    /// Plugin that only executes when specific attributes are modified
    /// </summary>
    public class FilteredAttributePlugin : IPlugin
    {
        public static int ExecutionCount { get; set; } = 0;

        public void Execute(IServiceProvider serviceProvider)
        {
            ExecutionCount++;
            
            var context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            
            if (context.InputParameters.Contains("Target") && context.InputParameters["Target"] is Entity target)
            {
                target["filtered_executed"] = true;
            }
        }
    }
}
