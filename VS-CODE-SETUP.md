# GolfBAIST - VS Code Development Guide

## 🚀 Running the Project in VS Code

This ASP.NET Core 8.0 project is fully compatible with VS Code! Here's how to get started:

### Prerequisites
1. **.NET 8.0 SDK** - Download from [dotnet.microsoft.com](https://dotnet.microsoft.com/download)
2. **VS Code Extensions:**
   - C# (ms-dotnettools.csharp) - Essential for C# development
   - C# Dev Kit (ms-dotnettools.csdevkit) - Enhanced C# experience (optional but recommended)

### Getting Started

#### Option 1: Using VS Code Tasks (Recommended)
1. Open the project folder in VS Code
2. Press `Ctrl+Shift+P` and type "Tasks: Run Task"
3. Select one of these tasks:
   - **`run`** - Build and run the application
   - **`watch`** - Run with hot reload (automatically restarts when files change)
   - **`build`** - Just build the project
   - **`clean`** - Clean build artifacts

#### Option 2: Using the Debugger
1. Press `F5` or go to Run and Debug panel
2. Select "Launch GolfBAIST"
3. The application will build and launch automatically

#### Option 3: Using Terminal
```bash
# Navigate to the project directory
cd "c:\GitHub\GolfBAIST-WebApp\GolfBAIST"

# Restore packages (first time only)
dotnet restore

# Run the application
dotnet run

# Or run with hot reload
dotnet watch run
```

### 🌐 Accessing the Application
Once running, the application will be available at:
- **HTTPS:** https://localhost:7001
- **HTTP:** http://localhost:5001

The browser should open automatically when using the debugger.

### 🔧 Development Features

#### Hot Reload
Use `dotnet watch run` or the "watch" task for automatic reloading when you make changes to:
- C# files (.cs)
- Razor pages (.cshtml)
- CSS files
- JavaScript files

#### Debugging
- Set breakpoints in C# code by clicking in the gutter
- Use `F5` to start debugging
- Use `F10`/`F11` for step over/into
- Inspect variables in the Debug Console

#### IntelliSense
VS Code provides full IntelliSense for:
- C# code completion
- Razor syntax highlighting
- HTML/CSS/JavaScript support
- Error highlighting and suggestions

### 📁 Project Structure
```
GolfBAIST/
├── Controllers/         # API controllers
├── Models/             # Data models
├── Pages/              # Razor pages
├── TechnicalServices/  # Business logic services
├── wwwroot/           # Static files (CSS, JS, images)
├── appsettings.json   # Configuration
└── Program.cs         # Application entry point
```

### 🆚 VS Code vs Visual Studio 2022

**You can use either, but here are the differences:**

#### VS Code Advantages:
- ✅ Lighter weight and faster startup
- ✅ Great for web development and modern .NET
- ✅ Excellent Git integration
- ✅ Cross-platform (Windows, Mac, Linux)
- ✅ Highly customizable with extensions
- ✅ Free and open source

#### Visual Studio 2022 Advantages:
- ✅ More advanced debugging tools
- ✅ Better IntelliSense for complex projects
- ✅ Built-in database tools
- ✅ Advanced refactoring tools
- ✅ Solution file (.sln) management

### 🔄 Switching Between VS Code and Visual Studio 2022

**No porting needed!** This is a standard .NET project that works in both:

1. **From VS Code to VS 2022:**
   - Simply open the `GolfBAIST.csproj` file in Visual Studio 2022
   - Or create a solution file: `dotnet new sln` then `dotnet sln add GolfBAIST.csproj`

2. **From VS 2022 to VS Code:**
   - Open the project folder in VS Code
   - The .csproj file contains all the necessary information

### 🛠️ Useful VS Code Commands

- `Ctrl+Shift+P` - Command Palette
- `Ctrl+` ` - Toggle Terminal
- `F5` - Start Debugging
- `Ctrl+F5` - Run Without Debugging
- `Ctrl+Shift+F5` - Restart Debugging
- `Ctrl+.` - Quick Fix (when on an error)

### 📦 Package Management

```bash
# Add a new package
dotnet add package PackageName

# Remove a package
dotnet remove package PackageName

# Restore packages
dotnet restore

# List packages
dotnet list package
```

### 🎯 Modernized Features

The scorecard page (`ViewScoreSheet.cshtml`) has been modernized with:
- ✨ Enhanced responsive design
- 📊 Real-time score calculations
- 🎯 Quick score entry buttons
- 📱 Mobile-friendly interface
- 🎨 Modern card-based layout
- ⚡ Performance optimizations

Happy coding! 🏌️‍♂️
