using System;
using System.Collections.Generic;
using Fake4Dataverse.Pipeline;
using Microsoft.Xrm.Sdk;
using InvalidPluginExecutionException = Microsoft.Xrm.Sdk.InvalidPluginExecutionException;
using Xunit;

namespace Fake4Dataverse.Tests
{
    public class PipelineTests
    {
        [Fact]
        public void PreValidation_FiresBeforeCreate()
        {
            var service = new FakeOrganizationService();
            var firedStages = new List<PipelineStage>();

            service.Pipeline.RegisterStep("Create", PipelineStage.PreValidation, ctx =>
            {
                firedStages.Add((PipelineStage)ctx.Stage);
            });

            service.Create(new Entity("account") { ["name"] = "Test" });

            Assert.Single(firedStages);
            Assert.Equal(PipelineStage.PreValidation, firedStages[0]);
        }

        [Fact]
        public void Pipeline_ExecutionOrder_PreValidation_PreOperation_PostOperation()
        {
            var service = new FakeOrganizationService();
            var order = new List<string>();

            service.Pipeline.RegisterStep("Create", PipelineStage.PreValidation, _ => order.Add("PreValidation"));
            service.Pipeline.RegisterStep("Create", PipelineStage.PreOperation, _ => order.Add("PreOperation"));
            service.Pipeline.RegisterStep("Create", PipelineStage.PostOperation, _ => order.Add("PostOperation"));

            service.Create(new Entity("account") { ["name"] = "Test" });

            Assert.Equal(new[] { "PreValidation", "PreOperation", "PostOperation" }, order);
        }

        [Fact]
        public void Pipeline_PreValidation_ThrowsAbortsPipeline()
        {
            var service = new FakeOrganizationService();
            bool postFired = false;

            service.Pipeline.RegisterStep("Create", PipelineStage.PreValidation, _ =>
            {
                throw new InvalidPluginExecutionException("Blocked by plugin");
            });
            service.Pipeline.RegisterStep("Create", PipelineStage.PostOperation, _ =>
            {
                postFired = true;
            });

            var ex = Assert.Throws<InvalidPluginExecutionException>(() =>
                service.Create(new Entity("account") { ["name"] = "Test" }));
            Assert.Equal("Blocked by plugin", ex.Message);
            Assert.False(postFired);
        }

        [Fact]
        public void Pipeline_EntityScoped_OnlyFiresForMatchingEntity()
        {
            var service = new FakeOrganizationService();
            var firedFor = new List<string>();

            service.Pipeline.RegisterStep("Create", PipelineStage.PreOperation, "account", ctx =>
            {
                firedFor.Add(ctx.PrimaryEntityName);
            });

            service.Create(new Entity("contact") { ["fullname"] = "John" });
            service.Create(new Entity("account") { ["name"] = "Contoso" });

            Assert.Single(firedFor);
            Assert.Equal("account", firedFor[0]);
        }

        [Fact]
        public void Pipeline_Context_HasCorrectProperties()
        {
            var service = new FakeOrganizationService();
            IPluginExecutionContext? capturedContext = null;

            service.Pipeline.RegisterStep("Create", PipelineStage.PreOperation, ctx =>
            {
                capturedContext = ctx;
            });

            service.Create(new Entity("account") { ["name"] = "Contoso" });

            Assert.NotNull(capturedContext);
            Assert.Equal("Create", capturedContext!.MessageName);
            Assert.Equal("account", capturedContext.PrimaryEntityName);
            Assert.True(capturedContext.InputParameters.ContainsKey("Target"));
            Assert.IsType<Entity>(capturedContext.InputParameters["Target"]);
        }

        [Fact]
        public void Pipeline_Context_ExposesSDKInterfaceProperties()
        {
            var service = new FakeOrganizationService();
            var orgId = Guid.NewGuid();
            var buId = Guid.NewGuid();
            service.OrganizationId = orgId;
            service.BusinessUnitId = buId;
            service.OrganizationName = "TestOrg";
            FakePipelineContext? captured = null;
            int capturedStage = 0;

            service.Pipeline.RegisterStep("Create", PipelineStage.PreOperation, ctx =>
            {
                captured = ctx as FakePipelineContext;
                capturedStage = ctx.Stage; // capture stage value at callback time
            });

            service.Create(new Entity("account") { ["name"] = "Contoso" });

            Assert.NotNull(captured);
            Assert.Equal(orgId, captured!.OrganizationId);
            Assert.Equal(buId, captured.BusinessUnitId);
            Assert.Equal("TestOrg", captured.OrganizationName);
            Assert.Equal((int)PipelineStage.PreOperation, capturedStage);
            Assert.Equal(PipelineStage.PreOperation, (PipelineStage)capturedStage);
        }

