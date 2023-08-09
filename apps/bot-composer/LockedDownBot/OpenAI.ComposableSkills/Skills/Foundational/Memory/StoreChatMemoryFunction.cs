using LockedDownBotSemanticKernel.Memory;
using LockedDownBotSemanticKernel.Primitives;
using LockedDownBotSemanticKernel.Primitives.Chains;

namespace LockedDownBotSemanticKernel.Skills.Foundational.Memory;

public static class StoreChatMemoryFunction
{
    public static IChainableSkill<TInput, TOutput> UpdateChatMemory<TInput, TOutput>(
        this IChainableSkill<TInput, TOutput> input, Func<TInput, TOutput, Chat> getNextChat,
        CosmosMemory memory, string memoryId)
    {
        return new StoreMemoryOpChainableCall<TInput, TOutput>(input, memory, memoryId, getNextChat);
    }


    class StoreMemoryOpChainableCall<TInput, TOutput> : IChainableSkill<TInput, TOutput>
    {
        private readonly IChainableSkill<TInput, TOutput> _inputSkill;
        private readonly CosmosMemory _memory;
        private readonly string _memoryId;
        private readonly Func<TInput, TOutput, Chat> _getNextChat;

        public StoreMemoryOpChainableCall(IChainableSkill<TInput, TOutput> inputSkill, CosmosMemory memory,
            string memoryId, Func<TInput, TOutput, Chat> getNextChat)
        {
            _inputSkill = inputSkill;
            _memory = memory;
            _memoryId = memoryId;
            _getNextChat = getNextChat;
        }

        public async Task<TOutput> Run(ChainableSkillWrapper client, TInput input, CancellationToken token)
        {
            var result = await client.RunSkill(_inputSkill, input, token);
            var conversation = await _memory.GetConversation(_memoryId, token);
            conversation.Chat.Add(_getNextChat(input, result));
            await _memory.SaveConversation(conversation, token);
            return result;
        }
    }
}