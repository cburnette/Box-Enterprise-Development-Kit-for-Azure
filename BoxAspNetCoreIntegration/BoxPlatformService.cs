using Box.V2;
using Box.V2.Config;
using Box.V2.JWTAuth;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;

namespace Box.AspNetCore.Integration
{
    public static class BoxPlatformServiceExtensions
    {
        public static IServiceCollection AddBoxPlatform(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<BoxPlatformServiceOptions>(configuration);
            services.AddSingleton<IBoxPlatformService, BoxPlatformService>();

            return services;
        }
    }

    public class BoxPlatformServiceOptions
    {
        public string BoxConfig { get; set; }
    }

    public interface IBoxPlatformService
    {
        BoxClient AdminClient(string asUser = null);
        BoxClient UserClient(string userId);
    }

    public class BoxPlatformService : IBoxPlatformService
    {
        private readonly IOptions<BoxPlatformServiceOptions> _boxOptions;
        private readonly IDistributedCache _distributedCache;
        private readonly ILogger<BoxPlatformService> _logger;
        private readonly IBoxConfig _boxConfig;
        private readonly BoxJWTAuth _boxJWTAuth;
        private readonly TimeSpan CACHE_ITEM_TTL = TimeSpan.FromMinutes(45);

        public BoxPlatformService(IOptions<BoxPlatformServiceOptions> boxOptions, IDistributedCache distributedCache, ILogger<BoxPlatformService> logger)
        {
            _boxOptions = boxOptions;
            _distributedCache = distributedCache;
            _logger = logger;

            _boxConfig = BoxConfig.CreateFromJsonString(_boxOptions.Value.BoxConfig);
            _boxJWTAuth = new BoxJWTAuth(_boxConfig);
        }

        public BoxClient AdminClient(string asUser = null)
        {
            //check cache for existing admin token
            var cacheKey = $"/box/{_boxConfig.ClientId}/admin-token";
            var adminToken = _distributedCache.GetString(cacheKey);
            if (string.IsNullOrEmpty(adminToken))
            {
                //fetch a new admin token from Box
                adminToken = _boxJWTAuth.AdminToken();

                //store the token in the cache with a 45 minute expiration
                var options = new DistributedCacheEntryOptions() { AbsoluteExpirationRelativeToNow = CACHE_ITEM_TTL };
                _distributedCache.SetString(cacheKey, adminToken, options);
            }

            return _boxJWTAuth.AdminClient(adminToken, asUser: asUser);
        }

        public BoxClient UserClient(string userId)
        {
            //check cache for existing user token
            var cacheKey = $"/box/{_boxConfig.ClientId}/user-token/{userId}";
            var userToken = _distributedCache.GetString(cacheKey);
            if (string.IsNullOrEmpty(userToken))
            {
                //fetch a new user token from Box
                userToken = _boxJWTAuth.UserToken(userId);

                //store the token in the cache with a 45 minute expiration
                var options = new DistributedCacheEntryOptions() { AbsoluteExpirationRelativeToNow = CACHE_ITEM_TTL };
                _distributedCache.SetString(cacheKey, userToken, options);
            }

            return _boxJWTAuth.UserClient(userToken, userId);
        }
    }
}
