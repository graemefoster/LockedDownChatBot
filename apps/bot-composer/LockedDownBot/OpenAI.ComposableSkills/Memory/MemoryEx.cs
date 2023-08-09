using System.Text;

namespace LockedDownBotSemanticKernel.Memory;

public static class MemoryEx
{
    public static string ChatsToString(this IEnumerable<Chat> chats)
    {
        var sb = new StringBuilder();
        foreach (var chat in chats)
        {
            sb.AppendLine($"{chat.Actor}: {chat.Message}");
        }

        return sb.ToString();
    }
}