using Microsoft.Extensions.Options;
using System.Collections.Concurrent;

namespace BackendApis.Utilities
{
    public interface IRateLimiterService
    {
        /// <summary>
        /// Checks if the request from the given IP address is allowed based on rate limiting rules.
        /// </summary>
        /// <param name="ip">Client IP address.</param>
        /// <param name="message">A message describing the result (e.g., reason for blocking).</param>
        /// <returns>True if request is allowed; false otherwise.</returns>
        bool IsRequestAllowed(string ip, out string message);
    }

    public class RateLimiterService : IRateLimiterService
    {
        private readonly RateLimitOptions _options;
        private readonly ConcurrentDictionary<string, RequestTracker> _trackers = new();
        private readonly ILogger<RateLimiterService> _logger;

        public RateLimiterService(IOptions<RateLimitOptions> options, ILogger<RateLimiterService> logger)
        {
            _options = options.Value;
            _logger = logger;
        }

        public bool IsRequestAllowed(string ip, out string message)
        {
            var now = DateTime.UtcNow;
            var tracker = _trackers.GetOrAdd(ip, _ => new RequestTracker());

            // Check if currently blocked
            if (tracker.IsBlocked(now, _options.SpammerBlockTime))
            {
                message = "You are temporarily blocked due to excessive requests.";
                return false;
            }

            tracker.TrimOldRequests(now, _options.TimeWindow);

            if (tracker.Count >= _options.Limit)
            {
                message = "Too many requests. Try again later.";
                _logger.LogWarning("Rate limit exceeded for {ip}", ip);

                if (tracker.Count >= _options.SpammersLimit)
                {
                    if (!tracker.IsMarkedSpammer)
                    {
                        tracker.MarkAsSpammer(now);
                        _logger.LogWarning("Spammer detected: {ip}", ip);
                    }
                }

                return false;
            }

            tracker.AddRequest(now);
            message = string.Empty;
            return true;
        }
    }

}
