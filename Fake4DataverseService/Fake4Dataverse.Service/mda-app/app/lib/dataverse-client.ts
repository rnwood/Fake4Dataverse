/**
 * OData API client for Dataverse Web API
 * Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/webapi/overview
 */

import type { ODataResponse, EntityRecord } from '../types/dataverse';

const API_BASE_URL = '/api/data/v9.2';

export class DataverseApiClient {
  /**
   * Fetch entities with OData query options
   * Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/webapi/query-data-web-api
   */
  async fetchEntities(
    entityPluralName: string,
    options?: {
      select?: string[];
      filter?: string;
      orderby?: string;
      top?: number;
      skip?: number;
      count?: boolean;
    }
  ): Promise<ODataResponse<EntityRecord>> {
    const params = new URLSearchParams();
    
    if (options?.select?.length) {
      params.append('$select', options.select.join(','));
    }
    if (options?.filter) {
      params.append('$filter', options.filter);
    }
    if (options?.orderby) {
      params.append('$orderby', options.orderby);
    }
    if (options?.top !== undefined) {
      params.append('$top', options.top.toString());
    }
    if (options?.skip !== undefined) {
      params.append('$skip', options.skip.toString());
    }
    if (options?.count) {
      params.append('$count', 'true');
    }

    const url = `${API_BASE_URL}/${entityPluralName}${params.toString() ? '?' + params.toString() : ''}`;
    
    const response = await fetch(url, {
      method: 'GET',
      headers: {
        'Accept': 'application/json',
        'OData-MaxVersion': '4.0',
        'OData-Version': '4.0',
      },
    });

    if (!response.ok) {
      throw new Error(`API request failed: ${response.status} ${response.statusText}`);
    }

    return response.json();
  }

  /**
   * Fetch a single entity by ID
   */
  async fetchEntity(
    entityPluralName: string,
    id: string,
    options?: {
      select?: string[];
    }
  ): Promise<EntityRecord> {
    const params = new URLSearchParams();
    
    if (options?.select?.length) {
      params.append('$select', options.select.join(','));
    }

    const url = `${API_BASE_URL}/${entityPluralName}(${id})${params.toString() ? '?' + params.toString() : ''}`;
    
    const response = await fetch(url, {
      method: 'GET',
      headers: {
        'Accept': 'application/json',
        'OData-MaxVersion': '4.0',
        'OData-Version': '4.0',
      },
    });

    if (!response.ok) {
      throw new Error(`API request failed: ${response.status} ${response.statusText}`);
    }

    return response.json();
  }

  /**
   * Create a new entity
   */
  async createEntity(
    entityPluralName: string,
    data: Record<string, any>
  ): Promise<string> {
    const url = `${API_BASE_URL}/${entityPluralName}`;
    
    const response = await fetch(url, {
      method: 'POST',
      headers: {
        'Accept': 'application/json',
        'Content-Type': 'application/json',
        'OData-MaxVersion': '4.0',
        'OData-Version': '4.0',
      },
      body: JSON.stringify(data),
    });

    if (!response.ok) {
      throw new Error(`API request failed: ${response.status} ${response.statusText}`);
    }

    // Extract ID from Location header
    const location = response.headers.get('Location');
    if (location) {
      const match = location.match(/\(([^)]+)\)/);
      if (match) {
        return match[1];
      }
    }

    return '';
  }

  /**
   * Update an existing entity
   */
  async updateEntity(
    entityPluralName: string,
    id: string,
    data: Record<string, any>
  ): Promise<void> {
    const url = `${API_BASE_URL}/${entityPluralName}(${id})`;
    
    const response = await fetch(url, {
      method: 'PATCH',
      headers: {
        'Accept': 'application/json',
        'Content-Type': 'application/json',
        'OData-MaxVersion': '4.0',
        'OData-Version': '4.0',
      },
      body: JSON.stringify(data),
    });

    if (!response.ok) {
      throw new Error(`API request failed: ${response.status} ${response.statusText}`);
    }
  }

  /**
   * Delete an entity
   */
  async deleteEntity(
    entityPluralName: string,
    id: string
  ): Promise<void> {
    const url = `${API_BASE_URL}/${entityPluralName}(${id})`;
    
    const response = await fetch(url, {
      method: 'DELETE',
      headers: {
        'OData-MaxVersion': '4.0',
        'OData-Version': '4.0',
      },
    });

    if (!response.ok) {
      throw new Error(`API request failed: ${response.status} ${response.statusText}`);
    }
  }

  /**
   * Fetch entity metadata (table definitions)
   * Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/webapi/reference/entitymetadata
   */
  async fetchEntityDefinitions(
    options?: {
      select?: string[];
      filter?: string;
    }
  ): Promise<any> {
    const params = new URLSearchParams();
    
    if (options?.select?.length) {
      params.append('$select', options.select.join(','));
    }
    if (options?.filter) {
      params.append('$filter', options.filter);
    }

    const url = `${API_BASE_URL}/EntityDefinitions${params.toString() ? '?' + params.toString() : ''}`;
    
    const response = await fetch(url, {
      method: 'GET',
      headers: {
        'Accept': 'application/json',
        'OData-MaxVersion': '4.0',
        'OData-Version': '4.0',
      },
    });

    if (!response.ok) {
      throw new Error(`API request failed: ${response.status} ${response.statusText}`);
    }

    return response.json();
  }

  /**
   * Fetch entity metadata by logical name
   * Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/webapi/reference/entitymetadata
   */
  async fetchEntityDefinition(
    logicalName: string,
    options?: {
      expand?: string[];
    }
  ): Promise<any> {
    const params = new URLSearchParams();
    
    if (options?.expand?.length) {
      params.append('$expand', options.expand.join(','));
    }

    const url = `${API_BASE_URL}/EntityDefinitions(LogicalName='${logicalName}')${params.toString() ? '?' + params.toString() : ''}`;
    
    const response = await fetch(url, {
      method: 'GET',
      headers: {
        'Accept': 'application/json',
        'OData-MaxVersion': '4.0',
        'OData-Version': '4.0',
      },
    });

    if (!response.ok) {
      throw new Error(`API request failed: ${response.status} ${response.statusText}`);
    }

    return response.json();
  }
}

export const dataverseClient = new DataverseApiClient();
