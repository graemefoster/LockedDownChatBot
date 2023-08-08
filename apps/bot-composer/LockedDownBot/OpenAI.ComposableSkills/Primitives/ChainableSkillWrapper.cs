﻿using Azure.AI.OpenAI;
using LockedDownBotSemanticKernel.Primitives.Chains;
using Microsoft.SemanticKernel.SkillDefinition;

namespace LockedDownBotSemanticKernel.Primitives;

public class ChainableSkillWrapper : ISkillResolver
{
    private readonly OpenAIClient _openAiClient;
    private readonly string _modelName;

    public ChainableSkillWrapper(OpenAIClient openAiClient, string modelName)
    {
        _openAiClient = openAiClient;
        _modelName = modelName;
    }
    
    public Task<TOutput> RunSkill<TInput, TOutput>(IChainableSkill<TInput, TOutput> chain, TInput input,
        CancellationToken cancellationToken)
    {
        return chain.Run(this, input, cancellationToken);
    }
    /// <summary>
    /// TODO Bring in proper container
    /// </summary>
    public T Resolve<T>() where T : new()
    {
        return new T();
    }

    public async Task<string> RunRaw(string prompt)
    {
        var output = await _openAiClient.GetChatCompletionsAsync(_modelName, new ChatCompletionsOptions()
        {
            Messages = { new ChatMessage(ChatRole.System, prompt) },
            Temperature = 0,
            FrequencyPenalty = 0,
            MaxTokens = 100,
            PresencePenalty = 0,
            NucleusSamplingFactor = 0
        });
        return output.Value.Choices[0].Message.Content;
    }
}