namespace Core.Services;

public interface IBaseService
{
    bool Verbose { get; set; }
}

public class BaseService : IBaseService
{
    public bool Verbose { get; set; } = true;
}
