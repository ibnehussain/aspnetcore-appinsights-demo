// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Application Insights Client-Side Configuration
window.addEventListener('load', function() {
    if (window.appInsights) {
        console.log('Application Insights JavaScript SDK loaded successfully');
        
        // Track custom page view with additional properties
        appInsights.trackPageView({
            name: document.title,
            properties: {
                'custom_page_name': document.title,
                'page_url': window.location.href,
                'user_agent': navigator.userAgent,
                'page_load_time': Date.now(),
                'demo_type': 'client_side_tracking'
            },
            measurements: {
                'page_load_duration': performance.timing.loadEventEnd - performance.timing.navigationStart,
                'dom_content_loaded': performance.timing.domContentLoadedEventEnd - performance.timing.navigationStart
            }
        });
        
        // Track custom event for Application Insights demo initialization
        appInsights.trackEvent({
            name: 'Demo_ClientSide_Initialized',
            properties: {
                'initialization_time': new Date().toISOString(),
                'page_url': window.location.href,
                'sdk_version': 'ai.2.min.js',
                'tracking_enabled': 'true'
            },
            measurements: {
                'initialization_duration': Date.now() - performance.timing.navigationStart
            }
        });
    } else {
        console.error('Application Insights JavaScript SDK failed to load');
    }
});

// Enhanced error handling with Application Insights tracking
window.addEventListener('error', function(event) {
    console.error('JavaScript Error:', event.error);
    
    if (window.appInsights) {
        appInsights.trackException({
            exception: event.error,
            properties: {
                'error_source': 'client_side',
                'page_url': window.location.href,
                'user_agent': navigator.userAgent,
                'demo_context': 'application_insights_demo'
            },
            severityLevel: 3 // Error level
        });
    }
});

// Track unhandled promise rejections
window.addEventListener('unhandledrejection', function(event) {
    console.error('Unhandled Promise Rejection:', event.reason);
    
    if (window.appInsights) {
        appInsights.trackException({
            exception: new Error(event.reason),
            properties: {
                'error_type': 'unhandled_promise_rejection',
                'error_source': 'client_side',
                'page_url': window.location.href,
                'demo_context': 'application_insights_demo'
            },
            severityLevel: 2 // Warning level
        });
    }
});

// Function to track custom button clicks with Application Insights
function trackButtonClick(buttonName, actionType) {
    if (window.appInsights) {
        appInsights.trackEvent({
            name: 'Demo_Button_Clicked',
            properties: {
                'button_name': buttonName,
                'action_type': actionType,
                'page_url': window.location.href,
                'timestamp': new Date().toISOString(),
                'demo_type': 'client_side_interaction'
            },
            measurements: {
                'click_time': Date.now()
            }
        });
    }
}

// Function to track AJAX call performance
function trackAjaxPerformance(url, method, duration, statusCode, success) {
    if (window.appInsights) {
        appInsights.trackDependency({
            name: 'AJAX_Request',
            data: url,
            duration: duration,
            success: success,
            properties: {
                'method': method,
                'status_code': statusCode.toString(),
                'request_url': url,
                'page_url': window.location.href,
                'demo_context': 'client_side_ajax'
            }
        });
    }
}

// Override fetch API to track all AJAX calls automatically
(function() {
    const originalFetch = window.fetch;
    
    window.fetch = function(...args) {
        const startTime = Date.now();
        const url = args[0];
        const options = args[1] || {};
        const method = options.method || 'GET';
        
        console.log('Tracking fetch request:', method, url);
        
        return originalFetch.apply(this, args)
            .then(response => {
                const duration = Date.now() - startTime;
                trackAjaxPerformance(url, method, duration, response.status, response.ok);
                return response;
            })
            .catch(error => {
                const duration = Date.now() - startTime;
                trackAjaxPerformance(url, method, duration, 0, false);
                
                // Track fetch errors
                if (window.appInsights) {
                    appInsights.trackException({
                        exception: error,
                        properties: {
                            'error_type': 'fetch_request_failed',
                            'request_url': url,
                            'request_method': method,
                            'page_url': window.location.href,
                            'demo_context': 'client_side_fetch_error'
                        },
                        severityLevel: 2
                    });
                }
                
                throw error;
            });
    };
})();

// Track page visibility changes
document.addEventListener('visibilitychange', function() {
    if (window.appInsights) {
        appInsights.trackEvent({
            name: 'Page_Visibility_Changed',
            properties: {
                'visibility_state': document.visibilityState,
                'page_url': window.location.href,
                'timestamp': new Date().toISOString()
            }
        });
    }
});
