using API_PI_Clubes.Application.DTOs;
using API_PI_Clubes.Model;

namespace API_PI_Clubes.Application.Interfaces.IRepositories;

public interface IAppSettingRepository
{
    Task<List<ResponseSettingBackofficeDTO>> GetAllForBackofficeAsync(CancellationToken ct);
    Task<AppSetting?> GetByKeyForUpdateAsync(string key, CancellationToken ct);
    void Add(AppSetting setting);
    Task SaveChangesAsync(CancellationToken ct);
}