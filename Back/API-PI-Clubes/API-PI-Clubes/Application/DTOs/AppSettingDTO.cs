namespace API_PI_Clubes.Application.DTOs;

public record SaveSettingDTO(string Key, string Value, bool IsSecret, string? Description);

public class ResponseSettingBackofficeDTO
{
    public string Key { get; set; } = default!;
    public string Value { get; set; } = default!;
    public bool IsSecret { get; set; }
    public string? Description { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}