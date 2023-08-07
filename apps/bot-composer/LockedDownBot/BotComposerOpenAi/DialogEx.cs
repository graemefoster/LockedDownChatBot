using LockedDownBotSemanticKernel.Memory;
using Microsoft.Bot.Builder.Dialogs;

namespace BotComposerOpenAi;

public static class DialogEx
{
    public static async Task<Conversation> GetConversationForDialog(this DialogContext context, CosmosMemory memoryClient, CancellationToken token)
    {
        var dialogueState = (IDictionary<string, object>)context.State["dialog"];
        var conversationId = dialogueState.TryGetValue("__gptconversationId", out var value)
            ? (string)value
            : Guid.NewGuid().ToString();

        dialogueState["__gptconversationId"] = conversationId;
        
        return await memoryClient.GetConversation(conversationId, token);
    }
    
}