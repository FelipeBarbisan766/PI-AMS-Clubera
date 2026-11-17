using API_PI_Clubes.Application.DTOs;
using API_PI_Clubes.Application.Exceptions;
using API_PI_Clubes.Application.Interfaces.IRepositories;
using API_PI_Clubes.Application.Interfaces.IServices;
using API_PI_Clubes.Model;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace API_PI_Clubes.Application.Services;

public class SettingService : ISettingService
{
    private readonly IAppSettingRepository _repository;
    private readonly IConfiguration _configuration;
    private readonly byte[] _masterKey;

    public SettingService(IAppSettingRepository repository, IConfiguration configuration)
    {
        _repository = repository;
        _configuration = configuration;
        _masterKey = Convert.FromBase64String(
            configuration["SETTINGS_MASTER_KEY"]
            ?? throw new InvalidOperationException("SETTINGS_MASTER_KEY não configurada."));
    }

    public Task<List<ResponseSettingBackofficeDTO>> GetAllForBackofficeAsync(CancellationToken ct) =>
        _repository.GetAllForBackofficeAsync(ct);

    public async Task SaveForBackofficeAsync(SaveSettingDTO dto, Guid userId, CancellationToken ct)
    {
        var key = dto.Key.Trim();
        var storedValue = dto.IsSecret ? SettingsCrypto.Encrypt(dto.Value, _masterKey) : dto.Value;

        var setting = await _repository.GetByKeyForUpdateAsync(key, ct);

        if (setting is null)
        {
            _repository.Add(new AppSetting
            {
                Key = key,
                Value = storedValue,
                IsSecret = dto.IsSecret,
                Description = dto.Description,
                UpdatedAt = DateTime.UtcNow,
                UpdatedBy = userId.ToString()
            });
        }
        else
        {
            setting.Value = storedValue;
            setting.IsSecret = dto.IsSecret;
            setting.Description = dto.Description;
            setting.UpdatedAt = DateTime.UtcNow;
            setting.UpdatedBy = userId.ToString();
        }

        try
        {
            await _repository.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 })
        {
            throw new ConflictException("Já existe uma configuração com essa chave.");
        }

        ((IConfigurationRoot)_configuration).Reload();
    }
}