using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.SkillDefinition;

namespace LockedDownBotSemanticKernel.Primitives;

public interface IAmAnSkFunction
{
    ISKFunction Register(IKernel kernel);
    string Prompt { get; }
}