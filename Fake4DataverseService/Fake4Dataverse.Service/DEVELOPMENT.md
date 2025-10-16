# Development Guide for Fake4Dataverse Service with MDA App

This guide explains how to develop and debug the Fake4Dataverse Service with its integrated Model-Driven App (MDA) frontend.

## Quick Start - Press F5 to Debug!

### Visual Studio Code (Recommended)

1. Open the repository root in VS Code
2. Press **F5** or select **Debug > Start Debugging**
3. Choose "Full Stack (Backend + Frontend)" from the dropdown
4. Both backend and frontend will start automatically with hot reload!

The debugger will:
- Build the solution
- Start the Next.js dev server (port 3000)
- Start the ASP.NET Core backend (port 5000)
- Open your browser to http://localhost:5000/main.aspx
- Attach debuggers to both processes

**Debugging:**
- Set breakpoints in C# code - they'll hit automatically
- Set breakpoints in TypeScript code - use VS Code's debugger
- Hot reload works for both frontend and backend changes

### Visual Studio 2022

1. Open `Fake4Dataverse.sln` in Visual Studio
2. **Important:** Before pressing F5, open a terminal and run:
   ```bash
   cd Fake4DataverseService/Fake4Dataverse.Service/mda-app
   npm run dev
   ```
3. Press **F5** to start debugging the backend
4. Your browser will open to http://localhost:5000/main.aspx
5. Changes to C# code will hot reload automatically
6. Changes to TypeScript/React code will hot reload in the browser

**Why?** Visual Studio doesn't support compound launch configurations like VS Code. You need to manually start the frontend once, then it will keep running with hot reload while you debug the backend.

## Alternative Methods

### Option 1: Automated Script (For Non-IDE Usage)

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

Then open http://localhost:5000/main.aspx in your browser.

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

### Full-Stack Debugging with F5 (VS Code)

**Easiest Method - Press F5:**

1. Open the repository in VS Code
2. Press **F5** or click "Run and Debug" in the sidebar
3. Select "**Full Stack (Backend + Frontend)**" configuration
4. Both services start automatically with debuggers attached!

**What happens:**
- Solution builds automatically
- Next.js dev server starts on port 3000
- ASP.NET Core backend starts on port 5000
- Browser opens to http://localhost:5000/main.aspx
- Breakpoints work in both C# and TypeScript

**Setting Breakpoints:**
- **C# Backend:** Click in the gutter next to line numbers in .cs files
- **TypeScript Frontend:** Click in the gutter next to line numbers in .ts/.tsx files
- Both debuggers run simultaneously!

**Hot Reload:**
- C# changes: Stop debugging, edit, press F5 again
- TypeScript changes: Just save the file - instant hot reload!

### Visual Studio 2022 Debugging

**One-Time Setup (per session):**

1. Open a terminal and run:
   ```bash
   cd Fake4DataverseService/Fake4Dataverse.Service/mda-app
   npm run dev
   ```
2. Leave this terminal running

**Then press F5** in Visual Studio to debug the backend. The frontend stays running with hot reload.

### Debugging Backend Only (C#)

**Visual Studio:**
1. Ensure frontend is running (`npm run dev` in mda-app)
2. Set breakpoints in C# code
3. Press F5 or select Debug > Start Debugging
4. The service starts on port 5000

**VS Code:**
1. Ensure frontend is running (`npm run dev` in mda-app)
2. Set breakpoints in C# code
3. Press F5 and select ".NET Core Launch (Fake4Dataverse Service with MDA)"

### Debugging Frontend Only (TypeScript/React)

**Browser DevTools (Easiest):**
1. Start the frontend: `npm run dev` in `mda-app/`
2. Open http://localhost:3000 in your browser
3. Press F12 to open DevTools
4. Source maps are enabled - set breakpoints in the Sources tab

**VS Code (Advanced):**
1. Start frontend: `npm run dev`
2. Use VS Code's "Attach to Next.js (Frontend)" configuration
3. Set breakpoints in TypeScript files
4. Debug panel shows variables and call stack

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
