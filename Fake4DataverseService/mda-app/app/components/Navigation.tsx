'use client';

/**
 * Navigation component for Model-Driven App
 * Uses Fluent UI Nav component to replicate Power Apps navigation
 * Reference: https://react.fluentui.dev/?path=/docs/components-nav--default
 */

import React from 'react';
import {
  makeStyles,
  tokens,
} from '@fluentui/react-components';
import {
  Nav,
  NavCategory,
  NavCategoryItem,
  NavItem,
} from '@fluentui/react-nav';
import {
  Navigation20Regular,
} from '@fluentui/react-icons';
import type { SiteMapArea, SiteMapGroup, SiteMapSubArea } from '../types/dataverse';
import { getIconComponent } from '../lib/icon-mapping';

const useStyles = makeStyles({
  container: {
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
    display: 'flex',
    alignItems: 'center',
    gap: '8px',
  },
  nav: {
    flex: 1,
    overflow: 'auto',
  },
});

interface NavigationProps {
  areas: SiteMapArea[];
  selectedEntity?: string;
  onNavigate?: (entity: string, clearHistory?: boolean) => void;
}

/**
 * Navigation component using Fluent UI Nav
 * Displays areas as categories and subareas as navigation items
 * Supports icon display from entity metadata
 */
export default function Navigation({ areas, selectedEntity, onNavigate }: NavigationProps) {
  const styles = useStyles();

  const handleNavItemSelect = (entity: string) => {
    if (onNavigate) {
      // Navigation from navbar should clear nav stack
      onNavigate(entity, true);
    }
  };

  // Build the selected value from entity
  const selectedValue = selectedEntity || '';

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <Navigation20Regular />
        <span>Model-Driven App</span>
      </div>
      <Nav
        className={styles.nav}
        selectedValue={selectedValue}
        multiple={true}
      >
        {areas.map((area) => (
          <NavCategory key={area.id} value={area.id}>
            <NavCategoryItem
              icon={getIconComponent(area.icon) ? React.createElement(getIconComponent(area.icon)!) : undefined}
            >
              {area.title}
            </NavCategoryItem>
            {area.groups.map((group) =>
              group.subareas.map((subarea) => {
                const IconComponent = getIconComponent(subarea.icon);
                return (
                  <NavItem
                    key={subarea.id}
                    value={subarea.entity || subarea.id}
                    icon={IconComponent ? React.createElement(IconComponent) : undefined}
                    onClick={() => {
                      if (subarea.entity) {
                        handleNavItemSelect(subarea.entity);
                      }
                    }}
                  >
                    {subarea.title}
                  </NavItem>
                );
              })
            )}
          </NavCategory>
        ))}
      </Nav>
    </div>
  );
}
