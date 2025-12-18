# ASP.NET Core Application Insights Demo

A comprehensive demonstration of ASP.NET Core Razor Pages integrated with Azure Application Insights, showcasing best practices for telemetry tracking, distributed tracing, and observability.

## 🚀 Features

- **ASP.NET Core 8.0** Razor Pages application
- **Azure Application Insights** integration with best practices
- **Activity-based distributed tracing** for proper correlation
- **Structured logging** with parameterized messages
- **Async/await patterns** throughout the application
- **Comprehensive telemetry tracking** (events, metrics, dependencies, exceptions)
- **Graceful degradation** when Application Insights is not configured
- **Demo-friendly setup** with placeholder connection strings
- **Production-ready architecture**

## 🏗️ Architecture & Best Practices

### Telemetry Best Practices Implemented

1. **Activity-Based Correlation**
   - Uses `System.Diagnostics.Activity` for proper trace correlation
   - Implements parent-child activity relationships
   - Tracks operation timing and context

2. **Structured Logging**
   - Parameterized log messages for better searchability
   - Consistent logging patterns with correlation IDs
   - Integration with Application Insights logging

3. **Comprehensive Telemetry Tracking**
   - Custom events with contextual properties
   - Performance metrics with custom dimensions
   - Dependency tracking for external services
   - Exception tracking with detailed context

4. **Separation of Concerns**
   - Helper methods for telemetry operations
   - Clean separation between business logic and observability
   - Reusable telemetry patterns

### Code Quality Features

- **Nullable reference types** for safer code
- **Modern dependency injection** patterns
- **Proper exception handling** with telemetry tracking
- **HTTPS redirection** and security headers
- **Session state management** for correlation tracking

## 🛠️ Getting Started

### Prerequisites

- **.NET 8.0 SDK** or later
- **Visual Studio 2022** or **VS Code** (optional)
- **Azure subscription** (optional, for live telemetry)

### Quick Start

1. **Clone the repository**
   ```bash
   git clone https://github.com/ibnehussain/aspnetcore-appinsights-demo.git
   cd aspnetcore-appinsights-demo
   ```

2. **Run the application**
   ```bash
   dotnet run
   ```

3. **Open your browser**
   ```
   https://localhost:5001
   ```

The application runs in demo mode without requiring Azure Application Insights setup!

### Configure Application Insights (Optional)

To enable live telemetry tracking:

1. **Create an Application Insights resource** in Azure
2. **Get the connection string** from the Azure portal
3. **Update configuration** in `appsettings.json`:
   ```json
   {
     "ApplicationInsights": {
       "ConnectionString": "YOUR_ACTUAL_CONNECTION_STRING_HERE"
     }
   }
   ```

## 📊 Demo Features

The application includes several interactive demonstrations:

### 🎯 Telemetry Tracking
- **Track Custom Events** - Demonstrates event tracking with properties
- **Track Metrics** - Shows custom metric tracking with dimensions  
- **Track Dependencies** - Simulates external service calls
- **Generate Exceptions** - Shows exception tracking and correlation

### 🔍 Observability Features
- **Distributed Tracing** - Activity-based correlation across operations
- **Custom Properties** - Contextual data attached to all telemetry
- **Performance Tracking** - Operation timing and performance metrics
- **Error Tracking** - Comprehensive exception handling and reporting

## 🏛️ Project Structure

```
aspnetcore-appinsights-demo/
├── Pages/
│   ├── Index.cshtml              # Main demo page
│   ├── Index.cshtml.cs           # Page model with telemetry examples
│   ├── Shared/
│   │   └── _Layout.cshtml        # Layout with AI JavaScript SDK
│   └── ...
├── Program.cs                    # Application configuration
├── appsettings.json             # Configuration with AI settings
└── README.md                    # This file
```

## 🎨 Key Code Examples

### Activity-Based Tracing
```csharp
using var activity = Activity.Current ?? new Activity("HomePage_Load").Start();

// Track telemetry with correlation
_telemetryClient.TrackEvent("HomePageLoaded", new Dictionary<string, string>
{
    { "TraceId", activity.TraceId.ToString() },
    { "PageName", "Index" }
});
```

### Structured Logging
```csharp
_logger.LogInformation(
    "Home page accessed. UserAgent: {UserAgent}, RemoteIP: {RemoteIP}, TraceId: {TraceId}", 
    Request.Headers.UserAgent.ToString(), 
    HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown",
    activity.TraceId);
```

### Smart Configuration Detection
```csharp
var isAIConfigured = !string.IsNullOrEmpty(_telemetryClient?.TelemetryConfiguration?.ConnectionString) &&
                    !_telemetryClient.TelemetryConfiguration.ConnectionString.Contains("PLACEHOLDER");
```

## 🚀 Deployment Options

### Local Development
- Runs with placeholder connection strings
- Full functionality without Azure dependencies
- Console logging for debugging

### Azure Deployment
- Update connection string in configuration
- Deploy to Azure App Service
- Automatic telemetry collection

### Docker Support
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0
COPY . /app
WORKDIR /app
EXPOSE 80
ENTRYPOINT ["dotnet", "appinsightsdemo.dll"]
```

## 📈 Best Practices Demonstrated

### Application Insights Integration
- ✅ Proper SDK initialization
- ✅ Connection string validation
- ✅ Graceful degradation
- ✅ Custom telemetry processors
- ✅ JavaScript SDK integration

### ASP.NET Core Patterns
- ✅ Dependency injection best practices
- ✅ Async/await implementation
- ✅ Proper error handling
- ✅ Configuration management
- ✅ Middleware pipeline setup

### Observability
- ✅ Distributed tracing correlation
- ✅ Custom dimensions and properties
- ✅ Performance monitoring
- ✅ Exception tracking
- ✅ Dependency monitoring

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## 📝 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙋‍♂️ Support

If you have questions or run into issues:

1. Check the [Issues](https://github.com/ibnehussain/aspnetcore-appinsights-demo/issues) page
2. Create a new issue with detailed information
3. Review the Application Insights documentation

## 🌟 Acknowledgments

- Built with ASP.NET Core 8.0
- Uses Azure Application Insights SDK
- Inspired by Microsoft's observability best practices
- Bootstrap for UI components

---

**Happy coding!** 🎉 Star this repository if you find it helpful!