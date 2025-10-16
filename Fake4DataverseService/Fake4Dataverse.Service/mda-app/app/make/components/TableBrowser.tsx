'use client';

/**
 * TableBrowser component - Searchable grid of tables
 * Displays all tables in a searchable data grid
 * Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/webapi/reference/entitymetadata
 */

import { useState, useMemo } from 'react';
import {
  makeStyles,
  tokens,
  Input,
  Table,
  TableBody,
  TableCell,
  TableRow,
  TableHeader,
  TableHeaderCell,
} from '@fluentui/react-components';
import { Search20Regular } from '@fluentui/react-icons';
import type { EntityDefinition } from '../../types/dataverse';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    height: '100%',
    padding: '16px',
    overflow: 'hidden',
  },
  header: {
    marginBottom: '16px',
  },
  title: {
    fontSize: tokens.fontSizeBase500,
    fontWeight: tokens.fontWeightSemibold,
    marginBottom: '8px',
  },
  searchBox: {
    maxWidth: '400px',
  },
  tableContainer: {
    flex: 1,
    overflow: 'auto',
    border: `1px solid ${tokens.colorNeutralStroke1}`,
    borderRadius: tokens.borderRadiusMedium,
  },
  tableRow: {
    cursor: 'pointer',
    '&:hover': {
      backgroundColor: tokens.colorNeutralBackground1Hover,
    },
  },
});

interface TableBrowserProps {
  tables: EntityDefinition[];
  onTableSelect: (logicalName: string) => void;
}

export default function TableBrowser({ tables, onTableSelect }: TableBrowserProps) {
  const styles = useStyles();
  const [searchQuery, setSearchQuery] = useState('');

  const filteredTables = useMemo(() => {
    if (!searchQuery.trim()) {
      return tables;
    }

    const query = searchQuery.toLowerCase();
    return tables.filter((table) => {
      const displayName = table.DisplayName?.UserLocalizedLabel?.Label?.toLowerCase() || '';
      const logicalName = table.LogicalName.toLowerCase();
      const schemaName = table.SchemaName?.toLowerCase() || '';
      
      return (
        displayName.includes(query) ||
        logicalName.includes(query) ||
        schemaName.includes(query)
      );
    });
  }, [tables, searchQuery]);

  const getTableDisplayName = (table: EntityDefinition): string => {
    return table.DisplayName?.UserLocalizedLabel?.Label || table.SchemaName || table.LogicalName;
  };

  const getTablePluralName = (table: EntityDefinition): string => {
    return table.DisplayCollectionName?.UserLocalizedLabel?.Label || '';
  };

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <div className={styles.title}>Tables</div>
        <Input
          className={styles.searchBox}
          contentBefore={<Search20Regular />}
          placeholder="Search tables..."
          value={searchQuery}
          onChange={(e, data) => setSearchQuery(data.value)}
        />
      </div>
      <div className={styles.tableContainer}>
        <Table size="small">
          <TableHeader>
            <TableRow>
              <TableHeaderCell>Display Name</TableHeaderCell>
              <TableHeaderCell>Logical Name</TableHeaderCell>
              <TableHeaderCell>Schema Name</TableHeaderCell>
              <TableHeaderCell>Plural Name</TableHeaderCell>
              <TableHeaderCell>Custom</TableHeaderCell>
            </TableRow>
          </TableHeader>
          <TableBody>
            {filteredTables.map((table) => (
              <TableRow
                key={table.LogicalName}
                className={styles.tableRow}
                onClick={() => onTableSelect(table.LogicalName)}
              >
                <TableCell>{getTableDisplayName(table)}</TableCell>
                <TableCell>{table.LogicalName}</TableCell>
                <TableCell>{table.SchemaName || ''}</TableCell>
                <TableCell>{getTablePluralName(table)}</TableCell>
                <TableCell>{table.IsCustomEntity ? 'Yes' : 'No'}</TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </div>
    </div>
  );
}
