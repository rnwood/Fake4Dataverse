/**
 * Xrm JavaScript API implementation for Dynamics 365 / Dataverse
 * Reference: https://learn.microsoft.com/en-us/power-apps/developer/model-driven-apps/clientapi/reference
 * 
 * This provides a browser-compatible implementation of the Xrm API that form scripts can use.
 */

import type { EntityRecord } from '../types/dataverse';

/**
 * Attribute interface - represents a form field
 * Reference: https://learn.microsoft.com/en-us/power-apps/developer/model-driven-apps/clientapi/reference/attributes
 */
export interface XrmAttribute {
  getName(): string;
  getValue(): any;
  setValue(value: any): void;
  getRequiredLevel(): 'none' | 'required' | 'recommended';
  setRequiredLevel(level: 'none' | 'required' | 'recommended'): void;
  addOnChange(handler: () => void): void;
  removeOnChange(handler: () => void): void;
  fireOnChange(): void;
}

/**
 * Control interface - represents a UI control
 * Reference: https://learn.microsoft.com/en-us/power-apps/developer/model-driven-apps/clientapi/reference/controls
 */
export interface XrmControl {
  getName(): string;
  getDisabled(): boolean;
  setDisabled(disabled: boolean): void;
  getVisible(): boolean;
  setVisible(visible: boolean): void;
  getLabel(): string;
  setLabel(label: string): void;
  getAttribute(): XrmAttribute | null;
  setFocus(): void;
}

/**
 * FormContext interface - provides access to form data and UI
 * Reference: https://learn.microsoft.com/en-us/power-apps/developer/model-driven-apps/clientapi/reference/formContext
 */
export interface FormContext {
  data: {
    entity: {
      getId(): string;
      getEntityName(): string;
      getPrimaryAttributeValue(): string;
      getIsDirty(): boolean;
      save(saveMode?: string): Promise<void>;
      attributes: {
        get(name: string): XrmAttribute | null;
        get(): XrmAttribute[];
      };
    };
    save(saveOptions?: any): Promise<void>;
    refresh(save?: boolean): Promise<void>;
  };
  ui: {
    tabs: {
      get(name: string): any;
      get(): any[];
    };
    controls: {
      get(name: string): XrmControl | null;
      get(): XrmControl[];
    };
    setFormNotification(message: string, level: 'ERROR' | 'WARNING' | 'INFO', uniqueId: string): void;
    clearFormNotification(uniqueId: string): void;
  };
  getEventArgs(): any;
}

/**
 * ExecutionContext interface - passed to form event handlers
 * Reference: https://learn.microsoft.com/en-us/power-apps/developer/model-driven-apps/clientapi/reference/executioncontext
 */
export interface ExecutionContext {
  getFormContext(): FormContext;
  getEventSource(): XrmAttribute | XrmControl;
  getDepth(): number;
}

/**
 * Xrm.Page interface (legacy, but still widely used)
 * Reference: https://learn.microsoft.com/en-us/power-apps/developer/model-driven-apps/clientapi/reference/xrm-page
 */
export interface XrmPage {
  data: FormContext['data'];
  ui: FormContext['ui'];
  context: {
    getUserId(): string;
    getUserName(): string;
    getOrgUniqueName(): string;
    getOrgLcid(): number;
    getUserLcid(): number;
  };
  getAttribute(name: string): XrmAttribute | null;
  getAttribute(): XrmAttribute[];
  getControl(name: string): XrmControl | null;
  getControl(): XrmControl[];
}

/**
 * Xrm.Utility interface
 * Reference: https://learn.microsoft.com/en-us/power-apps/developer/model-driven-apps/clientapi/reference/xrm-utility
 */
export interface XrmUtility {
  alertDialog(message: string, onCloseCallback?: () => void): void;
  confirmDialog(message: string, yesCloseCallback?: () => void, noCloseCallback?: () => void): void;
  showProgressIndicator(message: string): void;
  closeProgressIndicator(): void;
  openEntityForm(entityName: string, entityId?: string, formParameters?: any): Promise<any>;
  refreshParentGrid(record: any): void;
}

/**
 * Xrm.WebApi interface
 * Reference: https://learn.microsoft.com/en-us/power-apps/developer/model-driven-apps/clientapi/reference/xrm-webapi
 */
export interface XrmWebApi {
  retrieveRecord(entityLogicalName: string, id: string, options?: string): Promise<EntityRecord>;
  retrieveMultipleRecords(entityLogicalName: string, options?: string, maxPageSize?: number): Promise<{
    entities: EntityRecord[];
    nextLink?: string;
  }>;
  createRecord(entityLogicalName: string, data: any): Promise<{ id: string }>;
  updateRecord(entityLogicalName: string, id: string, data: any): Promise<{ id: string }>;
  deleteRecord(entityLogicalName: string, id: string): Promise<{ id: string }>;
}

/**
 * Main Xrm namespace
 * Reference: https://learn.microsoft.com/en-us/power-apps/developer/model-driven-apps/clientapi/reference
 */
export interface XrmStatic {
  Page: XrmPage;
  Utility: XrmUtility;
  WebApi: XrmWebApi;
  Navigation: {
    openAlertDialog(alertStrings: { text: string; title?: string }, options?: any): Promise<any>;
    openConfirmDialog(confirmStrings: { text: string; title?: string }, options?: any): Promise<{ confirmed: boolean }>;
    openErrorDialog(errorOptions: { message: string; details?: string }): Promise<any>;
    openForm(entityFormOptions: any, formParameters?: any): Promise<any>;
    openUrl(url: string, openUrlOptions?: any): void;
  };
}

// Global Xrm object will be attached to window
declare global {
  interface Window {
    Xrm: XrmStatic;
  }
}
