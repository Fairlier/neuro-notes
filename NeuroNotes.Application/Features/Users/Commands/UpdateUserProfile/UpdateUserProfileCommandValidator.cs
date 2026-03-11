using FluentValidation;

namespace NeuroNotes.Application.Features.Users.Commands.UpdateUserProfile
{
    public class UpdateUserProfileCommandValidator : AbstractValidator<UpdateUserProfileCommand>
    {
        public UpdateUserProfileCommandValidator()
        {
            RuleFor(v => v.Nickname)
                .NotEmpty().WithMessage("Nickname cannot be empty.") 
                .MaximumLength(50).WithMessage("Nickname is too long (max 50 chars).")
                .When(v => v.Nickname != null); 

            RuleFor(v => v.InterfaceLanguage)
                .NotEmpty().WithMessage("Language code cannot be empty.")
                .Length(2, 10).WithMessage("Invalid language code format (e.g. 'en', 'ru').")
                .When(v => v.InterfaceLanguage != null);

            RuleFor(v => v.Theme)
                .NotEmpty().WithMessage("Theme cannot be empty.")
                .MaximumLength(20).WithMessage("Theme name is too long (max 20 chars).")
                .Must(theme => new[] { "light", "dark", "system" }.Contains(theme.ToLower())).WithMessage("Invalid theme. Allowed values: light, dark, system.")
                .When(v => v.Theme != null);
        }
    }
}
