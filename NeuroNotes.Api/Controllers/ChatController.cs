using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NeuroNotes.Application.Features.Chat.Commands.ClearChatHistory;
using NeuroNotes.Application.Features.Chat.Commands.SendMessage;
using NeuroNotes.Application.Features.Chat.Queries.GetChatHistory;

namespace NeuroNotes.Api.Controllers
{
    [Authorize]
    [Produces("application/json")]
    public class ChatController : BaseController
    {
        /// <summary>
        /// Отправить сообщение в чат
        /// </summary>
        [HttpPost("send")]
        [ProducesResponseType(typeof(SendMessageResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<SendMessageResponse>> SendMessage([FromBody] SendMessageCommand command)
        {
            var result = await Mediator.Send(command);
            return Ok(result);
        }

        /// <summary>
        /// Получить историю сообщений
        /// </summary>
        /// <param name="noteId">ID заметки (если null - вернется глобальный чат)</param>
        [HttpGet("history")]
        [ProducesResponseType(typeof(ChatHistoryResponse), StatusCodes.Status200OK)]
        public async Task<ActionResult<ChatHistoryResponse>> GetHistory([FromQuery] Guid? noteId)
        {
            var query = new GetChatHistoryQuery(noteId);
            var result = await Mediator.Send(query);
            return Ok(result);
        }

        /// <summary>
        /// Очистить историю чата
        /// </summary>
        /// <param name="noteId">ID заметки или null для глобального чата</param>
        [HttpDelete("history")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> ClearHistory([FromQuery] Guid? noteId)
        {
            await Mediator.Send(new ClearChatHistoryCommand(noteId));
            return NoContent();
        }
    }
}
