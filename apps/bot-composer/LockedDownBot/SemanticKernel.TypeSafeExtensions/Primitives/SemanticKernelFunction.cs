using System.Reflection;
using LockedDownBotSemanticKernel.Primitives.Chains;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Orchestration;
using Microsoft.SemanticKernel.SemanticFunctions;
using Microsoft.SemanticKernel.SkillDefinition;
using Newtonsoft.Json;

namespace LockedDownBotSemanticKernel.Primitives;

public abstract class SemanticKernelFunction<TInput, TOutput> : IChainableSkill<TInput, TOutput>, IAmAnSkFunction
    where TInput : notnull
    where TOutput : notnull
{
    public ISKFunction Register(IKernel kernel)
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

        var promptTemplate = new PromptTemplate(Prompt, promptTemplateConfig, kernel);
        var functionConfig = new SemanticFunctionConfig(promptTemplateConfig, promptTemplate);
        return kernel.RegisterSemanticFunction("Intents", "ExtractMoreInformationToDetectIntent", functionConfig);
    }
    
    public abstract string Prompt { get;}

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

    public async Task<TOutput> Run(SemanticKernelWrapper wrapper, TInput input, CancellationToken token)
    {
        var context = wrapper.CreateNewContext(token);
        PopulateContext(context, input);
        var skFunction = wrapper.GetOrRegister(this);
        var kernelResult = await skFunction.InvokeAsync(context);
        if (kernelResult.ErrorOccurred)
        {
            throw new InvalidOperationException(kernelResult.LastException!.ToString());
        }
        return FromResult(input, kernelResult);
    }
}