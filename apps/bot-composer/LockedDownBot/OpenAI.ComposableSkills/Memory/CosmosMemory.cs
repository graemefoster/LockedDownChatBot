using System.Net;
using Microsoft.Azure.Cosmos;

namespace LockedDownBotSemanticKernel.Memory;

public class CosmosMemory
{
    private readonly CosmosClient _client;

    public CosmosMemory(CosmosClient client)
    {
        _client = client;
    }

    public async Task<Conversation> GetConversation(string conversationId, CancellationToken token)
    {
        var container = _client.GetContainer("bot-db", "conversationHistory");
        try
        {
            var chatResponse = await container.ReadItemAsync<Conversation>(
                conversationId,
                new PartitionKey(conversationId),
                new ItemRequestOptions(),
                token);
            return chatResponse;
        }
        catch (CosmosException e) when (e.StatusCode == HttpStatusCode.NotFound)
        {
            return new Conversation()
            {
                Chat = new List<Chat>(), Id = conversationId, StartDate = DateTimeOffset.Now,
                LastUpdateDate = DateTimeOffset.Now
            };
        }
    }


    public async Task SaveConversation(Conversation conversation, CancellationToken token)
    {
        var container = _client.GetContainer("bot-db", "conversationHistory");
        await container.UpsertItemAsync(conversation, new PartitionKey(conversation.Id), null, token);
    }
}