# MSBuild Integration Summary

This document summarizes the changes made to integrate the MDA app build process with MSBuild.

## Implementation Date
October 16, 2025

## Goal
Provide a unified development experience where developers can build, run, and debug both the ASP.NET Core backend and Next.js frontend with a single command and automatic hot reload.

## Changes Made

### 1. MSBuild Targets Added

Added to `Fake4Dataverse.Service.csproj`:

#### `EnsureNodeModules` Target
- **Runs:** Before Build (Debug configuration only)
- **Purpose:** Automatically installs npm dependencies if `node_modules` doesn't exist
- **Skip:** Use `-p:SkipSpaInstall=true` to skip

#### `PublishSpa` Target
- **Runs:** After ComputeFilesToPublish (during publish)
- **Purpose:** Builds the Next.js app for production
- **Output:** `wwwroot/mda/` directory with static files
- **Commands:** `npm ci` → `npm run build`

#### Custom Targets
- `BuildSpa`: Manually build the MDA app
- `RunSpaDev`: Run the MDA app in development mode

### 2. Development Scripts

#### `run-dev.sh` (Linux/Mac)
Bash script that:
- Starts ASP.NET Core backend on port 5000
- Starts Next.js frontend on port 3000
- Captures Ctrl+C to stop both
- Shows output from both services

#### `run-dev.ps1` (Windows)
PowerShell script with the same functionality as the bash version.

### 3. Configuration Updates

#### `next.config.ts`
- Fixed output path from `../src/Fake4Dataverse.Service/wwwroot/mda` to `../wwwroot/mda`
- This aligns with the actual project structure

#### `launchSettings.json`
- Added `ASPNETCORE_ENVIRONMENT=Development` environment variable
- Added `applicationUrl` for consistent development port
- Enabled browser launch

#### `Program.cs`
- Added conditional logic to handle Development vs Production modes
- In Production: Serves static files from `wwwroot/mda/`
- In Development: Static files not needed (frontend runs separately)

### 4. Test Fixes

#### `ServiceFixture.cs`
- Updated solution file name from `Fake4DataverseFree.sln` to `Fake4Dataverse.sln`
- Updated path from `src/Fake4Dataverse.Service` to `Fake4Dataverse.Service`

#### `ServiceClientAuthTests.cs`
- Added repository root discovery logic (same as ServiceFixture)
- Updated path construction to match new structure

### 5. Documentation

#### `DEVELOPMENT.md` (New)
Comprehensive development guide covering:
- Quick start (3 options)
- Architecture diagrams
- MSBuild integration
- Testing (unit, E2E, integration)
- Debugging (C#, TypeScript, full-stack)
- Hot reload
- Common issues

#### `README.md` Updates
- Added Quick Links section
- Added reference to MDA app
- Links to development guides

#### `.gitignore` Updates
- Added `Fake4DataverseService/Fake4Dataverse.Service/wwwroot/mda/` to exclude build artifacts

## Usage Examples

### Development

```bash
# Option 1: Automated (Recommended)
./run-dev.sh  # or run-dev.ps1 on Windows

# Option 2: Visual Studio
# Press F5, then run npm run dev in separate terminal

# Option 3: Manual
ASPNETCORE_ENVIRONMENT=Development dotnet run -- start --port 5000  # Terminal 1
cd mda-app && npm run dev  # Terminal 2
```

### Production Build

```bash
# Full publish (includes MDA app)
dotnet publish --configuration Release

# Just build MDA app
dotnet build -t:BuildSpa

# Build without npm install (faster)
dotnet build -p:SkipSpaInstall=true
```

## Testing Results

All tests pass:
- ✅ Integration tests: 35 passed, 2 skipped
- ✅ Unit tests (MDA): 23 passed
- ✅ Service starts correctly in Development and Production modes

## Benefits

1. **Single Command** - One script starts everything
2. **Hot Reload** - Changes reflected instantly
3. **Automated** - No manual build steps needed
4. **Flexible** - Multiple ways to run (script, VS, manual)
5. **Production Ready** - `dotnet publish` includes everything
6. **Well Documented** - Clear guides for all scenarios

## CI/CD Impact

No changes needed to CI workflow. The existing steps already:
- Install Node.js dependencies with `npm ci`
- Build the MDA app with `npm run build`
- Run tests

The MSBuild integration is complementary and doesn't affect CI.

## Breaking Changes

None. All existing workflows continue to work. This is purely additive functionality.

## Future Enhancements

Potential improvements for future consideration:
1. Watch mode for C# backend using `dotnet watch`
2. Integration with VS Code tasks for automated debugging
3. Docker Compose setup for containerized development
4. Development proxy configuration for different backend ports
