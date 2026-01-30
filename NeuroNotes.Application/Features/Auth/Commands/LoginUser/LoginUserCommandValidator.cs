using FluentValidation;

namespace NeuroNotes.Application.Features.Auth.Commands.LoginUser
{
    public class LoginUserCommandValidator : AbstractValidator<LoginUserCommand>
    {
        public LoginUserCommandValidator()
        {
            RuleFor(v => v.Email)
                .NotEmpty().WithMessage("Email is required.")
                .EmailAddress().WithMessage("A valid email address is required.");

            RuleFor(v => v.Password)
                .NotEmpty().WithMessage("Password is required.");
        }
    }
}
