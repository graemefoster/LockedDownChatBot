using Azure.AI.OpenAI;
using OpenAiSimplePipeline.OpenAI.Chains;

namespace OpenAiSimplePipeline.OpenAI;

public interface IOpenAiClient
{
    Task<TResult> Execute<TResult>(IChainableCall<TResult> chain, CancellationToken cancellationToken);

    Task<string> GetChatCompletionsAsync(
        ChatCompletionsOptions chatCompletionsOptions,
        CancellationToken cancellationToken = default);

    Task<string> CreativeOpenAiCall(
        string systemPrompt,
        string userInput,
        CancellationToken cancellationToken);

    Task<string> PredictableOpenAiCall(
        string systemPrompt,
        string userInput,
        CancellationToken cancellationToken);

    /// <summary>
    /// Uses same parameters as Open AI Studio in the portal 
    /// </summary>
    /// <param name="model"></param>
    /// <param name="systemPrompt"></param>
    /// <param name="userInput"></param>
    /// <param name="cancellationToken"></param>
    /// <param name="temperature"></param>
    /// <param name="presencePenalty"></param>
    /// <param name="topP"></param>
    /// <param name="frequencyPenalty"></param>
    /// <returns></returns>
    Task<string> DefaultOpenAiCall(
        string systemPrompt,
        string userInput,
        CancellationToken cancellationToken,
        float? temperature = 0.7f,
        float? presencePenalty = 0f,
        float? topP = 0.95f,
        float? frequencyPenalty = 0f);

    /// <summary>
    /// The LLM seems to produce better results with pretty printed JSON 
    /// </summary>
    /// <param name="json"></param>
    /// <returns></returns>
    string FormatJson(string json);
}
