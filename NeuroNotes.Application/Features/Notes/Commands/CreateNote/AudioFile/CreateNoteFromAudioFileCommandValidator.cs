using FluentValidation;
using NeuroNotes.Application.Common.Constants;

namespace NeuroNotes.Application.Features.Notes.Commands.CreateNote.AudioFile
{
    public class CreateNoteFromAudioFileCommandValidator : AbstractValidator<CreateNoteFromAudioFileCommand>
    {
        public CreateNoteFromAudioFileCommandValidator()
        {
            RuleFor(v => v.Title)
                .NotEmpty().WithMessage("Title is required.")
                .MaximumLength(200).WithMessage("Title must not exceed 200 characters.");

            RuleFor(v => v.FileStream)
                .NotNull().WithMessage("File stream is missing.")
                .Must(stream => stream.Length > 0).WithMessage("File cannot be empty.")
                .Must(stream => stream.Length <= FileConstants.MaxAudioUploadSizeBytes)
                .WithMessage($"Audio file size must not exceed {FileConstants.MaxAudioUploadSizeMbString}.");

            RuleFor(v => v.FileName)
                .NotEmpty().WithMessage("File name is required.")
                .Must(HaveValidExtension)
                .WithMessage($"Invalid file extension. Allowed: {FileConstants.GetSupportedExtensionsString()}");
        }

        private bool HaveValidExtension(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return false;

            var ext = Path.GetExtension(fileName)?.ToLowerInvariant();

            return !string.IsNullOrEmpty(ext) && FileConstants.SupportedAudioExtensions.Contains(ext);
        }
    }
}
