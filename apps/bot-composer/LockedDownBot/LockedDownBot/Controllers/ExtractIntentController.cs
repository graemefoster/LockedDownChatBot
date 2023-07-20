using System.Threading;
using System.Threading.Tasks;
using LockedDownBotSemanticKernel.Primitives;
using LockedDownBotSemanticKernel.Skills.Intent.DetectIntent;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LockedDownBot.Controllers
{
    /// <summary>
    /// A controller that handles skill replies to the bot.
    /// </summary>
    [ApiController]
    [Route("api/extractintent")]
    public class ExtractIntentController : Controller
    {
        private readonly ILogger<ExtractIntentController> _logger;
        private readonly IConfiguration _config;

        public ExtractIntentController(ILogger<ExtractIntentController> logger, IConfiguration config)
        {
            _logger = logger;
            _config = config;
        }

        public async Task<InputOutputs.DetectIntentOutput> Get(string input, CancellationToken token)
        {
            var kernel = new SemanticKernelWrapperFactory().GetFromSettings(
                _config["OPENAI_ENDPOINT"],
                _config["OPENAI_KEY"],
                _config["OPENAI_MODEL"]);

            var intents = new[] { "Accounts", "Information", "Payments", "Insurance" };

            var context = "You are a bank teller";

            var result = await
                new ExtractIntentFromInputFunction()
                    .Execute(kernel, new InputOutputs.DetectIntentInput(context, intents, input), token);

            return result;
        }
    }
}