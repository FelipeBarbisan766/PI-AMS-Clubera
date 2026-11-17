using API_PI_Clubes.Application.DTOs;
using API_PI_Clubes.Application.Interfaces.IRepositories;
using API_PI_Clubes.Infrastructure.Data;
using API_PI_Clubes.Model;
using Microsoft.EntityFrameworkCore;

namespace API_PI_Clubes.Infrastructure.Repositories;

public class AppSettingRepository : IAppSettingRepository
{
    private readonly AppDbContext _context;

    public AppSettingRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<List<ResponseSettingBackofficeDTO>> GetAllForBackofficeAsync(CancellationToken ct)
    {
        return _context.AppSettings
            .AsNoTracking()
            .OrderBy(s => s.Key)
            .Select(s => new ResponseSettingBackofficeDTO
            {
                Key = s.Key,
                Value = s.IsSecret ? "********" : s.Value,
                IsSecret = s.IsSecret,
                Description = s.Description,
                UpdatedAt = s.UpdatedAt,
                UpdatedBy = s.UpdatedBy
            })
            .ToListAsync(ct);
    }

    public Task<AppSetting?> GetByKeyForUpdateAsync(string key, CancellationToken ct)
    {
        return _context.AppSettings.FirstOrDefaultAsync(s => s.Key == key, ct);
    }

    public void Add(AppSetting setting)
    {
        _context.AppSettings.Add(setting);
    }

    public Task SaveChangesAsync(CancellationToken ct)
    {
        return _context.SaveChangesAsync(ct);
    }
}