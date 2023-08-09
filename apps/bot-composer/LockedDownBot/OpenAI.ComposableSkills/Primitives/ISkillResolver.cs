namespace LockedDownBotSemanticKernel.Primitives;

public interface ISkillResolver
{
    T Resolve<T>() where T:new();
}