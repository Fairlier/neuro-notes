
using FluentValidation;

namespace NeuroNotes.Application.Features.Notes.Queries.GetNoteList
{
    public class GetNoteListQueryValidator : AbstractValidator<GetNoteListQuery>
    {
        private const int MaxPageSize = 100;
        private const int MaxSearchTermLength = 500;

        public GetNoteListQueryValidator()
        {
            RuleFor(x => x.Page)
                .GreaterThan(0)
                .WithMessage("Page must be greater than 0.");

            RuleFor(x => x.PageSize)
                .InclusiveBetween(1, MaxPageSize)
                .WithMessage($"PageSize must be between 1 and {MaxPageSize}.");

            RuleFor(x => x.SearchTerm)
                .MaximumLength(MaxSearchTermLength)
                .When(x => x.SearchTerm != null)
                .WithMessage($"Search term must not exceed {MaxSearchTermLength} characters.");

            RuleFor(x => x.CreatedTo)
                .GreaterThanOrEqualTo(x => x.CreatedFrom)
                .When(x => x.CreatedFrom.HasValue && x.CreatedTo.HasValue)
                .WithMessage("CreatedTo must be greater than or equal to CreatedFrom.");

            RuleFor(x => x.UpdatedTo)
                .GreaterThanOrEqualTo(x => x.UpdatedFrom)
                .When(x => x.UpdatedFrom.HasValue && x.UpdatedTo.HasValue)
                .WithMessage("UpdatedTo must be greater than or equal to UpdatedFrom.");

            RuleFor(x => x.SearchMode)
                .IsInEnum()
                .WithMessage("Invalid search mode.");

            RuleFor(x => x.SortBy)
                .IsInEnum()
                .WithMessage("Invalid sort field.");

            RuleFor(x => x.SortDirection)
                .IsInEnum()
                .WithMessage("Invalid sort direction.");
        }
    }
}
