namespace API_PI_Clubes.Model;

public class AppSetting : BaseEntity
{
    public string Key { get; set; } = default!; 
    public string Value { get; set; } = default!;
    public bool IsSecret { get; set; }
    public string? Description { get; set; }
    public string? UpdatedBy { get; set; }
}
