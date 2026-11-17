using API_PI_Clubes.Infrastructure.Data;

namespace API_PI_Clubes.Infrastructure.Configuration;
public class DbConfigurationSource : IConfigurationSource
{
    public string ConnectionString { get; init; } = default!;
    public byte[] MasterKey { get; init; } = default!;
    public TimeSpan? ReloadInterval { get; init; }

    public IConfigurationProvider Build(IConfigurationBuilder b) => new DbConfigurationProvider(this);
}