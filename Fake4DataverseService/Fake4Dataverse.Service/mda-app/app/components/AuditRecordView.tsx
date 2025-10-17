'use client';

/**
 * Audit Record View component - displays audit history for a single record
 * Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/auditing/overview
 */

import { useState, useEffect } from 'react';
import {
  makeStyles,
  tokens,
  Spinner,
  Button,
  Accordion,
  AccordionItem,
  AccordionHeader,
  AccordionPanel,
  Badge,
  Caption1,
  Body1,
} from '@fluentui/react-components';
import {
  History20Regular,
  ArrowClockwise20Regular,
  ChevronRight20Regular,
} from '@fluentui/react-icons';
import { dataverseClient } from '../lib/dataverse-client';
import type { AuditRecord, AuditDetail } from '../types/dataverse';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    height: '100%',
    backgroundColor: tokens.colorNeutralBackground1,
  },
  header: {
    display: 'flex',
    alignItems: 'center',
    gap: '12px',
    padding: '16px 24px',
    borderBottom: `1px solid ${tokens.colorNeutralStroke1}`,
    backgroundColor: tokens.colorNeutralBackground1,
  },
  title: {
    flex: 1,
    fontSize: tokens.fontSizeBase500,
    fontWeight: tokens.fontWeightSemibold,
  },
  content: {
    flex: 1,
    overflow: 'auto',
    padding: '24px',
  },
  loadingContainer: {
    display: 'flex',
    justifyContent: 'center',
    alignItems: 'center',
    height: '100%',
  },
  errorContainer: {
    padding: '16px',
    color: tokens.colorPaletteRedForeground1,
    backgroundColor: tokens.colorPaletteRedBackground1,
    borderRadius: tokens.borderRadiusMedium,
  },
  emptyContainer: {
    padding: '48px',
    textAlign: 'center' as const,
    color: tokens.colorNeutralForeground2,
  },
  auditItem: {
    marginBottom: '12px',
  },
  auditHeader: {
    display: 'flex',
    alignItems: 'center',
    gap: '12px',
  },
  actionBadge: {
    minWidth: '60px',
  },
  changesList: {
    padding: '12px 0',
  },
  changeItem: {
    display: 'grid',
    gridTemplateColumns: '200px 1fr 1fr',
    gap: '12px',
    padding: '8px',
    borderBottom: `1px solid ${tokens.colorNeutralStroke2}`,
  },
  changeLabel: {
    fontWeight: tokens.fontWeightSemibold,
    color: tokens.colorNeutralForeground2,
  },
  oldValue: {
    color: tokens.colorPaletteRedForeground1,
    textDecoration: 'line-through',
  },
  newValue: {
    color: tokens.colorPaletteGreenForeground1,
  },
});

// Map audit action codes to display names
const ACTION_NAMES: Record<number, string> = {
  1: 'Create',
  2: 'Update',
  3: 'Delete',
  64: 'Access',
  101: 'Assign',
  102: 'Share',
  103: 'Unshare',
  104: 'Merge',
};

// Map action codes to badge colors
const ACTION_COLORS: Record<number, 'success' | 'warning' | 'danger' | 'informative'> = {
  1: 'success',
  2: 'informative',
  3: 'danger',
  64: 'informative',
  101: 'warning',
  102: 'success',
  103: 'warning',
  104: 'danger',
};

interface AuditRecordViewProps {
  entityName: string;
  recordId: string;
}

