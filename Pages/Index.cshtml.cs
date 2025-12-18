using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using System.Diagnostics;
using System.Threading.Tasks;

namespace appinsightsdemo.Pages;

public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;
    private readonly TelemetryClient _telemetryClient;
    private readonly HttpClient _httpClient;

    public IndexModel(ILogger<IndexModel> logger, TelemetryClient telemetryClient, HttpClient httpClient)
    {
        _logger = logger;
        _telemetryClient = telemetryClient;
        _httpClient = httpClient;
    }

    public async Task OnGet()
    {
        // Use Activity for better correlation tracking (ASP.NET Core best practice)
        using var activity = Activity.Current ?? new Activity("HomePage_Load").Start();
        
        // Create operation scope for Application Insights correlation (if available)
        var isAIConfigured = !string.IsNullOrEmpty(_telemetryClient?.TelemetryConfiguration?.ConnectionString) &&
                            !_telemetryClient.TelemetryConfiguration.ConnectionString.Contains("PLACEHOLDER");
        using var operation = isAIConfigured ? _telemetryClient.StartOperation<RequestTelemetry>(activity) : null;
        if (operation != null)
        {
            operation.Telemetry.Properties["PageName"] = "Index";
            operation.Telemetry.Properties["OperationType"] = "PageLoad";
        }
        
        try
        {
            // Use structured logging with proper parameters (ASP.NET Core best practice)
            _logger.LogInformation(
                "Home page accessed. UserAgent: {UserAgent}, RemoteIP: {RemoteIP}, TraceId: {TraceId}", 
                Request.Headers.UserAgent.ToString(), 
                HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown",
                activity.TraceId);

            // Track page load metrics first (if AI is available)
            if (isAIConfigured && operation != null)
            {
                await TrackPageLoadTelemetryAsync(operation, activity);
            }
            
            // Execute dependency calls with proper error handling
            await ExecuteDependencyCallsAsync(operation, activity);
            
            if (operation != null)
            {
                operation.Telemetry.Success = true;
            }
            _logger.LogInformation(
                "Home page load completed successfully. TraceId: {TraceId}, Duration: {Duration}ms", 
                activity.TraceId, activity.Duration.TotalMilliseconds);
        }
        catch (Exception ex)
        {
            operation.Telemetry.Success = false;
            _logger.LogError(ex, 
                "Home page load failed. TraceId: {TraceId}, Duration: {Duration}ms", 
                activity.TraceId, activity.Duration.TotalMilliseconds);
            
            // Track exception with proper correlation
            _telemetryClient.TrackException(ex, new Dictionary<string, string>
            {
                { "Operation", "HomePage_Load" },
                { "TraceId", activity.TraceId.ToString() },
                { "SpanId", activity.SpanId.ToString() },
                { "CorrelationContext", "PageLoad_Error" }
            });
            
            // Don't rethrow for page loads - show error page instead
            // In production, you might want to redirect to an error page
        }
    }

    /// <summary>
    /// Tracks page load telemetry following Application Insights best practices
    /// </summary>
    private async Task TrackPageLoadTelemetryAsync(IOperationHolder<RequestTelemetry> operation, Activity activity)
    {
        // Track page loaded event with proper correlation
        _telemetryClient.TrackEvent("HomePageLoaded", new Dictionary<string, string>
        {
            { "PageName", "Index" },
            { "LoadTime", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ") },
            { "UserAgent", Request.Headers.UserAgent.ToString() },
            { "TraceId", activity.TraceId.ToString() },
            { "SpanId", activity.SpanId.ToString() },
            { "OperationId", operation.Telemetry.Context.Operation.Id }
        });
        
        _logger.LogInformation(
            "Page load telemetry tracked. TraceId: {TraceId}", 
            activity.TraceId);
        
        // Track demo action performed event
        _telemetryClient.TrackEvent("DemoActionPerformed", new Dictionary<string, string>
        {
            { "PageName", "Index" },
            { "ActionType", "PageLoad" },
            { "UserAgent", Request.Headers.UserAgent.ToString() },
            { "RemoteIP", HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown" },
            { "SessionId", HttpContext.Session.Id ?? "Unknown" },
            { "RequestId", HttpContext.TraceIdentifier },
            { "TraceId", activity.TraceId.ToString() },
            { "SpanId", activity.SpanId.ToString() },
            { "CorrelationContext", "HomePage_Load" }
        });
        
        // Track page load metric
        _telemetryClient.TrackMetric("DemoMetric", 1, new Dictionary<string, string>
        {
            { "MetricType", "PageLoad" },
            { "PageName", "Index" },
            { "SessionId", HttpContext.Session.Id ?? "Unknown" },
            { "TraceId", activity.TraceId.ToString() }
        });
        
        _logger.LogInformation(
            "Page load metrics tracked successfully. TraceId: {TraceId}", 
            activity.TraceId);
            
        // Simulate async processing
        await Task.Delay(1);
    }

    /// <summary>
    /// Executes dependency calls with proper error handling and telemetry tracking
    /// </summary>
    private async Task ExecuteDependencyCallsAsync(IOperationHolder<RequestTelemetry> operation, Activity activity)
    {
        var dependencyTasks = new List<Task>
        {
            ExecuteSingleDependencyAsync("https://httpbin.org/delay/1", "SuccessfulDelay", activity),
            ExecuteSingleDependencyAsync("https://non-existent-domain-12345.invalid/api/data", "ExpectedFailure", activity),
            ExecuteSingleDependencyAsync("https://httpbin.org/status/404", "ExpectedNotFound", activity)
        };
        
        // Execute all dependency calls concurrently (best practice for performance)
        await Task.WhenAll(dependencyTasks);
        
        _logger.LogInformation(
            "All dependency calls completed. TraceId: {TraceId}", 
            activity.TraceId);
    }

    /// <summary>
    /// Executes a single dependency call with proper error handling
    /// </summary>
    private async Task ExecuteSingleDependencyAsync(string endpoint, string operationType, Activity parentActivity)
    {
        using var dependencyActivity = new Activity($"DependencyCall_{operationType}").Start();
        if (parentActivity.Id != null)
        {
            dependencyActivity.SetParentId(parentActivity.Id);
        }
        
        try
        {
            _logger.LogInformation(
                "Starting dependency call to {Endpoint}. TraceId: {TraceId}, SpanId: {SpanId}", 
                endpoint, dependencyActivity.TraceId, dependencyActivity.SpanId);
            
            using var response = await _httpClient.GetAsync(endpoint);
            
            _logger.LogInformation(
                "Dependency call completed. Endpoint: {Endpoint}, Status: {StatusCode}, TraceId: {TraceId}", 
                endpoint, response.StatusCode, dependencyActivity.TraceId);
                
            // Track successful dependency call
            _telemetryClient.TrackEvent($"DependencyCall_{operationType}_Success", new Dictionary<string, string>
            {
                { "Endpoint", endpoint },
                { "StatusCode", response.StatusCode.ToString() },
                { "Method", "GET" },
                { "TraceId", dependencyActivity.TraceId.ToString() },
                { "SpanId", dependencyActivity.SpanId.ToString() },
                { "ParentTraceId", parentActivity.TraceId.ToString() }
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, 
                "Dependency call failed. Endpoint: {Endpoint}, Error: {Error}, TraceId: {TraceId}", 
                endpoint, ex.Message, dependencyActivity.TraceId);
            
            // Track dependency failure with proper correlation
            _telemetryClient.TrackEvent($"DependencyCall_{operationType}_Failed", new Dictionary<string, string>
            {
                { "Endpoint", endpoint },
                { "ExceptionType", ex.GetType().Name },
                { "ErrorMessage", ex.Message },
                { "TraceId", dependencyActivity.TraceId.ToString() },
                { "SpanId", dependencyActivity.SpanId.ToString() },
                { "ParentTraceId", parentActivity.TraceId.ToString() },
                { "Expected", operationType.Contains("Expected") ? "true" : "false" }
            });
            
            // Only track as exception if it's unexpected
            if (!operationType.Contains("Expected"))
            {
                _telemetryClient.TrackException(ex, new Dictionary<string, string>
                {
                    { "Operation", $"DependencyCall_{operationType}" },
                    { "Endpoint", endpoint },
                    { "TraceId", dependencyActivity.TraceId.ToString() },
                    { "SpanId", dependencyActivity.SpanId.ToString() }
                });
            }
        }
    }

    public IActionResult OnPostThrowException(string message = "")
    {
        // Use Activity for proper distributed tracing (ASP.NET Core best practice)
        using var activity = Activity.Current ?? new Activity("Exception_Generation").Start();
        
        // Create operation scope with Activity correlation
        using var operation = _telemetryClient.StartOperation<RequestTelemetry>(activity);
        operation.Telemetry.Properties["ActionType"] = "ExceptionGeneration";
        operation.Telemetry.Properties["ButtonName"] = "ThrowException";
        
        try
        {
            var exceptionMessage = string.IsNullOrWhiteSpace(message) 
                ? "This is a test exception generated for Application Insights demonstration."
                : message;

            // Use structured logging with proper parameter names
            _logger.LogInformation(
                "Exception generation requested. Message: {ExceptionMessage}, UserIP: {UserIP}, TraceId: {TraceId}", 
                exceptionMessage, 
                HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown", 
                activity.TraceId);
            
            // Track telemetry before exception with proper correlation
            TrackExceptionGenerationTelemetry(exceptionMessage, activity, operation);
            
            // Create and track the exception
            var exception = new InvalidOperationException(exceptionMessage);
            
            // Explicitly track the exception with enhanced correlation
            _telemetryClient.TrackException(exception, 
                new Dictionary<string, string>
                {
                    { "ExceptionSource", "DemoButton" },
                    { "UserAction", "ThrowException" },
                    { "CustomMessage", exceptionMessage },
                    { "UserAgent", Request.Headers.UserAgent.ToString() },
                    { "RemoteIP", HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown" },
                    { "RequestId", HttpContext.TraceIdentifier },
                    { "TraceId", activity.TraceId.ToString() },
                    { "SpanId", activity.SpanId.ToString() },
                    { "CorrelationContext", "Exception_Generation" }
                },
                new Dictionary<string, double>
                {
                    { "MessageLength", exceptionMessage.Length },
                    { "ProcessingTimeMs", activity.Duration.TotalMilliseconds }
                });
            
            operation.Telemetry.Success = false;
            _logger.LogError(
                "Demo exception tracked and about to be thrown. TraceId: {TraceId}", 
                activity.TraceId);
            
            // Throw the exception (ASP.NET Core will handle it appropriately)
            throw exception;
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("demonstration"))
        {
            // This is our expected demo exception - let it propagate
            operation.Telemetry.Success = false;
            throw;
        }
        catch (Exception ex)
        {
            // Handle unexpected exceptions
            operation.Telemetry.Success = false;
            _logger.LogError(ex, 
                "Unexpected error in exception generation. TraceId: {TraceId}", 
                activity.TraceId);
            
            _telemetryClient.TrackException(ex, new Dictionary<string, string>
            {
                { "Operation", "Exception_Generation" },
                { "UnexpectedError", "true" },
                { "TraceId", activity.TraceId.ToString() },
                { "SpanId", activity.SpanId.ToString() }
            });
            throw;
        }
    }

    public async Task<IActionResult> OnPostLogWarning()
    {
        // Use Activity for proper distributed tracing
        using var activity = Activity.Current ?? new Activity("Warning_Generation").Start();
        
        // Create operation scope with Activity correlation
        using var operation = _telemetryClient.StartOperation<RequestTelemetry>(activity);
        operation.Telemetry.Properties["ActionType"] = "WarningGeneration";
        operation.Telemetry.Properties["ButtonName"] = "LogWarning";
        
        try
        {
            const string warningMessage = "This is a test warning message for Application Insights demonstration";
            
            _logger.LogInformation(
                "Warning generation requested. TraceId: {TraceId}", 
                activity.TraceId);
            
            // Track telemetry asynchronously (best practice for performance)
            await TrackWarningGenerationTelemetryAsync(warningMessage, activity, operation);
            
            // Generate the actual warning with structured logging
            _logger.LogWarning(
                "Demo warning generated. Message: {WarningMessage}, Severity: {Severity}, TraceId: {TraceId}", 
                warningMessage, "Medium", activity.TraceId);
            
            operation.Telemetry.Success = true;
            _logger.LogInformation(
                "Warning generation completed successfully. TraceId: {TraceId}, Duration: {Duration}ms", 
                activity.TraceId, activity.Duration.TotalMilliseconds);
            
            return new JsonResult(new 
            { 
                success = true, 
                message = "Warning logged successfully!", 
                traceId = activity.TraceId.ToString(),
                durationMs = activity.Duration.TotalMilliseconds
            });
        }
        catch (Exception ex)
        {
            operation.Telemetry.Success = false;
            _logger.LogError(ex, 
                "Warning generation failed. TraceId: {TraceId}", 
                activity.TraceId);
                
            _telemetryClient.TrackException(ex, new Dictionary<string, string>
            {
                { "Operation", "Warning_Generation" },
                { "TraceId", activity.TraceId.ToString() },
                { "SpanId", activity.SpanId.ToString() },
                { "CorrelationContext", "Warning_Generation_Error" }
            });
            
            return new JsonResult(new 
            {
                success = false,
                message = "Warning generation failed: " + ex.Message,
                traceId = activity.TraceId.ToString()
            });
        }
    }

    public async Task<IActionResult> OnPostTrackEvent()
    {
        // Use Activity for proper distributed tracing
        using var activity = Activity.Current ?? new Activity("CustomEvent_Tracking").Start();
        
        // Create operation scope with Activity correlation
        using var operation = _telemetryClient.StartOperation<RequestTelemetry>(activity);
        operation.Telemetry.Properties["ActionType"] = "CustomEventTracking";
        operation.Telemetry.Properties["ButtonName"] = "TrackEvent";
        
        try
        {
            _logger.LogInformation(
                "Custom event tracking initiated. TraceId: {TraceId}", 
                activity.TraceId);
            
            // Track telemetry asynchronously
            await TrackCustomEventTelemetryAsync(activity, operation);
            
            operation.Telemetry.Success = true;
            
            var response = new 
            { 
                success = true, 
                message = "Custom event tracked successfully!", 
                traceId = activity.TraceId.ToString(),
                spanId = activity.SpanId.ToString(),
                durationMs = activity.Duration.TotalMilliseconds
            };
            
            _logger.LogInformation(
                "Custom event tracking completed successfully. TraceId: {TraceId}, Duration: {Duration}ms", 
                activity.TraceId, activity.Duration.TotalMilliseconds);
                
            return new JsonResult(response);
        }
        catch (Exception ex)
        {
            operation.Telemetry.Success = false;
            _logger.LogError(ex, 
                "Custom event tracking failed. TraceId: {TraceId}", 
                activity.TraceId);
                
            _telemetryClient.TrackException(ex, new Dictionary<string, string>
            {
                { "Operation", "CustomEvent_Tracking" },
                { "TraceId", activity.TraceId.ToString() },
                { "SpanId", activity.SpanId.ToString() },
                { "CorrelationContext", "CustomEvent_Tracking_Error" }
            });
            
            return new JsonResult(new 
            {
                success = false,
                message = "Custom event tracking failed: " + ex.Message,
                traceId = activity.TraceId.ToString()
            });
        }
    }

    /// <summary>
    /// Tracks custom event telemetry asynchronously following best practices
    /// </summary>
    private async Task TrackCustomEventTelemetryAsync(Activity activity, IOperationHolder<RequestTelemetry> operation)
    {
        // Track the primary DemoActionPerformed event
        _telemetryClient.TrackEvent("DemoActionPerformed", new Dictionary<string, string>
        {
            { "PageName", "Index" },
            { "ActionType", "ButtonClick" },
            { "ButtonName", "TrackEvent" },
            { "UserAgent", Request.Headers.UserAgent.ToString() },
            { "RemoteIP", HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown" },
            { "SessionId", HttpContext.Session.Id ?? "Unknown" },
            { "RequestId", HttpContext.TraceIdentifier },
            { "TraceId", activity.TraceId.ToString() },
            { "SpanId", activity.SpanId.ToString() },
            { "EventCategory", "UserInteraction" },
            { "DemoType", "CustomEventTracking" },
            { "CorrelationContext", "CustomEvent_Tracking" }
        });
        
        // Track detailed custom event with metrics
        _telemetryClient.TrackEvent("CustomEvent_ButtonClick", 
            new Dictionary<string, string>
            {
                { "EventType", "UserInteraction" },
                { "ButtonType", "TrackEvent" },
                { "SessionId", HttpContext.Session.Id ?? "Unknown" },
                { "TraceId", activity.TraceId.ToString() },
                { "SpanId", activity.SpanId.ToString() },
                { "CorrelationContext", "CustomEvent_Tracking" }
            },
            new Dictionary<string, double>
            {
                { "ClickCount", 1 },
                { "ProcessingTimeMs", activity.Duration.TotalMilliseconds }
            });

        // Track processing time metric
        _telemetryClient.TrackMetric("DemoMetric", activity.Duration.TotalMilliseconds, new Dictionary<string, string>
        {
            { "MetricType", "EventProcessingTime" },
            { "ActionType", "CustomEventTracking" },
            { "ButtonName", "TrackEvent" },
            { "SessionId", HttpContext.Session.Id ?? "Unknown" },
            { "TraceId", activity.TraceId.ToString() }
        });
        
        _logger.LogInformation(
            "Custom event telemetry tracked. TraceId: {TraceId}, ProcessingTime: {ProcessingTime}ms", 
            activity.TraceId, activity.Duration.TotalMilliseconds);
        
        // Performance warning for slow processing
        if (activity.Duration.TotalMilliseconds > 100)
        {
            _logger.LogWarning(
                "Custom event processing slower than expected. TraceId: {TraceId}, Duration: {Duration}ms", 
                activity.TraceId, activity.Duration.TotalMilliseconds);
        }
        
        // Simulate async processing
        await Task.Delay(1);
    }

    public async Task<IActionResult> OnPostTrackDependency()
    {
        // Create operation scope for better correlation
        using var operation = _telemetryClient.StartOperation<RequestTelemetry>("Dependency_Tracking");
        operation.Telemetry.Properties["ActionType"] = "DependencyTracking";
        operation.Telemetry.Properties["ButtonName"] = "TrackDependency";
        
        var operationStartTime = DateTime.UtcNow;
        
        try
        {
            _logger.LogInformation("Dependency tracking operation initiated at {StartTime}. OperationId: {OperationId}", 
                operationStartTime, operation.Telemetry.Context.Operation.Id);
            
            // Track DemoActionPerformed event for dependency tracking
            _telemetryClient.TrackEvent("DemoActionPerformed", new Dictionary<string, string>
            {
                { "PageName", "Index" },
                { "ActionType", "DependencyTracking" },
                { "ButtonName", "TrackDependency" },
                { "UserAgent", Request.Headers["User-Agent"].ToString() },
                { "RemoteIP", HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown" },
                { "SessionId", HttpContext.Session.Id ?? "Unknown" },
                { "RequestId", HttpContext.TraceIdentifier },
                { "DemoType", "DependencyDemo" },
                { "RequestCount", "3" },
                { "OperationId", operation.Telemetry.Context.Operation.Id },
                { "CorrelationContext", "Dependency_Tracking" }
            });
            
            _logger.LogInformation("DemoActionPerformed event tracked for dependency tracking operation with correlation");
            
            // Make multiple HTTP requests to demonstrate various dependency scenarios
            var tasks = new List<Task<HttpResponseMessage>>
            {
                _httpClient.GetAsync("https://httpbin.org/status/200"),
                _httpClient.GetAsync("https://httpbin.org/delay/2"),
                _httpClient.GetAsync("https://api.github.com/users/octocat")
            };
            
            _logger.LogInformation("Executing {RequestCount} concurrent HTTP requests for dependency tracking. OperationId: {OperationId}", 
                tasks.Count, operation.Telemetry.Context.Operation.Id);
            
            var responses = await Task.WhenAll(tasks);
            var operationDuration = DateTime.UtcNow.Subtract(operationStartTime).TotalMilliseconds;
            
            // Track custom metrics for dependency operation
            _telemetryClient.TrackMetric("DemoMetric", operationDuration, new Dictionary<string, string>
            {
                { "MetricType", "DependencyOperationDuration" },
                { "ActionType", "DependencyTracking" },
                { "ButtonName", "TrackDependency" },
                { "RequestCount", tasks.Count.ToString() },
                { "SessionId", HttpContext.Session.Id ?? "Unknown" },
                { "OperationId", operation.Telemetry.Context.Operation.Id }
            });
            
            // Track success rate as a metric
            var successRate = (double)responses.Count(r => r.IsSuccessStatusCode) / responses.Length * 100;
            _telemetryClient.TrackMetric("DemoMetric", successRate, new Dictionary<string, string>
            {
                { "MetricType", "DependencySuccessRate" },
                { "ActionType", "DependencyTracking" },
                { "ButtonName", "TrackDependency" },
                { "SessionId", HttpContext.Session.Id ?? "Unknown" },
                { "OperationId", operation.Telemetry.Context.Operation.Id }
            });
            
            _logger.LogInformation("DemoMetric tracked - Duration: {Duration}ms, Success Rate: {SuccessRate}% with operation correlation", 
                operationDuration, successRate);
            
            // Track successful dependencies with metrics
            _telemetryClient.TrackEvent("MultiDependency_Success", 
                new Dictionary<string, string>
                {
                    { "Operation", "ConcurrentHttpRequests" },
                    { "RequestCount", tasks.Count.ToString() },
                    { "UserAction", "TrackDependency" },
                    { "OperationId", operation.Telemetry.Context.Operation.Id },
                    { "CorrelationContext", "Dependency_Tracking" }
                },
                new Dictionary<string, double>
                {
                    { "OperationDurationMs", operationDuration },
                    { "SuccessfulRequests", responses.Count(r => r.IsSuccessStatusCode) },
                    { "TotalRequests", responses.Length }
                });
            
            operation.Telemetry.Success = true;
            _logger.LogInformation("Dependency tracking completed successfully. Duration: {DurationMs}ms, Successful: {SuccessCount}/{TotalCount}, OperationId: {OperationId}", 
                operationDuration, responses.Count(r => r.IsSuccessStatusCode), responses.Length, operation.Telemetry.Context.Operation.Id);
            
            return new JsonResult(new { 
                success = true, 
                message = $"Dependency tracking completed! {responses.Count(r => r.IsSuccessStatusCode)}/{responses.Length} requests successful.",
                durationMs = operationDuration,
                operationId = operation.Telemetry.Context.Operation.Id
            });
        }
        catch (Exception ex)
        {
            var operationDuration = DateTime.UtcNow.Subtract(operationStartTime).TotalMilliseconds;
            
            // Track failure metric
            _telemetryClient.TrackMetric("DemoMetric", operationDuration, new Dictionary<string, string>
            {
                { "MetricType", "DependencyOperationFailure" },
                { "ActionType", "DependencyTracking" },
                { "ButtonName", "TrackDependency" },
                { "FailureReason", ex.GetType().Name },
                { "SessionId", HttpContext.Session.Id ?? "Unknown" },
                { "OperationId", operation.Telemetry.Context.Operation.Id }
            });
            
            _logger.LogInformation("DemoMetric tracked for dependency operation failure: {Duration}ms with operation correlation", operationDuration);
            
            operation.Telemetry.Success = false;
            _logger.LogError(ex, "Dependency tracking operation failed after {DurationMs}ms. OperationId: {OperationId}", 
                operationDuration, operation.Telemetry.Context.Operation.Id);
            
            // Explicitly track the dependency failure
            _telemetryClient.TrackException(ex, new Dictionary<string, string>
            {
                { "Operation", "Dependency_Tracking" },
                { "UserAction", "TrackDependency" },
                { "FailurePoint", "HttpRequestExecution" },
                { "OperationId", operation.Telemetry.Context.Operation.Id },
                { "CorrelationContext", "Dependency_Tracking_Error" }
            });
            
            return new JsonResult(new { 
                success = false, 
                message = "Dependency tracking failed: " + ex.Message,
                durationMs = operationDuration,
                operationId = operation.Telemetry.Context.Operation.Id
            });
        }
    }

    /// <summary>
    /// Tracks telemetry for exception generation following Application Insights best practices
    /// </summary>
    private void TrackExceptionGenerationTelemetry(string exceptionMessage, Activity activity, IOperationHolder<RequestTelemetry> operation)
    {
        // Track DemoActionPerformed event with enhanced correlation
        _telemetryClient.TrackEvent("DemoActionPerformed", new Dictionary<string, string>
        {
            { "PageName", "Index" },
            { "ActionType", "ExceptionGeneration" },
            { "ButtonName", "ThrowException" },
            { "ExceptionMessage", exceptionMessage },
            { "UserAgent", Request.Headers.UserAgent.ToString() },
            { "RemoteIP", HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown" },
            { "SessionId", HttpContext.Session.Id ?? "Unknown" },
            { "RequestId", HttpContext.TraceIdentifier },
            { "TraceId", activity.TraceId.ToString() },
            { "SpanId", activity.SpanId.ToString() },
            { "DemoType", "ExceptionDemo" },
            { "CorrelationContext", "Exception_Generation" }
        });
        
        // Track custom metric for exception message length
        _telemetryClient.TrackMetric("DemoMetric", exceptionMessage.Length, new Dictionary<string, string>
        {
            { "MetricType", "ExceptionMessageLength" },
            { "ActionType", "ExceptionGeneration" },
            { "ButtonName", "ThrowException" },
            { "SessionId", HttpContext.Session.Id ?? "Unknown" },
            { "TraceId", activity.TraceId.ToString() }
        });
        
        // Log warning for long messages (performance consideration)
        if (exceptionMessage.Length > 100)
        {
            _logger.LogWarning(
                "Exception message is quite long ({Length} characters). TraceId: {TraceId}", 
                exceptionMessage.Length, activity.TraceId);
        }
        
        _logger.LogInformation(
            "Exception generation telemetry tracked. TraceId: {TraceId}, MessageLength: {Length}", 
            activity.TraceId, exceptionMessage.Length);
    }

    /// <summary>
    /// Tracks telemetry for warning generation asynchronously
    /// </summary>
    private async Task TrackWarningGenerationTelemetryAsync(string warningMessage, Activity activity, IOperationHolder<RequestTelemetry> operation)
    {
        // Track DemoActionPerformed event
        _telemetryClient.TrackEvent("DemoActionPerformed", new Dictionary<string, string>
        {
            { "PageName", "Index" },
            { "ActionType", "WarningGeneration" },
            { "ButtonName", "LogWarning" },
            { "WarningMessage", warningMessage },
            { "UserAgent", Request.Headers.UserAgent.ToString() },
            { "RemoteIP", HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown" },
            { "SessionId", HttpContext.Session.Id ?? "Unknown" },
            { "RequestId", HttpContext.TraceIdentifier },
            { "TraceId", activity.TraceId.ToString() },
            { "SpanId", activity.SpanId.ToString() },
            { "DemoType", "WarningDemo" },
            { "Severity", "Medium" },
            { "CorrelationContext", "Warning_Generation" }
        });
        
        // Track timing metric
        var processingTime = activity.Duration.TotalMilliseconds;
        _telemetryClient.TrackMetric("DemoMetric", processingTime, new Dictionary<string, string>
        {
            { "MetricType", "WarningGenerationTiming" },
            { "ActionType", "WarningGeneration" },
            { "ButtonName", "LogWarning" },
            { "Severity", "Medium" },
            { "SessionId", HttpContext.Session.Id ?? "Unknown" },
            { "TraceId", activity.TraceId.ToString() }
        });
        
        // Track warning generated event
        _telemetryClient.TrackEvent("Warning_Generated", new Dictionary<string, string>
        {
            { "WarningMessage", warningMessage },
            { "UserAction", "LogWarning" },
            { "TraceId", activity.TraceId.ToString() },
            { "SpanId", activity.SpanId.ToString() },
            { "CorrelationContext", "Warning_Generation" }
        });
        
        _logger.LogInformation(
            "Warning generation telemetry tracked. TraceId: {TraceId}, ProcessingTime: {ProcessingTime}ms", 
            activity.TraceId, processingTime);
        
        // Simulate async processing (best practice: don't block on I/O)
        await Task.Delay(1);
    }
}
