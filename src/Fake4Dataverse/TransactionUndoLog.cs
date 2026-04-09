using System;
using System.Collections.Generic;
using Microsoft.Xrm.Sdk;

namespace Fake4Dataverse
{
    /// <summary>
    /// Records the inverse of each store mutation made during a transaction
    /// and replays them in reverse order on rollback. This is concurrency-safe:
    /// rolling back one transaction only undoes that transaction's changes,
    /// leaving concurrent transactions' writes intact.
    /// </summary>
    internal sealed class TransactionUndoLog
    {
        private readonly List<UndoAction> _actions = new List<UndoAction>();

        /// <summary>
        /// Records that an entity was created (undo = delete it).
        /// </summary>
        public void RecordCreate(string entityName, Guid id)
        {
            _actions.Add(new UndoAction(UndoType.Create, entityName, id, null));
        }

        /// <summary>
        /// Records the full state of an entity before it is updated (undo = restore that state).
        /// The caller must pass a deep clone.
        /// </summary>
        public void RecordPreUpdateState(Entity beforeState)
        {
            _actions.Add(new UndoAction(UndoType.Update, beforeState.LogicalName, beforeState.Id, beforeState));
        }

        /// <summary>
        /// Records the full state of an entity before it is deleted (undo = re-create it).
        /// The caller must pass a deep clone.
        /// </summary>
        public void RecordPreDeleteState(Entity deletedEntity)
        {
            _actions.Add(new UndoAction(UndoType.Delete, deletedEntity.LogicalName, deletedEntity.Id, deletedEntity));
        }

        /// <summary>
        /// Rolls back all recorded mutations in reverse order.
        /// Uses dedicated rollback methods on the store that bypass undo-log recording.
        /// </summary>
        public void Rollback(InMemoryEntityStore store)
        {
            for (int i = _actions.Count - 1; i >= 0; i--)
            {
                var action = _actions[i];
                switch (action.Type)
                {
                    case UndoType.Create:
                        // Undo a create by removing the entity
                        store.DeleteForRollback(action.EntityName, action.EntityId);
                        break;
                    case UndoType.Update:
                        // Undo an update by restoring the pre-update entity state
                        store.RestoreForRollback(action.EntityState!);
                        break;
                    case UndoType.Delete:
                        // Undo a delete by re-inserting the entity
                        store.CreateForRollback(action.EntityState!);
                        break;
                }
            }
        }

        private enum UndoType { Create, Update, Delete }

        private sealed class UndoAction
        {
            public UndoType Type { get; }
            public string EntityName { get; }
            public Guid EntityId { get; }
            public Entity? EntityState { get; }

            public UndoAction(UndoType type, string entityName, Guid entityId, Entity? entityState)
            {
                Type = type;
                EntityName = entityName;
                EntityId = entityId;
                EntityState = entityState;
            }
        }
    }
}
