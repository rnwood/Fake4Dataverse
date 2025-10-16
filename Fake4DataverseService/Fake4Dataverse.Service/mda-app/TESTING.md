# MDA App Testing

This document describes how to run tests for the Model-Driven App (MDA) front-end.

## Test Structure

The MDA app includes two types of tests:

1. **Unit Tests**: Test individual components in isolation using Jest and React Testing Library
2. **E2E Tests**: Test the complete application flow using Playwright

## Running Tests

### Unit Tests

Unit tests are located in `app/components/__tests__/` and test individual React components.

```bash
# Run all unit tests
npm test

# Run tests in watch mode (reruns on file changes)
npm run test:watch
```

**Coverage:**
- ✅ Navigation component - tests sitemap rendering and navigation
- ✅ EntityListView component - tests entity grid, views, and filtering
- ✅ EntityForm component - tests form rendering, tabs, sections, and controls

### E2E Tests

E2E tests are located in `e2e/` and test the complete application flow using Playwright.

**Prerequisites:**
- The Fake4Dataverse service must be running on `http://localhost:3000`
- MDA metadata must be initialized (appmodule, sitemap, views, and forms)

```bash
# Run e2e tests
npm run test:e2e

# Run e2e tests in UI mode (interactive)
npm run test:e2e:ui
```

**Coverage:**
- ✅ Navigation - tests sitemap display and entity selection
- ✅ List views - tests view switching and record display
- ✅ Forms - tests form opening, navigation, and field interactions

## Test Configuration

### Jest Configuration

- **File**: `jest.config.js`
- **Setup**: `jest.setup.js`
- **Test pattern**: `**/__tests__/**/*.[jt]s?(x)` and `**/?(*.)+(spec|test).[jt]s?(x)`
- **Excluded**: `/node_modules/`, `/e2e/`

### Playwright Configuration

- **File**: `playwright.config.ts`
- **Test directory**: `./e2e`
- **Browser**: Chromium (can be extended to Firefox, WebKit)
- **Base URL**: `http://localhost:3000`
- **Auto-start**: Playwright will automatically start the dev server before running tests

## Writing Tests

### Unit Test Example

```typescript
import { render, screen } from '@testing-library/react';
import MyComponent from '../MyComponent';

describe('MyComponent', () => {
  it('renders correctly', () => {
    render(<MyComponent />);
    expect(screen.getByText('Hello')).toBeInTheDocument();
  });
});
```

### E2E Test Example

```typescript
import { test, expect } from '@playwright/test';

test('navigates to entity', async ({ page }) => {
  await page.goto('/');
  await page.click('text=Accounts');
  await expect(page.locator('text=Accounts')).toBeVisible();
});
```

## Continuous Integration

Tests are designed to run in CI environments:

- Unit tests run quickly and don't require external dependencies
- E2E tests require the service to be running and can be run in CI with appropriate setup

## Troubleshooting

### Unit Tests

**Issue**: Tests timing out
**Solution**: Check that all async operations are properly mocked and awaited

**Issue**: Component not rendering
**Solution**: Ensure Fluent UI styles are mocked correctly

### E2E Tests

**Issue**: Service not available
**Solution**: Ensure Fake4Dataverse service is running on port 3000

**Issue**: Test data not available
**Solution**: Initialize MDA metadata using `MdaInitializer.InitializeExampleMda()`

## References

- [Jest Documentation](https://jestjs.io/)
- [React Testing Library](https://testing-library.com/react)
- [Playwright Documentation](https://playwright.dev/)
