using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.DataContracts;
using System.Web;

namespace appinsightsdemo;

public class CorrelationTelemetryInitializer : ITelemetryInitializer
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CorrelationTelemetryInitializer(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public void Initialize(ITelemetry telemetry)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null) return;

        // Add correlation properties to all telemetry
        if (telemetry is ISupportProperties propertiesTelemetry)
        {
            // Add request correlation ID
            if (!propertiesTelemetry.Properties.ContainsKey("RequestId"))
            {
                propertiesTelemetry.Properties["RequestId"] = httpContext.TraceIdentifier;
            }

            // Add session correlation
            var sessionId = httpContext.Session?.Id;
            if (!string.IsNullOrEmpty(sessionId) && !propertiesTelemetry.Properties.ContainsKey("SessionId"))
            {
                propertiesTelemetry.Properties["SessionId"] = sessionId;
            }

            // Add user context
            if (!propertiesTelemetry.Properties.ContainsKey("UserAgent"))
            {
                var userAgent = httpContext.Request.Headers["User-Agent"].ToString();
                if (!string.IsNullOrEmpty(userAgent))
                {
                    propertiesTelemetry.Properties["UserAgent"] = userAgent;
                }
            }

            // Add remote IP
            if (!propertiesTelemetry.Properties.ContainsKey("RemoteIP"))
            {
                var remoteIp = httpContext.Connection.RemoteIpAddress?.ToString();
                if (!string.IsNullOrEmpty(remoteIp))
                {
                    propertiesTelemetry.Properties["RemoteIP"] = remoteIp;
                }
            }

            // Add request path for better correlation
            if (!propertiesTelemetry.Properties.ContainsKey("RequestPath"))
            {
                propertiesTelemetry.Properties["RequestPath"] = httpContext.Request.Path;
            }

            // Add HTTP method
            if (!propertiesTelemetry.Properties.ContainsKey("HttpMethod"))
            {
                propertiesTelemetry.Properties["HttpMethod"] = httpContext.Request.Method;
            }

            // Add demo context for easier filtering
            if (!propertiesTelemetry.Properties.ContainsKey("ApplicationContext"))
            {
                propertiesTelemetry.Properties["ApplicationContext"] = "ApplicationInsightsDemo";
            }
        }

        // Set operation context for proper correlation
        if (telemetry.Context?.Operation != null)
        {
            // Ensure operation ID is set for correlation
            if (string.IsNullOrEmpty(telemetry.Context.Operation.Id))
            {
                telemetry.Context.Operation.Id = System.Diagnostics.Activity.Current?.RootId ?? httpContext.TraceIdentifier;
            }

            // Set operation name for better grouping
            if (string.IsNullOrEmpty(telemetry.Context.Operation.Name))
            {
                telemetry.Context.Operation.Name = $"{httpContext.Request.Method} {httpContext.Request.Path}";
            }
        }

        // Set cloud context
        if (telemetry.Context?.Cloud != null)
        {
            if (string.IsNullOrEmpty(telemetry.Context.Cloud.RoleName))
            {
                telemetry.Context.Cloud.RoleName = "ApplicationInsightsDemo";
            }
        }
    }
}