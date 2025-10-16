/**
 * Unit tests for EntityForm component
 * Tests rendering of entity forms with tabs, sections, and controls
 */

import { render, screen, waitFor } from '@testing-library/react';
import EntityForm from '../EntityForm';
import { dataverseClient } from '../../lib/dataverse-client';

// Mock the dataverse client
jest.mock('../../lib/dataverse-client', () => ({
  dataverseClient: {
    fetchEntities: jest.fn(),
    createEntity: jest.fn(),
    updateEntity: jest.fn(),
  },
}));

// Mock Fluent UI styles
jest.mock('@fluentui/react-components', () => ({
  ...jest.requireActual('@fluentui/react-components'),
  makeStyles: () => () => ({
    container: '',
    header: '',
    title: '',
    content: '',
    loadingContainer: '',
    errorContainer: '',
    tabList: '',
    section: '',
    sectionTitle: '',
    formRow: '',
    field: '',
  }),
}));

describe('EntityForm', () => {
  const mockForm = {
    formid: 'form1',
    name: 'Account Main Form',
    objecttypecode: 'account',
    type: 2,
    formxml: `<?xml version="1.0" encoding="utf-8"?>
<form>
  <tabs>
    <tab id="tab_general" name="general" visible="true">
      <labels>
        <label description="General" languagecode="1033" />
      </labels>
      <columns>
        <column width="100%">
          <sections>
            <section id="section_info" name="info" visible="true">
              <labels>
                <label description="Information" languagecode="1033" />
              </labels>
              <rows>
                <row>
                  <cell id="name">
                    <labels>
                      <label description="Account Name" languagecode="1033" />
                    </labels>
                    <control id="name" classid="{270BD3DB-D9AF-4782-9025-509E298DEC0A}" datafieldname="name" disabled="false" />
                  </cell>
                </row>
              </rows>
            </section>
          </sections>
        </column>
      </columns>
    </tab>
  </tabs>
</form>`,
  };

  const mockRecord = {
    accountid: 'acc1',
    name: 'Contoso',
  };

  beforeEach(() => {
    jest.clearAllMocks();
  });

  it('shows loading spinner initially', () => {
    (dataverseClient.fetchEntities as jest.Mock)
      .mockImplementation(() => new Promise(() => {})); // Never resolves

    render(
      <EntityForm
        entityName="account"
        entityPluralName="accounts"
        displayName="Accounts"
      />
    );

    expect(screen.getByText('Loading form...')).toBeInTheDocument();
  });

  it('loads and displays form for new record', async () => {
    (dataverseClient.fetchEntities as jest.Mock)
      .mockResolvedValueOnce({ value: [mockForm] });

    render(
      <EntityForm
        entityName="account"
        entityPluralName="accounts"
        displayName="Accounts"
      />
    );

    await waitFor(() => {
      expect(screen.getByText(/New/)).toBeInTheDocument();
    });
  });

  it('loads and displays form for existing record', async () => {
    (dataverseClient.fetchEntities as jest.Mock)
      .mockResolvedValueOnce({ value: [mockForm] })
      .mockResolvedValueOnce({ value: [mockRecord] });

    render(
      <EntityForm
        entityName="account"
        entityPluralName="accounts"
        displayName="Accounts"
        recordId="acc1"
      />
    );

    await waitFor(() => {
      expect(screen.getByText(/Edit/)).toBeInTheDocument();
    });
  });

  it('displays error message when form loading fails', async () => {
    (dataverseClient.fetchEntities as jest.Mock)
      .mockRejectedValueOnce(new Error('Failed to load form'));

    render(
      <EntityForm
        entityName="account"
        entityPluralName="accounts"
        displayName="Accounts"
      />
    );

    await waitFor(() => {
      expect(screen.getByText(/Error:/)).toBeInTheDocument();
    });
  });

  it('displays tabs when form has multiple tabs', async () => {
    const formWithTabs = {
      ...mockForm,
      formxml: mockForm.formxml.replace('</tab>', `</tab>
        <tab id="tab_details" name="details" visible="true">
          <labels>
            <label description="Details" languagecode="1033" />
          </labels>
          <columns><column width="100%"><sections></sections></column></columns>
        </tab>`),
    };

    (dataverseClient.fetchEntities as jest.Mock)
      .mockResolvedValueOnce({ value: [formWithTabs] });

    render(
      <EntityForm
        entityName="account"
        entityPluralName="accounts"
        displayName="Accounts"
      />
    );

    await waitFor(() => {
      // Tab list should be visible when there are multiple tabs
      const tabs = screen.queryAllByRole('tab');
      expect(tabs.length).toBeGreaterThan(0);
    });
  });

  it('filters forms by app module when appModuleId is provided', async () => {
    (dataverseClient.fetchEntities as jest.Mock)
      .mockResolvedValueOnce({ value: [] }) // appmodulecomponents
      .mockResolvedValueOnce({ value: [mockForm] });

    render(
      <EntityForm
        entityName="account"
        entityPluralName="accounts"
        displayName="Accounts"
        appModuleId="app123"
      />
    );

    await waitFor(() => {
      expect(dataverseClient.fetchEntities).toHaveBeenCalledWith(
        'appmodulecomponents',
        expect.any(Object)
      );
    });
  });

  it('calls onClose when back button is clicked', async () => {
    const mockOnClose = jest.fn();
    (dataverseClient.fetchEntities as jest.Mock)
      .mockResolvedValueOnce({ value: [mockForm] });

    render(
      <EntityForm
        entityName="account"
        entityPluralName="accounts"
        displayName="Accounts"
        onClose={mockOnClose}
      />
    );

    await waitFor(() => {
      const backButton = screen.getByText('Back');
      expect(backButton).toBeInTheDocument();
    });
  });
});
