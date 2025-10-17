'use client';

/**
 * Audit Summary View component - displays global audit history
 * Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/auditing/overview
 */

import { useState, useEffect } from 'react';
import {
  makeStyles,
  tokens,
  Spinner,
  Button,
  Table,
  TableBody,
  TableCell,
  TableRow,
  TableHeader,
  TableHeaderCell,
  Switch,
  Label,
  Caption1,
  Badge,
} from '@fluentui/react-components';
import {
  History20Regular,
  ArrowClockwise20Regular,
} from '@fluentui/react-icons';
import { dataverseClient } from '../lib/dataverse-client';
import type { AuditRecord } from '../types/dataverse';

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
  statusSection: {
    display: 'flex',
    alignItems: 'center',
    gap: '12px',
    marginBottom: '24px',
    padding: '16px',
    backgroundColor: tokens.colorNeutralBackground2,
    borderRadius: tokens.borderRadiusMedium,
  },
  emptyContainer: {
    padding: '48px',
    textAlign: 'center' as const,
    color: tokens.colorNeutralForeground2,
  },
  actionBadge: {
    minWidth: '60px',
  },
});

// Map audit action codes to display names
// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/auditing/overview
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
  1: 'success',    // Create - green
  2: 'informative', // Update - blue
  3: 'danger',     // Delete - red
  64: 'informative', // Access - blue
  101: 'warning',   // Assign - yellow
  102: 'success',   // Share - green
  103: 'warning',   // Unshare - yellow
  104: 'danger',    // Merge - red
};

export default function AuditSummaryView() {
  const styles = useStyles();
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [auditRecords, setAuditRecords] = useState<AuditRecord[]>([]);
  const [isAuditEnabled, setIsAuditEnabled] = useState(false);
  const [togglingAudit, setTogglingAudit] = useState(false);

  useEffect(() => {
    loadAuditData();
  }, []);

  const loadAuditData = async () => {
    setLoading(true);
    setError(null);

    try {
      // Fetch audit status
      const status = await dataverseClient.fetchAuditStatus();
      setIsAuditEnabled(status.isAuditEnabled);

      // Fetch audit records
      const response = await dataverseClient.fetchAuditRecords({
        top: 100,
        orderby: 'createdon desc',
      });
      setAuditRecords(response.value);
    } catch (err) {
      console.error('Error loading audit data:', err);
      setError(err instanceof Error ? err.message : 'Failed to load audit data');
    } finally {
      setLoading(false);
    }
  };

  const handleToggleAudit = async (enabled: boolean) => {
    setTogglingAudit(true);
    try {
      await dataverseClient.setAuditStatus(enabled);
      setIsAuditEnabled(enabled);
    } catch (err) {
      console.error('Error toggling audit:', err);
      setError(err instanceof Error ? err.message : 'Failed to toggle audit');
    } finally {
      setTogglingAudit(false);
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

  const getActionDisplay = (action: number) => {
    return ACTION_NAMES[action] || `Action ${action}`;
  };

  const getActionColor = (action: number) => {
    return ACTION_COLORS[action] || 'informative';
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
          onClick={loadAuditData}
        >
          Refresh
        </Button>
      </div>
      <div className={styles.content}>
        <div className={styles.statusSection}>
          <Label htmlFor="audit-toggle">
            Auditing {isAuditEnabled ? 'Enabled' : 'Disabled'}
          </Label>
          <Switch
            id="audit-toggle"
            checked={isAuditEnabled}
            disabled={togglingAudit}
            onChange={(_, data) => handleToggleAudit(data.checked)}
          />
          <Caption1>
            When enabled, all Create, Update, and Delete operations are tracked
          </Caption1>
        </div>

        {auditRecords.length === 0 ? (
          <div className={styles.emptyContainer}>
            <h3>No Audit Records</h3>
            <p>
              {isAuditEnabled
                ? 'No audit records have been created yet. Perform some Create, Update, or Delete operations to see them here.'
                : 'Auditing is disabled. Enable auditing to start tracking changes.'}
            </p>
          </div>
        ) : (
          <Table aria-label="Audit history table">
            <TableHeader>
              <TableRow>
                <TableHeaderCell>Action</TableHeaderCell>
                <TableHeaderCell>Entity</TableHeaderCell>
                <TableHeaderCell>Record ID</TableHeaderCell>
                <TableHeaderCell>User</TableHeaderCell>
                <TableHeaderCell>Date</TableHeaderCell>
              </TableRow>
            </TableHeader>
            <TableBody>
              {auditRecords.map((record) => (
                <TableRow key={record.auditid}>
                  <TableCell>
                    <Badge
                      appearance="filled"
                      color={getActionColor(record.action)}
                      className={styles.actionBadge}
                    >
                      {getActionDisplay(record.action)}
                    </Badge>
                  </TableCell>
                  <TableCell>{record.objecttypecode}</TableCell>
                  <TableCell>
                    <Caption1>{record.objectid.id.substring(0, 8)}...</Caption1>
                  </TableCell>
                  <TableCell>
                    {record.userid.name || record.userid.id.substring(0, 8) + '...'}
                  </TableCell>
                  <TableCell>{formatDate(record.createdon)}</TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        )}
      </div>
    </div>
  );
}
