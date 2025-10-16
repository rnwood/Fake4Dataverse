using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FakeItEasy;
using Fake4Dataverse.Abstractions;
using Fake4Dataverse.Abstractions.FakeMessageExecutors;
using Fake4Dataverse.Abstractions.Middleware;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;

namespace Fake4Dataverse.Middleware.Messages
{
    public static class MiddlewareBuilderMessagesExtensions 
    {
        public static IMiddlewareBuilder AddFakeMessageExecutors(this IMiddlewareBuilder builder) 
        {
            builder.Add(context => {
                var service = context.GetOrganizationService();
               
                var allExecutors = Assembly.GetExecutingAssembly()
                    .GetTypes()
                    .Where(t => t.GetInterfaces().Contains(typeof(IFakeMessageExecutor)))
                    .Select(t => Activator.CreateInstance(t) as IFakeMessageExecutor)
                    .ToList();

                // Group executors by responsible request type
                // For types with multiple executors (like OrganizationRequest), store them in a list
                var fakeMessageExecutorsDictionary = new Dictionary<Type, IFakeMessageExecutor>();
                var organizationRequestExecutors = new List<IFakeMessageExecutor>();

                foreach (var executor in allExecutors)
                {
                    var requestType = executor.GetResponsibleRequestType();
                    
                    // Special handling for OrganizationRequest type - store in separate list
                    // NavigateToNextEntityOrganizationRequestExecutor should be checked before CustomApiExecutor
                    if (requestType == typeof(OrganizationRequest))
                    {
                        organizationRequestExecutors.Add(executor);
                    }
                    else if (!fakeMessageExecutorsDictionary.ContainsKey(requestType))
                    {
                        fakeMessageExecutorsDictionary.Add(requestType, executor);
                    }
                }

                // Sort OrganizationRequest executors: specific executors before generic ones
                // NavigateToNextEntityOrganizationRequestExecutor should be checked before CustomApiExecutor
                organizationRequestExecutors.Sort((a, b) =>
                {
                    // CustomApiExecutor should come last
                    if (a.GetType().Name == "CustomApiExecutor") return 1;
                    if (b.GetType().Name == "CustomApiExecutor") return -1;
                    return 0;
                });
                    
                var messageExecutors = new MessageExecutors(fakeMessageExecutorsDictionary);
                context.SetProperty(messageExecutors);

                // Store OrganizationRequest executors separately for CanExecute-based routing
                if (organizationRequestExecutors.Count > 0)
                {
                    context.SetProperty(new OrganizationRequestExecutors(organizationRequestExecutors));
                }

                AddFakeAssociate(context, service);
                AddFakeDisassociate(context, service);
            });

            return builder;
        }

        public static IMiddlewareBuilder AddFakeMessageExecutor(this IMiddlewareBuilder builder, IFakeMessageExecutor executor) 
        {
            builder.Add(context => {
                
                var messageExecutors = context.GetProperty<MessageExecutors>();
                if (!messageExecutors.ContainsKey(executor.GetResponsibleRequestType()))
                    messageExecutors.Add(executor.GetResponsibleRequestType(), executor);
                else
                    messageExecutors[executor.GetResponsibleRequestType()] = executor;
            });

            return builder;
        }


        public static IMiddlewareBuilder AddExecutionMock<T>(this IMiddlewareBuilder builder, OrganizationRequestExecution mock) where T : OrganizationRequest
        {
            builder.Add(context => {
                if(!context.HasProperty<ExecutionMocks>())
                    context.SetProperty<ExecutionMocks>(new ExecutionMocks());

                var executionMocks = context.GetProperty<ExecutionMocks>();

                if (!executionMocks.ContainsKey(typeof(T)))
                    executionMocks.Add(typeof(T), mock);
                else
                    executionMocks[typeof(T)] = mock;
            });
           
           return builder;
        }

        public static IMiddlewareBuilder RemoveExecutionMock<T>(this IMiddlewareBuilder builder) where T : OrganizationRequest
        {
            builder.Add(context => {
                var executionMocks = context.GetProperty<ExecutionMocks>();
                if (executionMocks.ContainsKey(typeof(T))) 
                {
                    executionMocks.Remove(typeof(T));
                }
            });

            return builder;
        }

