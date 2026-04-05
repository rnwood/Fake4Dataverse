using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using Fake4Dataverse.Metadata;
using Fake4Dataverse.Pipeline;
using Fake4Dataverse.Security;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace Fake4Dataverse
{
    /// <summary>
    /// An in-memory fake implementation of <see cref="IOrganizationService"/> for unit testing
    /// Dataverse / Dynamics 365 applications without a live connection.
    /// </summary>
    public sealed class FakeOrganizationService : IOrganizationService
    {
        private readonly InMemoryEntityStore _store = new InMemoryEntityStore();
        private readonly AttributeIndex _attributeIndex = new AttributeIndex();
        private readonly OrganizationRequestHandlerRegistry _handlerRegistry;
        private readonly QueryExpressionEvaluator _queryEvaluator;
        private long _versionCounter;

        private readonly FetchXmlEvaluator _fetchXmlEvaluator;
        private readonly Dictionary<(string EntityName, Guid EntityId, string AttributeName), byte[]> _binaryStore =
            new Dictionary<(string, Guid, string), byte[]>();
        private readonly Dictionary<string, FileUploadSession> _uploadSessions = new Dictionary<string, FileUploadSession>();
        private readonly Dictionary<string, List<StatusTransition>> _statusTransitions = new Dictionary<string, List<StatusTransition>>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, (int StateCode, int StatusCode)> _defaultStatusCodes = new Dictionary<string, (int, int)>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Gets the handler registry for registering custom <see cref="OrganizationRequest"/> handlers.
        /// </summary>
        public OrganizationRequestHandlerRegistry HandlerRegistry => _handlerRegistry;

        /// <summary>
        /// Gets the metadata store for defining entity/attribute metadata and validation rules.
        /// </summary>
        public InMemoryMetadataStore MetadataStore { get; } = new InMemoryMetadataStore();

        /// <summary>
        /// Gets or sets whether metadata-based validation is applied on Create and Update.
        /// Defaults to <c>false</c> (opt-in).
        /// </summary>
        public bool ValidateWithMetadata { get; set; }

        /// <summary>
        /// Gets or sets the clock used for auto-generated timestamps.
        /// </summary>
        public IClock Clock { get; set; }

        /// <summary>
        /// Gets or sets the caller identity used for auto-generated createdby/modifiedby fields.
        /// </summary>
        public Guid CallerId { get; set; } = new Guid("00000000-0000-0000-0000-000000000001");

        /// <summary>
        /// Gets or sets the initiating user ID. In impersonation scenarios this differs from <see cref="CallerId"/>.
        /// Defaults to the same value as <see cref="CallerId"/>.
        /// </summary>
        public Guid InitiatingUserId { get; set; } = new Guid("00000000-0000-0000-0000-000000000001");

        /// <summary>
        /// Gets or sets the business unit ID of the caller.
        /// </summary>
        public Guid BusinessUnitId { get; set; } = new Guid("00000000-0000-0000-0000-000000000003");

        /// <summary>
        /// Gets or sets the organization ID.
        /// </summary>
        public Guid OrganizationId { get; set; } = new Guid("00000000-0000-0000-0000-000000000002");

        /// <summary>
        /// Gets or sets the organization name surfaced to plugins via <see cref="IPluginExecutionContext"/>.
        /// </summary>
        public string OrganizationName { get; set; } = "FakeOrganization";

        /// <summary>
        /// Gets the security manager for configuring roles, privileges, and record sharing.
        /// </summary>
        public SecurityManager Security { get; } = new SecurityManager();

        /// <summary>
        /// When <c>true</c>, all security checks are bypassed (system context).
        /// </summary>
        public bool UseSystemContext { get; set; }

        /// <summary>
        /// Gets the pipeline manager for registering pre/post-operation steps and
        /// <see cref="IPlugin"/> instances.
        /// </summary>
        public PipelineManager Pipeline { get; }

        /// <summary>
        /// Gets the calculated field manager for registering calculated and rollup fields.
        /// </summary>
        public CalculatedFieldManager CalculatedFields { get; } = new CalculatedFieldManager();

        /// <summary>
        /// Gets the currency manager for configuring exchange rates and base currency computation.
        /// </summary>
        public CurrencyManager Currency { get; } = new CurrencyManager();

        /// <summary>
        /// Gets the operation log that records all service calls for post-hoc assertions.
        /// </summary>
        public OperationLog OperationLog { get; } = new OperationLog();

        /// <summary>
        /// Gets the configuration options controlling automatic behaviors.
        /// </summary>
        public FakeOrganizationServiceOptions Options { get; }

        /// <summary>
        /// Creates a new <see cref="FakeOrganizationService"/> with built-in handlers pre-registered.
        /// </summary>
        public FakeOrganizationService() : this(new FakeOrganizationServiceOptions())
        {
        }

        /// <summary>
        /// Creates a new <see cref="FakeOrganizationService"/> configured with the specified options.
        /// </summary>
        /// <param name="options">The configuration options.</param>
        public FakeOrganizationService(FakeOrganizationServiceOptions options)
        {
            Options = options ?? throw new System.ArgumentNullException(nameof(options));
            ValidateWithMetadata = options.ValidateWithMetadata;
            Security.EnforceSecurityRoles = options.EnforceSecurityRoles;
            _store.Index = _attributeIndex;
            Clock = SystemClock.Instance;
            _queryEvaluator = new QueryExpressionEvaluator();
            _fetchXmlEvaluator = new FetchXmlEvaluator(_queryEvaluator);
            _handlerRegistry = new OrganizationRequestHandlerRegistry();
            Pipeline = new Pipeline.PipelineManager(_ => this, (entityName, id) =>
                _store.Exists(entityName, id) ? _store.Retrieve(entityName, id, new ColumnSet(true)) : null);
            RegisterBuiltInHandlers();
        }

        /// <inheritdoc />
        public Guid Create(Entity entity)
        {
            if (entity == null) throw new System.ArgumentNullException(nameof(entity));
            if (string.IsNullOrEmpty(entity.LogicalName)) throw new System.ArgumentException("Entity logical name must be specified.", nameof(entity));

            Guid id;
            if (!Options.EnablePipeline || !Pipeline.HasSteps)
            {
                id = CreateCore(entity);
            }
            else
            {
                var inputParams = new ParameterCollection { { "Target", entity } };
                var context = Pipeline.Execute("Create", entity.LogicalName, inputParams, ctx =>
                {
                    var target = (Entity)ctx.InputParameters["Target"];
                    var resultId = CreateCore(target);
                    return new ParameterCollection { { "id", resultId } };
                }, CallerId, InitiatingUserId, BusinessUnitId, OrganizationId, OrganizationName, Clock.UtcNow);
                id = (Guid)context.OutputParameters["id"];
            }
            if (Options.EnableOperationLog)
                OperationLog.Add(new OperationRecord("Create", entity.LogicalName, id, Clock.UtcNow, InMemoryEntityStore.CloneEntity(entity), null));
            return id;
        }

        private Guid CreateCore(Entity entity)
        {
            StripEmptyStrings(entity);

            if (ValidateWithMetadata)
                MetadataStore.ValidateOnCreate(entity);

            if (!UseSystemContext)
                Security.CheckPrivilege(CallerId, entity.LogicalName, PrivilegeType.Create);

            MetadataStore.AutoDiscover(entity);

            var now = Clock.UtcNow;
            var callerRef = new EntityReference("systemuser", CallerId);

            if (Options.AutoSetOwner)
            {
                if (!entity.Contains("ownerid")) entity["ownerid"] = callerRef;
                if (!entity.Contains("createdby")) entity["createdby"] = callerRef;
                if (!entity.Contains("modifiedby")) entity["modifiedby"] = callerRef;
            }

            if (Options.AutoSetTimestamps)
            {
                if (entity.Contains("overriddencreatedon"))
                {
                    entity["createdon"] = entity["overriddencreatedon"];
                }
                else if (!entity.Contains("createdon"))
                {
                    entity["createdon"] = now;
                }
                if (!entity.Contains("modifiedon")) entity["modifiedon"] = now;
            }

            if (Options.AutoSetStateCode)
            {
                if (!entity.Contains("statecode"))
                {
                    var defaultState = 0;
                    var defaultStatus = 1;
                    if (_defaultStatusCodes.TryGetValue(entity.LogicalName, out var defaults))
                    {
                        defaultState = defaults.StateCode;
                        defaultStatus = defaults.StatusCode;
                    }
                    entity["statecode"] = new OptionSetValue(defaultState);
                    if (!entity.Contains("statuscode"))
                        entity["statuscode"] = new OptionSetValue(defaultStatus);
                }
            }

            if (Options.AutoSetVersionNumber)
                entity["versionnumber"] = System.Threading.Interlocked.Increment(ref _versionCounter);

            if (Currency.IsConfigured)
                Currency.ComputeBaseCurrencyFields(entity);

            return _store.Create(entity);
        }

        /// <inheritdoc />
        public Entity Retrieve(string entityName, Guid id, ColumnSet columnSet)
        {
            if (!UseSystemContext)
                Security.CheckPrivilege(CallerId, entityName, PrivilegeType.Read);
            var entity = _store.Retrieve(entityName, id, columnSet);
            if (CalculatedFields.HasFields)
                CalculatedFields.ApplyCalculatedFields(entity, _store);
            PopulateEntityReferenceNames(entity);
            PopulateFormattedValues(entity);
            if (Options.EnableOperationLog)
                OperationLog.Add(new OperationRecord("Retrieve", entityName, id, Clock.UtcNow, null, null));
            return entity;
        }

        /// <summary>
        /// Retrieves an entity by alternate key.
        /// </summary>
        internal Entity RetrieveByAlternateKey(string entityName, KeyAttributeCollection keyAttributes, ColumnSet columnSet)
        {
            var entity = _store.RetrieveByAlternateKey(entityName, keyAttributes, columnSet, MetadataStore);
            if (CalculatedFields.HasFields)
                CalculatedFields.ApplyCalculatedFields(entity, _store);
            PopulateEntityReferenceNames(entity);
            PopulateFormattedValues(entity);
            return entity;
        }

        /// <inheritdoc />
        public EntityCollection RetrieveMultiple(QueryBase query)
        {
            EntityCollection result;

            if (query is QueryExpression qe)
            {
                _queryEvaluator.Clock = Clock;
                _queryEvaluator.CallerId = CallerId;
                result = _queryEvaluator.Evaluate(qe, _store);
            }
            else if (query is FetchExpression fe)
            {
                _queryEvaluator.Clock = Clock;
                _queryEvaluator.CallerId = CallerId;
                result = _fetchXmlEvaluator.Evaluate(fe.Query, _store);
            }
            else if (query is QueryByAttribute qba)
            {
                result = EvaluateQueryByAttribute(qba);
            }
            else
            {
                throw new NotSupportedException($"Query type '{query.GetType().Name}' is not supported.");
            }

            if (CalculatedFields.HasFields)
            {
                foreach (var entity in result.Entities)
                    CalculatedFields.ApplyCalculatedFields(entity, _store);
            }

            foreach (var entity in result.Entities)
            {
                PopulateEntityReferenceNames(entity);
                PopulateFormattedValues(entity);
            }

            var queryEntityName = (query as QueryExpression)?.EntityName ?? (query as QueryByAttribute)?.EntityName;
            if (Options.EnableOperationLog)
                OperationLog.Add(new OperationRecord("RetrieveMultiple", queryEntityName, null, Clock.UtcNow, null, null));

            return result;
        }

        /// <inheritdoc />
        public void Update(Entity entity)
        {
            if (entity == null) throw new System.ArgumentNullException(nameof(entity));
            if (string.IsNullOrEmpty(entity.LogicalName)) throw new System.ArgumentException("Entity logical name must be specified.", nameof(entity));

            if (!Options.EnablePipeline || !Pipeline.HasSteps)
            {
                UpdateCore(entity);
            }
            else
            {
                var inputParams = new ParameterCollection { { "Target", entity } };
                Pipeline.Execute("Update", entity.LogicalName, inputParams, ctx =>
                {
                    var target = (Entity)ctx.InputParameters["Target"];
                    UpdateCore(target);
                    return new ParameterCollection();
                }, CallerId, InitiatingUserId, BusinessUnitId, OrganizationId, OrganizationName, Clock.UtcNow);
            }
            if (Options.EnableOperationLog)
                OperationLog.Add(new OperationRecord("Update", entity.LogicalName, entity.Id, Clock.UtcNow, InMemoryEntityStore.CloneEntity(entity), null));
        }

        private void UpdateCore(Entity entity)
        {
            StripEmptyStrings(entity);

            if (ValidateWithMetadata)
                MetadataStore.ValidateOnUpdate(entity);

            ResolveAlternateKey(entity);
            if (!UseSystemContext)
                Security.CheckPrivilege(CallerId, entity.LogicalName, PrivilegeType.Write);

            var now = Clock.UtcNow;
            var callerRef = new EntityReference("systemuser", CallerId);

            if (Options.AutoSetTimestamps)
                entity["modifiedon"] = now;

            if (Options.AutoSetOwner)
                entity["modifiedby"] = callerRef;

            if (Options.AutoSetVersionNumber)
                entity["versionnumber"] = System.Threading.Interlocked.Increment(ref _versionCounter);

            if (Currency.IsConfigured)
                Currency.ComputeBaseCurrencyFields(entity);

            _store.Update(entity);

            if (entity.Contains("ownerid") && entity["ownerid"] is EntityReference newOwner)
            {
                ApplyCascadeAssign(entity.LogicalName, entity.Id, newOwner);
            }
        }

        /// <inheritdoc />
        public void Delete(string entityName, Guid id)
        {
            if (!UseSystemContext)
                Security.CheckPrivilege(CallerId, entityName, PrivilegeType.Delete);
            ApplyCascadeDelete(entityName, id);

            if (!Options.EnablePipeline || !Pipeline.HasSteps)
            {
                _store.Delete(entityName, id);
            }
            else
            {
                var inputParams = new ParameterCollection
                {
                    { "Target", new EntityReference(entityName, id) }
                };
                Pipeline.Execute("Delete", entityName, inputParams, ctx =>
                {
                    var target = (EntityReference)ctx.InputParameters["Target"];
                    _store.Delete(target.LogicalName, target.Id);
                    return new ParameterCollection();
                }, CallerId, InitiatingUserId, BusinessUnitId, OrganizationId, OrganizationName, Clock.UtcNow);
            }
            if (Options.EnableOperationLog)
                OperationLog.Add(new OperationRecord("Delete", entityName, id, Clock.UtcNow, null, null));
        }

        /// <inheritdoc />
        public void Associate(string entityName, Guid entityId, Relationship relationship, EntityReferenceCollection relatedEntities)
        {
            // Store associations as relationship records in a synthetic entity.
            if (relationship == null) throw new ArgumentNullException(nameof(relationship));
            if (relatedEntities == null) throw new ArgumentNullException(nameof(relatedEntities));

            if (!_store.Exists(entityName, entityId))
                throw DataverseFault.EntityNotFound(entityName, entityId);

            foreach (var related in relatedEntities)
            {
                if (!_store.Exists(related.LogicalName, related.Id))
                    throw DataverseFault.EntityNotFound(related.LogicalName, related.Id);
            }

            if (ValidateWithMetadata)
                MetadataStore.ValidateRelationship(entityName, relationship, relatedEntities);

            var associationName = $"association_{relationship.SchemaName}";
            var existingAssociations = _store.GetAll(associationName);
            foreach (var related in relatedEntities)
            {
                var relatedId = related.Id;
                var isDuplicate = existingAssociations.Any(a =>
                {
                    var src = a.GetAttributeValue<EntityReference>("sourceid");
                    var tgt = a.GetAttributeValue<EntityReference>("targetid");
                    return (src?.Id == entityId && tgt?.Id == relatedId) ||
                           (src?.Id == relatedId && tgt?.Id == entityId);
                });
                if (isDuplicate)
                    throw DataverseFault.Create(DataverseFault.DuplicateRecord, "A record with the specified key values already exists.");
            }

            foreach (var related in relatedEntities)
            {
                var association = new Entity(associationName);
                association["sourceid"] = new EntityReference(entityName, entityId);
                association["targetid"] = related;
                _store.Create(association);
            }
            if (Options.EnableOperationLog)
                OperationLog.Add(new OperationRecord("Associate", entityName, entityId, Clock.UtcNow, null, null));
        }

        /// <inheritdoc />
        public void Disassociate(string entityName, Guid entityId, Relationship relationship, EntityReferenceCollection relatedEntities)
        {
            if (relationship == null) throw new ArgumentNullException(nameof(relationship));
            if (relatedEntities == null) throw new ArgumentNullException(nameof(relatedEntities));

            if (ValidateWithMetadata)
                MetadataStore.ValidateRelationship(entityName, relationship, relatedEntities);

            var associationEntity = $"association_{relationship.SchemaName}";
            _store.RemoveAssociations(associationEntity, entityName, entityId, relatedEntities);
            if (Options.EnableOperationLog)
                OperationLog.Add(new OperationRecord("Disassociate", entityName, entityId, Clock.UtcNow, null, null));
        }

        /// <inheritdoc />
        public OrganizationResponse Execute(OrganizationRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            var response = _handlerRegistry.Execute(request, this);
            if (Options.EnableOperationLog)
                OperationLog.Add(new OperationRecord("Execute", null, null, Clock.UtcNow, null, request));
            return response;
        }

        /// <summary>
        /// Executes a saved query (userquery or savedquery) by its entity ID.
        /// Retrieves the query entity from the store, extracts its <c>fetchxml</c> attribute,
        /// and evaluates it via <see cref="FetchExpression"/>.
        /// </summary>
        /// <param name="queryId">The unique identifier of the <c>userquery</c> or <c>savedquery</c> record.</param>
        /// <returns>The <see cref="EntityCollection"/> result of executing the saved query's FetchXml.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the saved query has no <c>fetchxml</c> attribute.</exception>
        public EntityCollection ExecuteSavedQuery(Guid queryId)
        {
            // Try userquery first, then savedquery
            Entity? queryEntity = null;
            foreach (var entityName in new[] { "userquery", "savedquery" })
            {
                try
                {
                    queryEntity = _store.Retrieve(entityName, queryId, new ColumnSet(true));
                    break;
                }
                catch (System.ServiceModel.FaultException<OrganizationServiceFault>)
                {
                    // Not found in this entity type, try the next
                }
            }

            if (queryEntity == null)
                throw DataverseFault.EntityNotFound("userquery", queryId);

            var fetchXml = queryEntity.GetAttributeValue<string>("fetchxml");
            if (string.IsNullOrEmpty(fetchXml))
                throw new InvalidOperationException($"Saved query '{queryId}' does not contain a fetchxml attribute.");

            return RetrieveMultiple(new FetchExpression(fetchXml));
        }

        /// <summary>
        /// Clears all data from the in-memory store.
        /// </summary>
        public void Reset()
        {
            _store.Clear();
            _binaryStore.Clear();
        }

        /// <summary>
        /// Registers an equality-based attribute index to accelerate queries that filter on the
        /// specified attribute using <see cref="ConditionOperator.Equal"/>. Existing entities are
        /// retroactively indexed.
        /// </summary>
        /// <param name="entityName">The logical name of the entity to index.</param>
        /// <param name="attributeName">The logical name of the attribute to index.</param>
        public void AddIndex(string entityName, string attributeName)
        {
            _attributeIndex.AddIndex(entityName, attributeName);

            // Retroactively index existing entities
            var entities = _store.GetAll(entityName);
            foreach (var entity in entities)
            {
                if (entity.Contains(attributeName))
                    _attributeIndex.IndexAttribute(entity.LogicalName, entity.Id, attributeName, entity[attributeName]);
            }
        }

        /// <summary>
        /// Stores binary data (image or file) for a specific entity attribute.
        /// </summary>
        /// <param name="entityName">The logical name of the entity.</param>
        /// <param name="entityId">The unique identifier of the entity record.</param>
        /// <param name="attributeName">The logical name of the image/file attribute.</param>
        /// <param name="data">The binary data to store.</param>
        public void SetBinaryAttribute(string entityName, Guid entityId, string attributeName, byte[] data)
        {
            if (string.IsNullOrEmpty(entityName)) throw new ArgumentException("Entity name is required.", nameof(entityName));
            if (entityId == Guid.Empty) throw new ArgumentException("Entity ID is required.", nameof(entityId));
            if (string.IsNullOrEmpty(attributeName)) throw new ArgumentException("Attribute name is required.", nameof(attributeName));
            if (data == null) throw new ArgumentNullException(nameof(data));

            _binaryStore[(entityName, entityId, attributeName)] = (byte[])data.Clone();
        }

        /// <summary>
        /// Retrieves binary data (image or file) for a specific entity attribute.
        /// </summary>
        /// <param name="entityName">The logical name of the entity.</param>
        /// <param name="entityId">The unique identifier of the entity record.</param>
        /// <param name="attributeName">The logical name of the image/file attribute.</param>
        /// <returns>The binary data, or <c>null</c> if not set.</returns>
        public byte[]? GetBinaryAttribute(string entityName, Guid entityId, string attributeName)
        {
            if (_binaryStore.TryGetValue((entityName, entityId, attributeName), out var data))
                return (byte[])data.Clone();
            return null;
        }

        internal void CreateUploadSession(string token, string entityName, Guid entityId, string attributeName)
        {
            _uploadSessions[token] = new FileUploadSession(entityName, entityId, attributeName);
        }

        internal void AppendUploadBlock(string token, byte[] blockData)
        {
            if (!_uploadSessions.TryGetValue(token, out var session))
                throw DataverseFault.InvalidArgumentFault($"Invalid file continuation token: '{token}'.");
            session.Blocks.Add((byte[])blockData.Clone());
        }

        internal void CommitUploadSession(string token, string fileName)
        {
            if (!_uploadSessions.TryGetValue(token, out var session))
                throw DataverseFault.InvalidArgumentFault($"Invalid file continuation token: '{token}'.");

            // Combine all blocks into one byte array
            int totalLength = 0;
            foreach (var block in session.Blocks) totalLength += block.Length;
            var combined = new byte[totalLength];
            int offset = 0;
            foreach (var block in session.Blocks)
            {
                Array.Copy(block, 0, combined, offset, block.Length);
                offset += block.Length;
            }

            SetBinaryAttribute(session.EntityName, session.EntityId, session.AttributeName, combined);
        }

        internal long GetCommittedFileSize(string token)
        {
            if (!_uploadSessions.TryGetValue(token, out var session))
                return 0;
            var data = GetBinaryAttribute(session.EntityName, session.EntityId, session.AttributeName);
            return data?.Length ?? 0;
        }

        internal InMemoryEntityStore Store => _store;

        /// <summary>
        /// Bulk-inserts entities directly into the store without triggering pipeline,
        /// security checks, auto-fields, or operation logging.
        /// </summary>
        /// <param name="entities">The entities to seed.</param>
        public void Seed(params Entity[] entities)
        {
            Seed((IEnumerable<Entity>)entities);
        }

        /// <summary>
        /// Bulk-inserts entities directly into the store without triggering pipeline,
        /// security checks, auto-fields, or operation logging.
        /// </summary>
        /// <param name="entities">The entities to seed.</param>
        public void Seed(IEnumerable<Entity> entities)
        {
            if (entities == null) throw new ArgumentNullException(nameof(entities));
            foreach (var entity in entities)
            {
                _store.Create(entity);
            }
        }

        /// <summary>
        /// Seeds entities from a JSON string. Expected format:
        /// <code>[{"logicalName":"account","id":"...","attributes":{"name":"Contoso"}}]</code>
        /// Inserts directly without pipeline, security, auto-fields, or operation logging.
        /// Supports string, integer, decimal, and boolean attribute values.
        /// </summary>
        /// <param name="json">A JSON array of entity objects.</param>
        public void SeedFromJson(string json)
        {
            if (json == null) throw new ArgumentNullException(nameof(json));

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.ValueKind != JsonValueKind.Array)
                throw new ArgumentException("JSON must be an array of entity objects.", nameof(json));

            foreach (var element in root.EnumerateArray())
            {
                var entity = ParseEntityFromJson(element);
                _store.Create(entity);
            }
        }

        /// <summary>
        /// Seeds entities from a CSV string. The first line must be a header row with column names.
        /// The first column must be <c>logicalName</c> and the optional second column can be <c>id</c>.
        /// Remaining columns become entity attributes. Values are stored as strings.
        /// Inserts directly without pipeline, security, auto-fields, or operation logging.
        /// </summary>
        /// <param name="csv">A CSV string with header row.</param>
        public void SeedFromCsv(string csv)
        {
            if (csv == null) throw new ArgumentNullException(nameof(csv));

            var lines = csv.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            if (lines.Length < 2)
                throw new ArgumentException("CSV must contain a header row and at least one data row.", nameof(csv));

            var headers = ParseCsvLine(lines[0]);
            if (headers.Length == 0 || !string.Equals(headers[0], "logicalName", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("First CSV column must be 'logicalName'.", nameof(csv));

            bool hasId = headers.Length > 1 && string.Equals(headers[1], "id", StringComparison.OrdinalIgnoreCase);
            int attrStart = hasId ? 2 : 1;

            for (int i = 1; i < lines.Length; i++)
            {
                var line = lines[i];
                if (string.IsNullOrWhiteSpace(line)) continue;

                var fields = ParseCsvLine(line);
                if (fields.Length == 0 || string.IsNullOrWhiteSpace(fields[0])) continue;

                var entity = new Entity(fields[0]);
                if (hasId && fields.Length > 1 && Guid.TryParse(fields[1], out var id))
                    entity.Id = id;

                for (int j = attrStart; j < headers.Length && j < fields.Length; j++)
                {
                    var value = fields[j];
                    if (!string.IsNullOrEmpty(value))
                        entity[headers[j]] = value;
                }

                _store.Create(entity);
            }
        }

        private static string[] ParseCsvLine(string line)
        {
            var result = new List<string>();
            int i = 0;
            while (i < line.Length)
            {
                if (line[i] == '"')
                {
                    i++; // skip opening quote
                    int start = i;
                    var sb = new System.Text.StringBuilder();
                    while (i < line.Length)
                    {
                        if (line[i] == '"')
                        {
                            if (i + 1 < line.Length && line[i + 1] == '"')
                            {
                                sb.Append(line, start, i - start);
                                sb.Append('"');
                                i += 2;
                                start = i;
                            }
                            else
                            {
                                sb.Append(line, start, i - start);
                                i++; // skip closing quote
                                break;
                            }
                        }
                        else
                        {
                            i++;
                        }
                    }
                    result.Add(sb.ToString());
                    if (i < line.Length && line[i] == ',') i++; // skip comma
                }
                else
                {
                    int start = i;
                    while (i < line.Length && line[i] != ',') i++;
                    result.Add(line.Substring(start, i - start));
                    if (i < line.Length) i++; // skip comma
                }
            }
            return result.ToArray();
        }

        /// <summary>
        /// Takes a snapshot of the current store state (entities and binary data).
        /// The returned object can be passed to <see cref="RestoreSnapshot"/> to revert changes.
        /// </summary>
        /// <returns>An opaque snapshot token.</returns>
        public object TakeSnapshot()
        {
            return new Snapshot(
                _store.TakeSnapshot(),
                CloneBinaryStore(),
                System.Threading.Interlocked.Read(ref _versionCounter));
        }

        /// <summary>
        /// Restores the store to a previously captured snapshot.
        /// </summary>
        /// <param name="snapshot">A snapshot token returned by <see cref="TakeSnapshot"/>.</param>
        public void RestoreSnapshot(object snapshot)
        {
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
            if (!(snapshot is Snapshot s)) throw new ArgumentException("Invalid snapshot object.", nameof(snapshot));

            _store.RestoreSnapshot(s.EntityData);
            RestoreBinaryStore(s.BinaryData);
            System.Threading.Interlocked.Exchange(ref _versionCounter, s.VersionCounter);
        }

        /// <summary>
        /// Creates a disposable scope that automatically restores the store to its current state
        /// when disposed. Useful for test isolation:
        /// <code>using (service.Scope()) { /* modifications auto-reverted */ }</code>
        /// </summary>
        /// <returns>An <see cref="IDisposable"/> that restores the snapshot on disposal.</returns>
        public IDisposable Scope()
        {
            return new ServiceScope(this);
        }

        /// <summary>
        /// Advances the clock by the specified duration. Only works when <see cref="Clock"/>
        /// is a <see cref="FakeClock"/> instance; throws <see cref="InvalidOperationException"/> otherwise.
        /// </summary>
        /// <param name="duration">The time span to advance.</param>
        public void AdvanceTime(TimeSpan duration)
        {
            if (Clock is FakeClock fakeClock)
                fakeClock.Advance(duration);
            else
                throw new InvalidOperationException("AdvanceTime can only be used when Clock is a FakeClock instance.");
        }

        private static Entity ParseEntityFromJson(JsonElement element)
        {
            var logicalName = element.GetProperty("logicalName").GetString()
                ?? throw new ArgumentException("Entity must have a 'logicalName' property.");

            var entity = new Entity(logicalName);

            if (element.TryGetProperty("id", out var idElement))
            {
                var idString = idElement.GetString();
                if (idString != null && Guid.TryParse(idString, out var id))
                    entity.Id = id;
            }

            if (element.TryGetProperty("attributes", out var attrsElement))
            {
                foreach (var attr in attrsElement.EnumerateObject())
                {
                    entity[attr.Name] = ConvertJsonValue(attr.Value);
                }
            }

            return entity;
        }

        private static object? ConvertJsonValue(JsonElement value)
        {
            switch (value.ValueKind)
            {
                case JsonValueKind.String:
                    return value.GetString();
                case JsonValueKind.Number:
                    if (value.TryGetInt32(out var intVal))
                        return intVal;
                    if (value.TryGetInt64(out var longVal))
                        return longVal;
                    return value.GetDecimal();
                case JsonValueKind.True:
                    return true;
                case JsonValueKind.False:
                    return false;
                case JsonValueKind.Null:
                    return null;
                default:
                    return value.ToString();
            }
        }

        private Dictionary<(string EntityName, Guid EntityId, string AttributeName), byte[]> CloneBinaryStore()
        {
            var clone = new Dictionary<(string, Guid, string), byte[]>();
            foreach (var kvp in _binaryStore)
            {
                clone[kvp.Key] = (byte[])kvp.Value.Clone();
            }
            return clone;
        }

        private void RestoreBinaryStore(Dictionary<(string EntityName, Guid EntityId, string AttributeName), byte[]> data)
        {
            _binaryStore.Clear();
            foreach (var kvp in data)
            {
                _binaryStore[kvp.Key] = (byte[])kvp.Value.Clone();
            }
        }

        private EntityCollection EvaluateQueryByAttribute(QueryByAttribute qba)
        {
            var query = new QueryExpression(qba.EntityName)
            {
                ColumnSet = qba.ColumnSet ?? new ColumnSet(true)
            };

            for (int i = 0; i < qba.Attributes.Count; i++)
            {
                if (i < qba.Values.Count)
                    query.Criteria.AddCondition(qba.Attributes[i], ConditionOperator.Equal, qba.Values[i]);
                else
                    query.Criteria.AddCondition(qba.Attributes[i], ConditionOperator.Null);
            }

            foreach (var order in qba.Orders)
                query.Orders.Add(order);

            if (qba.TopCount.HasValue)
                query.TopCount = qba.TopCount;

            if (qba.PageInfo != null)
                query.PageInfo = qba.PageInfo;

            _queryEvaluator.Clock = Clock;
            _queryEvaluator.CallerId = CallerId;
            return _queryEvaluator.Evaluate(query, _store);
        }

        private void RegisterBuiltInHandlers()
        {
            _handlerRegistry.Register(new Handlers.CreateRequestHandler());
            _handlerRegistry.Register(new Handlers.RetrieveRequestHandler());
            _handlerRegistry.Register(new Handlers.RetrieveMultipleRequestHandler());
            _handlerRegistry.Register(new Handlers.UpdateRequestHandler());
            _handlerRegistry.Register(new Handlers.DeleteRequestHandler());
            _handlerRegistry.Register(new Handlers.WhoAmIRequestHandler());
            _handlerRegistry.Register(new Handlers.RetrieveEntityRequestHandler());
            _handlerRegistry.Register(new Handlers.RetrieveAllEntitiesRequestHandler());
            _handlerRegistry.Register(new Handlers.RetrieveAttributeRequestHandler());
            _handlerRegistry.Register(new Handlers.SetStateRequestHandler());
            _handlerRegistry.Register(new Handlers.AssignRequestHandler());
            _handlerRegistry.Register(new Handlers.ExecuteMultipleRequestHandler());
            _handlerRegistry.Register(new Handlers.ExecuteTransactionRequestHandler());
            _handlerRegistry.Register(new Handlers.UpsertRequestHandler());
            _handlerRegistry.Register(new Handlers.GrantAccessRequestHandler(Security));
            _handlerRegistry.Register(new Handlers.ModifyAccessRequestHandler(Security));
            _handlerRegistry.Register(new Handlers.RevokeAccessRequestHandler(Security));
            _handlerRegistry.Register(new Handlers.RetrievePrincipalAccessRequestHandler(Security, _store));
            _handlerRegistry.Register(new Handlers.AddMembersTeamRequestHandler());
            _handlerRegistry.Register(new Handlers.RemoveMembersTeamRequestHandler());
            _handlerRegistry.Register(new Handlers.AddListMembersListRequestHandler());
            _handlerRegistry.Register(new Handlers.RemoveMemberListRequestHandler());
            _handlerRegistry.Register(new Handlers.SendEmailRequestHandler());
            _handlerRegistry.Register(new Handlers.InitializeFromRequestHandler());
            _handlerRegistry.Register(new Handlers.CalculateRollupFieldRequestHandler());
            _handlerRegistry.Register(new Handlers.InitializeFileBlocksUploadRequestHandler());
            _handlerRegistry.Register(new Handlers.UploadBlockRequestHandler());
            _handlerRegistry.Register(new Handlers.CommitFileBlocksUploadRequestHandler());
            _handlerRegistry.Register(new Handlers.CreateMultipleRequestHandler());
            _handlerRegistry.Register(new Handlers.UpdateMultipleRequestHandler());
            _handlerRegistry.Register(new Handlers.RetrieveVersionRequestHandler());
            _handlerRegistry.Register(new Handlers.FetchXmlToQueryExpressionRequestHandler());
            _handlerRegistry.Register(new Handlers.IsValidStateTransitionRequestHandler());
            _handlerRegistry.Register(new Handlers.MergeRequestHandler());
            _handlerRegistry.Register(new Handlers.UpsertMultipleRequestHandler());
            _handlerRegistry.Register(new Handlers.BulkDeleteRequestHandler());
            _handlerRegistry.Register(new Handlers.RetrieveCurrentOrganizationRequestHandler());
            _handlerRegistry.Register(new Handlers.RetrieveOptionSetRequestHandler());
            _handlerRegistry.Register(new Handlers.InsertOptionValueRequestHandler());
            _handlerRegistry.Register(new Handlers.DownloadBlockRequestHandler());
            _handlerRegistry.Register(new Handlers.DeleteFileRequestHandler());
            _handlerRegistry.Register(new Handlers.QualifyLeadRequestHandler());
            _handlerRegistry.Register(new Handlers.CloseIncidentRequestHandler());
            _handlerRegistry.Register(new Handlers.CloseQuoteRequestHandler());
            _handlerRegistry.Register(new Handlers.ReviseQuoteRequestHandler());
            _handlerRegistry.Register(new Handlers.WinOpportunityRequestHandler());
            _handlerRegistry.Register(new Handlers.LoseOpportunityRequestHandler());
            _handlerRegistry.Register(new Handlers.PublishXmlRequestHandler());
            _handlerRegistry.Register(new Handlers.AddToQueueRequestHandler());
            _handlerRegistry.Register(new Handlers.RemoveFromQueueRequestHandler());
            _handlerRegistry.Register(new Handlers.InstantiateTemplateRequestHandler());
            _handlerRegistry.Register(new Handlers.SendEmailFromTemplateRequestHandler());
            _handlerRegistry.Register(new Handlers.SendFaxRequestHandler());
            _handlerRegistry.Register(new Handlers.SendTemplateRequestHandler());
            _handlerRegistry.Register(new Handlers.ExportPdfDocumentRequestHandler());
            _handlerRegistry.Register(new Handlers.GenericCreateRequestHandler());

            // Metadata entity CRUD
            _handlerRegistry.Register(new Handlers.CreateEntityRequestHandler());
            _handlerRegistry.Register(new Handlers.UpdateEntityRequestHandler());
            _handlerRegistry.Register(new Handlers.DeleteEntityRequestHandler());

            // Metadata attribute CRUD
            _handlerRegistry.Register(new Handlers.CreateAttributeRequestHandler());
            _handlerRegistry.Register(new Handlers.UpdateAttributeRequestHandler());
            _handlerRegistry.Register(new Handlers.DeleteAttributeRequestHandler());

            // Relationship CRUD
            _handlerRegistry.Register(new Handlers.CreateOneToManyRequestHandler());
            _handlerRegistry.Register(new Handlers.CreateManyToManyRequestHandler());
            _handlerRegistry.Register(new Handlers.DeleteRelationshipRequestHandler());
            _handlerRegistry.Register(new Handlers.UpdateRelationshipRequestHandler());
            _handlerRegistry.Register(new Handlers.RetrieveRelationshipRequestHandler());

            // Entity key CRUD
            _handlerRegistry.Register(new Handlers.CreateEntityKeyRequestHandler());
            _handlerRegistry.Register(new Handlers.DeleteEntityKeyRequestHandler());
            _handlerRegistry.Register(new Handlers.RetrieveEntityKeyRequestHandler());
            _handlerRegistry.Register(new Handlers.ReactivateEntityKeyRequestHandler());

            // OptionSet CRUD
            _handlerRegistry.Register(new Handlers.CreateOptionSetRequestHandler());
            _handlerRegistry.Register(new Handlers.UpdateOptionSetRequestHandler());
            _handlerRegistry.Register(new Handlers.DeleteOptionSetRequestHandler());
            _handlerRegistry.Register(new Handlers.RetrieveAllOptionSetsRequestHandler());

            // Option value manipulation
            _handlerRegistry.Register(new Handlers.InsertStatusValueRequestHandler());
            _handlerRegistry.Register(new Handlers.DeleteOptionValueRequestHandler());
            _handlerRegistry.Register(new Handlers.UpdateOptionValueRequestHandler());
            _handlerRegistry.Register(new Handlers.OrderOptionRequestHandler());
            _handlerRegistry.Register(new Handlers.UpdateStateValueRequestHandler());

            // Relationship validation
            _handlerRegistry.Register(new Handlers.CanBeReferencedRequestHandler());
            _handlerRegistry.Register(new Handlers.CanBeReferencingRequestHandler());
            _handlerRegistry.Register(new Handlers.CanManyToManyRequestHandler());
            _handlerRegistry.Register(new Handlers.GetValidManyToManyRequestHandler());
            _handlerRegistry.Register(new Handlers.GetValidReferencedEntitiesRequestHandler());
            _handlerRegistry.Register(new Handlers.GetValidReferencingEntitiesRequestHandler());
            _handlerRegistry.Register(new Handlers.CreateCustomerRelationshipsRequestHandler());

            // Metadata query / utility
            _handlerRegistry.Register(new Handlers.RetrieveMetadataChangesRequestHandler());
            _handlerRegistry.Register(new Handlers.RetrieveTimestampRequestHandler());
            _handlerRegistry.Register(new Handlers.RetrieveAllManagedPropertiesRequestHandler());
            _handlerRegistry.Register(new Handlers.RetrieveManagedPropertyRequestHandler());

            // Data encryption
            _handlerRegistry.Register(new Handlers.IsDataEncryptionActiveRequestHandler());
            _handlerRegistry.Register(new Handlers.RetrieveDataEncryptionKeyRequestHandler());
            _handlerRegistry.Register(new Handlers.SetDataEncryptionKeyRequestHandler());

            // Misc
            _handlerRegistry.Register(new Handlers.ConvertDateAndTimeBehaviorRequestHandler());
            _handlerRegistry.Register(new Handlers.ExecuteAsyncRequestHandler());
            _handlerRegistry.Register(new Handlers.RetrieveEntityChangesRequestHandler());
            _handlerRegistry.Register(new Handlers.CreateAsyncJobToRevokeInheritedAccessRequestHandler());
        }

        /// <summary>
        /// Registers a custom API handler that matches requests by <see cref="OrganizationRequest.RequestName"/>.
        /// </summary>
        /// <param name="requestName">The request name to handle.</param>
        /// <param name="handler">A function that processes the request and returns a response.</param>
        public void RegisterCustomApi(string requestName, Func<OrganizationRequest, IOrganizationService, OrganizationResponse> handler)
        {
            _handlerRegistry.Register(new Handlers.CustomApiRequestHandler(requestName, handler));
        }

        private void ResolveAlternateKey(Entity entity)
        {
            if (entity.Id != Guid.Empty) return;
            if (entity.KeyAttributes == null || entity.KeyAttributes.Count == 0) return;

            var found = _store.FindByAlternateKey(entity.LogicalName, entity.KeyAttributes, MetadataStore);
            entity.Id = found;
        }

        /// <summary>
        /// Converts empty string attribute values to null, matching Dataverse behavior.
        /// </summary>
        private static void StripEmptyStrings(Entity entity)
        {
            var keysToNull = new System.Collections.Generic.List<string>();
            foreach (var attr in entity.Attributes)
            {
                if (attr.Value is string s && s.Length == 0)
                    keysToNull.Add(attr.Key);
            }
            foreach (var key in keysToNull)
            {
                entity[key] = null;
            }
        }

        private sealed class FileUploadSession
        {
            internal string EntityName { get; }
            internal Guid EntityId { get; }
            internal string AttributeName { get; }
            internal System.Collections.Generic.List<byte[]> Blocks { get; } = new System.Collections.Generic.List<byte[]>();

            internal FileUploadSession(string entityName, Guid entityId, string attributeName)
            {
                EntityName = entityName;
                EntityId = entityId;
                AttributeName = attributeName;
            }
        }

        private sealed class Snapshot
        {
            internal Dictionary<string, Dictionary<Guid, Entity>> EntityData { get; }
            internal Dictionary<(string EntityName, Guid EntityId, string AttributeName), byte[]> BinaryData { get; }
            internal long VersionCounter { get; }

            internal Snapshot(
                Dictionary<string, Dictionary<Guid, Entity>> entityData,
                Dictionary<(string, Guid, string), byte[]> binaryData,
                long versionCounter)
            {
                EntityData = entityData;
                BinaryData = binaryData;
                VersionCounter = versionCounter;
            }
        }

        private void ApplyCascadeDelete(string entityName, Guid id)
        {
            var childRelationships = MetadataStore.GetChildRelationships(entityName);
            foreach (var rel in childRelationships)
            {
                var childEntities = _store.GetAll(rel.ReferencingEntity);
                foreach (var child in childEntities)
                {
                    var fk = child.GetAttributeValue<EntityReference>(rel.ReferencingAttribute);
                    if (fk == null || fk.Id != id) continue;

                    switch (rel.Cascade.Delete)
                    {
                        case CascadeType.Cascade:
                            Delete(child.LogicalName, child.Id);
                            break;
                        case CascadeType.RemoveLink:
                            var update = new Entity(child.LogicalName, child.Id);
                            update[rel.ReferencingAttribute] = null;
                            _store.Update(update);
                            break;
                        case CascadeType.Restrict:
                            throw DataverseFault.Create(DataverseFault.InvalidArgument,
                                $"Cannot delete '{entityName}' record '{id}' because related '{rel.ReferencingEntity}' records exist (relationship '{rel.SchemaName}' has Restrict delete).");
                    }
                }
            }
        }

        private void ApplyCascadeAssign(string entityName, Guid id, EntityReference newOwner)
        {
            var childRelationships = MetadataStore.GetChildRelationships(entityName);
            foreach (var rel in childRelationships)
            {
                if (rel.Cascade.Assign != CascadeType.Cascade) continue;

                var childEntities = _store.GetAll(rel.ReferencingEntity);
                foreach (var child in childEntities)
                {
                    var fk = child.GetAttributeValue<EntityReference>(rel.ReferencingAttribute);
                    if (fk == null || fk.Id != id) continue;

                    var update = new Entity(child.LogicalName, child.Id);
                    update["ownerid"] = newOwner;
                    _store.Update(update);
                }
            }
        }

        private void PopulateEntityReferenceNames(Entity entity)
        {
            foreach (var attr in entity.Attributes.ToArray())
            {
                if (attr.Value is EntityReference er && er.Id != Guid.Empty && string.IsNullOrEmpty(er.Name))
                {
                    var primaryNameAttr = GetPrimaryNameAttribute(er.LogicalName);
                    if (primaryNameAttr != null && _store.Exists(er.LogicalName, er.Id))
                    {
                        var related = _store.Retrieve(er.LogicalName, er.Id, new ColumnSet(primaryNameAttr));
                        if (related.Contains(primaryNameAttr))
                            er.Name = related.GetAttributeValue<string>(primaryNameAttr);
                    }
                }
            }
        }

        private static void PopulateFormattedValues(Entity entity)
        {
            foreach (var attr in entity.Attributes)
            {
                if (entity.FormattedValues.ContainsKey(attr.Key))
                    continue;

                switch (attr.Value)
                {
                    case OptionSetValue osv:
                        entity.FormattedValues[attr.Key] = osv.Value.ToString(CultureInfo.InvariantCulture);
                        break;
                    case Money money:
                        entity.FormattedValues[attr.Key] = money.Value.ToString("N2", CultureInfo.InvariantCulture);
                        break;
                    case bool b:
                        entity.FormattedValues[attr.Key] = b ? "Yes" : "No";
                        break;
                    case DateTime dt:
                        entity.FormattedValues[attr.Key] = dt.ToString("M/d/yyyy h:mm tt", CultureInfo.InvariantCulture);
                        break;
                }
            }
        }

        private static string? GetPrimaryNameAttribute(string entityName)
        {
            return entityName switch
            {
                "account" => "name",
                "contact" => "fullname",
                "lead" => "fullname",
                "opportunity" => "name",
                "incident" => "title",
                "systemuser" => "fullname",
                "team" => "name",
                "businessunit" => "name",
                _ => "name"
            };
        }

        /// <summary>
        /// Registers a valid status transition for the specified entity.
        /// When transitions are registered, only those transitions will be allowed.
        /// </summary>
        /// <param name="entityName">The logical name of the entity.</param>
        /// <param name="fromStateCode">The source state code.</param>
        /// <param name="fromStatusCode">The source status code.</param>
        /// <param name="toStateCode">The target state code.</param>
        /// <param name="toStatusCode">The target status code.</param>
        public void RegisterStatusTransition(string entityName, int fromStateCode, int fromStatusCode, int toStateCode, int toStatusCode)
        {
            if (!_statusTransitions.TryGetValue(entityName, out var transitions))
            {
                transitions = new List<StatusTransition>();
                _statusTransitions[entityName] = transitions;
            }
            transitions.Add(new StatusTransition(fromStateCode, fromStatusCode, toStateCode, toStatusCode));
        }

        /// <summary>
        /// Registers custom default state/status codes for an entity type.
        /// These are used when <see cref="FakeOrganizationServiceOptions.AutoSetStateCode"/> is enabled.
        /// </summary>
        /// <param name="entityName">The logical name of the entity.</param>
        /// <param name="stateCode">The default state code.</param>
        /// <param name="statusCode">The default status code.</param>
        public void RegisterDefaultStatusCode(string entityName, int stateCode, int statusCode)
        {
            _defaultStatusCodes[entityName] = (stateCode, statusCode);
        }

        /// <summary>
        /// Checks if a state transition is valid based on registered transitions.
        /// Returns <c>true</c> if no transitions are registered (all transitions allowed).
        /// </summary>
        internal bool IsValidTransition(string entityName, int fromState, int fromStatus, int toState, int toStatus)
        {
            if (!_statusTransitions.TryGetValue(entityName, out var transitions) || transitions.Count == 0)
                return true;

            return transitions.Any(t =>
                t.FromStateCode == fromState && t.FromStatusCode == fromStatus &&
                t.ToStateCode == toState && t.ToStatusCode == toStatus);
        }

        private sealed class StatusTransition
        {
            public int FromStateCode { get; }
            public int FromStatusCode { get; }
            public int ToStateCode { get; }
            public int ToStatusCode { get; }

            public StatusTransition(int fromState, int fromStatus, int toState, int toStatus)
            {
                FromStateCode = fromState;
                FromStatusCode = fromStatus;
                ToStateCode = toState;
                ToStatusCode = toStatus;
            }
        }

        private sealed class ServiceScope : IDisposable
        {
            private readonly FakeOrganizationService _service;
            private readonly object _snapshot;
            private bool _disposed;

            internal ServiceScope(FakeOrganizationService service)
            {
                _service = service;
                _snapshot = service.TakeSnapshot();
            }

            public void Dispose()
            {
                if (!_disposed)
                {
                    _service.RestoreSnapshot(_snapshot);
                    _disposed = true;
                }
            }
        }
    }
}
