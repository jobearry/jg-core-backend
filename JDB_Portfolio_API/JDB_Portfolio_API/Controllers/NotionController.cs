using JDB_Portfolio_API.Application.DTO;
using JDB_Portfolio_API.Domain.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Portfolio_API.Controllers.Notion
{
    [Route("api/v2/[controller]")]
    [ApiExplorerSettings(GroupName= "v2")] 
    [ApiController]
    public class NotionController : ControllerBase
    {
        private readonly INotionClientService _notionClient;
        public NotionController(INotionClientService notionClient)
        {
            _notionClient = notionClient;
        }

        [HttpGet("query/{pageId}")]
        public async Task<ActionResult<NotionPageCardDto>> QueryPage(string pageId)
        {
            var result = await _notionClient.QueryPageAsync(pageId);
            return Ok(result); // returns raw JSON string
        }

        [HttpGet("query/{pageId}/count")]
        public async Task<IActionResult> QueryPageCount(string pageId)
        {
            var result = await _notionClient.QueryPageAsync(pageId);
            return Ok(result.Count);
        }
    }
}
