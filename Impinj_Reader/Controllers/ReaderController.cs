using Impinj_Reader.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Impinj_Reader.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReaderController : ControllerBase
    {
        private readonly ReaderService _readerService;

        public ReaderController(ReaderService readerService)
        {
            _readerService = readerService;
        }

        [HttpPost("start")]
        public IActionResult Start()
        {
            try
            {
                _readerService.StartReader();
                return Ok("Reader started successfully.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error starting reader: {ex.Message}");
            }
        }

        [HttpPost("stop")]
        public IActionResult Stop()
        {
            try
            {
                _readerService.StopReader();
                return Ok("Reader stopped successfully.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error stopping reader: {ex.Message}");
            }
        }
    }
}
