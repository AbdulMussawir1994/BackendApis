namespace DAL.ServiceLayer.Extensions;

public class RateLimitOptions
{
    public int Limit { get; set; } = 10;
    public int SpammersLimit { get; set; } = 20;
    public TimeSpan TimeWindow { get; set; } = TimeSpan.FromSeconds(10);
    public TimeSpan SpammerBlockTime { get; set; } = TimeSpan.FromMinutes(1);
}
