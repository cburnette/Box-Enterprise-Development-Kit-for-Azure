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
        public const string APPINSIGHTS_INSTRUMENTATIONKEY_KEY = "APPINSIGHTS_INSTRUMENTATIONKEY";
        public const string TIMER_SCHEDULE_EXPRESSION = "*/5 * * * * *";

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

        public static BoxClient GetBoxAdminClient(ExecutionContext context, IConfigurationRoot config)
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
