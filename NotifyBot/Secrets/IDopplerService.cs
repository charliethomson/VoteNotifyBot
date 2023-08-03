namespace NotifyBot.Secrets;

public interface IDopplerService
{
    Task<string> Get(string key);
}