using JDB_Portfolio_API.Application.DTO;
using JDB_Portfolio_API.Application.Services;
using JDB_Portfolio_API.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Portfolio_API.Controllers.Notion
{
    [Route("api/v1/[controller]")]
    [ApiController]
    [Tags("ExternalServices")]
    [Authorize]
    public class NotionController : ControllerBase
    {
        private readonly PortfolioService _portfolioService;
        public NotionController(PortfolioService portfolioService)
        {
            _portfolioService = portfolioService;
        }

        [HttpGet("query/{pageId}")]
        public async Task<IActionResult> QueryPage(string pageId)
        {
            var result = await _portfolioService.GetPageAsync(pageId);
            if (!result.IsSuccess)
            {
                return StatusCode(502, new { error = result.Error });
            }

            return Ok(result.Value);
        }

        [HttpGet("query/{pageId}/count")]
        public async Task<IActionResult> QueryPageCount(string pageId)
        {
            var result = await _portfolioService.GetPageAsync(pageId);
            if (!result.IsSuccess)
            {
                return StatusCode(502, new { error = result.Error });
            }
            return Ok(result.Value!.Count);
        }
    }
}
