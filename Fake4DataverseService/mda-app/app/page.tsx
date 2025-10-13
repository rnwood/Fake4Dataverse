'use client';

/**
 * Main Model-Driven App page
 * Displays navigation and entity list views based on sitemap
 */

import { useState, useEffect } from 'react';
import { makeStyles, tokens, Spinner } from '@fluentui/react-components';
import Navigation from './components/Navigation';
import EntityListView from './components/EntityListView';
import { dataverseClient } from './lib/dataverse-client';
import { parseSiteMapXml } from './lib/sitemap-utils';
import type { SiteMapDefinition, AppModule, SiteMap } from './types/dataverse';

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
  welcomeContainer: {
    padding: '24px',
    backgroundColor: tokens.colorNeutralBackground1,
  },
});

// Entity plural name mapping
const ENTITY_PLURAL_NAMES: Record<string, string> = {
  'account': 'accounts',
  'contact': 'contacts',
  'opportunity': 'opportunities',
  'lead': 'leads',
  'incident': 'incidents',
  'knowledgearticle': 'knowledgearticles',
  'systemuser': 'systemusers',
  'team': 'teams',
};

// Entity display names
const ENTITY_DISPLAY_NAMES: Record<string, string> = {
  'account': 'Accounts',
  'contact': 'Contacts',
  'opportunity': 'Opportunities',
  'lead': 'Leads',
  'incident': 'Cases',
  'knowledgearticle': 'Knowledge Articles',
  'systemuser': 'Users',
  'team': 'Teams',
};

export default function Home() {
  const styles = useStyles();
  const [sitemap, setSitemap] = useState<SiteMapDefinition | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [selectedEntity, setSelectedEntity] = useState<string | null>(null);
  const [appModuleId, setAppModuleId] = useState<string | null>(null);

  useEffect(() => {
    loadSitemap();
  }, []);

  const loadSitemap = async () => {
    setLoading(true);
    setError(null);

    try {
      // Try to load sitemap from appmodule
      const appModulesResponse = await dataverseClient.fetchEntities('appmodules', {
        top: 1,
        select: ['appmoduleid', 'name'],
      });

      if (appModulesResponse.value.length > 0) {
        const appModule = appModulesResponse.value[0] as AppModule;
        setAppModuleId(appModule.appmoduleid);
        
        // Load sitemap for this app module
        const sitemapsResponse = await dataverseClient.fetchEntities('sitemaps', {
          filter: `appmoduleid eq ${appModule.appmoduleid}`,
          select: ['sitemapid', 'sitemapxml'],
          top: 1,
        });

        if (sitemapsResponse.value.length > 0) {
          const sitemapData = sitemapsResponse.value[0] as SiteMap;
          if (sitemapData.sitemapxml) {
            const parsedSitemap = parseSiteMapXml(sitemapData.sitemapxml);
            setSitemap(parsedSitemap);
            
            // Auto-select first entity
            if (parsedSitemap.areas.length > 0) {
              const firstArea = parsedSitemap.areas[0];
              if (firstArea.groups.length > 0) {
                const firstGroup = firstArea.groups[0];
                if (firstGroup.subareas.length > 0) {
                  const firstSubarea = firstGroup.subareas[0];
                  if (firstSubarea.entity) {
                    setSelectedEntity(firstSubarea.entity);
                  }
                }
              }
            }
          }
        } else {
          setError('No sitemap found for the app. Please initialize MDA data.');
        }
      } else {
        setError('No app module found. Please initialize MDA data using the CLI service.');
      }
    } catch (err) {
      console.error('Error loading sitemap:', err);
      setError(err instanceof Error ? err.message : 'Failed to load sitemap');
    } finally {
      setLoading(false);
    }
  };

  if (loading) {
    return (
      <div className={styles.loadingContainer}>
        <Spinner label="Loading Model-Driven App..." />
      </div>
    );
  }

  if (error) {
    return (
      <div className={styles.errorContainer}>
        <h2>Error Loading App</h2>
        <p>{error}</p>
        <p>
          Make sure the Fake4Dataverse service is running and has initialized MDA metadata.
        </p>
      </div>
    );
  }

  if (!sitemap || sitemap.areas.length === 0) {
    return (
      <div className={styles.welcomeContainer}>
        <h2>Welcome to Fake4Dataverse Model-Driven App</h2>
        <p>No sitemap configured. Please initialize MDA data.</p>
      </div>
    );
  }

  return (
    <div className={styles.container}>
      <Navigation
        areas={sitemap.areas}
        selectedEntity={selectedEntity || undefined}
        onNavigate={setSelectedEntity}
      />
      <main className={styles.main}>
        {selectedEntity ? (
          <EntityListView
            entityName={selectedEntity}
            entityPluralName={ENTITY_PLURAL_NAMES[selectedEntity] || selectedEntity + 's'}
            displayName={ENTITY_DISPLAY_NAMES[selectedEntity]}
            appModuleId={appModuleId || undefined}
          />
        ) : (
          <div className={styles.welcomeContainer}>
            <h2>Select an entity from the navigation</h2>
          </div>
        )}
      </main>
    </div>
  );
}
