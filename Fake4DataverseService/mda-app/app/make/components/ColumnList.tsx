'use client';

/**
 * ColumnList component - Display columns/attributes with expandable details
 * Shows column metadata with property grid for each column
 * Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/webapi/reference/attributemetadata
 */

import { useState } from 'react';
import {
  makeStyles,
  tokens,
  Accordion,
  AccordionItem,
  AccordionHeader,
  AccordionPanel,
  Badge,
} from '@fluentui/react-components';
import type { AttributeMetadata } from '../../types/dataverse';
import PropertyGrid from './PropertyGrid';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    gap: '8px',
  },
  columnHeader: {
    display: 'flex',
    alignItems: 'center',
    gap: '8px',
  },
  badge: {
    marginLeft: '8px',
  },
});

interface ColumnListProps {
  columns: AttributeMetadata[];
}

export default function ColumnList({ columns }: ColumnListProps) {
  const styles = useStyles();

  const getColumnDisplayName = (column: AttributeMetadata): string => {
    return column.DisplayName?.UserLocalizedLabel?.Label || column.SchemaName || column.LogicalName;
  };

  const getColumnProperties = (column: AttributeMetadata): Record<string, string> => {
    return {
      'Logical Name': column.LogicalName,
      'Schema Name': column.SchemaName || '',
      'Display Name': column.DisplayName?.UserLocalizedLabel?.Label || '',
      'Description': column.Description?.UserLocalizedLabel?.Label || '',
      'Attribute Type': column.AttributeTypeName?.Value || column.AttributeType || '',
      'Is Custom': column.IsCustomAttribute ? 'Yes' : 'No',
      'Is Primary ID': column.IsPrimaryId ? 'Yes' : 'No',
      'Is Primary Name': column.IsPrimaryName ? 'Yes' : 'No',
      'Required Level': column.RequiredLevel?.Value || '',
    };
  };

  return (
    <div className={styles.container}>
      <Accordion multiple collapsible>
        {columns.map((column) => (
          <AccordionItem key={column.LogicalName} value={column.LogicalName}>
            <AccordionHeader>
              <div className={styles.columnHeader}>
                <span>{getColumnDisplayName(column)}</span>
                {column.IsCustomAttribute && (
                  <Badge appearance="filled" color="brand" className={styles.badge}>
                    Custom
                  </Badge>
                )}
                {column.IsPrimaryId && (
                  <Badge appearance="filled" color="important" className={styles.badge}>
                    Primary ID
                  </Badge>
                )}
                {column.IsPrimaryName && (
                  <Badge appearance="filled" color="informative" className={styles.badge}>
                    Primary Name
                  </Badge>
                )}
              </div>
            </AccordionHeader>
            <AccordionPanel>
              <PropertyGrid properties={getColumnProperties(column)} />
            </AccordionPanel>
          </AccordionItem>
        ))}
      </Accordion>
    </div>
  );
}
