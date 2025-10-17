/**
 * @jest-environment jsdom
 */
import React from 'react';
import { render, screen, waitFor } from '@testing-library/react';
import '@testing-library/jest-dom';
import AuditRecordView from '../AuditRecordView';
import { dataverseClient } from '../../lib/dataverse-client';

// Mock the dataverse client
jest.mock('../../lib/dataverse-client');

describe('AuditRecordView', () => {
  const mockEntityName = 'account';
  const mockRecordId = '123e4567-e89b-12d3-a456-426614174000';

  beforeEach(() => {
    jest.clearAllMocks();
  });

  it('should render loading state initially', () => {
    (dataverseClient.fetchEntityAuditRecords as jest.Mock).mockImplementation(
      () => new Promise(() => {}) // Never resolves
    );

    render(<AuditRecordView entityName={mockEntityName} recordId={mockRecordId} />);
    expect(screen.getByText('Loading audit history...')).toBeInTheDocument();
  });

  it('should display audit records for entity', async () => {
    const mockAuditRecords = [
      {
        auditid: '123e4567-e89b-12d3-a456-426614174001',
        action: 1,
        operation: 'Create',
        objectid: {
          logicalName: 'account',
          id: mockRecordId,
        },
        objecttypecode: 'account',
        userid: {
          logicalName: 'systemuser',
          id: '123e4567-e89b-12d3-a456-426614174002',
          name: 'Test User',
        },
        createdon: '2024-01-01T12:00:00Z',
      },
      {
        auditid: '123e4567-e89b-12d3-a456-426614174003',
        action: 2,
        operation: 'Update',
        objectid: {
          logicalName: 'account',
          id: mockRecordId,
        },
        objecttypecode: 'account',
        userid: {
          logicalName: 'systemuser',
          id: '123e4567-e89b-12d3-a456-426614174002',
          name: 'Test User',
        },
        createdon: '2024-01-01T12:05:00Z',
      },
    ];

    (dataverseClient.fetchEntityAuditRecords as jest.Mock).mockResolvedValue({
      value: mockAuditRecords,
      count: 2,
    });

    render(<AuditRecordView entityName={mockEntityName} recordId={mockRecordId} />);

    await waitFor(() => {
      expect(screen.getByText('Audit History')).toBeInTheDocument();
    });

    await waitFor(() => {
      expect(screen.getByText('Create')).toBeInTheDocument();
      expect(screen.getByText('Update')).toBeInTheDocument();
      // Check for "Create by Test User" and "Update by Test User"
      expect(screen.getAllByText(/Test User/)).toHaveLength(2);
    });
  });

  it('should display empty state when no audit records', async () => {
    (dataverseClient.fetchEntityAuditRecords as jest.Mock).mockResolvedValue({
      value: [],
      count: 0,
    });

    render(<AuditRecordView entityName={mockEntityName} recordId={mockRecordId} />);

    await waitFor(() => {
      expect(screen.getByText('No Audit Records')).toBeInTheDocument();
      expect(screen.getByText('No audit history found for this record.')).toBeInTheDocument();
    });
  });

  it('should display error when API fails', async () => {
    (dataverseClient.fetchEntityAuditRecords as jest.Mock).mockRejectedValue(
      new Error('API Error')
    );

    render(<AuditRecordView entityName={mockEntityName} recordId={mockRecordId} />);

    await waitFor(() => {
      expect(screen.getByText('Error Loading Audit History')).toBeInTheDocument();
      expect(screen.getByText('API Error')).toBeInTheDocument();
    });
  });

  it('should load audit details when accordion is expanded', async () => {
    const mockAuditRecords = [
      {
        auditid: '123e4567-e89b-12d3-a456-426614174001',
        action: 2,
        operation: 'Update',
        objectid: {
          logicalName: 'account',
          id: mockRecordId,
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

    const mockAuditDetails = {
      auditRecord: mockAuditRecords[0],
      oldValue: {
        name: 'Old Name',
        revenue: 100000,
      },
      newValue: {
        name: 'New Name',
        revenue: 200000,
      },
    };

    (dataverseClient.fetchEntityAuditRecords as jest.Mock).mockResolvedValue({
      value: mockAuditRecords,
      count: 1,
    });
    (dataverseClient.fetchAuditDetails as jest.Mock).mockResolvedValue(mockAuditDetails);

    render(<AuditRecordView entityName={mockEntityName} recordId={mockRecordId} />);

    await waitFor(() => {
      expect(screen.getByText('Update')).toBeInTheDocument();
    });

    // Click the accordion to expand it
    const accordionHeader = screen.getByText(/Update by Test User/);
    accordionHeader.click();

    // Wait for audit details to load
    await waitFor(() => {
      expect(dataverseClient.fetchAuditDetails).toHaveBeenCalledWith(
        '123e4567-e89b-12d3-a456-426614174001'
      );
    });
  });
});
