using System.Threading;
using System.Threading.Tasks;
using LockedDownBotSemanticKernel.Primitives;
using LockedDownBotSemanticKernel.Primitives.Chains;
using LockedDownBotSemanticKernel.Skills.Intent.DetectIntent;
using LockedDownBotSemanticKernel.Skills.Intent.DetectIntentNextResponse;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LockedDownBot.Controllers
{
    /// <summary>
    /// A controller that handles skill replies to the bot.
    /// </summary>
    [ApiController]
    [Route("api/extractintentwithSuggestion")]
    public class ExtractIntentWithSuggestionController : Controller
    {
        private readonly ILogger<ExtractIntentWithSuggestionController> _logger;
        private readonly IConfiguration _config;

        public ExtractIntentWithSuggestionController(ILogger<ExtractIntentWithSuggestionController> logger,
            IConfiguration config)
        {
            _logger = logger;
            _config = config;
        }

        public async Task<ExtractIntentFromInputFunction.Output> Get(string input, CancellationToken token)
        {
            var kernel = new SemanticKernelWrapperFactory().GetFromSettings(
                _config["OPENAI_ENDPOINT"],
                _config["OPENAI_KEY"],
                _config["OPENAI_MODEL"]
            );

            var intents = new[] { "Accounts", "Information", "Payments", "Insurance" };

            var context = "You are a bank teller";

            var result = await
                new ExtractIntentFromInputFunction.Function().ThenIf(
                        r => !r.FoundIntent,
                        () => new GetMoreInputFromCustomerToDetectIntentInputFunction())
                    .Execute(kernel, new ExtractIntentFromInputFunction.Input(context, intents, input), token);

            return result;
        }
    }
}