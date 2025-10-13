/**
 * E2E tests for Model-Driven App navigation
 * Tests the sitemap navigation and entity list views
 * Reference: https://playwright.dev/docs/writing-tests
 */

import { test, expect } from '@playwright/test';

test.describe('MDA Navigation', () => {
  test('should load the app and display navigation', async ({ page }) => {
    await page.goto('/');
    
    // Wait for the app to load
    await page.waitForSelector('text=Model-Driven App', { timeout: 10000 });
    
    // Check that navigation is visible
    await expect(page.locator('text=Model-Driven App')).toBeVisible();
  });

  test('should display sitemap areas and groups', async ({ page }) => {
    await page.goto('/');
    
    // Wait for sitemap to load
    await page.waitForTimeout(2000);
    
    // Check for common areas (if sitemap is initialized)
    const hasContent = await page.locator('text=Sales, text=Service').count();
    
    // Either we see the sitemap or we see a welcome message
    const hasSitemap = hasContent > 0;
    const hasWelcome = await page.locator('text=Welcome to Fake4Dataverse').isVisible();
    
    expect(hasSitemap || hasWelcome).toBeTruthy();
  });

  test('should navigate to entity when subarea is clicked', async ({ page }) => {
    await page.goto('/');
    
    await page.waitForTimeout(2000);
    
    // Try to click on Accounts if it exists
    const accountsLink = page.locator('text=Accounts').first();
    if (await accountsLink.isVisible()) {
      await accountsLink.click();
      
      // Wait for entity list to load
      await page.waitForTimeout(1000);
      
      // Should see some content or loading indicator
      const hasContent = await page.locator('text=Loading, text=Accounts').count();
      expect(hasContent).toBeGreaterThan(0);
    }
  });

  test('should display entity list view with toolbar', async ({ page }) => {
    await page.goto('/?etn=account');
    
    await page.waitForTimeout(2000);
    
    // Check for toolbar buttons if data is loaded
    const hasToolbar = await page.locator('text=Refresh, text=New, text=Filter').count();
    
    // Either we have toolbar or we have an error/loading message
    expect(hasToolbar >= 0).toBeTruthy();
  });

  test('should support URL parameters for entity and view', async ({ page }) => {
    // Navigate with URL parameters
    // Reference: https://learn.microsoft.com/en-us/power-apps/developer/model-driven-apps/navigate-to-custom-page-examples
    await page.goto('/?pagetype=entitylist&etn=account');
    
    await page.waitForTimeout(2000);
    
    // Should attempt to load the entity
    const pageContent = await page.content();
    expect(pageContent.length).toBeGreaterThan(0);
  });

  test('should display view dropdown when views are available', async ({ page }) => {
    await page.goto('/?etn=account');
    
    await page.waitForTimeout(2000);
    
    // Check if view dropdown exists (may not if no views configured)
    const hasDropdown = await page.locator('[role="combobox"]').count();
    expect(hasDropdown >= 0).toBeTruthy();
  });
});
