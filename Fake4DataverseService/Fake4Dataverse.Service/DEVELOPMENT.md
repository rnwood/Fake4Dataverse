# Development Guide for Fake4Dataverse Service with MDA App

This guide explains how to develop and debug the Fake4Dataverse Service with its integrated Model-Driven App (MDA) frontend.

## Quick Start

### Option 1: Automated Full-Stack Development (Recommended)

Use the provided scripts to run both backend and frontend together:

**Windows (PowerShell):**
```powershell
cd Fake4DataverseService/Fake4Dataverse.Service
.\run-dev.ps1
```

**Linux/Mac (Bash):**
```bash
cd Fake4DataverseService/Fake4Dataverse.Service
./run-dev.sh
```

This will start:
- ASP.NET Core backend on http://localhost:5000
- Next.js frontend on http://localhost:3000

Both services will have hot reload enabled. Press Ctrl+C to stop all services.

### Option 2: Manual Development (Separate Terminals)

**Terminal 1 - Backend:**
```bash
cd Fake4DataverseService/Fake4Dataverse.Service
ASPNETCORE_ENVIRONMENT=Development dotnet run -- start --port 5000
```

**Terminal 2 - Frontend:**
```bash
cd Fake4DataverseService/Fake4Dataverse.Service/mda-app
npm run dev
```

Then open http://localhost:3000 in your browser.

### Option 3: Visual Studio / VS Code

Open the solution in Visual Studio or VS Code and press F5. The launch settings are configured to:
- Start the backend in Development mode on port 5000
- Launch a browser automatically

For full-stack development, also run the frontend separately:
```bash
cd mda-app
npm run dev
```

## Architecture

### Development Mode

In development:
- **Backend (ASP.NET Core)** runs on port 5000 and serves APIs
- **Frontend (Next.js)** runs on port 3000 with hot reload
- Next.js proxies API requests to the backend (configured in `next.config.ts`)
- Both support hot reload for rapid development

```
┌─────────────────┐      API Requests      ┌──────────────────┐
│   Browser       │────────────────────────>│  Next.js Dev     │
│  localhost:3000 │<────────────────────────│  Port 3000       │
└─────────────────┘      UI Responses       └──────────────────┘
                                                     │
                                                     │ Proxy
                                                     ▼
                                            ┌──────────────────┐
                                            │  ASP.NET Core    │
                                            │  Port 5000       │
                                            └──────────────────┘
```

### Production Mode

In production:
- Next.js is built to static files in `wwwroot/mda/`
- ASP.NET Core serves both APIs and the static frontend
- Single deployment artifact

```
┌─────────────────┐      All Requests      ┌──────────────────┐
│   Browser       │────────────────────────>│  ASP.NET Core    │
│  localhost:5000 │<────────────────────────│  Port 5000       │
└─────────────────┘      Responses          └──────────────────┘
                                                     │
                                                     ├─ API: /api/data/*
                                                     └─ Static: /*, /main.aspx
```

## MSBuild Integration

The project includes MSBuild targets that automate the build process:

### Automatic npm Install (Debug Build)

When building in Debug mode, npm dependencies are automatically installed if `node_modules` doesn't exist:

```bash
dotnet build Fake4Dataverse.Service.csproj --configuration Debug
```

To skip npm install (e.g., for faster builds):
```bash
dotnet build -p:SkipSpaInstall=true
```

### Building the MDA App for Production

During `dotnet publish`, the MDA app is automatically built:

```bash
dotnet publish Fake4Dataverse.Service.csproj --configuration Release
```

This runs:
1. `npm ci` - Installs dependencies
2. `npm run build` - Builds the Next.js app to `wwwroot/mda/`
3. Includes the static files in the publish output

### Custom MSBuild Targets

**Build MDA app only:**
```bash
dotnet build -t:BuildSpa
```

**Run MDA app in dev mode:**
```bash
dotnet build -t:RunSpaDev
```

## Testing

### Unit Tests (Frontend)

```bash
cd mda-app

# Run tests once
npm test

# Run with coverage
npm test -- --coverage

# Watch mode (during development)
npm run test:watch
```

### E2E Tests (Playwright)

E2E tests require both backend and frontend to be running:

