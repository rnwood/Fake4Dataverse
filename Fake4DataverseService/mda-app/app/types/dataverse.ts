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

/**
 * SystemForm entity - Defines entity forms
 * Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/reference/entities/systemform
 */
export interface SystemForm {
  formid: string;
  name: string;
  objecttypecode: string; // Entity logical name
  type: number; // Form type: 2=Main, 4=Quick Create, 6=Quick View, etc.
  formxml?: string;
  description?: string;
  isdefault?: boolean;
  statecode?: number;
  statuscode?: number;
}

/**
 * WebResource entity - Stores web resources (JavaScript, CSS, images, etc.)
 * Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/reference/entities/webresource
 */
export interface WebResource {
  webresourceid: string;
  name: string;
  displayname?: string;
  description?: string;
  webresourcetype: number; // 1=HTML, 2=CSS, 3=JavaScript, 4=XML, 5=PNG, etc.
  content?: string; // Base64 encoded content
  languagecode?: number;
}

/**
 * Parsed form definition from formxml
 */
export interface FormDefinition {
  tabs: FormTab[];
  formLibraries: FormLibrary[];
  events: FormEvent[];
}

export interface FormLibrary {
  name: string; // WebResource name
  libraryUniqueId: string;
}

export interface FormEvent {
  name: string; // Event name (onload, onsave, etc.)
  application: boolean;
  active: boolean;
  attribute?: string; // For field events
  functionName?: string;
  libraryName?: string;
  parameters?: string[];
}

export interface FormTab {
  id: string;
  name: string;
  label: string;
  visible: boolean;
  sections: FormSection[];
}

export interface FormSection {
  id: string;
  name: string;
  label: string;
  visible: boolean;
  rows: FormRow[];
}

export interface FormRow {
  cells: FormCell[];
}

export interface FormCell {
  control?: FormControl;
  colspan?: number;
  rowspan?: number;
}

export interface FormControl {
  id: string;
  datafieldname?: string;
  classid: string;
  label?: string;
  disabled?: boolean;
}
