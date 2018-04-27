﻿using Box.V2;
using Box.V2.Config;
using Box.V2.JWTAuth;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;

namespace Box.EnterpriseDevelopmentKit.Azure
{
    static class Config
    {
        public const string BOX_CONFIG_KEY = "BoxConfig";
        public const string BOX_DELIVERY_TIMESTAMP_HEADER = "BOX-DELIVERY-TIMESTAMP";
        public const string BOX_SIGNATURE_PRIMARY_HEADER = "BOX-SIGNATURE-PRIMARY";
        public const string BOX_SIGNATURE_SECONDARY_HEADER = "BOX-SIGNATURE-SECONDARY";
        public const string BOX_WEBHOOK_PRIMARY_KEY_KEY = "BoxWebhookPrimaryKey";
        public const string BOX_WEBHOOK_SECONDARY_KEY_KEY = "BoxWebhookSecondaryKey";

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