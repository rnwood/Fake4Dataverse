'use client';

/**
 * PropertyGrid component - Display properties in a grid format
 * Shows key-value pairs in a structured table layout
 */

import {
  makeStyles,
  tokens,
  Table,
  TableBody,
  TableCell,
  TableRow,
  TableHeader,
  TableHeaderCell,
} from '@fluentui/react-components';

const useStyles = makeStyles({
  tableContainer: {
    border: `1px solid ${tokens.colorNeutralStroke1}`,
    borderRadius: tokens.borderRadiusMedium,
    overflow: 'auto',
  },
  propertyName: {
    fontWeight: tokens.fontWeightSemibold,
    width: '40%',
    backgroundColor: tokens.colorNeutralBackground3,
  },
  propertyValue: {
    width: '60%',
  },
  emptyValue: {
    fontStyle: 'italic',
    color: tokens.colorNeutralForeground3,
  },
});

interface PropertyGridProps {
  properties: Record<string, string>;
}

export default function PropertyGrid({ properties }: PropertyGridProps) {
  const styles = useStyles();

  return (
    <div className={styles.tableContainer}>
      <Table size="small">
        <TableHeader>
          <TableRow>
            <TableHeaderCell className={styles.propertyName}>Property</TableHeaderCell>
            <TableHeaderCell className={styles.propertyValue}>Value</TableHeaderCell>
          </TableRow>
        </TableHeader>
        <TableBody>
          {Object.entries(properties).map(([key, value]) => (
            <TableRow key={key}>
              <TableCell className={styles.propertyName}>{key}</TableCell>
              <TableCell className={styles.propertyValue}>
                {value || <span className={styles.emptyValue}>(empty)</span>}
              </TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>
    </div>
  );
}
