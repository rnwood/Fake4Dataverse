import { defineConfig, devices } from '@playwright/test';

/**
 * Playwright configuration for Fake4Dataverse MDA e2e tests
 * Reference: https://playwright.dev/docs/test-configuration
 */
export default defineConfig({
  testDir: './e2e',
  fullyParallel: true,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 2 : 0,
  workers: process.env.CI ? 1 : undefined,
  reporter: 'html',
  use: {
    baseURL: 'http://localhost:3000',
    trace: 'on-first-retry',
    screenshot: 'only-on-failure',
  },

  projects: [
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] },
    },
  ],

  // Run both the backend service and Next.js dev server before starting tests
  webServer: [
    {
      // Start Fake4Dataverse backend service on port 5000
      command: 'dotnet run --project ../src/Fake4Dataverse.Service/Fake4Dataverse.Service.csproj -- start --port 5000',
      url: 'http://localhost:5000/health',
      reuseExistingServer: !process.env.CI,
      timeout: 120 * 1000,
      stdout: 'pipe',
      stderr: 'pipe',
    },
    {
      // Start Next.js MDA app on port 3000
      command: 'npm run dev:test',
      url: 'http://localhost:3000',
      reuseExistingServer: !process.env.CI,
      timeout: 120 * 1000,
    },
  ],
});
