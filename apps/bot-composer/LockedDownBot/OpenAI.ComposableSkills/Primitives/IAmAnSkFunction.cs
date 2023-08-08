using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.SkillDefinition;

namespace LockedDownBotSemanticKernel.Primitives;

public interface IAmAnSkFunction<TInput>
{
    string Prompt(TInput input);
}