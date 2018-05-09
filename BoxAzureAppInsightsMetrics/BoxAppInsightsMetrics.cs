using System;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using static Box.EnterpriseDevelopmentKit.Azure.Shared.Config;

namespace Box.EnterpriseDevelopmentKit.Azure
{
    public static class BoxAppInsightsMetrics
    {
        public const string BOX_CONFIG_KEY = "BoxConfig";
        public const string APPINSIGHTS_INSTRUMENTATIONKEY_KEY = "APPINSIGHTS_INSTRUMENTATIONKEY";
        public const string TIMER_SCHEDULE_EXPRESSION = "*/5 * * * * *";

        //https://docs.microsoft.com/en-us/azure/azure-functions/functions-monitoring


        [FunctionName("BoxAppInsightsMetrics")]
        public static void Run([TimerTrigger("%TimerScheduleExpression%")]TimerInfo myTimer, ILogger insightsLog, ExecutionContext context)
        {      
            var msg = $"C# Timer trigger function executed at: {DateTime.Now}";
            insightsLog.LogInformation(msg);

            var config = GetConfiguration(context);
            var key = config[APPINSIGHTS_INSTRUMENTATIONKEY_KEY];

            var telemetryClient = new TelemetryClient() { InstrumentationKey = key };
            DateTime start = DateTime.UtcNow;

            var username = "Some Name";

            // Track an Event
            var evt = new EventTelemetry("Function called");
            UpdateTelemetryContext(evt.Context, context, username);
            telemetryClient.TrackEvent(evt);

            // Track a Metric
            var metric = new MetricTelemetry("Test Metric", DateTime.Now.Millisecond);
            UpdateTelemetryContext(metric.Context, context, username);
            telemetryClient.TrackMetric(metric);

            //insightsLog.LogMetric("Test Metric New", DateTime.Now.Millisecond);

            // Track a Dependency
            var dependency = new DependencyTelemetry
            {
                Name = "GET users/me",
                Target = "box.com",
                Timestamp = start,
                Duration = TimeSpan.FromSeconds(1),
                Success = false
            };
            UpdateTelemetryContext(dependency.Context, context, username);
            telemetryClient.TrackDependency(dependency);
        }

        // This correllates all telemetry with the current Function invocation
        private static void UpdateTelemetryContext(TelemetryContext context, ExecutionContext functionContext, string userName)
        {
            context.Operation.Id = functionContext.InvocationId.ToString();
            context.Operation.ParentId = functionContext.InvocationId.ToString();
            context.Operation.Name = functionContext.FunctionName;
            context.User.Id = userName;
        }

    }
}
