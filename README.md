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

## Troubleshooting

### "Application launches in a new window"
If you run `dotnet run` inside the `StockFlowPro.Client` folder, it will launch the WPF Desktop application, which is a separate window.
If you want the Web Application, ensure you are in the root folder (`D:\StockFlow-Pro\StockFlow-Pro`) and run `dotnet run`.

### Build Errors
The `StockFlowPro.csproj` has been configured to exclude the `StockFlowPro.Client` folder to prevent build conflicts, as the Client folder is nested inside the Web App folder.
