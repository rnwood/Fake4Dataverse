'use client';

/**
 * FormList component - Display forms with expandable details
 * Shows form metadata with property grid for each form
 * Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/reference/entities/systemform
 */

import {
  makeStyles,
  tokens,
  Accordion,
  AccordionItem,
  AccordionHeader,
  AccordionPanel,
  Badge,
} from '@fluentui/react-components';
import type { SystemForm } from '../../types/dataverse';
import PropertyGrid from './PropertyGrid';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    gap: '8px',
  },
  formHeader: {
    display: 'flex',
    alignItems: 'center',
    gap: '8px',
  },
  badge: {
    marginLeft: '8px',
  },
});

interface FormListProps {
  forms: SystemForm[];
}

// Form type mapping
// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/reference/entities/systemform#type-choicesoptions
const FORM_TYPE_NAMES: Record<number, string> = {
  0: 'Dashboard',
  1: 'AppointmentBook',
  2: 'Main',
  4: 'Quick Create',
  5: 'Quick View Read-Only',
  6: 'Quick View',
  7: 'Dialog',
  8: 'Task Flow',
  9: 'InteractionCentric',
  11: 'Card',
  12: 'Main - Interactive experience',
};

export default function FormList({ forms }: FormListProps) {
  const styles = useStyles();

  const getFormTypeName = (typeCode: number): string => {
    return FORM_TYPE_NAMES[typeCode] || `Type ${typeCode}`;
  };

  const getFormProperties = (form: SystemForm): Record<string, string> => {
    return {
      'Form ID': form.formid,
      'Name': form.name,
      'Type': getFormTypeName(form.type),
      'Description': form.description || '',
      'Is Default': form.isdefault ? 'Yes' : 'No',
      'Entity': form.objecttypecode,
      'State': form.statecode === 0 ? 'Active' : 'Inactive',
      'Status': form.statuscode?.toString() || '',
    };
  };

  return (
    <div className={styles.container}>
      <Accordion multiple collapsible>
        {forms.map((form) => (
          <AccordionItem key={form.formid} value={form.formid}>
            <AccordionHeader>
              <div className={styles.formHeader}>
                <span>{form.name}</span>
                <Badge appearance="outline" className={styles.badge}>
                  {getFormTypeName(form.type)}
                </Badge>
                {form.isdefault && (
                  <Badge appearance="filled" color="success" className={styles.badge}>
                    Default
                  </Badge>
                )}
              </div>
            </AccordionHeader>
            <AccordionPanel>
              <PropertyGrid properties={getFormProperties(form)} />
            </AccordionPanel>
          </AccordionItem>
        ))}
      </Accordion>
    </div>
  );
}
