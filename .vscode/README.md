# VS Code Configuration for Fake4Dataverse

This directory contains VS Code configuration for full-stack debugging of the Fake4Dataverse Service with its MDA app.

## Quick Start

Press **F5** and select "Full Stack (Backend + Frontend)" to start debugging both services simultaneously.

## Launch Configurations

### Full Stack (Backend + Frontend) - Compound Configuration
- **What it does:** Starts both backend and frontend with debuggers attached
- **Ports:** Backend on 5000, Frontend on 3000
- **Hot reload:** Enabled for both
- **Use when:** You want to debug both C# and TypeScript code

### .NET Core Launch (Fake4Dataverse Service with MDA)
- **What it does:** Starts the ASP.NET Core backend
- **Pre-launch:** Automatically starts Next.js dev server
- **Port:** 5000
- **Opens:** http://localhost:5000/main.aspx
- **Use when:** You primarily debug C# code but need the frontend running

### Attach to Next.js (Frontend)
- **What it does:** Attaches debugger to running Next.js dev server
- **Port:** 9229 (Node.js debug port)
- **Use when:** Frontend is already running and you want to debug TypeScript

## Tasks

### build
- Builds the entire Fake4Dataverse solution
- Used by launch configurations before debugging

### Start Next.js Dev Server
- Starts the Next.js development server on port 3000
- Background task that runs automatically when debugging
- Hot reload enabled
- Problem matcher configured to detect when server is ready

## How It Works

When you press F5 with "Full Stack" selected:

1. **Build Task** runs `dotnet build` on the solution
2. **Start Next.js Dev Server** task launches `npm run dev` in mda-app directory
3. **Backend Launch** starts ASP.NET Core with debugger on port 5000
4. **Frontend Attach** attaches debugger to Next.js process
5. **Browser Opens** to http://localhost:5000/main.aspx

All services stop when you stop debugging (Shift+F5).

## Debugging Tips

**Setting Breakpoints:**
- C# files: Click in gutter or press F9
- TypeScript files: Click in gutter or press F9
- Both debuggers work simultaneously!

**Viewing Output:**
- Backend: Debug Console shows ASP.NET Core output
- Frontend: Terminal panel shows Next.js output

**Hot Reload:**
- C#: Stop debugging, edit, press F5 again (or use `dotnet watch`)
- TypeScript: Just save the file - changes appear instantly!

**Stopping:**
- Press Shift+F5 to stop all processes
- Or click the red stop button in the debug toolbar

## Requirements

- Visual Studio Code with C# extension installed
- Node.js and npm installed
- .NET 8.0 SDK installed

## Troubleshooting

**Port 5000 already in use:**
- Stop any other processes using port 5000
- On Linux/Mac: `lsof -ti:5000 | xargs kill -9`
- On Windows: `netstat -ano | findstr :5000` then `taskkill /PID <pid> /F`

**Port 3000 already in use:**
- Stop any other Next.js dev servers
- On Linux/Mac: `lsof -ti:3000 | xargs kill -9`
- On Windows: `netstat -ano | findstr :3000` then `taskkill /PID <pid> /F`

**Next.js doesn't start:**
- Ensure `node_modules` is installed: `cd mda-app && npm ci`
- Check terminal output for errors
- Try running `npm run dev` manually in mda-app directory

**Breakpoints not hitting:**
- Ensure you're debugging the right configuration
- Check that the code is actually executing (add console.log/Console.WriteLine)
- For TypeScript: Ensure source maps are enabled (they are by default)

## Alternative: Visual Studio 2022

Visual Studio doesn't support compound launch configurations like VS Code. Instead:

1. Open a terminal and run: `cd mda-app && npm run dev`
2. Press F5 in Visual Studio to debug the backend

The frontend will keep running with hot reload while you debug the backend.
