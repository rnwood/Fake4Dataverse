/**
 * Xrm JavaScript API implementation for form scripts
 * Reference: https://learn.microsoft.com/en-us/power-apps/developer/model-driven-apps/clientapi/reference
 */

import type {
  XrmStatic,
  XrmPage,
  XrmAttribute,
  XrmControl,
  FormContext,
  ExecutionContext,
  XrmUtility,
  XrmWebApi,
} from './xrm-api-types';
import type { EntityRecord } from '../types/dataverse';
import { dataverseClient } from './dataverse-client';

/**
 * Creates an Xrm API instance for a form
 */
export class XrmApiImplementation {
  private entityName: string;
  private entityPluralName: string;
  private recordData: EntityRecord;
  private isDirty: boolean = false;
  private attributes: Map<string, XrmAttributeImpl> = new Map();
  private controls: Map<string, XrmControlImpl> = new Map();
  private onChangeHandlers: Map<string, Set<() => void>> = new Map();
  private onSaveCallback?: (recordId: string) => void;
  private notifications: Map<string, { message: string; level: string }> = new Map();

  constructor(
    entityName: string,
    entityPluralName: string,
    recordData: EntityRecord,
    onSave?: (recordId: string) => void
  ) {
    this.entityName = entityName;
    this.entityPluralName = entityPluralName;
    this.recordData = { ...recordData };
    this.onSaveCallback = onSave;
  }

  /**
   * Register an attribute (form field)
   */
  registerAttribute(name: string): void {
    if (!this.attributes.has(name)) {
      const attribute = new XrmAttributeImpl(name, this);
      this.attributes.set(name, attribute);
    }
  }

  /**
   * Register a control
   */
  registerControl(name: string, attributeName?: string): void {
    if (!this.controls.has(name)) {
      const attribute = attributeName ? this.attributes.get(attributeName) : null;
      const control = new XrmControlImpl(name, attribute || null);
      this.controls.set(name, control);
    }
  }

  /**
   * Get attribute value from record data
   */
  getAttributeValue(name: string): any {
    return this.recordData[name];
  }

  /**
   * Set attribute value in record data
   */
  setAttributeValue(name: string, value: any): void {
    this.recordData[name] = value;
    this.isDirty = true;
    this.fireOnChange(name);
  }

  /**
   * Fire onChange handlers for an attribute
   */
  fireOnChange(attributeName: string): void {
    const handlers = this.onChangeHandlers.get(attributeName);
    if (handlers) {
      handlers.forEach(handler => {
        try {
          handler();
        } catch (error) {
          console.error(`Error in onChange handler for ${attributeName}:`, error);
        }
      });
    }
  }

  /**
   * Add onChange handler for an attribute
   */
  addOnChange(attributeName: string, handler: () => void): void {
    if (!this.onChangeHandlers.has(attributeName)) {
      this.onChangeHandlers.set(attributeName, new Set());
    }
    this.onChangeHandlers.get(attributeName)!.add(handler);
  }

  /**
   * Remove onChange handler for an attribute
   */
  removeOnChange(attributeName: string, handler: () => void): void {
    const handlers = this.onChangeHandlers.get(attributeName);
    if (handlers) {
      handlers.delete(handler);
    }
  }

  /**
   * Save the record
   */
  async save(): Promise<void> {
    const recordId = this.recordData[`${this.entityName}id`];
    
    try {
      if (recordId) {
        // Update existing record
        await dataverseClient.updateEntity(this.entityPluralName, recordId, this.recordData);
      } else {
        // Create new record
        const response = await dataverseClient.createEntity(this.entityPluralName, this.recordData);
        this.recordData[`${this.entityName}id`] = response.id;
      }
      
      this.isDirty = false;
      
      if (this.onSaveCallback) {
        this.onSaveCallback(this.recordData[`${this.entityName}id`]);
      }
    } catch (error) {
      console.error('Error saving record:', error);
      throw error;
    }
  }

