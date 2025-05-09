namespace DAL.RepositoryLayer.IRepositories;

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
