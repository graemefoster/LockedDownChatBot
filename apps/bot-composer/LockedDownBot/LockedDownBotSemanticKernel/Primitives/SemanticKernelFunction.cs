using System.Reflection;
using LockedDownBotSemanticKernel.Primitives.Chains;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Orchestration;
using Microsoft.SemanticKernel.SemanticFunctions;
using Microsoft.SemanticKernel.SkillDefinition;
using Newtonsoft.Json;

namespace LockedDownBotSemanticKernel.Primitives;

public abstract class SemanticKernelFunction<TInput, TOutput> : IChainableSkill<TInput, TOutput>
    where TInput : notnull
    where TOutput : notnull
{
    internal static ISKFunction? Register(IKernel kernel, string prompt)
    {
        var promptTemplateConfig = new PromptTemplateConfig()
        {
            Completion = new PromptTemplateConfig.CompletionConfig()
            {
                Temperature = 0,
                FrequencyPenalty = 0,
                MaxTokens = 100,
                PresencePenalty = 0,
                TopP = 0
            }
        };

        var promptTemplate = new PromptTemplate(prompt, promptTemplateConfig, kernel);
        var functionConfig = new SemanticFunctionConfig(promptTemplateConfig, promptTemplate);
        return kernel.RegisterSemanticFunction("Intents", "ExtractMoreInformationToDetectIntent", functionConfig);
    }

    protected virtual void PopulateContext(SKContext context, TInput input)
    {
        foreach (var prop in input.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (prop.PropertyType == typeof(string) || prop.PropertyType == typeof(int) ||
                prop.PropertyType == typeof(double) || prop.PropertyType == typeof(float))
            {
                context[prop.Name] = prop.GetValue(input)?.ToString() ?? string.Empty;
            }
            else
            {
                context[prop.Name] = JsonConvert.SerializeObject(prop.GetValue(input));
            }
        }
    }

    protected abstract TOutput FromResult(TInput input, SKContext context);

    public async Task<TOutput> Execute(SemanticKernelWrapper wrapper, TInput input, CancellationToken token)
    {
        var context = wrapper.CreateNewContext(token);
        PopulateContext(context, input);

        var skFunction = wrapper.Get(GetType());
        var kernelResult = await skFunction.InvokeAsync(context);
        return FromResult(input, kernelResult);
    }
}