        [Fact]
        public void Pipeline_PostOperation_HasOutputParameters()
        {
            var service = new FakeOrganizationService();
            IPluginExecutionContext? capturedContext = null;

            service.Pipeline.RegisterStep("Create", PipelineStage.PostOperation, ctx =>
            {
                capturedContext = ctx;
            });

            var id = service.Create(new Entity("account") { ["name"] = "Contoso" });

            Assert.NotNull(capturedContext);
            Assert.True(capturedContext!.OutputParameters.ContainsKey("id"));
            Assert.Equal(id, (Guid)capturedContext.OutputParameters["id"]);
        }

        [Fact]
        public void Pipeline_Update_FiresSteps()
        {
            var service = new FakeOrganizationService();
            var id = service.Create(new Entity("account") { ["name"] = "Contoso" });
            var order = new List<string>();

            service.Pipeline.RegisterStep("Update", PipelineStage.PreOperation, _ => order.Add("PreOp"));
            service.Pipeline.RegisterStep("Update", PipelineStage.PostOperation, _ => order.Add("PostOp"));

            service.Update(new Entity("account", id) { ["name"] = "Fabrikam" });

            Assert.Equal(new[] { "PreOp", "PostOp" }, order);
        }

        [Fact]
        public void Pipeline_Delete_FiresSteps()
        {
            var service = new FakeOrganizationService();
            var id = service.Create(new Entity("account") { ["name"] = "Contoso" });
            var order = new List<string>();

            service.Pipeline.RegisterStep("Delete", PipelineStage.PreValidation, _ => order.Add("PreVal"));
            service.Pipeline.RegisterStep("Delete", PipelineStage.PostOperation, _ => order.Add("PostOp"));

            service.Delete("account", id);

            Assert.Equal(new[] { "PreVal", "PostOp" }, order);
        }

        [Fact]
        public void Pipeline_PreOperation_ThrowsAborts_Update()
        {
            var service = new FakeOrganizationService();
            var id = service.Create(new Entity("account") { ["name"] = "Contoso" });

            service.Pipeline.RegisterStep("Update", PipelineStage.PreOperation, _ =>
            {
                throw new InvalidPluginExecutionException("Cannot update");
            });

            Assert.Throws<InvalidPluginExecutionException>(() =>
                service.Update(new Entity("account", id) { ["name"] = "Fabrikam" }));

            // Original value should be unchanged since core operation never ran
            var retrieved = service.Retrieve("account", id, new Microsoft.Xrm.Sdk.Query.ColumnSet("name"));
            Assert.Equal("Contoso", retrieved.GetAttributeValue<string>("name"));
        }

        [Fact]
        public void Pipeline_StepRegistration_Dispose_Unregisters()
        {
            var service = new FakeOrganizationService();
            int callCount = 0;

            var registration = service.Pipeline.RegisterStep("Create", PipelineStage.PreOperation, _ => callCount++);
            service.Create(new Entity("account") { ["name"] = "Test1" });
            Assert.Equal(1, callCount);

            registration.Dispose();
            service.Create(new Entity("account") { ["name"] = "Test2" });
            Assert.Equal(1, callCount); // Should not have fired again
        }

        [Fact]
        public void Pipeline_Context_UserId_MatchesCallerId()
        {
            var service = new FakeOrganizationService();
            var customCaller = Guid.NewGuid();
            service.CallerId = customCaller;
            IPluginExecutionContext? capturedContext = null;

            service.Pipeline.RegisterStep("Create", PipelineStage.PreValidation, ctx =>
            {
                capturedContext = ctx;
            });

            service.Create(new Entity("account") { ["name"] = "Test" });

            Assert.NotNull(capturedContext);
            Assert.Equal(customCaller, capturedContext!.UserId);
        }

        // ── Convenience helpers ──────────────────────────────────────────────

        [Fact]
        public void Pipeline_RegisterPreOperation_Convenience_Fires()
        {
            var service = new FakeOrganizationService();
            bool fired = false;

            service.Pipeline.RegisterPreOperation("Create", "account", _ => fired = true);
            service.Create(new Entity("account") { ["name"] = "Contoso" });

            Assert.True(fired);
        }

        [Fact]
        public void Pipeline_RegisterPostOperation_Convenience_Fires()
        {
            var service = new FakeOrganizationService();
            bool fired = false;

            service.Pipeline.RegisterPostOperation("Delete", _ => fired = true);
            var id = service.Create(new Entity("account") { ["name"] = "Contoso" });
            service.Delete("account", id);

            Assert.True(fired);
        }

