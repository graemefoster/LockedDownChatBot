using Azure;
using Azure.AI.OpenAI;
using Azure.Identity;
using LockedDownBotSemanticKernel.Memory;
using Microsoft.Azure.Cosmos;

namespace LockedDownBotSemanticKernel.Primitives;

public class SkillWrapperFactory
{
    private ChainableSkillWrapper? _client;

    public ChainableSkillWrapper GetFromSettings(IDictionary<string, object> config)
    {
        var endpoint = (string)config["OPENAI_ENDPOINT"];
        var model = (string)config["OPENAI_MODEL"];
        config.TryGetValue("OPENAI_KEY", out var openAiKey);
        var clientId = config["OPENAI_MANAGED_IDENTITY_CLIENT_ID"] as string;
        return GetFromSettings(endpoint, openAiKey, clientId, model);
    }

    public CosmosMemory GetMemoryFromSettings(IDictionary<string, object> config)
    {
        var endpoint = (string)config["BOT_MEMORY_HOST"];
        var gotKey = config.TryGetValue("BOT_MEMORY_KEY", out var memoryKey);
        var clientId = config["OPENAI_MANAGED_IDENTITY_CLIENT_ID"] as string;
        return gotKey
            ? new CosmosMemory(new CosmosClient(endpoint, (string)memoryKey!))
            : new CosmosMemory(new CosmosClient(endpoint, new ManagedIdentityCredential(clientId)));
    }

    public ChainableSkillWrapper GetFromSettings(string endpoint, object? key, string? clientId, string model)
    {
        if (_client == null)
        {
            _client = new ChainableSkillWrapper(GetRawClientFromSettings(endpoint, key, clientId), model);
        }

        return _client;
    }

    public OpenAIClient GetRawClientFromSettings(IDictionary<string, object> config, out string model,
        out string embeddingModel)
    {
        var endpoint = (string)config["OPENAI_ENDPOINT"];
        config.TryGetValue("OPENAI_KEY", out var openAiKey);
        var clientId = config["OPENAI_MANAGED_IDENTITY_CLIENT_ID"] as string;
        model = (string)config["OPENAI_MODEL"];
        embeddingModel = (string)config["OPENAI_EMBEDDING_MODEL"];

        return GetRawClientFromSettings(endpoint, openAiKey, clientId);
    }

    private OpenAIClient GetRawClientFromSettings(string endpoint, object? openAiKey, string? clientId)
    {
        var gotKey = openAiKey != null;
        var useManagedIdentity = !gotKey;
        return useManagedIdentity
            ? new OpenAIClient(new Uri(endpoint), new ManagedIdentityCredential(clientId))
            : new OpenAIClient(new Uri(endpoint), new AzureKeyCredential((string)openAiKey!));
    }
}