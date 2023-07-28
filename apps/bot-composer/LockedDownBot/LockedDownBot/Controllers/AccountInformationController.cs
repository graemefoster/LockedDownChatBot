using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LockedDownBotSemanticKernel.Primitives;
using LockedDownBotSemanticKernel.Primitives.Chains;
using LockedDownBotSemanticKernel.Skills.Functions.FunctionCalling;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace LockedDownBot.Controllers
{
    /// <summary>
    /// A controller that handles skill replies to the bot.
    /// </summary>
    [ApiController]
    [Route("api/accountinformation")]
    public class AccountInformationController : Controller
    {
        private readonly ILogger<ExtractIntentController> _logger;
        private readonly IConfiguration _config;

        public AccountInformationController(ILogger<ExtractIntentController> logger, IConfiguration config)
        {
            _logger = logger;
            _config = config;
        }


        const string Function = """
{
    "name": "accounts",
    "description": "get some information on the customers bank account",
    "parameters": {
        "type": "object",
        "properties": {
            "accountNumber": {
                "type": "string",
                "description": "The bank account number"
            },
            "service": {
                "type": "string",
                "enum": [
                    "balance",
                    "details",
                    "payments",
                    "schedules",
                    "unknown"
                ],
                "description": "What the user wants to do"
            }
        }
    }
}
""";

        public async Task<ExtractInformationToCallFunction.Output> Get(string input, CancellationToken token)
        {
            var client = new SemanticKernelWrapperFactory().GetFromSettings(
                _config["OPENAI_ENDPOINT"],
                _config["OPENAI_KEY"],
                _config["OPENAI_MODEL"]);

            var result = await
                new ExtractInformationToCallFunction.Function()
                    .ThenIf(output => output.MissingParameters.Any(),
                        s => s.Resolve<GetMoreInputFromCustomerToCallInputFunction.Function>()
                    )
                    .Run(
                        client,
                        new ExtractInformationToCallFunction.Input(
                            "You are a bank teller",
                            input,
                            JsonConvert.DeserializeObject<ExtractInformationToCallFunction.JsonSchemaFunctionInput>(
                                Function)!),
                        token);

            return result;
        }
    }
}