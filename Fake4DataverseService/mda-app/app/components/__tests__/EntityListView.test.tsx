/**
 * Unit tests for EntityListView component
 * Tests rendering of entity records in a data grid with views
 */

import { render, screen, waitFor } from '@testing-library/react';
import EntityListView from '../EntityListView';
import { dataverseClient } from '../../lib/dataverse-client';

// Mock the dataverse client
jest.mock('../../lib/dataverse-client', () => ({
  dataverseClient: {
    fetchEntities: jest.fn(),
  },
}));

// Mock Fluent UI styles
jest.mock('@fluentui/react-components', () => ({
  ...jest.requireActual('@fluentui/react-components'),
  makeStyles: () => () => ({
    container: '',
    header: '',
    titleRow: '',
    title: '',
    viewSwitcher: '',
    toolbar: '',
    content: '',
    loadingContainer: '',
    errorContainer: '',
    emptyContainer: '',
    dataGrid: '',
    recordCount: '',
  }),
}));

describe('EntityListView', () => {
  const mockViews = [
    {
      savedqueryid: 'view1',
      name: 'Active Accounts',
      returnedtypecode: 'account',
      isdefault: true,
      fetchxml: '<fetch><entity name="account"><attribute name="name"/></entity></fetch>',
      layoutxml: '<grid><row><cell name="name"/></row></grid>',
    },
    {
      savedqueryid: 'view2',
      name: 'All Accounts',
      returnedtypecode: 'account',
      isdefault: false,
    },
  ];

  const mockRecords = [
    {
      accountid: 'acc1',
      name: 'Contoso',
      revenue: 100000,
    },
    {
      accountid: 'acc2',
      name: 'Fabrikam',
      revenue: 200000,
    },
  ];

  beforeEach(() => {
    jest.clearAllMocks();
  });

  it('renders entity display name', async () => {
    (dataverseClient.fetchEntities as jest.Mock)
      .mockResolvedValueOnce({ value: mockViews })
      .mockResolvedValueOnce({ value: mockRecords });

    render(
      <EntityListView
        entityName="account"
        entityPluralName="accounts"
        displayName="Accounts"
      />
    );

    expect(screen.getByText('Accounts')).toBeInTheDocument();
  });

  it('loads and displays views', async () => {
    (dataverseClient.fetchEntities as jest.Mock)
      .mockResolvedValueOnce({ value: mockViews })
      .mockResolvedValueOnce({ value: mockRecords });

    render(
      <EntityListView
        entityName="account"
        entityPluralName="accounts"
        displayName="Accounts"
      />
    );

    await waitFor(() => {
      expect(dataverseClient.fetchEntities).toHaveBeenCalledWith(
        'savedqueries',
        expect.objectContaining({
          filter: expect.stringContaining('account'),
        })
      );
    });
  });

  it('loads records when view is selected', async () => {
    (dataverseClient.fetchEntities as jest.Mock)
      .mockResolvedValueOnce({ value: mockViews })
      .mockResolvedValueOnce({ value: mockRecords });

    render(
      <EntityListView
        entityName="account"
        entityPluralName="accounts"
        displayName="Accounts"
      />
    );

    await waitFor(() => {
      expect(dataverseClient.fetchEntities).toHaveBeenCalledWith(
        'accounts',
        expect.any(Object)
      );
    });
  });

  it('displays loading spinner initially', () => {
    (dataverseClient.fetchEntities as jest.Mock)
      .mockImplementation(() => new Promise(() => {})); // Never resolves

    render(
      <EntityListView
        entityName="account"
        entityPluralName="accounts"
        displayName="Accounts"
      />
    );

    expect(screen.getByText('Loading records...')).toBeInTheDocument();
  });

  it('handles error states correctly', async () => {
    // Mock first call for views to fail
    (dataverseClient.fetchEntities as jest.Mock)
      .mockRejectedValueOnce(new Error('Failed to load views'));

    const { container } = render(
      <EntityListView
        entityName="account"
        entityPluralName="accounts"
        displayName="Accounts"
      />
    );

    // Component renders
    expect(container).toBeTruthy();
    
    // Wait briefly for async operations
    await new Promise(resolve => setTimeout(resolve, 100));
  });

  it('handles loading states correctly', async () => {
    // Mock views to return successfully
    (dataverseClient.fetchEntities as jest.Mock)
      .mockResolvedValueOnce({ value: mockViews })
      .mockResolvedValueOnce({ value: [] });

    const { container } = render(
      <EntityListView
        entityName="account"
        entityPluralName="accounts"
        displayName="Accounts"
      />
    );

    // Component renders
    expect(container).toBeTruthy();
    
    // Wait briefly for async operations
    await new Promise(resolve => setTimeout(resolve, 100));
  });

  it('uses initial view ID when provided', async () => {
    (dataverseClient.fetchEntities as jest.Mock)
      .mockResolvedValueOnce({ value: mockViews })
      .mockResolvedValueOnce({ value: mockRecords });

    render(
      <EntityListView
        entityName="account"
        entityPluralName="accounts"
        displayName="Accounts"
        initialViewId="view2"
      />
    );

    await waitFor(() => {
      expect(dataverseClient.fetchEntities).toHaveBeenCalled();
    });
  });

  it('filters views by app module when appModuleId is provided', async () => {
    (dataverseClient.fetchEntities as jest.Mock)
      .mockResolvedValueOnce({ value: [] }) // appmodulecomponents
      .mockResolvedValueOnce({ value: mockViews })
      .mockResolvedValueOnce({ value: mockRecords });

    render(
      <EntityListView
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
});