**Option 1: Using CI workflow (recommended for consistency)**
```bash
cd mda-app

# Install Playwright browsers
npx playwright install --with-deps chromium

# Make sure backend is running
cd ..
dotnet run -- start --port 5000 &

# Wait for backend to start, then run tests
cd mda-app
npm run test:e2e

# For interactive testing
npm run test:e2e:ui
```

**Option 2: Using the test command (starts backend automatically)**
The E2E tests will automatically start the backend service if not running.

### Integration Tests (Backend)

```bash
cd Fake4DataverseService/Fake4Dataverse.Service.IntegrationTests
dotnet test --configuration Debug
```

Integration tests automatically start the service with test data.

## Debugging

### Debugging the Backend (C#)

**Visual Studio:**
1. Set breakpoints in C# code
2. Press F5 or select Debug > Start Debugging
3. The service will start on port 5000

**VS Code:**
1. Install C# extension
2. Set breakpoints in C# code
3. Press F5 or use Debug panel
4. Select "Fake4Dataverse.Service" launch configuration

### Debugging the Frontend (TypeScript/React)

**Browser DevTools:**
1. Start the frontend: `npm run dev` in `mda-app/`
2. Open http://localhost:3000
3. Use browser DevTools (F12)
4. Source maps are enabled for debugging

**VS Code:**
1. Install "Debugger for Chrome" or "Debugger for Edge" extension
2. Start the frontend: `npm run dev`
3. Use VS Code's JavaScript debugging
4. Set breakpoints in TypeScript files

### Full-Stack Debugging

To debug both frontend and backend simultaneously:

1. Start backend in debug mode (VS/VS Code)
2. Start frontend in debug mode (`npm run dev`)
3. Set breakpoints in both C# and TypeScript code
4. Debug from http://localhost:3000

## Hot Reload

### Backend Hot Reload (C#)

Use `dotnet watch` for automatic recompilation:

```bash
cd Fake4DataverseService/Fake4Dataverse.Service
dotnet watch run -- start --port 5000
```

### Frontend Hot Reload (Next.js)

Next.js has built-in hot reload. Just save files and see changes instantly:

```bash
cd mda-app
npm run dev
```

Hot reload works for:
- React components (`.tsx`, `.ts`, `.jsx`, `.js`)
- CSS files
- Configuration files

## Common Issues

### Port Already in Use

If port 5000 or 3000 is already in use:

```bash
# Find process using the port (Linux/Mac)
lsof -i :5000
lsof -i :3000

# Kill the process
kill -9 <PID>

# Or use different ports
dotnet run -- start --port 5001
# Then update mda-app/next.config.ts proxy destination
```

### npm ci Fails

If `npm ci` fails, try:

```bash
cd mda-app
rm -rf node_modules package-lock.json
npm install
```

### Build Output Not Updating

Clean and rebuild:

```bash
# Clean MDA build output
rm -rf Fake4DataverseService/Fake4Dataverse.Service/wwwroot/mda/

# Clean .NET build output
dotnet clean

# Rebuild everything
dotnet build
```

### E2E Tests Fail

Make sure:
1. Backend is running on port 5000
2. Playwright browsers are installed: `npx playwright install --with-deps chromium`
3. No other services are using the test ports

## Project Structure

```
Fake4DataverseService/Fake4Dataverse.Service/
├── Controllers/              # API controllers (OData, Metadata)
├── Services/                 # WCF Organization Service implementation
├── mda-app/                  # Next.js frontend application
│   ├── app/                  # Next.js app router pages
│   ├── components/           # React components
│   ├── e2e/                  # Playwright E2E tests
│   ├── public/               # Static assets
│   ├── next.config.ts        # Next.js configuration
│   ├── package.json          # npm dependencies
│   └── playwright.config.ts  # E2E test configuration
├── wwwroot/                  # Static files served by ASP.NET
│   └── mda/                  # Built Next.js app (generated)
├── Program.cs                # ASP.NET Core entry point
├── Fake4Dataverse.Service.csproj  # Project file with MSBuild targets
├── run-dev.sh                # Development script (Linux/Mac)
└── run-dev.ps1               # Development script (Windows)
```

## Next Steps

- Read the [MDA App Documentation](mda-app/README.md) for frontend-specific details
- Check the [Service README](README.md) for service configuration
- See the [Testing Guide](mda-app/TESTING.md) for comprehensive testing information
- Review the [Implementation Details](mda-app/IMPLEMENTATION.md) for architecture
