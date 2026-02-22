using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NeuroNotes.Application.Features.Chat.Commands.ClearChatHistory.Global;
using NeuroNotes.Application.Features.Chat.Commands.ClearChatHistory.Note;
using NeuroNotes.Application.Features.Chat.Commands.SendChatMessage;
using NeuroNotes.Application.Features.Chat.Commands.SendChatMessage.Global;
using NeuroNotes.Application.Features.Chat.Commands.SendChatMessage.Note;
using NeuroNotes.Application.Features.Chat.Queries.GetChatHistory;
using NeuroNotes.Application.Features.Chat.Queries.GetChatHistory.Global;
using NeuroNotes.Application.Features.Chat.Queries.GetChatHistory.Note;

namespace NeuroNotes.Api.Controllers
{
    [Authorize]
    [Produces("application/json")]
    public class ChatController : BaseController
    {
        /// <summary>
        /// Отправить сообщение в глобальный чат (RAG по заметкам)
        /// </summary>
        [HttpPost("global/send")]
        [ProducesResponseType(typeof(SendChatMessageResponse), StatusCodes.Status200OK)]
        public async Task<ActionResult<SendChatMessageResponse>> SendGlobalChatMessage([FromBody] SendGlobalChatMessageCommand command)
        {
            var result = await Mediator.Send(command);
            return Ok(result);
        }

        /// <summary>
        /// Получить историю сообщений глобального чата
        /// </summary>
        [HttpGet("global/history")]
        [ProducesResponseType(typeof(ChatHistoryResponse), StatusCodes.Status200OK)]
        public async Task<ActionResult<ChatHistoryResponse>> GetGlobalChatHistory()
        {
            var query = new GetGlobalChatHistoryQuery();
            var result = await Mediator.Send(query);
            return Ok(result);
        }

        /// <summary>
        /// Очистить историю сообщений глобального чата
        /// </summary>
        [HttpDelete("global/history")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> ClearGlobalChatHistory()
        {
            await Mediator.Send(new ClearGlobalChatHistoryCommand());
            return NoContent();
        }

        /// <summary>
        /// Отправить сообщение в чат по заметке
        /// </summary>
        [HttpPost("notes/{noteId:guid}/send")]
        [ProducesResponseType(typeof(SendChatMessageResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<SendChatMessageResponse>> SendNoteMessage(
            [FromRoute] Guid noteId,
            [FromBody] SendNoteChatMessageDto dto)
        {
            var command = new SendNoteChatMessageCommand(noteId, dto.Message);
            var result = await Mediator.Send(command);
            return Ok(result);
        }

        /// <summary>
        /// Получить историю сообщений чата по заметке
        /// </summary>
        [HttpGet("notes/{noteId:guid}/history")]
        [ProducesResponseType(typeof(ChatHistoryResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ChatHistoryResponse>> GetNoteHistory([FromRoute] Guid noteId)
        {
            var result = await Mediator.Send(new GetNoteChatHistoryQuery(noteId));
            return Ok(result);
        }

        /// <summary>
        /// Очистить историю сообщений чата по заметке
        /// </summary>
        [HttpDelete("notes/{noteId:guid}/history")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ClearNoteHistory([FromRoute] Guid noteId)
        {
            await Mediator.Send(new ClearNoteChatHistoryCommand(noteId));
            return NoContent();
        }
    }
}
