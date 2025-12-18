# Application Insights Correlation Implementation

## Overview

This Application Insights demo now implements comprehensive correlation best practices to ensure that logs, dependencies, and exceptions are properly correlated with incoming HTTP requests.

## Correlation Features Implemented

### 1. Custom Telemetry Initializer (`CorrelationTelemetryInitializer.cs`)

**Purpose**: Automatically adds correlation context to ALL telemetry items.

**Key Features**:
- Adds `RequestId` (HttpContext.TraceIdentifier) to all telemetry
- Includes `SessionId` for user session tracking
- Adds user context (UserAgent, RemoteIP, RequestPath, HttpMethod)
- Sets operation context for proper correlation hierarchy
- Provides application context for easier filtering
- Ensures cloud role name is set consistently

**Automatic Properties Added**:
- `RequestId`: Unique identifier for each HTTP request
- `SessionId`: User session correlation
- `UserAgent`: Browser/client information
- `RemoteIP`: Client IP address
- `RequestPath`: HTTP request path
- `HttpMethod`: HTTP method (GET, POST, etc.)
- `ApplicationContext`: Set to "ApplicationInsightsDemo"
- `OperationId`: Root operation ID for correlation
- `OperationName`: Formatted as "METHOD /path"

### 2. Operation Scopes for Request Correlation

**Implementation**: Each action method now uses `_telemetryClient.StartOperation<RequestTelemetry>()` to create proper operation scopes.

**Benefits**:
- All telemetry within an operation shares the same `OperationId`
- Proper parent-child relationship tracking
- Success/failure status tracking at operation level
- Enhanced error correlation

**Operations Implemented**:
- `HomePage_Load`: Page loading and initialization
- `Exception_Generation`: Exception throwing demonstration
- `Warning_Generation`: Warning logging demonstration
- `CustomEvent_Tracking`: Custom event tracking
- `Dependency_Tracking`: HTTP dependency calls

### 3. Enhanced Correlation Properties

**All telemetry now includes**:
- `OperationId`: Links all telemetry within the same request
- `CorrelationContext`: Identifies the specific operation type
- `ParentOperationId`: For hierarchical tracking (where applicable)

### 4. Session Middleware Configuration

**Added to Program.cs**:
```csharp
app.UseSession();
```

**Purpose**: Enables session-based correlation tracking across multiple requests from the same user.

### 5. Built-in Telemetry Initializers

**Additional initializers registered**:
- `AspNetCoreEnvironmentTelemetryInitializer`: Environment context
- `DomainNameRoleInstanceTelemetryInitializer`: Role instance information
- `HttpDependenciesParsingTelemetryInitializer`: Enhanced HTTP dependency correlation

## Correlation Benefits

### Request-Level Correlation
- All logs, events, metrics, dependencies, and exceptions within a single HTTP request share the same `OperationId`
- Easy to trace the complete flow of a single request in Application Insights

### Session-Level Correlation  
- User activities across multiple requests can be correlated via `SessionId`
- Enables user journey analysis and behavior tracking

### Operation-Level Correlation
- Each logical operation (page load, button click, etc.) has its own operation scope
- Success/failure status is tracked at the operation level
- Parent-child relationships are maintained for nested operations

### Automatic Context Enrichment
- Every telemetry item automatically receives request context (user agent, IP, path, etc.)
- No need to manually add correlation properties in each tracking call
- Consistent context across all telemetry types

## Querying Correlated Data in Application Insights

### Find all telemetry for a specific request:
```kusto
union traces, requests, dependencies, exceptions, customEvents, customMetrics
| where customDimensions.RequestId == "specific-request-id"
| order by timestamp asc
```

### Find all telemetry for a specific operation:
```kusto
union traces, requests, dependencies, exceptions, customEvents, customMetrics
| where customDimensions.OperationId == "specific-operation-id"
| order by timestamp asc
```

### Find user session activity:
```kusto
union traces, requests, dependencies, exceptions, customEvents, customMetrics
| where customDimensions.SessionId == "specific-session-id"
| order by timestamp asc
```

### Application-specific filtering:
```kusto
union traces, requests, dependencies, exceptions, customEvents, customMetrics
| where customDimensions.ApplicationContext == "ApplicationInsightsDemo"
| order by timestamp asc
```

## Best Practices Implemented

1. **Operation Scopes**: Using `StartOperation<T>()` for logical operation boundaries
2. **Automatic Context**: Telemetry initializers add context without manual intervention
3. **Consistent Naming**: Standardized property names across all telemetry
4. **Hierarchical Tracking**: Parent-child operation relationships
5. **Error Correlation**: Exceptions linked to their originating operations
6. **Performance Tracking**: Operation duration and success status
7. **User Context**: Session and request-level user identification
8. **Application Filtering**: Easy filtering of demo-specific telemetry

## Testing Correlation

1. **Open browser developer tools** and monitor the Network tab
2. **Load the page** and note the request ID in logs
3. **Click various buttons** to generate telemetry
4. **In Application Insights**, query for the specific RequestId or OperationId
5. **Verify** that all related telemetry items appear in the results

The correlation implementation ensures that every piece of telemetry can be traced back to its originating HTTP request and logical operation, providing comprehensive observability for the Application Insights demonstration.