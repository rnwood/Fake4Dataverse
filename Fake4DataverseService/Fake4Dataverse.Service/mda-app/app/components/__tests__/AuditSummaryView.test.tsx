/**
 * @jest-environment jsdom
 */
import React from 'react';
import { render, screen, waitFor } from '@testing-library/react';
import '@testing-library/jest-dom';
import AuditSummaryView from '../AuditSummaryView';
import { dataverseClient } from '../../lib/dataverse-client';

// Mock the dataverse client
jest.mock('../../lib/dataverse-client');

describe('AuditSummaryView', () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  it('should render loading state initially', () => {
    (dataverseClient.fetchAuditStatus as jest.Mock).mockImplementation(
      () => new Promise(() => {}) // Never resolves
    );
    (dataverseClient.fetchAuditRecords as jest.Mock).mockImplementation(
      () => new Promise(() => {})
    );

    render(<AuditSummaryView />);
    expect(screen.getByText('Loading audit history...')).toBeInTheDocument();
  });

  it('should display audit records when loaded', async () => {
    const mockAuditRecords = [
      {
        auditid: '123e4567-e89b-12d3-a456-426614174000',
        action: 1,
        operation: 'Create',
        objectid: {
          logicalName: 'account',
          id: '123e4567-e89b-12d3-a456-426614174001',
          name: 'Test Account',
        },
        objecttypecode: 'account',
        userid: {
          logicalName: 'systemuser',
          id: '123e4567-e89b-12d3-a456-426614174002',
          name: 'Test User',
        },
        createdon: '2024-01-01T12:00:00Z',
      },
    ];

    (dataverseClient.fetchAuditStatus as jest.Mock).mockResolvedValue({
      isAuditEnabled: true,
    });
    (dataverseClient.fetchAuditRecords as jest.Mock).mockResolvedValue({
      value: mockAuditRecords,
      count: 1,
    });

    render(<AuditSummaryView />);

    await waitFor(() => {
      expect(screen.getByText('Audit History')).toBeInTheDocument();
    });

    await waitFor(() => {
      expect(screen.getByText('Create')).toBeInTheDocument();
      expect(screen.getByText('account')).toBeInTheDocument();
      expect(screen.getByText('Test User')).toBeInTheDocument();
    });
  });

  it('should display empty state when no audit records', async () => {
    (dataverseClient.fetchAuditStatus as jest.Mock).mockResolvedValue({
      isAuditEnabled: false,
    });
    (dataverseClient.fetchAuditRecords as jest.Mock).mockResolvedValue({
      value: [],
      count: 0,
    });

    render(<AuditSummaryView />);

    await waitFor(() => {
      expect(screen.getByText('No Audit Records')).toBeInTheDocument();
    });
  });

  it('should display error when API fails', async () => {
    (dataverseClient.fetchAuditStatus as jest.Mock).mockRejectedValue(
      new Error('API Error')
    );
    (dataverseClient.fetchAuditRecords as jest.Mock).mockRejectedValue(
      new Error('API Error')
    );

    render(<AuditSummaryView />);

    await waitFor(() => {
      expect(screen.getByText('Error Loading Audit History')).toBeInTheDocument();
      expect(screen.getByText('API Error')).toBeInTheDocument();
    });
  });

  it('should display action badges with correct colors', async () => {
    const mockAuditRecords = [
      {
        auditid: '123e4567-e89b-12d3-a456-426614174000',
        action: 1, // Create
        operation: 'Create',
        objectid: {
          logicalName: 'account',
          id: '123e4567-e89b-12d3-a456-426614174001',
        },
        objecttypecode: 'account',
        userid: {
          logicalName: 'systemuser',
          id: '123e4567-e89b-12d3-a456-426614174002',
        },
        createdon: '2024-01-01T12:00:00Z',
      },
      {
        auditid: '123e4567-e89b-12d3-a456-426614174003',
        action: 2, // Update
        operation: 'Update',
        objectid: {
          logicalName: 'account',
          id: '123e4567-e89b-12d3-a456-426614174001',
        },
        objecttypecode: 'account',
        userid: {
          logicalName: 'systemuser',
          id: '123e4567-e89b-12d3-a456-426614174002',
        },
        createdon: '2024-01-01T12:05:00Z',
      },
      {
        auditid: '123e4567-e89b-12d3-a456-426614174004',
        action: 3, // Delete
        operation: 'Delete',
        objectid: {
          logicalName: 'account',
          id: '123e4567-e89b-12d3-a456-426614174001',
        },
        objecttypecode: 'account',
        userid: {
          logicalName: 'systemuser',
          id: '123e4567-e89b-12d3-a456-426614174002',
        },
        createdon: '2024-01-01T12:10:00Z',
      },
    ];

    (dataverseClient.fetchAuditStatus as jest.Mock).mockResolvedValue({
      isAuditEnabled: true,
    });
    (dataverseClient.fetchAuditRecords as jest.Mock).mockResolvedValue({
      value: mockAuditRecords,
      count: 3,
    });

    render(<AuditSummaryView />);

    await waitFor(() => {
      // Check that each action type appears in the table
      expect(screen.getByRole('table')).toBeInTheDocument();
      expect(screen.getAllByText('account')).toHaveLength(3);
    });
  });
});
