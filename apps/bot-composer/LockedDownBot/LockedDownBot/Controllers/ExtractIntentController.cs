using System.Threading;
using System.Threading.Tasks;
using LockedDownBotSemanticKernel.Primitives;
using LockedDownBotSemanticKernel.Primitives.Chains;
using LockedDownBotSemanticKernel.Skills.Foundational.ChitChat;
using LockedDownBotSemanticKernel.Skills.Foundational.ExtractKeyTerms;
using LockedDownBotSemanticKernel.Skills.Foundational.ResponseToUserSuggestion;
using LockedDownBotSemanticKernel.Skills.Foundational.SummariseAsk;
using LockedDownBotSemanticKernel.Skills.Intent.DetectIntent;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.Skills.Core;

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

        public async Task<ExtractIntentFromInputFunction.Output> Get(string input, CancellationToken token)
        {
            var kernel = new SkillWrapperFactory().GetFromSettings(
                _config["OPENAI_ENDPOINT"],
                _config["OPENAI_KEY"],
                _config["OPENAI_MANAGED_IDENTITY_CLIENT_ID"],
                _config["OPENAI_MODEL"]);

            var intents = new[] { "Accounts", "Information", "Payments", "Insurance" };

            var context = "You are a bank teller";

            // var result = await
            //     new ExtractIntentFromInputFunction.Function()
            //         .Execute(kernel, new ExtractIntentFromInputFunction.Input(context, intents, input), token);

            var result = await
                new ExtractIntentFromInputFunction.Function()
                    .ThenEither(x => x.Intent == "Account",
                        sr => sr.Resolve<ExtractKeyTermsFunction.Function>(),
                        (i1,o1) => new ExtractKeyTermsFunction.Input("FOO", "BAR"),
                        sr => sr.Resolve<SummariseAskFunction.Function>(),
                        (i1,o1) => new SummariseAskFunction.Input("FOO", "BAR")
                    )
                    .Combine(
                        (i, output) => new ExtractIntentFromInputFunction.Input(context, intents, input),
                        (i, output) => new ExtractIntentFromInputFunction.Input(context, intents, input),
                        r => r.Resolve<ExtractIntentFromInputFunction.Function>()
                        )
                    .Run(
                        kernel, 
                        new ExtractIntentFromInputFunction.Input(context, intents, input), 
                        token)
                ;
            
            return result;
        }
    }
}