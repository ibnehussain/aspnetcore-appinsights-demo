var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

// Add HttpClient for dependency tracking demonstrations
builder.Services.AddHttpClient();

// Add session services for correlation tracking
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Modern Application Insights configuration - only if connection string is available
var aiConnectionString = builder.Configuration.GetConnectionString("ApplicationInsights") 
    ?? builder.Configuration["ApplicationInsights:ConnectionString"];

if (!string.IsNullOrWhiteSpace(aiConnectionString) && 
    !aiConnectionString.Contains("PLACEHOLDER") && 
    !aiConnectionString.Contains("YOUR_"))
{
    builder.Services.AddApplicationInsightsTelemetry(options =>
    {
        options.ConnectionString = aiConnectionString;
    });

    // Configure logging to include Application Insights
    builder.Logging.AddApplicationInsights(
        configureTelemetryConfiguration: (config) => config.ConnectionString = aiConnectionString,
        configureApplicationInsightsLoggerOptions: (options) => { }
    );
}
else
{
    // Add console logging when Application Insights is not configured
    builder.Logging.AddConsole();
    
    // Register a NullTelemetryClient to satisfy DI requirements
    builder.Services.AddSingleton<Microsoft.ApplicationInsights.TelemetryClient>(_ => 
        new Microsoft.ApplicationInsights.TelemetryClient());
}

// Add Application Insights telemetry processors for enhanced data collection (only if AI is configured)
if (!string.IsNullOrWhiteSpace(aiConnectionString) && 
    !aiConnectionString.Contains("PLACEHOLDER") && 
    !aiConnectionString.Contains("YOUR_"))
{
    builder.Services.AddApplicationInsightsTelemetryProcessor<Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.QuickPulse.QuickPulseTelemetryProcessor>();
}

// Add HTTP context accessor for telemetry correlation
builder.Services.AddHttpContextAccessor();

// Configure telemetry for better performance and cost optimization
builder.Services.Configure<Microsoft.ApplicationInsights.Extensibility.TelemetryConfiguration>(telemetryConfiguration =>
{
    // Enable adaptive sampling to manage data volume and costs
    var telemetryProcessorChainBuilder = telemetryConfiguration.DefaultTelemetrySink.TelemetryProcessorChainBuilder;
    telemetryProcessorChainBuilder.Build();
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Add session middleware for correlation tracking
app.UseSession();

app.UseAuthorization();

app.MapRazorPages();

app.Run();
