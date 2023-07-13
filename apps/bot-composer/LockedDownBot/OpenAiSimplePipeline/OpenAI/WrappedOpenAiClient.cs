using Azure;
using Azure.AI.OpenAI;
using Newtonsoft.Json;
using OpenAiSimplePipeline.OpenAI.Chains;

namespace OpenAiSimplePipeline.OpenAI;

class WrappedOpenAiClient : IOpenAiClient
{
    private readonly string _model;
    private readonly OpenAIClient _client;

    public WrappedOpenAiClient(Uri uri, AzureKeyCredential azureKeyCredential, string model)
    {
        _model = model;
        _client = new OpenAIClient(uri, azureKeyCredential);
    }

    public async Task<TResult> Execute<TResult>(IChainableCall<TResult> chain, CancellationToken cancellationToken)
    {
        var result = await chain.Execute(this, cancellationToken);
        return result;
    }

    public async Task<string> GetChatCompletionsAsync(
        ChatCompletionsOptions chatCompletionsOptions,
        CancellationToken cancellationToken = default)
    {
        var response =
            await _client.GetChatCompletionsAsync(_model, chatCompletionsOptions, cancellationToken);
        return response.Value.Choices[0].Message.Content;
    }

    public Task<string> CreativeOpenAiCall(string systemPrompt, string userInput,
        CancellationToken cancellationToken)
    {
        return DefaultOpenAiCall(
            systemPrompt,
            userInput,
            cancellationToken,
            temperature: 0.7f,
            presencePenalty: 0f,
            topP: 0.95f,
            frequencyPenalty: 0f);
    }

    public Task<string> PredictableOpenAiCall(string systemPrompt, string userInput,
        CancellationToken cancellationToken)
    {
        return DefaultOpenAiCall(
            systemPrompt,
            userInput,
            cancellationToken,
            temperature: 0f,
            presencePenalty: 0f,
            topP: 0f,
            frequencyPenalty: 0f);
    }

    public Task<string> DefaultOpenAiCall(string systemPrompt, string userInput,
        CancellationToken cancellationToken, float? temperature = 1f, float? presencePenalty = 0f, float? topP = 0f,
        float? frequencyPenalty = 0f)
    {
        return GetChatCompletionsAsync(
            new ChatCompletionsOptions()
            {
                Messages =
                {
                    new ChatMessage(ChatRole.System, systemPrompt.Replace("\\n", "\n").ReplaceLineEndings("\n")),
                    new ChatMessage(ChatRole.User, userInput)
                },
                Temperature = temperature,
                PresencePenalty = presencePenalty,
                FrequencyPenalty = frequencyPenalty,
                MaxTokens = 800,
                NucleusSamplingFactor = topP,
                ChoicesPerPrompt = 1
            }, cancellationToken);
    }

    public string FormatJson(string json)
    {
        return JsonConvert.SerializeObject(
            JsonConvert.DeserializeObject(json),
            new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
            });
    }

}