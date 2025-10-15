'use client';

/**
 * /make route - Solution and table browser
 * Displays solutions which expand to show tables, and provides detailed views of tables, columns, and forms
 * Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/reference/entities/solution
 */

import { useState, useEffect } from 'react';
import { makeStyles, tokens, Spinner } from '@fluentui/react-components';
import MakeNavigation from './components/MakeNavigation';
import TableBrowser from './components/TableBrowser';
import TableDetailView from './components/TableDetailView';
import type { Solution, EntityDefinition } from '../types/dataverse';
import { dataverseClient } from '../lib/dataverse-client';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    height: '100vh',
    overflow: 'hidden',
  },
  main: {
    flex: 1,
    display: 'flex',
    flexDirection: 'column',
    overflow: 'hidden',
  },
  loadingContainer: {
    display: 'flex',
    justifyContent: 'center',
    alignItems: 'center',
    height: '100vh',
    backgroundColor: tokens.colorNeutralBackground1,
  },
  errorContainer: {
    padding: '24px',
    color: tokens.colorPaletteRedForeground1,
    backgroundColor: tokens.colorNeutralBackground1,
  },
});

export default function MakePage() {
  const styles = useStyles();
  const [solutions, setSolutions] = useState<Solution[]>([]);
  const [tables, setTables] = useState<EntityDefinition[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [selectedTable, setSelectedTable] = useState<string | null>(null);
  const [viewMode, setViewMode] = useState<'browser' | 'detail'>('browser');

  useEffect(() => {
    loadData();
  }, []);

  const loadData = async () => {
    try {
      setLoading(true);
      
      // Load solutions
      const solutionsResponse = await dataverseClient.fetchEntities('solutions', {
        select: ['solutionid', 'uniquename', 'friendlyname', 'version', 'ismanaged'],
        orderby: 'friendlyname asc',
      });
      
      setSolutions(solutionsResponse.value as Solution[]);

      // Load all tables (entity definitions)
      const tablesResponse = await dataverseClient.fetchEntityDefinitions({
        select: ['MetadataId', 'LogicalName', 'SchemaName', 'DisplayName', 'DisplayCollectionName', 'EntitySetName', 'IsCustomEntity'],
      });
      
      setTables(tablesResponse.value || []);
      setLoading(false);
    } catch (err) {
      console.error('Error loading make data:', err);
      setError(err instanceof Error ? err.message : 'Failed to load data');
      setLoading(false);
    }
  };

  const handleTableSelect = (logicalName: string) => {
    setSelectedTable(logicalName);
    setViewMode('detail');
  };

  const handleBackToBrowser = () => {
    setViewMode('browser');
    setSelectedTable(null);
  };

  if (loading) {
    return (
      <div className={styles.loadingContainer}>
        <Spinner size="large" label="Loading solutions and tables..." />
      </div>
    );
  }

  if (error) {
    return (
      <div className={styles.errorContainer}>
        <h2>Error</h2>
        <p>{error}</p>
      </div>
    );
  }

  return (
    <div className={styles.container}>
      <MakeNavigation
        solutions={solutions}
        tables={tables}
        selectedTable={selectedTable}
        onTableSelect={handleTableSelect}
      />
      <main className={styles.main}>
        {viewMode === 'browser' && (
          <TableBrowser tables={tables} onTableSelect={handleTableSelect} />
        )}
        {viewMode === 'detail' && selectedTable && (
          <TableDetailView
            logicalName={selectedTable}
            onBack={handleBackToBrowser}
          />
        )}
      </main>
    </div>
  );
}
