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

namespace NeuroNotes.Api.Controllers
{
    [Authorize]
    [Produces("application/json")]
    public class NotesController : BaseController
    {
        /// <summary>
        /// Получить список всех заметок пользователя
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(NoteListResponse), StatusCodes.Status200OK)]
        public async Task<ActionResult<NoteListResponse>> GetAll()
        {
            return Ok(await Mediator.Send(new GetNoteListQuery()));
        }

        /// <summary>
        /// Получить детали заметки по ID
        /// </summary>
        [HttpGet("{id}")]
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
        [HttpPatch("{id}")]
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
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(Guid id)
        {
            await Mediator.Send(new DeleteNoteCommand(id));
            return NoContent();
        }

        // --- НОВЫЕ МЕТОДЫ ДЛЯ ЗАПУСКА AI ЗАДАЧ ---

        /// <summary>
        /// Запустить транскрибацию аудио (повторно или вручную)
        /// </summary>
        [HttpPost("{id}/transcribe")]
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
        [HttpPost("{id}/structure")]
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
        [HttpPost("{id}/summarize")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Summarize(Guid id)
        {
            await Mediator.Send(new SummarizeNoteCommand(id));
            return NoContent();
        }
    }
}
