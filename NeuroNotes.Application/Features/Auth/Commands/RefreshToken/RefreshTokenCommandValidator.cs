using FluentValidation;

namespace NeuroNotes.Application.Features.Auth.Commands.RefreshToken
{
    public class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
    {
        public RefreshTokenCommandValidator()
        {
            RuleFor(v => v.Token)
                .NotEmpty().WithMessage("Refresh Token is required.");
        }
    }
}
