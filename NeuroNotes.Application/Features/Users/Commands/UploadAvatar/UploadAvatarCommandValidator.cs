using FluentValidation;
using NeuroNotes.Application.Common.Constants;

namespace NeuroNotes.Application.Features.Users.Commands.UploadAvatar
{
    public class UploadAvatarCommandValidator : AbstractValidator<UploadAvatarCommand>
    {
        public UploadAvatarCommandValidator()
        {
            RuleFor(x => x.FileStream)
                .NotNull().WithMessage("File is required.")
                .Must(stream => stream != null && stream.Length > 0)
                .WithMessage("File cannot be empty.")
                .Must(stream => stream != null && stream.Length <= FileConstants.MaxImageUploadSizeBytes)
                .WithMessage($"File size cannot exceed {FileConstants.MaxImageUploadSizeMbString}.");

            RuleFor(x => x.ContentType)
                .NotEmpty().WithMessage("Content type is required.")
                .Must(ct => FileConstants.SupportedImageContentTypes.Contains(ct.ToLowerInvariant()))
                .WithMessage($"Invalid file type. Allowed types: {FileConstants.GetSupportedImageContentTypesString()}");

            RuleFor(x => x.FileName)
                .NotEmpty().WithMessage("File name is required.")
                .Must(fn => FileConstants.SupportedImageExtensions.Contains(Path.GetExtension(fn).ToLowerInvariant()))
                .WithMessage($"Invalid file extension. Allowed extensions: {FileConstants.GetSupportedImageExtensionsString()}");
        }
    }
}
