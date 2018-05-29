using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using System.Collections.Generic;
using static Box.EnterpriseDevelopmentKit.Azure.BoxAzureCDNIntegration.Config;
using static Box.EnterpriseDevelopmentKit.Azure.Shared.Config;

namespace Box.EnterpriseDevelopmentKit.Azure
{
    public static class PurgeFileFromCDN
    {
        [FunctionName("PurgeFileFromCDN")]
        public static void Run([QueueTrigger("cdnpurgequeue", Connection = "AzureWebJobsStorage")]string fileId, TraceWriter log, ExecutionContext context)
        {
            var config = GetConfiguration(context);

            log.Info($"Queue trigger function processed fileId {fileId}");

            var creds = new AzureCredentialsFactory().FromServicePrincipal(config[AZURE_APP_CLIENT_ID_KEY], config[AZURE_APP_KEY_KEY], config[AZURE_AD_TENANT_ID_KEY], AzureEnvironment.AzureGlobalCloud);
            var azure = Microsoft.Azure.Management.Fluent.Azure.Authenticate(creds).WithSubscription(config[AZURE_SUBSCRIPTION_ID_KEY]);

            var contentPath = $"/{fileId}/*";
            log.Info($"Requesting purge of {contentPath} on CDN endpoint {config[CDN_ENDPOINT_NAME_KEY]}");
            azure.CdnProfiles.PurgeEndpointContent(config[CDN_RESOURCE_GROUP_NAME_KEY], config[CDN_PROFILE_NAME_KEY], config[CDN_ENDPOINT_NAME_KEY], new List<string>() { contentPath });
            log.Info($"Completed purge of {contentPath} on CDN endpoint {config[CDN_ENDPOINT_NAME_KEY]}");
        }
    }
}
