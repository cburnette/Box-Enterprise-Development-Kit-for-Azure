using Microsoft.Extensions.Configuration;

namespace Box.EnterpriseDevelopmentKit.Azure.BoxAzureCDNIntegration
{
    static class Config
    {
        public const string BOX_CONFIG_KEY = "BoxConfig";
        public const string BOX_WEBHOOK_PRIMARY_KEY_KEY = "BoxWebhookPrimaryKey";
        public const string BOX_WEBHOOK_SECONDARY_KEY_KEY = "BoxWebhookSecondaryKey";
        public const string CDN_RESOURCE_GROUP_NAME_KEY = "CDNResourceGroupName";
        public const string CDN_PROFILE_NAME_KEY = "CDNProfileName";
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
        public const string TABLE_STORAGE_CONNECTION_STRING_KEY = "AzureWebJobsStorage";
        public const string USE_STORAGE_ORIGIN_KEY = "UseStorageOrigin";

        public const string STORAGE_CONTAINER_FILENAME_FORMAT_STRING = "{0}/{1}";
        public const string CDN_FILE_TABLE_NAME = "boxcdnfileinfo";
        public const string CDN_GUID_TABLE_NAME = "boxcdnguidinfo";
        public const string CDN_URL_FORMAT_STRING = "https://{0}.azureedge.net/{1}/{2}";
        public const string CDN_URL_FORMAT_STRING_CUSTOM_ORIGIN = "https://{0}.azureedge.net/{1}";
        public const string CDN_PURGE_QUEUE_NAME = "cdnpurgequeue";
        public const string METADATA_SCOPE = "enterprise";

        public static bool UseStorageOrigin(IConfigurationRoot config)
        {
            var useStorageOrigin = config[USE_STORAGE_ORIGIN_KEY];

            if (string.IsNullOrEmpty(useStorageOrigin))
            {
                //default to true
                return true;
            }
            else
            {
                return useStorageOrigin.ToLower() == "true";
            }
        }
    }
}
