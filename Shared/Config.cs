using Box.V2;
using Box.V2.Config;
using Box.V2.JWTAuth;
using Microsoft.Extensions.Configuration;
using Microsoft.Azure.WebJobs;
using Box.V2.Auth;
using System;

namespace Box.EnterpriseDevelopmentKit.Azure.Shared
{
    public static class Config
    {
        public const string BOX_CONFIG_KEY = "BoxConfig";

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
            var session = GetBoxSession(config);
            var adminToken = session.AdminToken();

            return session.AdminClient(adminToken);
        }

        public static BoxClient GetBoxUserClient(IConfigurationRoot config, string userId)
        {
            var session = GetBoxSession(config);
            var userToken = session.UserToken(userId);

            return session.UserClient(userToken, userId);
        }

        public static BoxClient GetBoxClientWithApiKeyAndToken(string apiKey, string accessToken)
        {
            var auth = new OAuthSession(accessToken, "NOT_USED", 3600, "bearer");
            var boxConfig = new BoxConfig(apiKey, "NOT_USED", new Uri("http://boxsdk"));
            var boxClient = new BoxClient(boxConfig, auth);

            return boxClient;
        }

        private static BoxJWTAuth GetBoxSession(IConfigurationRoot config)
        {
            IBoxConfig boxConfig = null;
            var configJson = config[BOX_CONFIG_KEY];
            boxConfig = BoxConfig.CreateFromJsonString(configJson);

            var session = new BoxJWTAuth(boxConfig);
            return session;
        }
    }
}
