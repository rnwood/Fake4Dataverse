#!/bin/bash
# Development script to run both backend and frontend concurrently
# This provides a single command to start full-stack development with hot reload

set -e

echo "=== Starting Fake4Dataverse Full-Stack Development ==="
echo ""
echo "This script will start:"
echo "  1. ASP.NET Core backend on http://localhost:5000"
echo "  2. Next.js frontend on http://localhost:3000"
echo ""
echo "Both will have hot reload enabled. Press Ctrl+C to stop both."
echo ""

# Trap Ctrl+C to cleanup processes
trap 'echo ""; echo "Stopping services..."; kill $BACKEND_PID $FRONTEND_PID 2>/dev/null; wait; echo "Services stopped."; exit' INT TERM

# Get script directory
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"

# Start backend in background
echo "Starting backend service..."
cd "$SCRIPT_DIR"
ASPNETCORE_ENVIRONMENT=Development dotnet run -- start --port 5000 &
BACKEND_PID=$!

# Wait for backend to start
sleep 3

# Start frontend in background
echo "Starting frontend development server..."
cd "$SCRIPT_DIR/mda-app"
npm run dev &
FRONTEND_PID=$!

echo ""
echo "=== Services Started ==="
echo "Backend: http://localhost:5000"
echo "Frontend: http://localhost:3000"
echo ""
echo "Press Ctrl+C to stop all services..."
echo ""

# Wait for both processes
wait $BACKEND_PID $FRONTEND_PID
