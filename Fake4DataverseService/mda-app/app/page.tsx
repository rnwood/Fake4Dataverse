'use client';

/**
 * Main Model-Driven App page
 * Displays navigation and entity list views based on sitemap
 * Supports URL parameters matching Dynamics 365:
 * - appid: Application ID (GUID)
 * - pagetype: Page type (e.g., "entitylist")
 * - etn: Entity type name
 * - viewid: View ID (SavedQuery GUID)
 * Reference: https://learn.microsoft.com/en-us/power-apps/developer/model-driven-apps/navigate-to-custom-page-examples
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

/**
 * Parse URL parameters from window.location
 * Reference: https://learn.microsoft.com/en-us/power-apps/developer/model-driven-apps/navigate-to-custom-page-examples
 */
function parseUrlParameters() {
  if (typeof window === 'undefined') return {};
  
  const params = new URLSearchParams(window.location.search);
  return {
    appid: params.get('appid') || undefined,
    pagetype: params.get('pagetype') || undefined,
    etn: params.get('etn') || undefined,  // Entity type name
    viewid: params.get('viewid') || undefined,
  };
}

export default function Home() {
  const styles = useStyles();
  const [sitemap, setSitemap] = useState<SiteMapDefinition | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [selectedEntity, setSelectedEntity] = useState<string | null>(null);
  const [appModuleId, setAppModuleId] = useState<string | null>(null);
  const [selectedViewId, setSelectedViewId] = useState<string | undefined>(undefined);
  
  // Navigation history stack
  // Reference: https://learn.microsoft.com/en-us/power-apps/developer/model-driven-apps/clientapi/navigate-to-custom-page-examples
  const [navigationHistory, setNavigationHistory] = useState<string[]>([]);

  useEffect(() => {
    loadSitemap();
  }, []);

  const loadSitemap = async () => {
    setLoading(true);
    setError(null);

    try {
      // Parse URL parameters
      const urlParams = parseUrlParameters();
      
      // Try to load sitemap from appmodule
      const appModulesResponse = await dataverseClient.fetchEntities('appmodules', {
        top: 1,
        select: ['appmoduleid', 'name'],
        // If appid is provided in URL, filter by it
        ...(urlParams.appid && { filter: `appmoduleid eq ${urlParams.appid}` }),
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
            
            // Check if entity is specified in URL
            if (urlParams.etn) {
              setSelectedEntity(urlParams.etn);
              setSelectedViewId(urlParams.viewid);
            } else {
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

  /**
   * Navigate to entity and update URL
   * @param entityName Entity to navigate to
   * @param clearHistory If true, clears the navigation history (used for navbar navigation)
   */
  const handleNavigate = (entityName: string, clearHistory: boolean = false) => {
    // Update navigation history
    if (clearHistory) {
      // Clear history when navigating from navbar
      setNavigationHistory([]);
    } else if (selectedEntity && selectedEntity !== entityName) {
      // Add current entity to history when navigating away (not from navbar)
      setNavigationHistory(prev => [...prev, selectedEntity]);
    }
    
    setSelectedEntity(entityName);
    setSelectedViewId(undefined); // Reset view when changing entity
    
    // Update URL with query parameters
    if (typeof window !== 'undefined' && appModuleId) {
      const params = new URLSearchParams();
      params.set('appid', appModuleId);
      params.set('pagetype', 'entitylist');
      params.set('etn', entityName);
      
      const newUrl = `${window.location.pathname}?${params.toString()}`;
      window.history.pushState({}, '', newUrl);
    }
  };

  /**
   * Navigate back in history
   */
  const handleBack = () => {
    if (navigationHistory.length > 0) {
      const previousEntity = navigationHistory[navigationHistory.length - 1];
      setNavigationHistory(prev => prev.slice(0, -1));
      setSelectedEntity(previousEntity);
      setSelectedViewId(undefined);
      
      // Update URL
      if (typeof window !== 'undefined' && appModuleId) {
        const params = new URLSearchParams();
        params.set('appid', appModuleId);
        params.set('pagetype', 'entitylist');
        params.set('etn', previousEntity);
        
        const newUrl = `${window.location.pathname}?${params.toString()}`;
        window.history.pushState({}, '', newUrl);
      }
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
        onNavigate={handleNavigate}
      />
      <main className={styles.main}>
        {selectedEntity ? (
          <EntityListView
            entityName={selectedEntity}
            entityPluralName={ENTITY_PLURAL_NAMES[selectedEntity] || selectedEntity + 's'}
            displayName={ENTITY_DISPLAY_NAMES[selectedEntity]}
            appModuleId={appModuleId || undefined}
            initialViewId={selectedViewId}
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
