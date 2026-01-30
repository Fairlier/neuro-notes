using FluentValidation;

namespace NeuroNotes.Application.Features.Auth.Commands.RegisterUser
{
    public class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>
    {
        public RegisterUserCommandValidator()
        {
            RuleFor(v => v.Email)
                .NotEmpty().WithMessage("Email is required.")
                .EmailAddress().WithMessage("Email format is invalid.");

            RuleFor(v => v.Password)
                .NotEmpty().WithMessage("Password is required.");
        }
    }
}