        [Fact]
        public void Pipeline_RegisterPreValidation_Convenience_Fires()
        {
            var service = new FakeOrganizationService();
            bool fired = false;

            service.Pipeline.RegisterPreValidation("Create", _ => fired = true);
            service.Create(new Entity("account") { ["name"] = "Contoso" });

            Assert.True(fired);
        }

        // ── IPlugin support ──────────────────────────────────────────────────

        [Fact]
        public void Pipeline_IPlugin_ExecuteIsCalled()
        {
            var service = new FakeOrganizationService();
            var plugin = new CapturingPlugin();

            service.Pipeline.RegisterStep("Create", PipelineStage.PreOperation, "account", plugin);
            service.Create(new Entity("account") { ["name"] = "Contoso" });

            Assert.NotNull(plugin.CapturedContext);
            Assert.Equal("Create", plugin.CapturedContext!.MessageName);
            Assert.Equal("account", plugin.CapturedContext.PrimaryEntityName);
        }

        [Fact]
        public void Pipeline_IPlugin_ServiceProviderProvidesContext()
        {
            var service = new FakeOrganizationService();
            IPluginExecutionContext? pluginContext = null;
            IOrganizationServiceFactory? pluginFactory = null;
            ITracingService? pluginTracing = null;

            var plugin = new LambdaPlugin(sp =>
            {
                pluginContext = (IPluginExecutionContext)sp.GetService(typeof(IPluginExecutionContext))!;
                pluginFactory = (IOrganizationServiceFactory)sp.GetService(typeof(IOrganizationServiceFactory))!;
                pluginTracing = (ITracingService)sp.GetService(typeof(ITracingService))!;
            });

            service.Pipeline.RegisterStep("Create", PipelineStage.PostOperation, plugin);
            service.Create(new Entity("account") { ["name"] = "Contoso" });

            Assert.NotNull(pluginContext);
            Assert.NotNull(pluginFactory);
            Assert.NotNull(pluginTracing);
        }

        [Fact]
        public void Pipeline_IPlugin_CanCreateRecordsViaFactory()
        {
            var service = new FakeOrganizationService();

            var plugin = new LambdaPlugin(sp =>
            {
                var ctx = (IPluginExecutionContext)sp.GetService(typeof(IPluginExecutionContext))!;
                var factory = (IOrganizationServiceFactory)sp.GetService(typeof(IOrganizationServiceFactory))!;
                var orgService = factory.CreateOrganizationService(ctx.UserId);

                var target = (Entity)ctx.InputParameters["Target"];
                orgService.Create(new Entity("contact")
                {
                    ["parentcustomerid"] = new EntityReference("account", target.Id),
                    ["lastname"] = "Plugin Contact"
                });
            });

            service.Pipeline.RegisterStep("Create", PipelineStage.PostOperation, "account", plugin);
            service.Create(new Entity("account") { ["name"] = "Contoso" });

            var contacts = service.RetrieveMultiple(new Microsoft.Xrm.Sdk.Query.QueryExpression("contact")
            {
                ColumnSet = new Microsoft.Xrm.Sdk.Query.ColumnSet("lastname")
            });
            Assert.Single(contacts.Entities);
            Assert.Equal("Plugin Contact", contacts.Entities[0]["lastname"]);
        }

        [Fact]
        public void Pipeline_IPlugin_TracingServiceCapturesMessages()
        {
            var service = new FakeOrganizationService();

            var plugin = new LambdaPlugin(sp =>
            {
                var tracing = (ITracingService)sp.GetService(typeof(ITracingService))!;
                tracing.Trace("Plugin executed for {0}", "account");
                tracing.Trace("Done");
            });

            service.Pipeline.RegisterStep("Create", PipelineStage.PostOperation, plugin);
            service.Create(new Entity("account") { ["name"] = "Contoso" });

            Assert.Equal(2, service.Pipeline.Traces.Count);
            Assert.Equal("Plugin executed for account", service.Pipeline.Traces[0]);
            Assert.Equal("Done", service.Pipeline.Traces[1]);
        }

        [Fact]
        public void Pipeline_ClearTraces_ResetsCollection()
        {
            var service = new FakeOrganizationService();

            service.Pipeline.RegisterPostOperation("Create", ctx =>
            {
                // no-op — traces come from IPlugin steps; ClearTraces just needs to empty the list
            });
            var plugin = new LambdaPlugin(sp =>
            {
                ((ITracingService)sp.GetService(typeof(ITracingService))!).Trace("hello");
            });
            service.Pipeline.RegisterStep("Create", PipelineStage.PostOperation, plugin);

            service.Create(new Entity("account") { ["name"] = "Contoso" });
            Assert.NotEmpty(service.Pipeline.Traces);

            service.Pipeline.ClearTraces();
            Assert.Empty(service.Pipeline.Traces);
        }