  /**
   * Create the Xrm API object
   */
  createXrmApi(): XrmStatic {
    const formContext = this.createFormContext();
    const xrmPage = this.createXrmPage(formContext);
    
    return {
      Page: xrmPage,
      Utility: this.createXrmUtility(),
      WebApi: this.createXrmWebApi(),
      Navigation: {
        openAlertDialog: async (alertStrings) => {
          alert(alertStrings.text);
        },
        openConfirmDialog: async (confirmStrings) => {
          const confirmed = confirm(confirmStrings.text);
          return { confirmed };
        },
        openErrorDialog: async (errorOptions) => {
          alert(`Error: ${errorOptions.message}${errorOptions.details ? '\n\n' + errorOptions.details : ''}`);
        },
        openForm: async (entityFormOptions) => {
          // This would navigate to a form
          console.log('openForm called:', entityFormOptions);
        },
        openUrl: (url, options) => {
          if (options?.newWindow) {
            window.open(url, '_blank');
          } else {
            window.location.href = url;
          }
        },
      },
    };
  }

  private createFormContext(): FormContext {
    return {
      data: {
        entity: {
          getId: () => this.recordData[`${this.entityName}id`] || '',
          getEntityName: () => this.entityName,
          getPrimaryAttributeValue: () => this.recordData.name || this.recordData.firstname || '',
          getIsDirty: () => this.isDirty,
          save: async () => await this.save(),
          attributes: {
            get: (name?: string) => {
              if (name) {
                return this.attributes.get(name) || null;
              }
              return Array.from(this.attributes.values());
            },
          } as any,
        },
        save: async () => await this.save(),
        refresh: async (save?: boolean) => {
          if (save) {
            await this.save();
          }
          // Reload form data
          const recordId = this.recordData[`${this.entityName}id`];
          if (recordId) {
            const refreshedData = await dataverseClient.fetchEntity(this.entityPluralName, recordId);
            this.recordData = refreshedData;
          }
        },
      },
      ui: {
        tabs: {
          get: (name?: string) => {
            // TODO: Implement tab access
            return name ? null : [];
          },
        } as any,
        controls: {
          get: (name?: string) => {
            if (name) {
              return this.controls.get(name) || null;
            }
            return Array.from(this.controls.values());
          },
        } as any,
        setFormNotification: (message, level, uniqueId) => {
          this.notifications.set(uniqueId, { message, level });
          // TODO: Display notification in UI
          console.log(`[${level}] ${message}`);
        },
        clearFormNotification: (uniqueId) => {
          this.notifications.delete(uniqueId);
        },
      },
      getEventArgs: () => null,
    };
  }

  private createXrmPage(formContext: FormContext): XrmPage {
    return {
      data: formContext.data,
      ui: formContext.ui,
      context: {
        getUserId: () => '00000000-0000-0000-0000-000000000000',
        getUserName: () => 'Test User',
        getOrgUniqueName: () => 'fake4dataverse',
        getOrgLcid: () => 1033,
        getUserLcid: () => 1033,
      },
      getAttribute: ((name?: string) => {
        if (name) {
          return this.attributes.get(name) || null;
        }
        return Array.from(this.attributes.values());
      }) as any,
      getControl: ((name?: string) => {
        if (name) {
          return this.controls.get(name) || null;
        }
        return Array.from(this.controls.values());
      }) as any,
    };
  }

  private createXrmUtility(): XrmUtility {
    return {
      alertDialog: (message, onCloseCallback) => {
        alert(message);
        if (onCloseCallback) {
          onCloseCallback();
        }
      },
      confirmDialog: (message, yesCloseCallback, noCloseCallback) => {
        const result = confirm(message);
        if (result && yesCloseCallback) {
          yesCloseCallback();
        } else if (!result && noCloseCallback) {
          noCloseCallback();
        }
      },
      showProgressIndicator: (message) => {
        console.log('Progress:', message);
        // TODO: Show progress indicator in UI
      },
      closeProgressIndicator: () => {
        // TODO: Close progress indicator
      },
      openEntityForm: async (entityName, entityId, formParameters) => {
        console.log('openEntityForm:', entityName, entityId, formParameters);
        // TODO: Navigate to entity form
      },
      refreshParentGrid: (record) => {
        console.log('refreshParentGrid:', record);
        // TODO: Refresh parent grid
      },
    };
  }

