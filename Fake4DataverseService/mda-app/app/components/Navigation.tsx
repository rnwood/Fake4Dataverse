'use client';

/**
 * Navigation component for Model-Driven App
 * Uses Fluent UI components to replicate Power Apps navigation
 */

import {
  makeStyles,
  tokens,
  Button,
  Tree,
  TreeItem,
  TreeItemLayout,
  Divider,
} from '@fluentui/react-components';
import {
  Navigation20Regular,
  Home20Regular,
  ChevronRight20Regular,
  Settings20Regular,
} from '@fluentui/react-icons';
import type { SiteMapArea, SiteMapGroup, SiteMapSubArea } from '../types/dataverse';

const useStyles = makeStyles({
  nav: {
    width: '250px',
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
  areaTitle: {
    padding: '8px 12px',
    fontWeight: tokens.fontWeightSemibold,
    fontSize: tokens.fontSizeBase300,
    color: tokens.colorNeutralForeground2,
    textTransform: 'uppercase' as const,
    letterSpacing: '0.5px',
  },
  groupTitle: {
    padding: '4px 12px',
    fontWeight: tokens.fontWeightSemibold,
    fontSize: tokens.fontSizeBase200,
    color: tokens.colorNeutralForeground3,
  },
  subAreaItem: {
    cursor: 'pointer',
    '&:hover': {
      backgroundColor: tokens.colorNeutralBackground1Hover,
    },
  },
  selectedSubArea: {
    backgroundColor: tokens.colorNeutralBackground1Selected,
  },
  makeButton: {
    width: '100%',
    justifyContent: 'flex-start',
    marginBottom: '8px',
  },
  divider: {
    marginBottom: '8px',
  },
});

interface NavigationProps {
  areas: SiteMapArea[];
  selectedEntity?: string;
  onNavigate?: (entity: string) => void;
}

export default function Navigation({ areas, selectedEntity, onNavigate }: NavigationProps) {
  const styles = useStyles();

  const handleSubAreaClick = (subarea: SiteMapSubArea) => {
    if (subarea.entity && onNavigate) {
      onNavigate(subarea.entity);
    }
  };

  return (
    <div className={styles.nav}>
      <div className={styles.header}>
        <Navigation20Regular /> Model-Driven App
      </div>
      <div className={styles.scrollArea}>
        <Button
          appearance="subtle"
          icon={<Settings20Regular />}
          className={styles.makeButton}
          onClick={() => {
            if (typeof window !== 'undefined') {
              window.location.href = '/make';
            }
          }}
        >
          Solutions & Tables
        </Button>
        <Divider className={styles.divider} />
        {areas.map((area) => (
          <div key={area.id}>
            <div className={styles.areaTitle}>
              {area.title}
            </div>
            {area.groups.map((group) => (
              <div key={group.id}>
                <div className={styles.groupTitle}>
                  {group.title}
                </div>
                <Tree aria-label={group.title}>
                  {group.subareas.map((subarea) => (
                    <TreeItem
                      key={subarea.id}
                      itemType="leaf"
                      value={subarea.id}
                      className={
                        subarea.entity === selectedEntity
                          ? `${styles.subAreaItem} ${styles.selectedSubArea}`
                          : styles.subAreaItem
                      }
                    >
                      <TreeItemLayout
                        onClick={() => handleSubAreaClick(subarea)}
                      >
                        {subarea.title}
                      </TreeItemLayout>
                    </TreeItem>
                  ))}
                </Tree>
              </div>
            ))}
          </div>
        ))}
      </div>
    </div>
  );
}