        [Fact]
        public void Pipeline_IPlugin_ThrowsAbortsPipeline()
        {
            var service = new FakeOrganizationService();
            bool postFired = false;

            service.Pipeline.RegisterStep("Create", PipelineStage.PreOperation,
                new LambdaPlugin(_ => throw new InvalidPluginExecutionException("plugin abort")));
            service.Pipeline.RegisterStep("Create", PipelineStage.PostOperation, _ => postFired = true);

            Assert.Throws<InvalidPluginExecutionException>(() =>
                service.Create(new Entity("account") { ["name"] = "Test" }));
            Assert.False(postFired);
        }

        // ── Helper plugin implementations ────────────────────────────────────

        private sealed class CapturingPlugin : IPlugin
        {
            public IPluginExecutionContext? CapturedContext { get; private set; }

            public void Execute(IServiceProvider serviceProvider)
            {
                CapturedContext = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext))!;
            }
        }

        private sealed class LambdaPlugin : IPlugin
        {
            private readonly Action<IServiceProvider> _action;
            public LambdaPlugin(Action<IServiceProvider> action) => _action = action;
            public void Execute(IServiceProvider serviceProvider) => _action(serviceProvider);
        }

        [Fact]
        public void Pipeline_SharedVariables_PropagateAcrossStages()
        {
            var service = new FakeOrganizationService();
            string? captured = null;

            service.Pipeline.RegisterStep("Create", PipelineStage.PreOperation, ctx =>
            {
                ctx.SharedVariables["key"] = "FromPreOp";
            });

            service.Pipeline.RegisterStep("Create", PipelineStage.PostOperation, ctx =>
            {
                captured = ctx.SharedVariables.ContainsKey("key")
                    ? (string)ctx.SharedVariables["key"]
                    : null;
            });

            service.Create(new Entity("account") { ["name"] = "Test" });

            Assert.Equal("FromPreOp", captured);
        }

        [Fact]
        public void Pipeline_Context_Depth_IsOne()
        {
            var service = new FakeOrganizationService();
            int depth = 0;

            service.Pipeline.RegisterStep("Create", PipelineStage.PreOperation, ctx =>
            {
                depth = ctx.Depth;
            });

            service.Create(new Entity("account") { ["name"] = "Test" });

            Assert.Equal(1, depth);
        }

        [Fact]
        public void Pipeline_Context_Mode_IsSynchronous()
        {
            var service = new FakeOrganizationService();
            int mode = -1;

            service.Pipeline.RegisterStep("Create", PipelineStage.PreOperation, ctx =>
            {
                mode = ctx.Mode;
            });

            service.Create(new Entity("account") { ["name"] = "Test" });

            Assert.Equal(0, mode); // 0 = Synchronous
        }

        [Fact]
        public void Pipeline_Context_IsInTransaction_IsTrue()
        {
            var service = new FakeOrganizationService();
            bool inTransaction = false;

            service.Pipeline.RegisterStep("Create", PipelineStage.PreOperation, ctx =>
            {
                inTransaction = ctx.IsInTransaction;
            });

            service.Create(new Entity("account") { ["name"] = "Test" });

            Assert.True(inTransaction);
        }

        [Fact]
        public void Pipeline_PreOperation_CanModifyTarget()
        {
            var service = new FakeOrganizationService();

            service.Pipeline.RegisterStep("Create", PipelineStage.PreOperation, ctx =>
            {
                var target = (Entity)ctx.InputParameters["Target"];
                target["name"] = "ModifiedByPlugin";
            });

            var id = service.Create(new Entity("account") { ["name"] = "Original" });

            var retrieved = service.Retrieve("account", id, new Microsoft.Xrm.Sdk.Query.ColumnSet("name"));
            Assert.Equal("ModifiedByPlugin", retrieved.GetAttributeValue<string>("name"));
        }

        [Fact]
        public void Pipeline_MultipleStepsInSameStage_AllFire()
        {
            var service = new FakeOrganizationService();
            var firedSteps = new List<string>();

            service.Pipeline.RegisterStep("Create", PipelineStage.PreOperation, _ => firedSteps.Add("Step1"));
            service.Pipeline.RegisterStep("Create", PipelineStage.PreOperation, _ => firedSteps.Add("Step2"));

            service.Create(new Entity("account") { ["name"] = "Test" });

            Assert.Equal(2, firedSteps.Count);
            Assert.Contains("Step1", firedSteps);
            Assert.Contains("Step2", firedSteps);
        }
    }
}

