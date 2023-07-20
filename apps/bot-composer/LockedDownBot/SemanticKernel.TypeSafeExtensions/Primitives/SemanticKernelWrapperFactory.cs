using Microsoft.SemanticKernel;

namespace LockedDownBotSemanticKernel.Primitives;

public class SemanticKernelWrapperFactory
{
    private SemanticKernelWrapper? _client;

    public SemanticKernelWrapper GetFromSettings(IDictionary<string, object> config, out string model)
    {
        var endpoint = (string)config["OPENAI_ENDPOINT"];
        var key = (string)config["OPENAI_KEY"];
        model = (string)config["OPENAI_MODEL"];
        return GetFromSettings(endpoint, key, model);
    }

    public SemanticKernelWrapper GetFromSettings(string endpoint, string key, string model)
    {
        if (_client == null)
        {
            _client = new SemanticKernelWrapper(
                new KernelBuilder().WithAzureChatCompletionService(model, endpoint, key).Build());
            _client.ImportSkills(typeof(SemanticKernelWrapperFactory).Assembly);
        }

        return _client;
    }

}