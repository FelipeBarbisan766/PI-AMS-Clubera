using API_PI_Clubes.Application.DTOs;

namespace API_PI_Clubes.Application.Interfaces.IServices;

public interface ISettingService
{
    Task<List<ResponseSettingBackofficeDTO>> GetAllForBackofficeAsync(CancellationToken ct);
    Task SaveForBackofficeAsync(SaveSettingDTO dto, Guid userId, CancellationToken ct);
}