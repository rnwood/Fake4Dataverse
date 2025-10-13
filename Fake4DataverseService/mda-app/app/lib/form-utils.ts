/**
 * Utility functions for parsing FormXML
 * Reference: https://learn.microsoft.com/en-us/power-apps/developer/model-driven-apps/customize-entity-forms
 */

import type { FormDefinition, FormTab, FormSection, FormRow, FormCell, FormControl, FormLibrary, FormEvent } from '../types/dataverse';

/**
 * Parse FormXML string into structured FormDefinition
 * Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/reference/entities/systemform
 */
export function parseFormXml(formXml: string): FormDefinition {
  try {
    const parser = new DOMParser();
    const xmlDoc = parser.parseFromString(formXml, 'text/xml');
    
    const formElement = xmlDoc.getElementsByTagName('form')[0];
    if (!formElement) {
      console.warn('No form element found in FormXML');
      return { tabs: [], formLibraries: [], events: [] };
    }
    
    // Parse form libraries (script references)
    const formLibraries = parseFormLibraries(formElement);
    
    // Parse form events
    const events = parseFormEvents(formElement);
    
    const tabsElement = formElement.getElementsByTagName('tabs')[0];
    if (!tabsElement) {
      return { tabs: [], formLibraries, events };
    }
    
    const tabs: FormTab[] = [];
    const tabElements = tabsElement.getElementsByTagName('tab');
    
    for (let i = 0; i < tabElements.length; i++) {
      const tabElement = tabElements[i];
      const tab = parseTab(tabElement);
      if (tab) {
        tabs.push(tab);
      }
    }
    
    return { tabs, formLibraries, events };
  } catch (error) {
    console.error('Error parsing FormXML:', error);
    return { tabs: [], formLibraries: [], events: [] };
  }
}

function parseTab(tabElement: Element): FormTab | null {
  const id = tabElement.getAttribute('id') || '';
  const name = tabElement.getAttribute('name') || '';
  const visible = tabElement.getAttribute('visible') !== 'false';
  
  // Get label
  let label = name;
  const labelsElement = tabElement.getElementsByTagName('labels')[0];
  if (labelsElement) {
    const labelElements = labelsElement.getElementsByTagName('label');
    if (labelElements.length > 0) {
      label = labelElements[0].getAttribute('description') || name;
    }
  }
  
  // Parse sections
  const sections: FormSection[] = [];
  const columnsElement = tabElement.getElementsByTagName('columns')[0];
  if (columnsElement) {
    const columnElements = columnsElement.getElementsByTagName('column');
    for (let i = 0; i < columnElements.length; i++) {
      const sectionsElement = columnElements[i].getElementsByTagName('sections')[0];
      if (sectionsElement) {
        const sectionElements = sectionsElement.getElementsByTagName('section');
        for (let j = 0; j < sectionElements.length; j++) {
          const section = parseSection(sectionElements[j]);
          if (section) {
            sections.push(section);
          }
        }
      }
    }
  }
  
  return {
    id,
    name,
    label,
    visible,
    sections,
  };
}

function parseSection(sectionElement: Element): FormSection | null {
  const id = sectionElement.getAttribute('id') || '';
  const name = sectionElement.getAttribute('name') || '';
  const visible = sectionElement.getAttribute('visible') !== 'false';
  
  // Get label
  let label = name;
  const labelsElement = sectionElement.getElementsByTagName('labels')[0];
  if (labelsElement) {
    const labelElements = labelsElement.getElementsByTagName('label');
    if (labelElements.length > 0) {
      label = labelElements[0].getAttribute('description') || name;
    }
  }
  
  // Parse rows
  const rows: FormRow[] = [];
  const rowsElement = sectionElement.getElementsByTagName('rows')[0];
  if (rowsElement) {
    const rowElements = rowsElement.getElementsByTagName('row');
    for (let i = 0; i < rowElements.length; i++) {
      const row = parseRow(rowElements[i]);
      if (row) {
        rows.push(row);
      }
    }
  }
  
  return {
    id,
    name,
    label,
    visible,
    rows,
  };
}

