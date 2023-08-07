using BotComposerOpenAi.EnterpriseVectorSearch;
using BotComposerOpenAi.OpenAiResponseWithSystemPrompt;
using BotComposerOpenAi.SuggestFunctionCall;
using BotComposerOpenAi.SummariseConversation;
using BotComposerOpenAi.TryToFindUserIntent;
using BotComposerOpenAi.UpdateGptCoversation;
using LockedDownBotSemanticKernel.Primitives;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs.Declarative;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BotComposerOpenAi;

/// <summary>
/// Definition of a <see cref="Microsoft.Bot.Builder.BotComponent"/> that allows registration of
/// services, custom actions, memory scopes and adapters.
/// </summary>
public class OpenAiComponents : BotComponent
{
    /// <summary>
    /// Entry point for bot components to register types in resource explorer, consume configuration and register services in the
    /// services collection.
    /// </summary>
    /// <param name="services">Services collection to register dependency injection.</param>
    /// <param name="configuration">Configuration for the bot component.</param>
    public override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // Anything that could be done in Startup.ConfigureServices can be done here.
        services.AddSingleton<SemanticKernelWrapperFactory>();
        services.AddSingleton<DeclarativeType>(_ => new DeclarativeType<EnterpriseVectorSearchActivity>(EnterpriseVectorSearchActivity.Kind));
        services.AddSingleton<DeclarativeType>(_ => new DeclarativeType<EnterpriseSearchActivity>(EnterpriseSearchActivity.Kind));
        services.AddSingleton<DeclarativeType>(_ => new DeclarativeType<OpenAiResponseWithSystemPromptActivity>(OpenAiResponseWithSystemPromptActivity.Kind));
        services.AddSingleton<DeclarativeType>(_ => new DeclarativeType<OpenAiDetectIntentActivity>(OpenAiDetectIntentActivity.Kind));
        services.AddSingleton<DeclarativeType>(_ => new DeclarativeType<OpenAiSuggestFunctionCallActivity>(OpenAiSuggestFunctionCallActivity.Kind));
        services.AddSingleton<DeclarativeType>(_ => new DeclarativeType<SummariseConversationActivity>(SummariseConversationActivity.Kind));
        services.AddSingleton<DeclarativeType>(_ => new DeclarativeType<UpdateGptConversationActivity>(UpdateGptConversationActivity.Kind));
    }
}
