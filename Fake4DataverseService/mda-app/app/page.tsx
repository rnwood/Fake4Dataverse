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
import EntityForm from './components/EntityForm';
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
    id: params.get('id') || undefined,  // Record ID for forms
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
  const [pageType, setPageType] = useState<string | undefined>(undefined);
  const [recordId, setRecordId] = useState<string | undefined>(undefined);
  
  // Navigation stack for managing back/forward navigation
  // Stack stores navigation states: { entity, viewId, pageType, recordId }
  const [navigationStack, setNavigationStack] = useState<Array<{
    entity: string;
    viewId?: string;
    pageType?: string;
    recordId?: string;
  }>>([]);

  useEffect(() => {
    loadSitemap();
    
    // Listen for URL changes triggered by navigation
    const handleUrlChange = () => {
      const params = parseUrlParameters();
      const newPageType = params.pagetype;
      const newEntity = params.etn || null;
      const newRecordId = params.id;
      const newViewId = params.viewid;
      
      // If navigating to a record form, add current state to navigation stack
      if (newPageType === 'entityrecord' && newRecordId && selectedEntity && pageType !== 'entityrecord') {
        setNavigationStack(prev => [...prev, {
          entity: selectedEntity,
          viewId: selectedViewId,
          pageType: pageType,
        }]);
      }
      
      setPageType(newPageType);
      setSelectedEntity(newEntity);
      setRecordId(newRecordId);
      setSelectedViewId(newViewId);
    };
    
    window.addEventListener('urlchange', handleUrlChange);
    window.addEventListener('popstate', handleUrlChange);
    
    return () => {
      window.removeEventListener('urlchange', handleUrlChange);
      window.removeEventListener('popstate', handleUrlChange);
    };
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
              setPageType(urlParams.pagetype);
              setRecordId(urlParams.id);
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
   * This is called from the navigation sidebar - should clear the navigation stack
   */
  const handleNavigate = (entityName: string) => {
    // Clear navigation stack when navigating from navbar
    setNavigationStack([]);
    
    setSelectedEntity(entityName);
    setSelectedViewId(undefined); // Reset view when changing entity
    setPageType(undefined); // Clear page type
    setRecordId(undefined); // Clear record ID
    
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
   * Navigate to a record form
   * This adds to the navigation stack for back navigation
   */
  const handleNavigateToRecord = (entityName: string, recordId: string) => {
    // Add current state to navigation stack
    if (selectedEntity) {
      setNavigationStack(prev => [...prev, {
        entity: selectedEntity,
        viewId: selectedViewId,
        pageType: pageType,
        recordId: recordId
      }]);
    }
    
    setSelectedEntity(entityName);
    setRecordId(recordId);
    setPageType('entityrecord');
    
    // Update URL
    if (typeof window !== 'undefined' && appModuleId) {
      const params = new URLSearchParams();
      params.set('appid', appModuleId);
      params.set('pagetype', 'entityrecord');
      params.set('etn', entityName);
      params.set('id', recordId);
      
      const newUrl = `${window.location.pathname}?${params.toString()}`;
      window.history.pushState({}, '', newUrl);
    }
  };

  /**
   * Navigate back using the navigation stack
   */
  const handleNavigateBack = () => {
    if (navigationStack.length > 0) {
      // Pop the last state from stack
      const previousState = navigationStack[navigationStack.length - 1];
      setNavigationStack(prev => prev.slice(0, -1));
      
      // Restore previous state
      setSelectedEntity(previousState.entity);
      setSelectedViewId(previousState.viewId);
      setPageType(previousState.pageType);
      setRecordId(previousState.recordId);
      
      // Update URL
      if (typeof window !== 'undefined' && appModuleId) {
        const params = new URLSearchParams();
        params.set('appid', appModuleId);
        if (previousState.pageType) {
          params.set('pagetype', previousState.pageType);
        } else {
          params.set('pagetype', 'entitylist');
        }
        params.set('etn', previousState.entity);
        if (previousState.viewId) {
          params.set('viewid', previousState.viewId);
        }
        if (previousState.recordId) {
          params.set('id', previousState.recordId);
        }
        
        const newUrl = `${window.location.pathname}?${params.toString()}`;
        window.history.pushState({}, '', newUrl);
      }
    } else {
      // If no history, just clear the form/record view
      setPageType(undefined);
      setRecordId(undefined);
      
      if (typeof window !== 'undefined') {
        const params = new URLSearchParams(window.location.search);
        params.delete('pagetype');
        params.delete('id');
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
        {selectedEntity && pageType === 'entityrecord' ? (
          <EntityForm
            entityName={selectedEntity}
            entityPluralName={ENTITY_PLURAL_NAMES[selectedEntity] || selectedEntity + 's'}
            displayName={ENTITY_DISPLAY_NAMES[selectedEntity]}
            recordId={recordId}
            appModuleId={appModuleId || undefined}
            onClose={() => {
              // Use navigation stack to go back
              handleNavigateBack();
            }}
            onSave={(savedRecordId) => {
              console.log('Record saved:', savedRecordId);
              // Navigate back to list view after save
              handleNavigateBack();
            }}
          />
        ) : selectedEntity ? (
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
