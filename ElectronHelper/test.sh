#!/bin/bash

echo "=== ElectronHelper Test Runner ==="
echo

# Check if dotnet is available
if ! command -v dotnet &> /dev/null; then
    echo "❌ .NET is not installed or not in PATH"
    exit 1
fi

echo "Choose how to run the test:"
echo "1. Run ElectronHelper and TestClient together (recommended)"
echo "2. Run only ElectronHelper (you'll connect manually)"
echo "3. Run only TestClient (ElectronHelper should already be running)"
echo
read -p "Enter choice (1-3): " choice

case $choice in
    1)
        echo "🚀 Starting ElectronHelper and TestClient..."
        echo
        
        # Build the project first
        echo "🔨 Building project..."
        dotnet build ElectronHelper.csproj
        if [ $? -ne 0 ]; then
            echo "❌ Build failed"
            exit 1
        fi
        
        echo "✅ Build successful"
        echo
        
        # Start ElectronHelper in background
        echo "🔌 Starting ElectronHelper..."
        dotnet run --project ElectronHelper.csproj Program.cs &
        HELPER_PID=$!
        
        # Wait a moment for helper to start
        sleep 2
        
        # Start TestClient
        echo "🧪 Starting TestClient..."
        dotnet run --project ElectronHelper.csproj TestClient.cs
        
        # Clean up background process
        echo
        echo "🛑 Stopping ElectronHelper..."
        kill $HELPER_PID 2>/dev/null
        wait $HELPER_PID 2>/dev/null
        echo "✅ Done!"
        ;;
        
    2)
        echo "🔌 Starting ElectronHelper..."
        echo "💡 Connect with TestClient by running: dotnet run --project ElectronHelper.csproj TestClient.cs"
        echo
        dotnet run --project ElectronHelper.csproj Program.cs
        ;;
        
    3)
        echo "🧪 Starting TestClient..."
        echo "💡 Make sure ElectronHelper is running in another terminal"
        echo
        dotnet run --project ElectronHelper.csproj TestClient.cs
        ;;
        
    *)
        echo "❌ Invalid choice"
        exit 1
        ;;
esac