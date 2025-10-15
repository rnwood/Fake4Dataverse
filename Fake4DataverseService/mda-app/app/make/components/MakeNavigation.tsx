'use client';

/**
 * MakeNavigation component - Solution and table tree navigation
 * Displays solutions (including special "Active" solution) which expand to show tables
 * Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/reference/entities/solution
 */

import {
  makeStyles,
  tokens,
  Tree,
  TreeItem,
  TreeItemLayout,
  TreeItemPersonaLayout,
  TreeOpenChangeData,
  TreeOpenChangeEvent,
} from '@fluentui/react-components';
import {
  Folder20Regular,
  FolderOpen20Regular,
  Table20Regular,
} from '@fluentui/react-icons';
import { useState } from 'react';
import type { Solution, EntityDefinition } from '../../types/dataverse';

const useStyles = makeStyles({
  nav: {
    width: '300px',
    height: '100vh',
    backgroundColor: tokens.colorNeutralBackground3,
    borderRight: `1px solid ${tokens.colorNeutralStroke1}`,
    display: 'flex',
    flexDirection: 'column',
    overflow: 'hidden',
  },
  header: {
    padding: '16px',
    borderBottom: `1px solid ${tokens.colorNeutralStroke1}`,
    fontWeight: tokens.fontWeightSemibold,
    fontSize: tokens.fontSizeBase400,
  },
  scrollArea: {
    flex: 1,
    overflow: 'auto',
    padding: '8px',
  },
  tableItem: {
    cursor: 'pointer',
  },
  selectedTableItem: {
    backgroundColor: tokens.colorNeutralBackground1Selected,
  },
});

interface MakeNavigationProps {
  solutions: Solution[];
  tables: EntityDefinition[];
  selectedTable: string | null;
  onTableSelect: (logicalName: string) => void;
}

// Microsoft's Active solution ID
// Reference: https://learn.microsoft.com/en-us/power-platform/alm/solution-concepts-alm#active-solution
const ACTIVE_SOLUTION_ID = 'FD140AAF-4DF4-11DD-BD17-0019B9312238';

export default function MakeNavigation({
  solutions,
  tables,
  selectedTable,
  onTableSelect,
}: MakeNavigationProps) {
  const styles = useStyles();
  const [openItems, setOpenItems] = useState<Set<string>>(new Set());

  const handleOpenChange = (_event: TreeOpenChangeEvent, data: TreeOpenChangeData) => {
    setOpenItems(data.openItems as Set<string>);
  };

  const handleTableClick = (logicalName: string) => {
    onTableSelect(logicalName);
  };

  // Create the Active solution (special solution containing all components)
  const activeSolution: Solution = {
    solutionid: ACTIVE_SOLUTION_ID.toLowerCase(),
    uniquename: 'Active',
    friendlyname: 'Active',
    ismanaged: false,
    isvisible: true,
  };

  // Combine Active solution with other solutions
  const allSolutions = [activeSolution, ...solutions];

  const getTableDisplayName = (table: EntityDefinition): string => {
    return table.DisplayName?.UserLocalizedLabel?.Label || table.SchemaName || table.LogicalName;
  };

  return (
    <div className={styles.nav}>
      <div className={styles.header}>Solutions & Tables</div>
      <div className={styles.scrollArea}>
        <Tree
          aria-label="Solutions and tables"
          openItems={openItems}
          onOpenChange={handleOpenChange}
        >
          {allSolutions.map((solution) => (
            <TreeItem
              key={solution.solutionid}
              itemType="branch"
              value={solution.solutionid}
            >
              <TreeItemLayout
                iconBefore={
                  openItems.has(solution.solutionid) ? (
                    <FolderOpen20Regular />
                  ) : (
                    <Folder20Regular />
                  )
                }
              >
                {solution.friendlyname || solution.uniquename}
                {solution.uniquename === 'Active' && ' (All Components)'}
              </TreeItemLayout>
              <Tree>
                {tables.map((table) => (
                  <TreeItem
                    key={`${solution.solutionid}-${table.LogicalName}`}
                    itemType="leaf"
                    value={`${solution.solutionid}-${table.LogicalName}`}
                  >
                    <TreeItemLayout
                      iconBefore={<Table20Regular />}
                      className={
                        selectedTable === table.LogicalName
                          ? `${styles.tableItem} ${styles.selectedTableItem}`
                          : styles.tableItem
                      }
                      onClick={() => handleTableClick(table.LogicalName)}
                    >
                      {getTableDisplayName(table)}
                    </TreeItemLayout>
                  </TreeItem>
                ))}
              </Tree>
            </TreeItem>
          ))}
        </Tree>
      </div>
    </div>
  );
}
