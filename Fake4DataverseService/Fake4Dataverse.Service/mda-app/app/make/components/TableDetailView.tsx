'use client';

/**
 * TableDetailView component - Detailed view of a table with properties, columns, and forms
 * Shows table metadata in property grid format with tabs for different aspects
 * Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/webapi/reference/entitymetadata
 */

import { useState, useEffect } from 'react';
import {
  makeStyles,
  tokens,
  Button,
  Spinner,
  TabList,
  Tab,
} from '@fluentui/react-components';
import { ArrowLeft20Regular } from '@fluentui/react-icons';
import type { EntityDefinition, AttributeMetadata, SystemForm } from '../../types/dataverse';
import { dataverseClient } from '../../lib/dataverse-client';
import PropertyGrid from './PropertyGrid';
import ColumnList from './ColumnList';
import FormList from './FormList';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    height: '100%',
    overflow: 'hidden',
  },
  toolbar: {
    display: 'flex',
    alignItems: 'center',
    gap: '12px',
    padding: '16px',
    borderBottom: `1px solid ${tokens.colorNeutralStroke1}`,
  },
  title: {
    fontSize: tokens.fontSizeBase500,
    fontWeight: tokens.fontWeightSemibold,
  },
  tabs: {
    padding: '0 16px',
    borderBottom: `1px solid ${tokens.colorNeutralStroke1}`,
  },
  content: {
    flex: 1,
    overflow: 'auto',
    padding: '16px',
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
});

interface TableDetailViewProps {
  logicalName: string;
  onBack: () => void;
}

export default function TableDetailView({ logicalName, onBack }: TableDetailViewProps) {
  const styles = useStyles();
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [entityMetadata, setEntityMetadata] = useState<EntityDefinition | null>(null);
  const [columns, setColumns] = useState<AttributeMetadata[]>([]);
  const [forms, setForms] = useState<SystemForm[]>([]);
  const [selectedTab, setSelectedTab] = useState<string>('properties');

  useEffect(() => {
    loadTableDetails();
  }, [logicalName]);

  const loadTableDetails = async () => {
    try {
      setLoading(true);
      setError(null);

      // Load entity metadata with attributes (columns)
      const metadata = await dataverseClient.fetchEntityDefinition(logicalName, {
        expand: ['Attributes'],
      });
      
      setEntityMetadata(metadata);
      setColumns(metadata.Attributes || []);

      // Load forms for this entity
      const formsResponse = await dataverseClient.fetchEntities('systemforms', {
        filter: `objecttypecode eq '${logicalName}'`,
        select: ['formid', 'name', 'type', 'description', 'isdefault'],
        orderby: 'name asc',
      });
      
      setForms(formsResponse.value as SystemForm[]);
      setLoading(false);
    } catch (err) {
      console.error('Error loading table details:', err);
      setError(err instanceof Error ? err.message : 'Failed to load table details');
      setLoading(false);
    }
  };

  const getTableDisplayName = (): string => {
    if (!entityMetadata) return logicalName;
    return entityMetadata.DisplayName?.UserLocalizedLabel?.Label || entityMetadata.SchemaName || logicalName;
  };

  const getTableProperties = (): Record<string, string> => {
    if (!entityMetadata) return {};
    
    return {
      'Logical Name': entityMetadata.LogicalName || '',
      'Schema Name': entityMetadata.SchemaName || '',
      'Display Name': entityMetadata.DisplayName?.UserLocalizedLabel?.Label || '',
      'Plural Name': entityMetadata.DisplayCollectionName?.UserLocalizedLabel?.Label || '',
      'Description': entityMetadata.Description?.UserLocalizedLabel?.Label || '',
      'Entity Set Name': entityMetadata.EntitySetName || '',
      'Primary ID Attribute': entityMetadata.PrimaryIdAttribute || '',
      'Primary Name Attribute': entityMetadata.PrimaryNameAttribute || '',
      'Object Type Code': entityMetadata.ObjectTypeCode?.toString() || '',
      'Is Custom Entity': entityMetadata.IsCustomEntity ? 'Yes' : 'No',
      'Is Activity': entityMetadata.IsActivity ? 'Yes' : 'No',
      'Ownership Type': entityMetadata.OwnershipType || '',
    };
  };

  if (loading) {
    return (
      <div className={styles.loadingContainer}>
        <Spinner size="large" label="Loading table details..." />
      </div>
    );
  }

  if (error) {
    return (
      <div className={styles.errorContainer}>
        <h2>Error</h2>
        <p>{error}</p>
        <Button onClick={onBack}>Back to Tables</Button>
      </div>
    );
  }

  return (
    <div className={styles.container}>
      <div className={styles.toolbar}>
        <Button
          icon={<ArrowLeft20Regular />}
          appearance="subtle"
          onClick={onBack}
        >
          Back
        </Button>
        <div className={styles.title}>{getTableDisplayName()}</div>
      </div>
      
      <div className={styles.tabs}>
        <TabList selectedValue={selectedTab} onTabSelect={(_, data) => setSelectedTab(data.value as string)}>
          <Tab value="properties">Properties</Tab>
          <Tab value="columns">Columns ({columns.length})</Tab>
          <Tab value="forms">Forms ({forms.length})</Tab>
        </TabList>
      </div>

      <div className={styles.content}>
        {selectedTab === 'properties' && (
          <PropertyGrid properties={getTableProperties()} />
        )}
        {selectedTab === 'columns' && (
          <ColumnList columns={columns} />
        )}
        {selectedTab === 'forms' && (
          <FormList forms={forms} />
        )}
      </div>
    </div>
  );
}
