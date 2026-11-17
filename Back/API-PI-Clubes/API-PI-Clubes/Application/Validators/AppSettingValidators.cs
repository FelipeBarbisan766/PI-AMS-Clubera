using API_PI_Clubes.Application.DTOs;
using FluentValidation;

namespace API_PI_Clubes.Application.Validators;
public class SaveSettingDTOValidator : AbstractValidator<SaveSettingDTO>
{
    private static readonly string[] ForbiddenKeys = ["ConnectionStrings", "Security", "SETTINGS_MASTER_KEY"];
    private static readonly string[] SensitiveSuffixes = ["Key", "Password", "Secret", "Token", "ConnectionString"];

    public SaveSettingDTOValidator()
    {
        RuleFor(x => x.Key)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MaximumLength(200)
            .Matches(@"^[A-Za-z0-9_\-\.]+(:[A-Za-z0-9_\-\.]+)*$")
            .WithMessage("Chave inválida. Use o formato Secao:Chave (letras, números, _, - e .).")
            .Must(k => !ForbiddenKeys.Any(f =>
                k.Equals(f, StringComparison.OrdinalIgnoreCase) ||
                k.StartsWith(f + ":", StringComparison.OrdinalIgnoreCase)))
            .WithMessage("Essa chave só pode ser definida no .env.");

        RuleFor(x => x.Value).NotEmpty().MaximumLength(4000);
        RuleFor(x => x.Description).MaximumLength(500);

        RuleFor(x => x.IsSecret)
            .Must((dto, isSecret) => isSecret || !SensitiveSuffixes.Any(s =>
                (dto.Key ?? "").EndsWith(s, StringComparison.OrdinalIgnoreCase)))
            .WithMessage("Chaves terminadas em Key, Password, Secret, Token ou ConnectionString precisam de isSecret = true.");
    }
}