using System.Text;
using Newtonsoft.Json;

namespace LockedDownBotSemanticKernel.Memory;

public class Conversation
{
    [JsonProperty("id")] public string Id { get; set; }
    public DateTimeOffset StartDate { get; set; }
    public DateTimeOffset LastUpdateDate { get; set; }
    public List<Chat> Chat { get; set; }

    public Conversation UpdateConversationWithSystemResponse(string input)
    {
        UpdateConversation("System", input);
        return this;
    }

    public Conversation UpdateConversationWithUserInput(string input)
    {
        UpdateConversation("User", input);
        return this;
    }

    public override string ToString()
    {
        var sb = new StringBuilder();
        foreach (var chat in Chat)
        {
            sb.AppendLine($"{chat.Actor}: {chat.Message}");
        }

        return sb.ToString();
    }

    private void UpdateConversation(string actor, string response)
    {
        Chat.Add(new Chat()
        {
            Actor = actor,
            Message = response,
            Timestamp = DateTimeOffset.Now
        });

        if (Chat.Count > 10)
        {
            Chat = Chat.Skip(Chat.Count - 10).ToList();
        }

        LastUpdateDate = DateTimeOffset.Now;
    }
}