using Box.V2;
using Box.V2.Config;
using Box.V2.JWTAuth;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;

namespace Box.EnterpriseDevelopmentKit.Azure
{
    static class Config
    {
        public const int INITIAL_LOOKBACK_MINUTES = 60;
        public const string BOX_CONFIG_KEY = "BoxConfig";
        public const string STORAGE_CONNECTION_STRING_KEY = "AzureWebJobsStorage";
        public const string STREAM_POSITION_TABLE_NAME_KEY = "StreamPositionTableName";
        public const string ROW_KEY_VALUE = "BoxStreamPosition";
        public const string SERVICE_BUS_CONNECTION_STRING_KEY = "ServiceBusConnectionString";
        public const string SERVICE_BUS_TOPIC_NAME_KEY = "ServiceBusTopicName";

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

        public static BoxClient GetBoxAdminClient(IConfigurationRoot config)
        {
            IBoxConfig boxConfig = null;
            var configJson = config[BOX_CONFIG_KEY];
            boxConfig = BoxConfig.CreateFromJsonString(configJson);

            var session = new BoxJWTAuth(boxConfig);
            var adminToken = session.AdminToken();

            return session.AdminClient(adminToken);
        }
    }
}
