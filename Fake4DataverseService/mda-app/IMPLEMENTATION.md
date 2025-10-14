# MDA Forms Implementation Summary

## Overview

This PR implements form rendering functionality for the Fake4Dataverse Model-Driven App, along with comprehensive unit and e2e tests.

## Features Implemented

### 1. Form Rendering Component (`EntityForm.tsx`)

A new React component that renders entity forms based on SystemForm metadata:

**Key Features:**
- ✅ Parses FormXML to extract tabs, sections, and controls
- ✅ Renders multi-tab forms with tab navigation
- ✅ Displays sections with proper titles
- ✅ Renders basic input controls (text, number, money, date)
- ✅ Supports both create (new) and edit (existing) record modes
- ✅ Integrates with Dataverse Web API for CRUD operations
- ✅ Filters forms by AppModule when provided

**Reference:**
- Microsoft Docs: [SystemForm Entity](https://learn.microsoft.com/en-us/power-apps/developer/data-platform/reference/entities/systemform)
- Microsoft Docs: [Customize Entity Forms](https://learn.microsoft.com/en-us/power-apps/developer/model-driven-apps/customize-entity-forms)

### 2. Form XML Parser (`form-utils.ts`)

Utility functions to parse FormXML into structured TypeScript objects:

**Parsed Structure:**
```typescript
FormDefinition
├── tabs: FormTab[]
    ├── id, name, label, visible
    ├── sections: FormSection[]
        ├── id, name, label, visible
        ├── rows: FormRow[]
            ├── cells: FormCell[]
                ├── control: FormControl
                    ├── datafieldname
                    ├── classid (control type)
                    ├── label
                    ├── disabled
```

### 3. Navigation Integration

**Row Click Handler:**
- Clicking any row in EntityListView opens the form for that record
- URL format: `?pagetype=entityrecord&etn=account&id={guid}`

**New Button:**
- Enabled the "New" button in EntityListView toolbar
- Opens a blank form for creating new records
- URL format: `?pagetype=entityrecord&etn=account`

**Back Navigation:**
- Form includes "Back" button to return to list view
- Removes pagetype and id parameters from URL

### 4. URL Parameter Support

Following Microsoft Dynamics 365 URL conventions:
- `pagetype=entityrecord` - Opens a form
- `etn={entity}` - Entity type name
- `id={guid}` - Record ID (omitted for new records)
- `appid={guid}` - Application module ID
- `viewid={guid}` - View ID (for list views)

**Reference:**
- Microsoft Docs: [Navigate to Custom Pages](https://learn.microsoft.com/en-us/power-apps/developer/model-driven-apps/navigate-to-custom-page-examples)

### 5. SystemForm Metadata Generation

Updated `MdaInitializer.cs` to create sample SystemForm records:

**Forms Created:**
- Account Main Form - 2 tabs (General, Details), multiple sections
- Contact Main Form - 1 tab with contact fields
- Opportunity Main Form - 1 tab with opportunity fields

**Form XML Structure:**
```xml
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
                    <control id="name" 
                             classid="{270BD3DB-D9AF-4782-9025-509E298DEC0A}"
                             datafieldname="name" 
                             disabled="false" />
                  </cell>
                </row>
              </rows>
            </section>
          </sections>
        </column>
      </columns>
    </tab>
  </tabs>
</form>
```

**Control ClassIDs:**
- `{270BD3DB-D9AF-4782-9025-509E298DEC0A}` - Text input
- `{533B9E00-756B-4312-95A0-DC888637AC78}` - Money
- `{C3EFE0C3-0EC6-42BE-8349-CBD9079DFD8E}` - Whole number
- `{5B773807-9FB2-42DB-97C3-7A91EFF8ADFF}` - Date picker

**Reference:**
- Microsoft Docs: [SystemForm formxml](https://learn.microsoft.com/en-us/power-apps/developer/data-platform/reference/entities/systemform#BKMK_FormXml)

## Testing

### Unit Tests (Jest + React Testing Library)

**Coverage:**
- ✅ Navigation component (8 tests)
- ✅ EntityListView component (7 tests)
- ✅ EntityForm component (7 tests)
- ✅ All 22 tests passing

**Test Files:**
- `app/components/__tests__/Navigation.test.tsx`
- `app/components/__tests__/EntityListView.test.tsx`
- `app/components/__tests__/EntityForm.test.tsx`

**Run Tests:**
```bash
cd Fake4DataverseService/mda-app
npm test
```

### E2E Tests (Playwright)

**Coverage:**
- ✅ Navigation - sitemap display and entity selection (6 tests)
- ✅ Forms - form rendering, navigation, and interactions (8 tests)

**Test Files:**
- `e2e/navigation.spec.ts`
- `e2e/forms.spec.ts`

**Run Tests:**
```bash
cd Fake4DataverseService/mda-app
npm run test:e2e
```

## Type Definitions

Added comprehensive TypeScript types for SystemForm:

```typescript
interface SystemForm {
  formid: string;
  name: string;
  objecttypecode: string;
  type: number; // 2=Main, 4=Quick Create, etc.
  formxml?: string;
  isdefault?: boolean;
}

interface FormDefinition {
  tabs: FormTab[];
}

interface FormTab {
  id: string;
  name: string;
  label: string;
  visible: boolean;
  sections: FormSection[];
}

// ... and more
```

## Usage Examples

### Opening a Form for a New Record

```typescript
// Via URL
window.location = '/?pagetype=entityrecord&etn=account';

// Via Navigation
const params = new URLSearchParams();
params.set('pagetype', 'entityrecord');
params.set('etn', 'account');
window.history.pushState({}, '', `/?${params}`);
```

### Opening a Form for an Existing Record

```typescript
// Via URL
window.location = '/?pagetype=entityrecord&etn=account&id={guid}';

// Via Row Click (handled automatically)
<DataGridRow onClick={() => handleRowClick(record.accountid)} />
```

### Form Component Props

```typescript
<EntityForm
  entityName="account"
  entityPluralName="accounts"
  displayName="Accounts"
  recordId="12345678-..." // Optional, omit for new records
  appModuleId="app-guid"  // Optional
  onClose={() => navigateBack()}
  onSave={(recordId) => console.log('Saved:', recordId)}
/>
```

## Architecture

### Component Hierarchy

```
page.tsx (Main App)
├── Navigation (Sitemap)
├── EntityListView (List of Records)
│   └── DataGrid with row click handlers
└── EntityForm (Form for Create/Edit)
    ├── Tab Navigation
    ├── Sections
    └── Form Controls
```

### Data Flow

```
1. User clicks "New" or clicks a row
   ↓
2. URL parameters updated (pagetype, etn, id)
   ↓
3. page.tsx detects pagetype=entityrecord
   ↓
4. EntityForm component mounted
   ↓
5. EntityForm loads SystemForm metadata
   ↓
6. FormXML parsed into FormDefinition
   ↓
7. Form rendered with tabs, sections, controls
   ↓
8. User interacts with form
   ↓
9. Save triggers create/update API call
   ↓
10. onSave callback navigates back to list
```

## Configuration

### Jest Configuration (`jest.config.js`)
- Uses Next.js Jest configuration
- Excludes e2e tests from unit test runs
- Configures jsdom test environment

### Playwright Configuration (`playwright.config.ts`)
- Auto-starts dev server on port 3000
- Runs tests in Chromium browser
- Captures screenshots on failure
- Includes retry logic for CI

## Dependencies Added

```json
{
  "devDependencies": {
    "@testing-library/react": "latest",
    "@testing-library/jest-dom": "latest",
    "@testing-library/user-event": "latest",
    "jest": "latest",
    "jest-environment-jsdom": "latest",
    "@playwright/test": "latest"
  }
}
```

## Documentation

- `TESTING.md` - Complete testing guide with examples
- Inline code comments reference Microsoft documentation
- JSDoc comments on all public functions

## Breaking Changes

None. This is purely additive functionality.

## Future Enhancements

Potential improvements not included in this PR:
- [ ] Advanced control types (lookup, optionset, subgrid)
- [ ] Form validation
- [ ] Dirty state warning on navigation
- [ ] Form ribbon/command bar
- [ ] Business rules execution
- [ ] Field-level security
- [ ] Quick create forms
- [ ] Form scripts (JavaScript)

## References

- [SystemForm Entity Reference](https://learn.microsoft.com/en-us/power-apps/developer/data-platform/reference/entities/systemform)
- [Customize Entity Forms](https://learn.microsoft.com/en-us/power-apps/developer/model-driven-apps/customize-entity-forms)
- [URL Navigation Examples](https://learn.microsoft.com/en-us/power-apps/developer/model-driven-apps/navigate-to-custom-page-examples)
- [Control Types](https://learn.microsoft.com/en-us/power-apps/developer/model-driven-apps/clientapi/reference/controls/getcontroltype)