  private createXrmWebApi(): XrmWebApi {
    return {
      retrieveRecord: async (entityLogicalName, id, options) => {
        return await dataverseClient.fetchEntity(entityLogicalName + 's', id);
      },
      retrieveMultipleRecords: async (entityLogicalName, options, maxPageSize) => {
        const response = await dataverseClient.fetchEntities(entityLogicalName + 's', {
          top: maxPageSize || 50,
        });
        return {
          entities: response.value,
          nextLink: response['@odata.nextLink'],
        };
      },
      createRecord: async (entityLogicalName, data) => {
        const id = await dataverseClient.createEntity(entityLogicalName + 's', data);
        return { id };
      },
      updateRecord: async (entityLogicalName, id, data) => {
        await dataverseClient.updateEntity(entityLogicalName + 's', id, data);
        return { id };
      },
      deleteRecord: async (entityLogicalName, id) => {
        await dataverseClient.deleteEntity(entityLogicalName + 's', id);
        return { id };
      },
    };
  }
}

/**
 * XrmAttribute implementation
 */
class XrmAttributeImpl implements XrmAttribute {
  private name: string;
  private xrmApi: XrmApiImplementation;
  private requiredLevel: 'none' | 'required' | 'recommended' = 'none';

  constructor(name: string, xrmApi: XrmApiImplementation) {
    this.name = name;
    this.xrmApi = xrmApi;
  }

  getName(): string {
    return this.name;
  }

  getValue(): any {
    return this.xrmApi.getAttributeValue(this.name);
  }

  setValue(value: any): void {
    this.xrmApi.setAttributeValue(this.name, value);
  }

  getRequiredLevel(): 'none' | 'required' | 'recommended' {
    return this.requiredLevel;
  }

  setRequiredLevel(level: 'none' | 'required' | 'recommended'): void {
    this.requiredLevel = level;
  }

  addOnChange(handler: () => void): void {
    this.xrmApi.addOnChange(this.name, handler);
  }

  removeOnChange(handler: () => void): void {
    this.xrmApi.removeOnChange(this.name, handler);
  }

  fireOnChange(): void {
    this.xrmApi.fireOnChange(this.name);
  }
}

/**
 * XrmControl implementation
 */
class XrmControlImpl implements XrmControl {
  private name: string;
  private attribute: XrmAttribute | null;
  private disabled: boolean = false;
  private visible: boolean = true;
  private label: string = '';

  constructor(name: string, attribute: XrmAttribute | null) {
    this.name = name;
    this.attribute = attribute;
  }

  getName(): string {
    return this.name;
  }

  getDisabled(): boolean {
    return this.disabled;
  }

  setDisabled(disabled: boolean): void {
    this.disabled = disabled;
    // TODO: Update UI
  }

  getVisible(): boolean {
    return this.visible;
  }

  setVisible(visible: boolean): void {
    this.visible = visible;
    // TODO: Update UI
  }

  getLabel(): string {
    return this.label;
  }

  setLabel(label: string): void {
    this.label = label;
    // TODO: Update UI
  }

  getAttribute(): XrmAttribute | null {
    return this.attribute;
  }

  setFocus(): void {
    // TODO: Focus the control in UI
  }
}

/**
 * Execute a form script
 */
export async function executeFormScript(
  scriptContent: string,
  xrmApi: XrmStatic,
  functionName?: string,
  ...args: any[]
): Promise<void> {
  try {
    // Attach Xrm to window for scripts
    (window as any).Xrm = xrmApi;
    
    // Execute the script
    const scriptFunction = new Function('Xrm', scriptContent);
    scriptFunction(xrmApi);
    
    // If a specific function name is provided, call it
    if (functionName && typeof (window as any)[functionName] === 'function') {
      await (window as any)[functionName](...args);
    }
  } catch (error) {
    console.error('Error executing form script:', error);
    throw error;
  }
}
