# StockFlowPro

This solution contains two applications:

1.  **StockFlowPro (Web App)**: An ASP.NET Core MVC web application.
    -   Location: `D:\StockFlow-Pro\StockFlow-Pro` (Root)
    -   Run Command: `dotnet run` (from the root folder)
    -   Access: http://localhost:5243

2.  **StockFlowPro.Client (Desktop App)**: A WPF Desktop application with MVVM architecture.
    -   Location: `D:\StockFlow-Pro\StockFlow-Pro\StockFlowPro.Client`
    -   Run Command: `dotnet run` (from the Client folder)
    -   Features: Scanner View, Products View, Offline capabilities.

## Quickstart: Run the Website
- Install .NET SDK 9.0
- In the project root (`D:\StockFlow-Pro\StockFlow-Pro`), run: `dotnet run`
- Open: http://localhost:5243/
- Database: SQLite file `app.db` is created automatically
- Admin login (seeded): `admin@foodie.com` / `Admin123!`
- Alternate self-host ports: http://localhost:5000 and https://localhost:5001

## Troubleshooting

### "Application launches in a new window"
If you run `dotnet run` inside the `StockFlowPro.Client` folder, it will launch the WPF Desktop application, which is a separate window.
If you want the Web Application, ensure you are in the root folder (`D:\StockFlow-Pro\StockFlow-Pro`) and run `dotnet run`.

### Build Errors
The `StockFlowPro.csproj` has been configured to exclude the `StockFlowPro.Client` folder to prevent build conflicts, as the Client folder is nested inside the Web App folder.

### Port or binary lock
If the build fails due to a locked `StockFlowPro.exe`, stop any running instance and retry:
- Windows PowerShell: `Get-Process -Name StockFlowPro -ErrorAction SilentlyContinue | Stop-Process -Force`
