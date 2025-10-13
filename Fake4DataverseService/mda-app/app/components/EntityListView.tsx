'use client';

/**
 * Entity list view component
 * Uses Fluent UI DataGrid to display entity records
 * Supports view switching based on SavedQuery (system views)
 * Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/reference/entities/savedquery
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
  Dropdown,
  Option,
} from '@fluentui/react-components';
import {
  ArrowSyncCircle20Regular,
  Filter20Regular,
  Add20Regular,
} from '@fluentui/react-icons';
import { dataverseClient } from '../lib/dataverse-client';
import type { EntityRecord, SavedQuery, ViewColumn } from '../types/dataverse';

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
  titleRow: {
    display: 'flex',
    alignItems: 'center',
    gap: '12px',
    marginBottom: '12px',
  },
  title: {
    fontSize: tokens.fontSizeBase500,
    fontWeight: tokens.fontWeightSemibold,
  },
  viewSwitcher: {
    minWidth: '200px',
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
  appModuleId?: string;
  initialViewId?: string;
}

export default function EntityListView({
  entityName,
  entityPluralName,
  displayName,
  appModuleId,
  initialViewId,
}: EntityListViewProps) {
  const styles = useStyles();
  const [records, setRecords] = useState<EntityRecord[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [columns, setColumns] = useState<TableColumnDefinition<EntityRecord>[]>([]);
  const [views, setViews] = useState<SavedQuery[]>([]);
  const [selectedViewId, setSelectedViewId] = useState<string | null>(initialViewId || null);
  const [viewsLoading, setViewsLoading] = useState(true);

  // Load available views for this entity
  useEffect(() => {
    loadViews();
  }, [entityName, appModuleId]);

  // Load records when view changes
  useEffect(() => {
    if (selectedViewId) {
      loadRecords();
    }
  }, [selectedViewId, entityPluralName]);

  const loadViews = async () => {
    setViewsLoading(true);
    
    try {
      let filter = `returnedtypecode eq '${entityName}'`;
      
      // If appModuleId is provided, filter views by app module
      // We need to query appmodulecomponent to find views associated with this app
      if (appModuleId) {
        try {
          const appModuleComponentsResponse = await dataverseClient.fetchEntities('appmodulecomponents', {
            filter: `appmoduleidunique eq ${appModuleId} and componenttype eq 26`,
            select: ['objectid'],
          });
          
          if (appModuleComponentsResponse.value.length > 0) {
            const viewIds = appModuleComponentsResponse.value
              .map((c: any) => c.objectid)
              .filter((id: any) => id);
            
            if (viewIds.length > 0) {
              // Add filter for specific view IDs
              const viewIdFilters = viewIds.map((id: string) => `savedqueryid eq ${id}`).join(' or ');
              filter = `(${filter}) and (${viewIdFilters})`;
            }
          }
        } catch (err) {
          console.warn('Failed to load app module components, showing all views:', err);
        }
      }
      
      const viewsResponse = await dataverseClient.fetchEntities('savedqueries', {
        filter: filter,
        select: ['savedqueryid', 'name', 'returnedtypecode', 'fetchxml', 'layoutxml', 'isdefault'],
        orderby: 'isdefault desc,name asc',
      });
      
      const loadedViews = viewsResponse.value as SavedQuery[];
      setViews(loadedViews);
      
      // Select initial view, default view, or first view
      if (loadedViews.length > 0) {
        let viewToSelect: SavedQuery | undefined;
        
        if (initialViewId) {
          viewToSelect = loadedViews.find(v => v.savedqueryid === initialViewId);
        }
        
        if (!viewToSelect) {
          viewToSelect = loadedViews.find(v => v.isdefault) || loadedViews[0];
        }
        
        setSelectedViewId(viewToSelect.savedqueryid);
      }
    } catch (err) {
      console.error('Error loading views:', err);
      // Fallback to loading all records without a view
      setViews([]);
      setSelectedViewId(null);
    } finally {
      setViewsLoading(false);
    }
  };

  const loadRecords = async () => {
    setLoading(true);
    setError(null);
    
    try {
      const selectedView = views.find(v => v.savedqueryid === selectedViewId);
      
      // Parse layoutxml to get columns
      let viewColumns: string[] = [];
      if (selectedView?.layoutxml) {
        viewColumns = parseLayoutXml(selectedView.layoutxml);
      }
      
      // Parse FetchXML to get filter and order
      // Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/use-fetchxml-construct-query
      let filterConditions: string | undefined;
      let orderBy: string | undefined;
      
      if (selectedView?.fetchxml) {
        const fetchInfo = parseFetchXml(selectedView.fetchxml);
        filterConditions = fetchInfo.filter;
        orderBy = fetchInfo.orderby;
      }
      
      // Build query parameters
      const queryParams: any = {
        top: 50,
        count: true,
      };
      
      // Use columns from view if available
      if (viewColumns.length > 0) {
        queryParams.select = viewColumns;
      }
      
      // Apply filter from FetchXML
      if (filterConditions) {
        queryParams.filter = filterConditions;
      }
      
      // Apply ordering from FetchXML
      if (orderBy) {
        queryParams.orderby = orderBy;
      }
      
      const response = await dataverseClient.fetchEntities(entityPluralName, queryParams);
      
      setRecords(response.value);
      
      // Generate columns from view layout or first record
      if (response.value.length > 0) {
        const cols: TableColumnDefinition<EntityRecord>[] = [];
        
        // Determine which keys to display
        let displayKeys: string[];
        if (viewColumns.length > 0) {
          displayKeys = viewColumns;
        } else {
          // Fallback: use keys from first record
          const firstRecord = response.value[0];
          const keys = Object.keys(firstRecord).filter(
            key => !key.startsWith('@') && !key.startsWith('_')
          );
          displayKeys = keys.slice(0, 6);
        }
        
        displayKeys.forEach((key) => {
          cols.push(
            createTableColumn<EntityRecord>({
              columnId: key,
              compare: (a, b) => {
                const aVal = String(a[key] || '');
                const bVal = String(b[key] || '');
                return aVal.localeCompare(bVal);
              },
              renderHeaderCell: () => formatColumnName(key),
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

  /**
   * Parse layoutxml to extract column names
   * Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/customize-entity-views
   */
  const parseLayoutXml = (layoutxml: string): string[] => {
    try {
      const parser = new DOMParser();
      const xmlDoc = parser.parseFromString(layoutxml, 'text/xml');
      const cells = xmlDoc.getElementsByTagName('cell');
      const columnNames: string[] = [];
      
      for (let i = 0; i < cells.length; i++) {
        const nameAttr = cells[i].getAttribute('name');
        if (nameAttr) {
          columnNames.push(nameAttr);
        }
      }
      
      return columnNames;
    } catch (err) {
      console.error('Error parsing layoutxml:', err);
      return [];
    }
  };

  /**
   * Parse FetchXML to extract filter conditions and ordering
   * Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/use-fetchxml-construct-query
   * 
   * This is a simplified parser that converts FetchXML to OData query syntax.
   * Supports basic filter conditions and ordering.
   */
  const parseFetchXml = (fetchxml: string): { filter?: string; orderby?: string } => {
    try {
      const parser = new DOMParser();
      const xmlDoc = parser.parseFromString(fetchxml, 'text/xml');
      
      const result: { filter?: string; orderby?: string } = {};
      
      // Parse filter conditions
      const filterElement = xmlDoc.getElementsByTagName('filter')[0];
      if (filterElement) {
        const conditions = filterElement.getElementsByTagName('condition');
        const filterParts: string[] = [];
        
        for (let i = 0; i < conditions.length; i++) {
          const condition = conditions[i];
          const attribute = condition.getAttribute('attribute');
          const operator = condition.getAttribute('operator');
          const value = condition.getAttribute('value');
          
          if (attribute && operator) {
            // Convert FetchXML operator to OData operator
            // Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/fetchxml-operators
            let odataCondition = '';
            
            switch (operator) {
              case 'eq':
                odataCondition = `${attribute} eq ${value !== null ? `'${value}'` : 'null'}`;
                break;
              case 'ne':
                odataCondition = `${attribute} ne ${value !== null ? `'${value}'` : 'null'}`;
                break;
              case 'gt':
                odataCondition = `${attribute} gt ${value}`;
                break;
              case 'ge':
                odataCondition = `${attribute} ge ${value}`;
                break;
              case 'lt':
                odataCondition = `${attribute} lt ${value}`;
                break;
              case 'le':
                odataCondition = `${attribute} le ${value}`;
                break;
              case 'like':
                // OData uses 'contains' for like
                odataCondition = `contains(${attribute}, '${value}')`;
                break;
              case 'not-like':
                odataCondition = `not contains(${attribute}, '${value}')`;
                break;
              case 'null':
                odataCondition = `${attribute} eq null`;
                break;
              case 'not-null':
                odataCondition = `${attribute} ne null`;
                break;
              default:
                console.warn(`Unsupported FetchXML operator: ${operator}`);
            }
            
            if (odataCondition) {
              filterParts.push(odataCondition);
            }
          }
        }
        
        // Combine filters based on type (default is 'and')
        const filterType = filterElement.getAttribute('type') || 'and';
        if (filterParts.length > 0) {
          result.filter = filterParts.join(` ${filterType} `);
        }
      }
      
      // Parse order element
      const orderElements = xmlDoc.getElementsByTagName('order');
      if (orderElements.length > 0) {
        const orderParts: string[] = [];
        
        for (let i = 0; i < orderElements.length; i++) {
          const order = orderElements[i];
          const attribute = order.getAttribute('attribute');
          const descending = order.getAttribute('descending') === 'true';
          
          if (attribute) {
            orderParts.push(`${attribute} ${descending ? 'desc' : 'asc'}`);
          }
        }
        
        if (orderParts.length > 0) {
          result.orderby = orderParts.join(',');
        }
      }
      
      return result;
    } catch (err) {
      console.error('Error parsing FetchXML:', err);
      return {};
    }
  };

  /**
   * Handle view selection change and update URL
   */
  const handleViewChange = (viewId: string) => {
    setSelectedViewId(viewId);
    
    // Update URL with viewid parameter
    if (typeof window !== 'undefined' && appModuleId) {
      const params = new URLSearchParams(window.location.search);
      params.set('viewid', viewId);
      
      const newUrl = `${window.location.pathname}?${params.toString()}`;
      window.history.pushState({}, '', newUrl);
    }
  };
  /**
   * Format column name for display (convert camelCase to Title Case)
   */
  const formatColumnName = (name: string): string => {
    // Handle common patterns
    if (name.endsWith('id')) {
      name = name.substring(0, name.length - 2);
    }
    
    // Convert camelCase to Title Case
    return name
      .replace(/([A-Z])/g, ' $1')
      .replace(/^./, str => str.toUpperCase())
      .trim();
  };

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <div className={styles.titleRow}>
          <div className={styles.title}>
            {displayName || entityName}
          </div>
          {viewsLoading ? (
            <Spinner size="tiny" />
          ) : views.length > 0 && (
            <Dropdown
              className={styles.viewSwitcher}
              value={views.find(v => v.savedqueryid === selectedViewId)?.name || ''}
              selectedOptions={selectedViewId ? [selectedViewId] : []}
              onOptionSelect={(_, data) => handleViewChange(data.optionValue as string)}
              placeholder="Select view"
            >
              {views.map((view) => (
                <Option key={view.savedqueryid} value={view.savedqueryid}>
                  {view.name}
                </Option>
              ))}
            </Dropdown>
          )}
        </div>
        <Toolbar className={styles.toolbar}>
          <ToolbarButton
            icon={<ArrowSyncCircle20Regular />}
            onClick={loadRecords}
            disabled={loading || !selectedViewId}
          >
            Refresh
          </ToolbarButton>
          <ToolbarButton
            icon={<Add20Regular />}
            onClick={() => {
              // Open form for new record
              if (typeof window !== 'undefined') {
                const params = new URLSearchParams(window.location.search);
                params.set('pagetype', 'entityrecord');
                params.set('etn', entityName);
                params.delete('id'); // Ensure no id for new record
                const newUrl = `${window.location.pathname}?${params.toString()}`;
                window.history.pushState({}, '', newUrl);
                // Trigger a custom event to notify the parent
                window.dispatchEvent(new Event('urlchange'));
              }
            }}
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
        
        {!loading && !error && !selectedViewId && views.length === 0 && (
          <div className={styles.emptyContainer}>
            No views available for this entity
          </div>
        )}
        
        {!loading && !error && records.length === 0 && selectedViewId && (
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
                  <DataGridRow<EntityRecord> 
                    key={rowId}
                    onClick={() => {
                      // Open form for this record
                      const recordId = item[entityName + 'id'];
                      if (recordId && typeof window !== 'undefined') {
                        const params = new URLSearchParams(window.location.search);
                        params.set('pagetype', 'entityrecord');
                        params.set('etn', entityName);
                        params.set('id', recordId);
                        const newUrl = `${window.location.pathname}?${params.toString()}`;
                        window.history.pushState({}, '', newUrl);
                        // Trigger a custom event to notify the parent
                        window.dispatchEvent(new Event('urlchange'));
                      }
                    }}
                    style={{ cursor: 'pointer' }}
                  >
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
