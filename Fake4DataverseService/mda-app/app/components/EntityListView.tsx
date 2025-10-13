'use client';

/**
 * Entity list view component
 * Uses Fluent UI DataGrid to display entity records
 */

import { useState, useEffect } from 'react';
import {
  makeStyles,
  tokens,
  DataGrid,
  DataGridBody,
  DataGridRow,
  DataGridHeader,
  DataGridHeaderCell,
  DataGridCell,
  TableColumnDefinition,
  createTableColumn,
  Spinner,
  Button,
  Input,
  Toolbar,
  ToolbarButton,
  Caption1,
} from '@fluentui/react-components';
import {
  ArrowSyncCircle20Regular,
  Filter20Regular,
  Add20Regular,
} from '@fluentui/react-icons';
import { dataverseClient } from '../lib/dataverse-client';
import type { EntityRecord } from '../types/dataverse';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    height: '100%',
    backgroundColor: tokens.colorNeutralBackground1,
  },
  header: {
    padding: '16px 24px',
    borderBottom: `1px solid ${tokens.colorNeutralStroke1}`,
    backgroundColor: tokens.colorNeutralBackground1,
  },
  title: {
    fontSize: tokens.fontSizeBase500,
    fontWeight: tokens.fontWeightSemibold,
    marginBottom: '12px',
  },
  toolbar: {
    marginBottom: '8px',
  },
  content: {
    flex: 1,
    overflow: 'auto',
    padding: '16px 24px',
  },
  loadingContainer: {
    display: 'flex',
    justifyContent: 'center',
    alignItems: 'center',
    height: '100%',
  },
  errorContainer: {
    padding: '24px',
    color: tokens.colorPaletteRedForeground1,
  },
  emptyContainer: {
    padding: '24px',
    textAlign: 'center' as const,
    color: tokens.colorNeutralForeground3,
  },
  dataGrid: {
    minWidth: '100%',
  },
  recordCount: {
    marginTop: '12px',
    color: tokens.colorNeutralForeground3,
  },
});

interface EntityListViewProps {
  entityName: string;
  entityPluralName: string;
  displayName?: string;
}

export default function EntityListView({
  entityName,
  entityPluralName,
  displayName,
}: EntityListViewProps) {
  const styles = useStyles();
  const [records, setRecords] = useState<EntityRecord[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [columns, setColumns] = useState<TableColumnDefinition<EntityRecord>[]>([]);

  const loadRecords = async () => {
    setLoading(true);
    setError(null);
    
    try {
      const response = await dataverseClient.fetchEntities(entityPluralName, {
        top: 50,
        count: true,
      });
      
      setRecords(response.value);
      
      // Generate columns from first record
      if (response.value.length > 0) {
        const firstRecord = response.value[0];
        const cols: TableColumnDefinition<EntityRecord>[] = [];
        
        // Get all keys except OData metadata keys
        const keys = Object.keys(firstRecord).filter(
          key => !key.startsWith('@') && !key.startsWith('_')
        );
        
        // Limit to first 6 columns for readability
        const displayKeys = keys.slice(0, 6);
        
        displayKeys.forEach((key) => {
          cols.push(
            createTableColumn<EntityRecord>({
              columnId: key,
              compare: (a, b) => {
                const aVal = String(a[key] || '');
                const bVal = String(b[key] || '');
                return aVal.localeCompare(bVal);
              },
              renderHeaderCell: () => key,
              renderCell: (item) => {
                const value = item[key];
                if (value === null || value === undefined) return '';
                if (typeof value === 'object') {
                  // Handle EntityReference, Money, OptionSetValue
                  if (value.Name) return value.Name;
                  if (value.Value !== undefined) return String(value.Value);
                  return JSON.stringify(value);
                }
                return String(value);
              },
            })
          );
        });
        
        setColumns(cols);
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load records');
      console.error('Error loading records:', err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadRecords();
  }, [entityPluralName]);

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <div className={styles.title}>
          {displayName || entityName}
        </div>
        <Toolbar className={styles.toolbar}>
          <ToolbarButton
            icon={<ArrowSyncCircle20Regular />}
            onClick={loadRecords}
          >
            Refresh
          </ToolbarButton>
          <ToolbarButton
            icon={<Add20Regular />}
            disabled
          >
            New
          </ToolbarButton>
          <ToolbarButton
            icon={<Filter20Regular />}
            disabled
          >
            Filter
          </ToolbarButton>
        </Toolbar>
      </div>
      
      <div className={styles.content}>
        {loading && (
          <div className={styles.loadingContainer}>
            <Spinner label="Loading records..." />
          </div>
        )}
        
        {error && !loading && (
          <div className={styles.errorContainer}>
            <strong>Error:</strong> {error}
          </div>
        )}
        
        {!loading && !error && records.length === 0 && (
          <div className={styles.emptyContainer}>
            No records found
          </div>
        )}
        
        {!loading && !error && records.length > 0 && (
          <>
            <DataGrid
              items={records}
              columns={columns}
              sortable
              getRowId={(item) => item[entityName + 'id'] || JSON.stringify(item)}
              className={styles.dataGrid}
            >
              <DataGridHeader>
                <DataGridRow>
                  {({ renderHeaderCell }) => (
                    <DataGridHeaderCell>
                      {renderHeaderCell()}
                    </DataGridHeaderCell>
                  )}
                </DataGridRow>
              </DataGridHeader>
              <DataGridBody<EntityRecord>>
                {({ item, rowId }) => (
                  <DataGridRow<EntityRecord> key={rowId}>
                    {({ renderCell }) => (
                      <DataGridCell>{renderCell(item)}</DataGridCell>
                    )}
                  </DataGridRow>
                )}
              </DataGridBody>
            </DataGrid>
            <Caption1 className={styles.recordCount}>
              Showing {records.length} records
            </Caption1>
          </>
        )}
      </div>
    </div>
  );
}
