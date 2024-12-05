using Impinj_Reader.Hubs;
using Impinj_Reader.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace Impinj_Reader.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EpcController : ControllerBase
    {
        private readonly IHubContext<MessageHub> _hubContext;

        public EpcController(IHubContext<MessageHub> hubContext)
        {
            _hubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));
        }

        [HttpPost]
        public async Task<IActionResult> Send([FromBody] AssociationMessage message)
        {
            if (message == null || string.IsNullOrWhiteSpace(message.Tarima) || string.IsNullOrWhiteSpace(message.Operador))
            {
                return BadRequest("El mensaje debe incluir Tarima, Operador y Timestamp.");
            }

            // Enviar mensaje a través de SignalR
            await _hubContext.Clients.All.SendAsync("sendMessage", message);

            return Ok("Mensaje enviado correctamente.");
        }
    }
}
