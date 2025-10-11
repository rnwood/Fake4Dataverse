using Fake4Dataverse.Extensions;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;
using System;
using Fake4Dataverse.Abstractions;
using Fake4Dataverse.Abstractions.FakeMessageExecutors;
using Fake4Dataverse.Query;

namespace Fake4Dataverse.Middleware.Crud.FakeMessageExecutors
{
    public class RetrieveRequestExecutor : IFakeMessageExecutor
    {
        public bool CanExecute(OrganizationRequest request)
        {
            return request.GetType().Equals(GetResponsibleRequestType());
        }



        public OrganizationResponse Execute(OrganizationRequest req, IXrmFakedContext ctx)
        {
            var context = (ctx as XrmFakedContext);
            var request = req as RetrieveRequest;

            if (request.Target == null)
            {
                throw new ArgumentNullException("Target", "RetrieveRequest without Target is invalid.");
            }

            var entityName = request.Target.LogicalName;
            var columnSet = request.ColumnSet;
            if (columnSet == null)
            {
                throw FakeOrganizationServiceFaultFactory.New("Required field 'ColumnSet' is missing");
            }

            var id = context.GetRecordUniqueId(request.Target);

            //Entity logical name exists, so , check if the requested entity exists
            if (context.Data.ContainsKey(entityName) && context.Data[entityName] != null
                && context.Data[entityName].ContainsKey(id))
            {
                //Return the subset of columns requested only
                var reflectedType = context.FindReflectedType(entityName);

                //Entity found => return only the subset of columns specified or all of them
                var resultEntity = context.Data[entityName][id].Clone(reflectedType, context);
                if (!columnSet.AllColumns)
                {
                    resultEntity = resultEntity.ProjectAttributes(columnSet, context);
                }
                resultEntity.ApplyDateBehaviour(context);

                // Evaluate calculated fields for the retrieved entity
                // Reference: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/define-calculated-fields
                // "Calculated columns are calculated in real-time when they are retrieved"
                context.EvaluateCalculatedFieldsForEntity(resultEntity);

                if (request.RelatedEntitiesQuery != null && request.RelatedEntitiesQuery.Count > 0)
                {
                    foreach (var relatedEntitiesQuery in request.RelatedEntitiesQuery)
                    {
                        if (relatedEntitiesQuery.Value == null)
                        {
                            throw new ArgumentNullException("relateEntitiesQuery.Value",
                                string.Format("RelatedEntitiesQuery for \"{0}\" does not contain a Query Expression.",
                                    relatedEntitiesQuery.Key.SchemaName));
                        }

                        var fakeRelationship = context.GetRelationship(relatedEntitiesQuery.Key.SchemaName);
                        if (fakeRelationship == null)
                        {
                            throw new Exception(string.Format("Relationship \"{0}\" does not exist in the metadata cache.",
                                relatedEntitiesQuery.Key.SchemaName));
                        }

                        var relatedEntitiesQueryValue = (QueryExpression)relatedEntitiesQuery.Value;
                        QueryExpression retrieveRelatedEntitiesQuery = relatedEntitiesQueryValue.Clone();

                        if (fakeRelationship.RelationshipType == XrmFakedRelationship.FakeRelationshipType.OneToMany)
                        {
                            var isFrom1to2 = relatedEntitiesQueryValue.EntityName == fakeRelationship.Entity1LogicalName
                                || request.Target.LogicalName != fakeRelationship.Entity1LogicalName
                                || string.IsNullOrWhiteSpace(relatedEntitiesQueryValue.EntityName);

                            if (isFrom1to2)
                            {
                                var fromAttribute = isFrom1to2 ? fakeRelationship.Entity1Attribute : fakeRelationship.Entity2Attribute;
                                var toAttribute = isFrom1to2 ? fakeRelationship.Entity2Attribute : fakeRelationship.Entity1Attribute;

                                var linkEntity = new LinkEntity
                                {
                                    Columns = new ColumnSet(false),
                                    LinkFromAttributeName = fromAttribute,
                                    LinkFromEntityName = retrieveRelatedEntitiesQuery.EntityName,
                                    LinkToAttributeName = toAttribute,
                                    LinkToEntityName = resultEntity.LogicalName
                                };

                                if (retrieveRelatedEntitiesQuery.Criteria == null)
                                {
                                    retrieveRelatedEntitiesQuery.Criteria = new FilterExpression();
                                }

                                retrieveRelatedEntitiesQuery.Criteria
                                    .AddFilter(LogicalOperator.And)
                                    .AddCondition(linkEntity.LinkFromAttributeName, ConditionOperator.Equal, resultEntity.Id);
                            }
                            else
                            {
                                var link = retrieveRelatedEntitiesQuery.AddLink(fakeRelationship.Entity1LogicalName, fakeRelationship.Entity2Attribute, fakeRelationship.Entity1Attribute);
                                link.LinkCriteria.AddCondition(resultEntity.LogicalName + "id", ConditionOperator.Equal, resultEntity.Id);
                            }
                        }
                        else
                        {
                            var isFrom1 = fakeRelationship.Entity1LogicalName == retrieveRelatedEntitiesQuery.EntityName;
                            var linkAttributeName = isFrom1 ? fakeRelationship.Entity1Attribute : fakeRelationship.Entity2Attribute;
                            var conditionAttributeName = isFrom1 ? fakeRelationship.Entity2Attribute : fakeRelationship.Entity1Attribute;

                            var linkEntity = new LinkEntity
                            {
                                Columns = new ColumnSet(false),
                                LinkFromAttributeName = linkAttributeName,
                                LinkFromEntityName = retrieveRelatedEntitiesQuery.EntityName,
                                LinkToAttributeName = linkAttributeName,
                                LinkToEntityName = fakeRelationship.IntersectEntity,
                                LinkCriteria = new FilterExpression
                                {
                                    Conditions =
                                {
                                    new ConditionExpression(conditionAttributeName , ConditionOperator.Equal, resultEntity.Id)
                                }
                                }
                            };
                            retrieveRelatedEntitiesQuery.LinkEntities.Add(linkEntity);
                        }

                        var retrieveRelatedEntitiesRequest = new RetrieveMultipleRequest
                        {
                            Query = retrieveRelatedEntitiesQuery
                        };

                        //use of an executor directly; if to use service.RetrieveMultiple then the result will be
                        //limited to the number of records per page (somewhere in future release).
                        //ALL RECORDS are needed here.
                        var executor = new RetrieveMultipleRequestExecutor();
                        var retrieveRelatedEntitiesResponse = executor
                            .Execute(retrieveRelatedEntitiesRequest, context) as RetrieveMultipleResponse;

                        if (retrieveRelatedEntitiesResponse.EntityCollection.Entities.Count == 0)
                            continue;

                        resultEntity.RelatedEntities
                            .Add(relatedEntitiesQuery.Key, retrieveRelatedEntitiesResponse.EntityCollection);
                    }
                }

                return new RetrieveResponse
                {
                    Results = new ParameterCollection { { "Entity", resultEntity } }
                };
            }
            else
            {
                // Entity not found in the context => FaultException //unchecked((int)0x80040217)
                throw FakeOrganizationServiceFaultFactory.New(ErrorCodes.ObjectDoesNotExist, $"{entityName} With Id = {id:D} Does Not Exist");
            }
        }

        public Type GetResponsibleRequestType()
        {
            return typeof(RetrieveRequest);
        }
    }
}