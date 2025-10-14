'use client';

/**
 * Entity form component for displaying and editing records
 * Renders forms based on SystemForm entity with tabs, sections, and controls
 * Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/reference/entities/systemform
 */

import { useState, useEffect } from 'react';
import {
  makeStyles,
  tokens,
  Spinner,
  Button,
  Input,
  Field,
  Tab,
  TabList,
  TabValue,
} from '@fluentui/react-components';
import {
  ArrowLeft20Regular,
  Save20Regular,
  Dismiss20Regular,
} from '@fluentui/react-icons';
import { dataverseClient } from '../lib/dataverse-client';
import { parseFormXml } from '../lib/form-utils';
import { XrmApiImplementation, executeFormScript } from '../lib/xrm-api';
import type { EntityRecord, SystemForm, FormDefinition, WebResource } from '../types/dataverse';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    height: '100%',
    backgroundColor: tokens.colorNeutralBackground1,
  },
  header: {
    display: 'flex',
    alignItems: 'center',
    gap: '12px',
    padding: '16px 24px',
    borderBottom: `1px solid ${tokens.colorNeutralStroke1}`,
    backgroundColor: tokens.colorNeutralBackground1,
  },
  title: {
    flex: 1,
    fontSize: tokens.fontSizeBase500,
    fontWeight: tokens.fontWeightSemibold,
  },
  content: {
    flex: 1,
    overflow: 'auto',
    padding: '24px',
  },
  loadingContainer: {
    display: 'flex',
    justifyContent: 'center',
    alignItems: 'center',
    height: '100%',
  },
  errorContainer: {
    padding: '16px',
    color: tokens.colorPaletteRedForeground1,
    backgroundColor: tokens.colorPaletteRedBackground1,
    borderRadius: tokens.borderRadiusMedium,
  },
  tabList: {
    marginBottom: '24px',
  },
  section: {
    marginBottom: '32px',
  },
  sectionTitle: {
    fontSize: tokens.fontSizeBase400,
    fontWeight: tokens.fontWeightSemibold,
    marginBottom: '16px',
    color: tokens.colorNeutralForeground1,
  },
  formRow: {
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fit, minmax(300px, 1fr))',
    gap: '16px',
    marginBottom: '12px',
  },
  field: {
    width: '100%',
  },
});

interface EntityFormProps {
  entityName: string;
  entityPluralName: string;
  recordId?: string; // Undefined for new records
  displayName?: string;
  appModuleId?: string;
  onClose?: () => void;
  onSave?: (recordId: string) => void;
}

