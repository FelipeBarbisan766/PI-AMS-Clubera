using API_PI_Clubes.Infrastructure.Configuration;
using Microsoft.Data.SqlClient;

namespace API_PI_Clubes.Infrastructure.Data;

public class DbConfigurationProvider : ConfigurationProvider, IDisposable
{
    private readonly DbConfigurationSource _src;
    private Timer? _timer;

    public DbConfigurationProvider(DbConfigurationSource src)
    {
        _src = src;
        if (src.ReloadInterval is { } i)
            _timer = new Timer(_ =>
            {
                Load();
                OnReload();
            }, null, i, i);
    }

    public override void Load()
    {
        var data = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        try
        {
            using var conn = new SqlConnection(_src.ConnectionString);
            conn.Open();
            using var cmd = new SqlCommand("SELECT [Key], [Value], IsSecret FROM AppSettings", conn);
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                var value = r.GetString(1);
                data[r.GetString(0)] = r.GetBoolean(2)
                    ? SettingsCrypto.Decrypt(value, _src.MasterKey)
                    : value;
            }
        }
        catch (SqlException)
        {
        }

        Data = data;
    }

    public void Dispose() => _timer?.Dispose();
}