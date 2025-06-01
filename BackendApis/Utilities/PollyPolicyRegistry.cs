using Polly;
using Polly.Extensions.Http;

namespace BackendApis.Utilities;

public static class PollyPolicyRegistry
{
    public static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy() =>
        HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
                onRetry: (outcome, timespan, retryAttempt, context) =>
                {
                    Console.WriteLine($"[Retry] Attempt {retryAttempt} after {timespan.TotalSeconds}s: {outcome.Exception?.Message}");
                });

    public static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy() =>
        HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: 3,
                durationOfBreak: TimeSpan.FromSeconds(30),
                onBreak: (outcome, breakDelay) =>
                {
                    Console.WriteLine($"[CircuitBreaker] OPEN for {breakDelay.TotalSeconds}s due to: {outcome.Exception?.Message}");
                },
                onReset: () => Console.WriteLine("[CircuitBreaker] CLOSED"));

    public static IAsyncPolicy<string> GetFallbackPolicy() =>
        Policy<string>
            .Handle<Exception>()
            .FallbackAsync(
                fallbackValue: "Sorry, something went wrong. Please try again later.",
                onFallbackAsync: async e =>
                {
                    await Task.Run(() => Console.WriteLine($"[Fallback] Triggered due to: {e.Exception?.Message}"));
                });
}
