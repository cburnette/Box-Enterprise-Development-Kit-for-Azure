using Box.V2;
using Box.V2.Config;
using Box.V2.JWTAuth;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;

namespace Box.EnterpriseDevelopmentKit.Azure
{
    static class Config
    {
        public const string BOX_CONFIG_KEY = "BoxConfig";
        public const string BOX_WEBHOOK_PRIMARY_KEY_KEY = "BoxWebhookPrimaryKey";
        public const string BOX_WEBHOOK_SECONDARY_KEY_KEY = "BoxWebhookSecondaryKey";
        public const string CDN_STORAGE_CONNECTION_STRING_KEY = "CDNStorageConnectionString";
        public const string CDN_STORAGE_CONTAINER_NAME_KEY = "CDNStorageContainerName";
        public const string CDN_STORAGE_QUEUE_CONNECTION_STRING_KEY = "AzureWebJobsStorage";
        public const string CDN_ENDPOINT_NAME_KEY = "CDNEndpointName";
        public const string CDN_FORCE_PURGE_KEY = "CDNForcePurge";
        public const string METADATA_TEMPLATE_NAME_KEY = "BoxMetadataTemplateName";
        public const string AZURE_AD_TENANT_ID_KEY = "AzureADTenantId";
        public const string AZURE_APP_CLIENT_ID_KEY = "AzureAppClientId";
        public const string AZURE_APP_KEY_KEY = "AzureAppKey";
        public const string AZURE_SUBSCRIPTION_ID_KEY = "AzureSubscriptionId";

        public const string STORAGE_CONTAINER_FILENAME_FORMAT_STRING = "{0}/{1}";
        public const string CDN_URL_FORMAT_STRING = "https://{0}.azureedge.net/{1}/{2}";
        public const string CDN_PURGE_QUEUE_NAME = "cdnpurgequeue";
        public const string METADATA_SCOPE = "enterprise";
        public const string BOX_DELIVERY_TIMESTAMP_HEADER = "BOX-DELIVERY-TIMESTAMP";
        public const string BOX_SIGNATURE_PRIMARY_HEADER = "BOX-SIGNATURE-PRIMARY";
        public const string BOX_SIGNATURE_SECONDARY_HEADER = "BOX-SIGNATURE-SECONDARY";

        public static IConfigurationRoot GetConfiguration(ExecutionContext context)
        {
            //https://blog.jongallant.com/2018/01/azure-function-config/
            var config = new ConfigurationBuilder()
                .SetBasePath(context.FunctionAppDirectory)
                .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();

            return config;
        }

        public static BoxClient GetBoxUserClient(IConfigurationRoot config, string userId)
        {
            IBoxConfig boxConfig = null;
            var configJson = config[BOX_CONFIG_KEY];
            boxConfig = BoxConfig.CreateFromJsonString(configJson);

            var session = new BoxJWTAuth(boxConfig);
            var userToken = session.UserToken(userId);

            return session.UserClient(userToken, userId);
        }
    }
}
