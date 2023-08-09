using System.ComponentModel;
using LockedDownBotSemanticKernel.Memory;
using LockedDownBotSemanticKernel.Primitives;
using LockedDownBotSemanticKernel.Primitives.Chains;

namespace LockedDownBotSemanticKernel.Skills.Foundational.Memory;

public static class RecallMemoryFunction
{
    public enum MemoryType
    {
        AllConversation,
        LastTurn,
        Last10Turns
    }
    
    public record Input([property:Description("Type of memory to recall")]MemoryType Type);

    public record Output([property:Description("Memories")] Chat[] Memories);

    [Description("Fetches memories that can be used in skills")]
    public class Function : IChainableSkill<Input, Output>
    {
        private readonly CosmosMemory _cosmosMemory;
        private readonly string _memoryId;

        public Function(CosmosMemory cosmosMemory, string memoryId)
        {
            _cosmosMemory = cosmosMemory;
            _memoryId = memoryId;
        }
        public async Task<Output> Run(ChainableSkillWrapper wrapper, Input input, CancellationToken token)
        {
            var conversation = await _cosmosMemory.GetConversation(_memoryId, token);
            return input.Type switch 
            {
                MemoryType.AllConversation => new Output(conversation.Chat.ToArray()),
                MemoryType.LastTurn => new Output(new[] {conversation.Chat.Last()}),
                MemoryType.Last10Turns => new Output(conversation.Chat.TakeLast(10).ToArray()),
                _ => throw new ArgumentOutOfRangeException(nameof(input.Type))
            };
        }
    }
}