export default function AuditRecordView({ entityName, recordId }: AuditRecordViewProps) {
  const styles = useStyles();
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [auditRecords, setAuditRecords] = useState<AuditRecord[]>([]);
  const [auditDetails, setAuditDetails] = useState<Map<string, AuditDetail>>(new Map());
  const [loadingDetails, setLoadingDetails] = useState<Set<string>>(new Set());

  useEffect(() => {
    loadAuditHistory();
  }, [entityName, recordId]);

  const loadAuditHistory = async () => {
    setLoading(true);
    setError(null);

    try {
      const response = await dataverseClient.fetchEntityAuditRecords(entityName, recordId);
      setAuditRecords(response.value);
    } catch (err) {
      console.error('Error loading audit history:', err);
      setError(err instanceof Error ? err.message : 'Failed to load audit history');
    } finally {
      setLoading(false);
    }
  };

  const loadAuditDetails = async (auditId: string) => {
    if (auditDetails.has(auditId) || loadingDetails.has(auditId)) {
      return;
    }

    setLoadingDetails(prev => new Set(prev).add(auditId));

    try {
      const details = await dataverseClient.fetchAuditDetails(auditId);
      setAuditDetails(prev => new Map(prev).set(auditId, details));
    } catch (err) {
      console.error('Error loading audit details:', err);
    } finally {
      setLoadingDetails(prev => {
        const next = new Set(prev);
        next.delete(auditId);
        return next;
      });
    }
  };

  const formatDate = (dateString: string) => {
    try {
      const date = new Date(dateString);
      return date.toLocaleString();
    } catch {
      return dateString;
    }
  };

  const formatValue = (value: any): string => {
    if (value === null || value === undefined) {
      return '(empty)';
    }
    if (typeof value === 'object') {
      if (value.logicalName && value.id) {
        // EntityReference
        return value.name || `${value.logicalName} (${value.id.substring(0, 8)}...)`;
      }
      if (value.value !== undefined) {
        // OptionSetValue or Money
        return String(value.value);
      }
      return JSON.stringify(value);
    }
    return String(value);
  };

  const getActionDisplay = (action: number) => {
    return ACTION_NAMES[action] || `Action ${action}`;
  };

  const getActionColor = (action: number) => {
    return ACTION_COLORS[action] || 'informative';
  };

  const getChangedAttributes = (details: AuditDetail): string[] => {
    const oldKeys = Object.keys(details.oldValue || {});
    const newKeys = Object.keys(details.newValue || {});
    const allKeys = new Set([...oldKeys, ...newKeys]);
    
    // Filter out system attributes
    return Array.from(allKeys).filter(key => 
      !key.endsWith('id') && 
      key !== 'statecode' && 
      key !== 'statuscode'
    );
  };

  if (loading) {
    return (
      <div className={styles.container}>
        <div className={styles.header}>
          <History20Regular />
          <div className={styles.title}>Audit History</div>
        </div>
        <div className={styles.loadingContainer}>
          <Spinner label="Loading audit history..." />
        </div>
      </div>
    );
  }

  if (error) {
    return (
      <div className={styles.container}>
        <div className={styles.header}>
          <History20Regular />
          <div className={styles.title}>Audit History</div>
        </div>
        <div className={styles.content}>
          <div className={styles.errorContainer}>
            <h3>Error Loading Audit History</h3>
            <p>{error}</p>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <History20Regular />
        <div className={styles.title}>Audit History</div>
        <Button
          appearance="subtle"
          icon={<ArrowClockwise20Regular />}
          onClick={loadAuditHistory}
        >
          Refresh
        </Button>
      </div>
      <div className={styles.content}>
        {auditRecords.length === 0 ? (
          <div className={styles.emptyContainer}>
            <h3>No Audit Records</h3>
            <p>No audit history found for this record.</p>
          </div>
        ) : (
          <Accordion collapsible>
            {auditRecords.map((record) => {
              const details = auditDetails.get(record.auditid);
              const isLoadingDetails = loadingDetails.has(record.auditid);
              
              return (
                <AccordionItem
                  key={record.auditid}
                  value={record.auditid}
                  className={styles.auditItem}
                >
                  <AccordionHeader
                    expandIconPosition="end"
                    onClick={() => loadAuditDetails(record.auditid)}
                  >
                    <div className={styles.auditHeader}>
                      <Badge
                        appearance="filled"
                        color={getActionColor(record.action)}
                        className={styles.actionBadge}
                      >
                        {getActionDisplay(record.action)}
                      </Badge>
                      <Body1>
                        {record.operation} by {record.userid.name || 'System'}
                      </Body1>
                      <Caption1>{formatDate(record.createdon)}</Caption1>
                    </div>
                  </AccordionHeader>
                  <AccordionPanel>
                    {isLoadingDetails ? (
                      <Spinner size="small" label="Loading details..." />
                    ) : details ? (
                      <div className={styles.changesList}>
                        {getChangedAttributes(details).length === 0 ? (
                          <Caption1>No attribute changes recorded</Caption1>
                        ) : (
                          getChangedAttributes(details).map(attr => (
                            <div key={attr} className={styles.changeItem}>
                              <div className={styles.changeLabel}>{attr}</div>
                              <div className={styles.oldValue}>
                                {formatValue(details.oldValue?.[attr])}
                              </div>
                              <div className={styles.newValue}>
                                {formatValue(details.newValue?.[attr])}
                              </div>
                            </div>
                          ))
                        )}
                      </div>
                    ) : (
                      <Caption1>No details available</Caption1>
                    )}
                  </AccordionPanel>
                </AccordionItem>
              );
            })}
          </Accordion>
        )}
      </div>
    </div>
  );
}
