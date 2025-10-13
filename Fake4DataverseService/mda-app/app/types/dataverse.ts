/**
 * Dataverse Model-Driven App metadata types
 * Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/reference/entities/appmodule
 * Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/reference/entities/sitemap
 * Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/reference/entities/savedquery
 */

/**
 * AppModule entity - Represents a model-driven app
 * Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/reference/entities/appmodule
 */
export interface AppModule {
  appmoduleid: string;
  name: string;
  uniquename?: string;
  description?: string;
  webresourceid?: string;
  appmoduleversion?: string;
  clienttype?: number;
  statecode?: number;
  statuscode?: number;
}

/**
 * SiteMap entity - Defines the navigation structure
 * Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/reference/entities/sitemap
 * The SiteMap defines areas, groups, and subareas for app navigation
 */
export interface SiteMap {
  sitemapid: string;
  sitemapname?: string;
  sitemapnameunique?: string;
  appmoduleid?: string;
  sitemapxml?: string;
  statecode?: number;
  statuscode?: number;
}

/**
 * SiteMap XML structure (parsed from sitemapxml)
 */
export interface SiteMapDefinition {
  areas: SiteMapArea[];
}

export interface SiteMapArea {
  id: string;
  title: string;
  icon?: string;
  groups: SiteMapGroup[];
}

export interface SiteMapGroup {
  id: string;
  title: string;
  subareas: SiteMapSubArea[];
}

export interface SiteMapSubArea {
  id: string;
  title: string;
  entity?: string;
  url?: string;
  icon?: string;
}

/**
 * SavedQuery (SystemView) entity - Defines entity views
 * Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/reference/entities/savedquery
 */
export interface SavedQuery {
  savedqueryid: string;
  name: string;
  returnedtypecode: string; // Entity logical name
  fetchxml?: string;
  layoutxml?: string;
  querytype?: number;
  isdefault?: boolean;
  statecode?: number;
  statuscode?: number;
}

/**
 * OData response wrapper
 */
export interface ODataResponse<T> {
  value: T[];
  '@odata.count'?: number;
  '@odata.nextLink'?: string;
}

/**
 * Generic entity record from OData API
 */
export interface EntityRecord {
  [key: string]: any;
}

/**
 * View column definition
 */
export interface ViewColumn {
  name: string;
  width: number;
  label: string;
}