function parseRow(rowElement: Element): FormRow | null {
  const cells: FormCell[] = [];
  const cellElements = rowElement.getElementsByTagName('cell');
  
  for (let i = 0; i < cellElements.length; i++) {
    const cellElement = cellElements[i];
    const cell = parseCell(cellElement);
    if (cell) {
      cells.push(cell);
    }
  }
  
  return { cells };
}

function parseCell(cellElement: Element): FormCell | null {
  const colspan = parseInt(cellElement.getAttribute('colspan') || '1');
  const rowspan = parseInt(cellElement.getAttribute('rowspan') || '1');
  
  // Get control
  let control: FormControl | undefined;
  const controlElements = cellElement.getElementsByTagName('control');
  if (controlElements.length > 0) {
    const controlElement = controlElements[0];
    
    // Get label from cell's labels element
    let label = '';
    const labelsElement = cellElement.getElementsByTagName('labels')[0];
    if (labelsElement) {
      const labelElements = labelsElement.getElementsByTagName('label');
      if (labelElements.length > 0) {
        label = labelElements[0].getAttribute('description') || '';
      }
    }
    
    control = {
      id: controlElement.getAttribute('id') || '',
      datafieldname: controlElement.getAttribute('datafieldname') || undefined,
      classid: controlElement.getAttribute('classid') || '',
      label: label || undefined,
      disabled: controlElement.getAttribute('disabled') === 'true',
    };
  }
  
  return {
    control,
    colspan,
    rowspan,
  };
}

/**
 * Parse form libraries (script references) from FormXML
 * Reference: https://learn.microsoft.com/en-us/power-apps/developer/model-driven-apps/clientapi/reference/formcontext-data-process
 */
function parseFormLibraries(formElement: Element): FormLibrary[] {
  const libraries: FormLibrary[] = [];
  
  const formLibrariesElement = formElement.getElementsByTagName('formLibraries')[0];
  if (!formLibrariesElement) {
    return libraries;
  }
  
  const libraryElements = formLibrariesElement.getElementsByTagName('Library');
  for (let i = 0; i < libraryElements.length; i++) {
    const libraryElement = libraryElements[i];
    const name = libraryElement.getAttribute('name') || '';
    const libraryUniqueId = libraryElement.getAttribute('libraryUniqueId') || '';
    
    if (name) {
      libraries.push({ name, libraryUniqueId });
    }
  }
  
  return libraries;
}

/**
 * Parse form events from FormXML
 * Reference: https://learn.microsoft.com/en-us/power-apps/developer/model-driven-apps/clientapi/reference/events
 */
function parseFormEvents(formElement: Element): FormEvent[] {
  const events: FormEvent[] = [];
  
  const eventsElement = formElement.getElementsByTagName('events')[0];
  if (!eventsElement) {
    return events;
  }
  
  const eventElements = eventsElement.getElementsByTagName('event');
  for (let i = 0; i < eventElements.length; i++) {
    const eventElement = eventElements[i];
    const name = eventElement.getAttribute('name') || '';
    const application = eventElement.getAttribute('application') === 'true';
    const active = eventElement.getAttribute('active') === 'true';
    const attribute = eventElement.getAttribute('attribute') || undefined;
    
    // Parse handlers for this event
    const handlerElements = eventElement.getElementsByTagName('Handler');
    for (let j = 0; j < handlerElements.length; j++) {
      const handlerElement = handlerElements[j];
      const functionName = handlerElement.getAttribute('functionName') || undefined;
      const libraryName = handlerElement.getAttribute('libraryName') || undefined;
      const parametersAttr = handlerElement.getAttribute('parameters') || '';
      const parameters = parametersAttr ? parametersAttr.split(',').map(p => p.trim()) : [];
      
      events.push({
        name,
        application,
        active,
        attribute,
        functionName,
        libraryName,
        parameters,
      });
    }
  }
  
  return events;
}
