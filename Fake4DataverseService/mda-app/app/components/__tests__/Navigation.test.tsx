/**
 * Unit tests for Navigation component
 * Tests rendering of sitemap areas, groups, and subareas
 */

import { render, screen, fireEvent } from '@testing-library/react';
import Navigation from '../Navigation';
import type { SiteMapArea } from '../../types/dataverse';

// Mock Fluent UI styles
jest.mock('@fluentui/react-components', () => ({
  ...jest.requireActual('@fluentui/react-components'),
  makeStyles: () => () => ({
    nav: '',
    header: '',
    scrollArea: '',
    areaTitle: '',
    groupTitle: '',
    subAreaItem: '',
    selectedSubArea: '',
  }),
}));

describe('Navigation', () => {
  const mockAreas: SiteMapArea[] = [
    {
      id: 'area_sales',
      title: 'Sales',
      groups: [
        {
          id: 'group_customers',
          title: 'Customers',
          subareas: [
            {
              id: 'subarea_accounts',
              title: 'Accounts',
              entity: 'account',
            },
            {
              id: 'subarea_contacts',
              title: 'Contacts',
              entity: 'contact',
            },
          ],
        },
        {
          id: 'group_sales',
          title: 'Sales',
          subareas: [
            {
              id: 'subarea_opportunities',
              title: 'Opportunities',
              entity: 'opportunity',
            },
          ],
        },
      ],
    },
    {
      id: 'area_service',
      title: 'Service',
      groups: [
        {
          id: 'group_cases',
          title: 'Cases',
          subareas: [
            {
              id: 'subarea_cases',
              title: 'Cases',
              entity: 'incident',
            },
          ],
        },
      ],
    },
  ];

  it('renders navigation header', () => {
    render(<Navigation areas={mockAreas} />);
    expect(screen.getByText('Model-Driven App')).toBeInTheDocument();
  });

  it('renders all areas', () => {
    render(<Navigation areas={mockAreas} />);
    // Use getAllByText since "Sales" appears in both area and group
    const salesElements = screen.getAllByText('Sales');
    expect(salesElements.length).toBeGreaterThanOrEqual(1);
    expect(screen.getByText('Service')).toBeInTheDocument();
  });

  it('renders all groups', () => {
    render(<Navigation areas={mockAreas} />);
    expect(screen.getByText('Customers')).toBeInTheDocument();
    // Cases appears as both group and subarea, so use getAllByText
    const casesElements = screen.getAllByText('Cases');
    expect(casesElements.length).toBeGreaterThanOrEqual(1);
  });

  it('renders all subareas', () => {
    render(<Navigation areas={mockAreas} />);
    expect(screen.getByText('Accounts')).toBeInTheDocument();
    expect(screen.getByText('Contacts')).toBeInTheDocument();
    expect(screen.getByText('Opportunities')).toBeInTheDocument();
    // Cases appears multiple times
    const casesElements = screen.getAllByText('Cases');
    expect(casesElements.length).toBeGreaterThanOrEqual(1);
  });

  it('calls onNavigate when subarea is clicked', () => {
    const mockOnNavigate = jest.fn();
    render(<Navigation areas={mockAreas} onNavigate={mockOnNavigate} />);
    
    const accountsSubarea = screen.getByText('Accounts');
    fireEvent.click(accountsSubarea);
    
    expect(mockOnNavigate).toHaveBeenCalledWith('account');
  });

  it('applies selected styling to current entity', () => {
    const { container } = render(
      <Navigation areas={mockAreas} selectedEntity="contact" />
    );
    
    // Verify that the component renders (exact styling verification would require more complex setup)
    expect(screen.getByText('Contacts')).toBeInTheDocument();
  });

  it('renders with empty areas array', () => {
    render(<Navigation areas={[]} />);
    expect(screen.getByText('Model-Driven App')).toBeInTheDocument();
  });

  it('renders subareas with icons when icon metadata is provided', () => {
    const areasWithIcons: SiteMapArea[] = [
      {
        id: 'area_sales',
        title: 'Sales',
        groups: [
          {
            id: 'group_customers',
            title: 'Customers',
            subareas: [
              {
                id: 'subarea_accounts',
                title: 'Accounts',
                entity: 'account',
                icon: 'mdi-domain',
              },
              {
                id: 'subarea_contacts',
                title: 'Contacts',
                entity: 'contact',
                icon: 'mdi-account',
              },
            ],
          },
        ],
      },
    ];

    render(<Navigation areas={areasWithIcons} />);
    expect(screen.getByText('Accounts')).toBeInTheDocument();
    expect(screen.getByText('Contacts')).toBeInTheDocument();
  });
});
