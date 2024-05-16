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
        public int AddNumberOfInstalls([FromBody] Guid guid)
        {
            var installs = _fixesProvider.IncreaseFixInstallsCount(guid);
            return installs;
        }


        [HttpPut("score/change")]
        public int ChangeRating([FromBody] Tuple<Guid, sbyte> message)
        {
            var score = _fixesProvider.ChangeFixScore(message.Item1, message.Item2);
            return score;
        }


        [HttpPost("report")]
        public void ReportFix([FromBody] Tuple<Guid, string> message)
        {
            _fixesProvider.AddReport(message.Item1, message.Item2);
        }
    }
}