export default function EntityForm({
  entityName,
  entityPluralName,
  recordId,
  displayName,
  appModuleId,
  onClose,
  onSave,
}: EntityFormProps) {
  const styles = useStyles();
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [form, setForm] = useState<SystemForm | null>(null);
  const [formDefinition, setFormDefinition] = useState<FormDefinition | null>(null);
  const [record, setRecord] = useState<EntityRecord>({});
  const [selectedTab, setSelectedTab] = useState<TabValue>('');
  const [isDirty, setIsDirty] = useState(false);
  const [xrmApi, setXrmApi] = useState<any>(null);
  const [scriptsLoaded, setScriptsLoaded] = useState(false);

  useEffect(() => {
    loadFormAndRecord();
  }, [entityName, recordId]);

  // Initialize Xrm API and load scripts when form is loaded
  useEffect(() => {
    if (formDefinition && !scriptsLoaded) {
      initializeXrmApi();
    }
  }, [formDefinition, record]);

  const initializeXrmApi = async () => {
    try {
      // Create Xrm API instance
      const xrmApiImpl = new XrmApiImplementation(
        entityName,
        entityPluralName,
        record,
        (savedRecordId) => {
          if (onSave) {
            onSave(savedRecordId);
          }
        }
      );

      // Register attributes from form
      if (formDefinition) {
        formDefinition.tabs.forEach(tab => {
          tab.sections.forEach(section => {
            section.rows.forEach(row => {
              row.cells.forEach(cell => {
                if (cell.control?.datafieldname) {
                  xrmApiImpl.registerAttribute(cell.control.datafieldname);
                  xrmApiImpl.registerControl(cell.control.id, cell.control.datafieldname);
                }
              });
            });
          });
        });
      }

      const api = xrmApiImpl.createXrmApi();
      setXrmApi(api);

      // Load and execute form libraries (scripts)
      if (formDefinition?.formLibraries && formDefinition.formLibraries.length > 0) {
        await loadFormScripts(formDefinition, api);
      }

      // Execute OnLoad events
      if (formDefinition?.events) {
        executeFormEvents(formDefinition.events.filter(e => e.name === 'onload'), api);
      }

      setScriptsLoaded(true);
    } catch (error) {
      console.error('Error initializing Xrm API:', error);
    }
  };

  const loadFormScripts = async (formDef: FormDefinition, api: any) => {
    try {
      // Load all webresources referenced in the form
      for (const library of formDef.formLibraries) {
        try {
          // Fetch the webresource
          const webResourceResponse = await dataverseClient.fetchEntities('webresources', {
            filter: `name eq '${library.name}'`,
            select: ['webresourceid', 'name', 'content', 'webresourcetype'],
            top: 1,
          });

          if (webResourceResponse.value.length > 0) {
            const webResource = webResourceResponse.value[0] as WebResource;
            
            // Only load JavaScript webresources (type 3)
            if (webResource.webresourcetype === 3 && webResource.content) {
              // Decode base64 content
              const scriptContent = atob(webResource.content);
              
              // Execute the script to load functions into global scope
              await executeFormScript(scriptContent, api);
              
              console.log(`Loaded form script: ${library.name}`);
            }
          }
        } catch (err) {
          console.warn(`Failed to load webresource ${library.name}:`, err);
        }
      }
    } catch (error) {
      console.error('Error loading form scripts:', error);
    }
  };

  const executeFormEvents = (events: any[], api: any) => {
    events.forEach(event => {
      if (event.active && event.functionName) {
        try {
          // Get the function from global scope
          const func = (window as any)[event.functionName];
          if (typeof func === 'function') {
            // Create execution context
            const executionContext = {
              getFormContext: () => api.Page,
              getEventSource: () => null,
              getDepth: () => 1,
            };
            
            // Call the function with execution context
            func(executionContext);
          } else {
            console.warn(`Function ${event.functionName} not found for event ${event.name}`);
          }
        } catch (err) {
          console.error(`Error executing ${event.name} event handler ${event.functionName}:`, err);
        }
      }
    });
  };

  const loadFormAndRecord = async () => {
    setLoading(true);
    setError(null);

    try {
      // Load form definition
      // Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/reference/entities/systemform
      let filter = `objecttypecode eq '${entityName}' and type eq 2`; // type 2 = Main form

      // If appModuleId is provided, try to filter by forms in the app
      if (appModuleId) {
        try {
          const componentsResponse = await dataverseClient.fetchEntities('appmodulecomponents', {
            filter: `appmoduleidunique eq ${appModuleId} and componenttype eq 60`, // 60 = SystemForm
            select: ['objectid'],
          });

          const formIds = componentsResponse.value
            .map((c: any) => c.objectid)
            .filter((id: any) => id);

          if (formIds.length > 0) {
            const formIdFilters = formIds.map((id: string) => `formid eq ${id}`).join(' or ');
            filter = `(${filter}) and (${formIdFilters})`;
          }
        } catch (err) {
          console.warn('Failed to load app module form components:', err);
        }
      }

      const formsResponse = await dataverseClient.fetchEntities('systemforms', {
        filter: filter,
        select: ['formid', 'name', 'objecttypecode', 'type', 'formxml', 'isdefault'],
        orderby: 'isdefault desc,name asc',
        top: 1,
      });

      if (formsResponse.value.length === 0) {
        throw new Error(`No form found for entity: ${entityName}`);
      }

      const loadedForm = formsResponse.value[0] as SystemForm;
      setForm(loadedForm);

      // Parse formxml
      if (loadedForm.formxml) {
        const parsedForm = parseFormXml(loadedForm.formxml);
        setFormDefinition(parsedForm);

        // Set initial tab
        if (parsedForm.tabs.length > 0) {
          setSelectedTab(parsedForm.tabs[0].id);
        }
      }

      // Load record if recordId is provided
      if (recordId) {
        const recordResponse = await dataverseClient.fetchEntities(entityPluralName, {
          filter: `${entityName}id eq ${recordId}`,
          top: 1,
        });

        if (recordResponse.value.length > 0) {
          setRecord(recordResponse.value[0]);
        }
      }
    } catch (err) {
      console.error('Error loading form:', err);
      setError(err instanceof Error ? err.message : 'Failed to load form');
    } finally {
      setLoading(false);
    }
  };

  const handleFieldChange = (fieldName: string, value: any) => {
    setRecord((prev) => ({
      ...prev,
      [fieldName]: value,
    }));
    setIsDirty(true);
  };

  const handleSave = async () => {
    setSaving(true);
    setError(null);

    try {
      let savedRecordId = recordId;

      if (recordId) {
        // Update existing record
        await dataverseClient.updateEntity(entityPluralName, recordId, record);
      } else {
        // Create new record
        savedRecordId = await dataverseClient.createEntity(entityPluralName, record);
      }

      setIsDirty(false);

      if (onSave && savedRecordId) {
        onSave(savedRecordId);
      }
    } catch (err) {
      console.error('Error saving record:', err);
      setError(err instanceof Error ? err.message : 'Failed to save record');
    } finally {
      setSaving(false);
    }
  };

  const handleTabSelect = (_event: any, data: any) => {
    setSelectedTab(data.value);
  };

  const renderControl = (control: any) => {
    if (!control || !control.datafieldname) {
      return null;
    }

    const fieldName = control.datafieldname;
    const value = record[fieldName] || '';

    // Map class IDs to control types
    // Reference: https://learn.microsoft.com/en-us/power-apps/developer/model-driven-apps/clientapi/reference/controls/getcontroltype
    // Common class IDs:
    // {270BD3DB-D9AF-4782-9025-509E298DEC0A} = Text input
    // {533B9E00-756B-4312-95A0-DC888637AC78} = Money
    // {C3EFE0C3-0EC6-42BE-8349-CBD9079DFD8E} = Whole number
    // {5B773807-9FB2-42DB-97C3-7A91EFF8ADFF} = Date picker

    return (
      <Field
        key={control.id}
        label={control.label || fieldName}
        className={styles.field}
      >
        <Input
          value={String(value)}
          onChange={(e) => handleFieldChange(fieldName, e.target.value)}
          disabled={control.disabled}
        />
      </Field>
    );
  };

  const renderSection = (section: any) => {
    if (!section.visible) {
      return null;
    }

    return (
      <div key={section.id} className={styles.section}>
        <div className={styles.sectionTitle}>{section.label}</div>
        {section.rows.map((row: any, rowIndex: number) => (
          <div key={rowIndex} className={styles.formRow}>
            {row.cells.map((cell: any, cellIndex: number) =>
              cell.control ? (
                <div key={cellIndex}>{renderControl(cell.control)}</div>
              ) : (
                <div key={cellIndex} />
              )
            )}
          </div>
        ))}
      </div>
    );
  };

  const renderTab = (tab: any) => {
    if (!tab.visible) {
      return null;
    }

    return (
      <div key={tab.id}>
        {tab.sections.map((section: any) => renderSection(section))}
      </div>
    );
  };

  if (loading) {
    return (
      <div className={styles.container}>
        <div className={styles.loadingContainer}>
          <Spinner label="Loading form..." />
        </div>
      </div>
    );
  }

  if (error) {
    return (
      <div className={styles.container}>
        <div className={styles.header}>
          <Button
            appearance="subtle"
            icon={<ArrowLeft20Regular />}
            onClick={onClose}
          >
            Back
          </Button>
        </div>
        <div className={styles.content}>
          <div className={styles.errorContainer}>
            <strong>Error:</strong> {error}
          </div>
        </div>
      </div>
    );
  }

  if (!formDefinition) {
    return (
      <div className={styles.container}>
        <div className={styles.header}>
          <Button
            appearance="subtle"
            icon={<ArrowLeft20Regular />}
            onClick={onClose}
          >
            Back
          </Button>
        </div>
        <div className={styles.content}>
          <div className={styles.errorContainer}>
            No form definition available
          </div>
        </div>
      </div>
    );
  }

  const visibleTabs = formDefinition.tabs.filter((tab) => tab.visible);
  const currentTab = visibleTabs.find((tab) => tab.id === selectedTab);

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <Button
          appearance="subtle"
          icon={<ArrowLeft20Regular />}
          onClick={onClose}
        >
          Back
        </Button>
        <div className={styles.title}>
          {displayName || entityName} - {recordId ? 'Edit' : 'New'}
        </div>
        <Button
          appearance="primary"
          icon={<Save20Regular />}
          onClick={handleSave}
          disabled={!isDirty || saving}
        >
          {saving ? 'Saving...' : 'Save'}
        </Button>
        <Button
          appearance="subtle"
          icon={<Dismiss20Regular />}
          onClick={onClose}
        >
          Close
        </Button>
      </div>
      <div className={styles.content}>
        {visibleTabs.length > 1 && (
          <TabList
            selectedValue={selectedTab}
            onTabSelect={handleTabSelect}
            className={styles.tabList}
          >
            {visibleTabs.map((tab) => (
              <Tab key={tab.id} value={tab.id}>
                {tab.label}
              </Tab>
            ))}
          </TabList>
        )}
        {currentTab && renderTab(currentTab)}
      </div>
    </div>
  );
}
