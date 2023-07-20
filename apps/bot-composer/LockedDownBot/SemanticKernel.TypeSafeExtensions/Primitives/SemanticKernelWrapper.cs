using System.Reflection;
using LockedDownBotSemanticKernel.Primitives.Chains;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Orchestration;
using Microsoft.SemanticKernel.SkillDefinition;

namespace LockedDownBotSemanticKernel.Primitives;

public class SemanticKernelWrapper
{
    private readonly IKernel _kernel;
    private readonly Dictionary<Type, ISKFunction?> _register = new();

    public SemanticKernelWrapper(IKernel kernel)
    {
        _kernel = kernel;
    }

    public ISKFunction Get<T>()
    {
        return _register[typeof(T)]!;
    }

    public ISKFunction Get(Type t)
    {
        return _register[t]!;
    }

    public SKContext CreateNewContext(CancellationToken token)
    {
        return _kernel.CreateNewContext(token);
    }

    public void ImportSkills(params Assembly[] assemblies)
    {
        foreach (var assembly in assemblies)
        foreach (var type in assembly.GetTypes()
                     .Where(x => x is { IsInterface: false, IsAbstract: false })
                     .Where(x => x.GetMethod("Register", BindingFlags.Public | BindingFlags.Static, new [] { typeof(IKernel)}) != null))
        {
            var registrationMethod = type.GetMethod("Register", BindingFlags.Public | BindingFlags.Static, new [] { typeof(IKernel)});
            _register[type] = (ISKFunction)registrationMethod!.Invoke(null, new[] {_kernel});
        }
    }

    public Task<TOutput> Execute<TInput, TOutput>(IChainableSkill<TInput, TOutput> chain, TInput input, CancellationToken cancellationToken)
    {
        return chain.Execute(this, input, cancellationToken);
    }

}