        public static IMiddlewareBuilder UseMessages(this IMiddlewareBuilder builder) 
        {

            Func<OrganizationRequestDelegate, OrganizationRequestDelegate> middleware = next => {

                return (IXrmFakedContext context, OrganizationRequest request) => {
                    
                    if(CanHandleRequest(context, request)) 
                    {
                        return ProcessRequest(context, request);
                    }
                    else 
                    {
                        return next.Invoke(context, request);
                    }
                };
            };
            
            builder.Use(middleware);
            return builder;
        }

        private static bool CanHandleRequest(IXrmFakedContext context, OrganizationRequest request) 
        {
            if(context.HasProperty<ExecutionMocks>()) 
            {
                var executionMocks = context.GetProperty<ExecutionMocks>();
                if(executionMocks.ContainsKey(request.GetType()))
                {
                    return true;
                }
            }
            
            if(context.HasProperty<MessageExecutors>())
            {
                var messageExecutors = context.GetProperty<MessageExecutors>();
                if (messageExecutors.ContainsKey(request.GetType()))
                {
                    return true;
                }
            }

            // Check if any OrganizationRequest executor can handle this request
            if (request.GetType() == typeof(OrganizationRequest) && 
                context.HasProperty<OrganizationRequestExecutors>())
            {
                var orgRequestExecutors = context.GetProperty<OrganizationRequestExecutors>();
                return orgRequestExecutors.Any(executor => executor.CanExecute(request));
            }
            
            return false;
        }

        private static OrganizationResponse ProcessRequest(IXrmFakedContext context, OrganizationRequest request) 
        {
            if(context.HasProperty<ExecutionMocks>()) 
            {
                var executionMocks = context.GetProperty<ExecutionMocks>();
                if(executionMocks.ContainsKey(request.GetType()))
                {
                    return executionMocks[request.GetType()].Invoke(request);
                }
            }

            var messageExecutors = context.GetProperty<MessageExecutors>();
            if (messageExecutors.ContainsKey(request.GetType()))
            {
                return messageExecutors[request.GetType()].Execute(request, context);
            }

            // Handle OrganizationRequest with CanExecute-based routing
            if (request.GetType() == typeof(OrganizationRequest) && 
                context.HasProperty<OrganizationRequestExecutors>())
            {
                var orgRequestExecutors = context.GetProperty<OrganizationRequestExecutors>();
                var executor = orgRequestExecutors.FirstOrDefault(e => e.CanExecute(request));
                if (executor != null)
                {
                    return executor.Execute(request, context);
                }
            }

            throw PullRequestException.NotImplementedOrganizationRequest(request.GetType());
        }
        

        private static void AddFakeAssociate(IXrmFakedContext context, IOrganizationService service)
        {
            A.CallTo(() => service.Associate(A<string>._, A<Guid>._, A<Relationship>._, A<EntityReferenceCollection>._))
                .Invokes((string entityName, Guid entityId, Relationship relationship, EntityReferenceCollection entityCollection) =>
                {
                    var messageExecutors = context.GetProperty<MessageExecutors>();

                    if (messageExecutors.ContainsKey(typeof(AssociateRequest)))
                    {
                        var request = new AssociateRequest()
                        {
                            Target = new EntityReference() { Id = entityId, LogicalName = entityName },
                            Relationship = relationship,
                            RelatedEntities = entityCollection
                        };
                        service.Execute(request);
                    }
                    else
                        throw PullRequestException.NotImplementedOrganizationRequest(typeof(AssociateRequest));
                });
        }

        private static void AddFakeDisassociate(IXrmFakedContext context, IOrganizationService service)
        {
            A.CallTo(() => service.Disassociate(A<string>._, A<Guid>._, A<Relationship>._, A<EntityReferenceCollection>._))
                .Invokes((string entityName, Guid entityId, Relationship relationship, EntityReferenceCollection entityCollection) =>
                {
                    var messageExecutors = context.GetProperty<MessageExecutors>();

                    if (messageExecutors.ContainsKey(typeof(DisassociateRequest)))
                    {
                        var request = new DisassociateRequest()
                        {
                            Target = new EntityReference() { Id = entityId, LogicalName = entityName },
                            Relationship = relationship,
                            RelatedEntities = entityCollection
                        };
                        service.Execute(request);
                    }
                    else
                        throw PullRequestException.NotImplementedOrganizationRequest(typeof(DisassociateRequest));
                });
        }
    }
}