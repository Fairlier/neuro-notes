using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NeuroNotes.Application.Features.Notes.Commands.CreateNote;
using NeuroNotes.Application.Features.Notes.Commands.CreateNote.AudioFile;
using NeuroNotes.Application.Features.Notes.Commands.CreateNote.DirectText;
using NeuroNotes.Application.Features.Notes.Commands.DeleteNote;
using NeuroNotes.Application.Features.Notes.Commands.StructureNote;
using NeuroNotes.Application.Features.Notes.Commands.SummarizeNote;
using NeuroNotes.Application.Features.Notes.Commands.TranscribeNote;
using NeuroNotes.Application.Features.Notes.Commands.UpdateNote;
using NeuroNotes.Application.Features.Notes.Queries.GetNoteDetails;
using NeuroNotes.Application.Features.Notes.Queries.GetNoteList;
using NeuroNotes.Domain.Enums;

namespace NeuroNotes.Api.Controllers
{
    [Authorize]
    [Produces("application/json")]
    public class NotesController : BaseController
    {
        /// <summary>
        /// Получить список заметок пользователя с фильтрацией, поиском и сортировкой
        /// </summary>
        /// <param name="status">Фильтр по статусу (Pending, Raw, Structured, Summarized, Failed)</param>
        /// <param name="category">Фильтр по категории</param>
        /// <param name="createdFrom">Дата создания от (включительно)</param>
        /// <param name="createdTo">Дата создания до (включительно)</param>
        /// <param name="updatedFrom">Дата обновления от (включительно)</param>
        /// <param name="updatedTo">Дата обновления до (включительно)</param>
        /// <param name="searchTerm">Поисковый запрос по содержимому заметок</param>
        /// <param name="searchMode">Режим поиска: Semantic (по смыслу) или Text (точное совпадение)</param>
        /// <param name="sortBy">Поле сортировки (игнорируется при поиске - результаты по релевантности)</param>
        /// <param name="sortDirection">Направление сортировки</param>
        /// <param name="page">Номер страницы (по умолчанию 1)</param>
        /// <param name="pageSize">Размер страницы (по умолчанию 20, максимум 100)</param>
        /// <returns>Список заметок с метаданными пагинации</returns>
        [HttpGet]
        [ProducesResponseType(typeof(NoteListResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<NoteListResponse>> GetAll(
            [FromQuery] NoteStatus? status = null,
            [FromQuery] NoteCategory? category = null,
            [FromQuery] DateTime? createdFrom = null,
            [FromQuery] DateTime? createdTo = null,
            [FromQuery] DateTime? updatedFrom = null,
            [FromQuery] DateTime? updatedTo = null,
            [FromQuery] string? searchTerm = null,
            [FromQuery] SearchMode searchMode = SearchMode.Semantic,
            [FromQuery] NoteSortBy sortBy = NoteSortBy.CreatedAt,
            [FromQuery] SortDirection sortDirection = SortDirection.Descending,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var query = new GetNoteListQuery
            {
                Status = status,
                Category = category,
                CreatedFrom = createdFrom,
                CreatedTo = createdTo,
                UpdatedFrom = updatedFrom,
                UpdatedTo = updatedTo,
                SearchTerm = searchTerm,
                SearchMode = searchMode,
                SortBy = sortBy,
                SortDirection = sortDirection,
                Page = page,
                PageSize = pageSize
            };

            return Ok(await Mediator.Send(query));
        }

        /// <summary>
        /// Получить детали заметки по ID
        /// </summary>
        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(NoteDetailsResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<NoteDetailsResponse>> Get(Guid id)
        {
            return Ok(await Mediator.Send(new GetNoteDetailsQuery(id)));
        }

        /// <summary>
        /// Создать заметку из прямого текста
        /// </summary>
        [HttpPost("directText")]
        [ProducesResponseType(typeof(CreateNoteResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<CreateNoteResponse>> CreateFromDirectText(
            [FromBody] CreateNoteFromDirectTextCommand command)
        {
            var result = await Mediator.Send(command);
            return Ok(result);
        }

        /// <summary>
        /// Создать заметку из аудиофайла
        /// </summary>
        [HttpPost("audioFile")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(CreateNoteResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<CreateNoteResponse>> CreateFromAudioFile(
            [FromForm] UploadFileNoteDto dto)
        {
            if (dto.File == null || dto.File.Length == 0)
            {
                return BadRequest("File is empty.");
            }

            using var stream = dto.File.OpenReadStream();

            var command = new CreateNoteFromAudioFileCommand
            {
                Title = dto.Title,
                FileStream = stream,
                FileName = dto.File.FileName,
                ContentType = dto.File.ContentType
            };

            var result = await Mediator.Send(command);
            return Ok(result);
        }

        /// <summary>
        /// Обновить заголовок или текст заметки вручную
        /// </summary>
        [HttpPatch("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateNoteCommand command)
        {
            if (id != command.Id && command.Id != Guid.Empty)
            {
                return BadRequest("ID mismatch");
            }

            command.Id = id;
            await Mediator.Send(command);

            return NoContent();
        }

        /// <summary>
        /// Удалить заметку
        /// </summary>
        [HttpDelete("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(Guid id)
        {
            await Mediator.Send(new DeleteNoteCommand(id));
            return NoContent();
        }

        /// <summary>
        /// Запустить транскрибацию аудио (повторно или вручную)
        /// </summary>
        [HttpPost("{id:guid}/transcribe")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Transcribe(Guid id)
        {
            await Mediator.Send(new TranscribeNoteCommand(id));
            return NoContent();
        }

        /// <summary>
        /// Запустить генерацию структуры (Markdown)
        /// </summary>
        [HttpPost("{id:guid}/structure")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Structure(Guid id)
        {
            await Mediator.Send(new StructureNoteCommand(id));
            return NoContent();
        }

        /// <summary>
        /// Запустить генерацию краткого содержания (Summary)
        /// </summary>
        [HttpPost("{id:guid}/summarize")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Summarize(Guid id)
        {
            await Mediator.Send(new SummarizeNoteCommand(id));
            return NoContent();
        }
    }
}
