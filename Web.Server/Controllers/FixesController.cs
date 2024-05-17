using Common.Entities.Fixes;
using Microsoft.AspNetCore.Mvc;
using Superheater.Web.Server.Providers;

namespace Superheater.Web.Server.Controllers
{
    [ApiController]
    [Route("api/fixes")]
    public sealed class FixesController : ControllerBase
    {
        private readonly ILogger<FixesController> _logger;
        private readonly FixesProvider _fixesProvider;

        public FixesController(
            ILogger<FixesController> logger,
            FixesProvider fixesProvider
            )
        {
            _logger = logger;
            _fixesProvider = fixesProvider;
        }


        [HttpGet]
        public List<FixesList> GetFixesList() => _fixesProvider.GetFixesList();


        [HttpGet("{guid:Guid}")]
        public bool GetFixByGuid(Guid guid) => _fixesProvider.CheckIfFixExists(guid);


        [HttpPut("installs/add")]
        public int AddNumberOfInstalls([FromBody] Guid guid) => _fixesProvider.IncreaseFixInstallsCount(guid);


        [HttpPut("score/change")]
        public int ChangeRating([FromBody] Tuple<Guid, sbyte> message) => _fixesProvider.ChangeFixScore(message.Item1, message.Item2);


        [HttpPost("report")]
        public void ReportFix([FromBody] Tuple<Guid, string> message) => _fixesProvider.AddReport(message.Item1, message.Item2);


        [HttpPut("delete")]
        public StatusCodeResult DeleteFix([FromBody] Tuple<Guid, bool, string> message)
        {
            var result = _fixesProvider.ChangeFixDisabledState(message.Item1, message.Item2, message.Item3);

            if (result)
            {
                return StatusCode(StatusCodes.Status200OK);
            }
            else
            {
                return StatusCode(StatusCodes.Status403Forbidden);
            }
        }


        [HttpPost("add")]
        public async Task<StatusCodeResult> AddFixAsync([FromBody] Tuple<int, string, string, string> message)
        {
            var result = await _fixesProvider.AddFixAsync(message.Item1, message.Item2, message.Item3, message.Item4).ConfigureAwait(false);

            if (result)
            {
                return StatusCode(StatusCodes.Status200OK);
            }
            else
            {
                return StatusCode(StatusCodes.Status403Forbidden);
            }
        }
    }
}
