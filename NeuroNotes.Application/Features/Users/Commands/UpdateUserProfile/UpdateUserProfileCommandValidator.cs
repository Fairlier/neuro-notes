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
        }
    }
}
