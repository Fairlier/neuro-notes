using FluentValidation;

namespace NeuroNotes.Application.Features.Notes.Queries.GetNoteDetails
{
    public class GetNoteDetailsQueryValidator : AbstractValidator<GetNoteDetailsQuery>
    {
        public GetNoteDetailsQueryValidator()
        {
            RuleFor(v => v.Id)
                .NotEmpty();
        }
    }
}
