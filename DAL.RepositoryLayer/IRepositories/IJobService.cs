namespace DAL.RepositoryLayer.IRepositories
{
    public interface IJobService
    {
        Task ProcessJobAsync(string message);
        Task CreateNotificationsAutomatically();
    }
}
