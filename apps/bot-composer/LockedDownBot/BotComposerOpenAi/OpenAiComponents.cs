using BotComposerOpenAi.EnterpriseVectorSearch;
using BotComposerOpenAi.SuggestFunctionCall;
using BotComposerOpenAi.TryToFindUserIntent;
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
        services.AddSingleton<SkillWrapperFactory>();
        services.AddSingleton<DeclarativeType>(_ => new DeclarativeType<EnterpriseVectorSearchActivity>(EnterpriseVectorSearchActivity.Kind));
        services.AddSingleton<DeclarativeType>(_ => new DeclarativeType<EnterpriseSearchActivity>(EnterpriseSearchActivity.Kind));
        services.AddSingleton<DeclarativeType>(_ => new DeclarativeType<OpenAiResponseWithSystemPrompt.OpenAiResponseWithSystemPrompt>(OpenAiResponseWithSystemPrompt.OpenAiResponseWithSystemPrompt.Kind));
        services.AddSingleton<DeclarativeType>(_ => new DeclarativeType<OpenAiDetectIntent>(OpenAiDetectIntent.Kind));
        services.AddSingleton<DeclarativeType>(_ => new DeclarativeType<OpenAiSuggestFunctionCall>(OpenAiSuggestFunctionCall.Kind));
        services.AddSingleton<DeclarativeType>(_ => new DeclarativeType<SummariseConversation.SummariseConversation>(SummariseConversation.SummariseConversation.Kind));
    }
}
