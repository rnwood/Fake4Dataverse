/**
 * Utilities for parsing and working with SiteMap XML
 * Reference: https://learn.microsoft.com/en-us/power-apps/developer/model-driven-apps/create-site-map-app
 */

import type { SiteMapDefinition, SiteMapArea, SiteMapGroup, SiteMapSubArea } from '../types/dataverse';

/**
 * Parse SiteMap XML into a structured format
 * SiteMap XML structure:
 * <SiteMap>
 *   <Area Id="..." Title="...">
 *     <Group Id="..." Title="...">
 *       <SubArea Id="..." Title="..." Entity="..." />
 *     </Group>
 *   </Area>
 * </SiteMap>
 */
export function parseSiteMapXml(xml: string): SiteMapDefinition {
  if (typeof window === 'undefined') {
    // Server-side: return empty structure
    return { areas: [] };
  }

  try {
    const parser = new DOMParser();
    const doc = parser.parseFromString(xml, 'text/xml');
    
    const areas: SiteMapArea[] = [];
    const areaElements = doc.querySelectorAll('SiteMap > Area');
    
    areaElements.forEach(areaEl => {
      const area: SiteMapArea = {
        id: areaEl.getAttribute('Id') || '',
        title: areaEl.getAttribute('Title') || areaEl.getAttribute('Id') || 'Untitled',
        icon: areaEl.getAttribute('Icon') || undefined,
        groups: []
      };
      
      const groupElements = areaEl.querySelectorAll(':scope > Group');
      groupElements.forEach(groupEl => {
        const group: SiteMapGroup = {
          id: groupEl.getAttribute('Id') || '',
          title: groupEl.getAttribute('Title') || groupEl.getAttribute('Id') || 'Untitled',
          subareas: []
        };
        
        const subareaElements = groupEl.querySelectorAll(':scope > SubArea');
        subareaElements.forEach(subareaEl => {
          const subarea: SiteMapSubArea = {
            id: subareaEl.getAttribute('Id') || '',
            title: subareaEl.getAttribute('Title') || subareaEl.getAttribute('Id') || 'Untitled',
            entity: subareaEl.getAttribute('Entity') || undefined,
            url: subareaEl.getAttribute('Url') || undefined,
            icon: subareaEl.getAttribute('Icon') || undefined
          };
          group.subareas.push(subarea);
        });
        
        area.groups.push(group);
      });
      
      areas.push(area);
    });
    
    return { areas };
  } catch (error) {
    console.error('Error parsing sitemap XML:', error);
    return { areas: [] };
  }
}

/**
 * Create a simple sitemap XML for testing
 */
export function createDefaultSiteMapXml(): string {
  return `<?xml version="1.0" encoding="utf-8"?>
<SiteMap>
  <Area Id="area_sales" Title="Sales" Icon="mdi-cash">
    <Group Id="group_customers" Title="Customers">
      <SubArea Id="subarea_accounts" Title="Accounts" Entity="account" Icon="mdi-domain" />
      <SubArea Id="subarea_contacts" Title="Contacts" Entity="contact" Icon="mdi-account" />
    </Group>
    <Group Id="group_sales" Title="Sales">
      <SubArea Id="subarea_opportunities" Title="Opportunities" Entity="opportunity" Icon="mdi-currency-usd" />
      <SubArea Id="subarea_leads" Title="Leads" Entity="lead" Icon="mdi-account-star" />
    </Group>
  </Area>
  <Area Id="area_service" Title="Service" Icon="mdi-lifebuoy">
    <Group Id="group_cases" Title="Cases">
      <SubArea Id="subarea_cases" Title="Cases" Entity="incident" Icon="mdi-ticket" />
      <SubArea Id="subarea_knowledge" Title="Knowledge Articles" Entity="knowledgearticle" Icon="mdi-book-open-variant" />
    </Group>
  </Area>
</SiteMap>`;
}
