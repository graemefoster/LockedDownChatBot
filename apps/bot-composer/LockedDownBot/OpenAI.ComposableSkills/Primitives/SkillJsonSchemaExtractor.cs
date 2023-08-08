using LockedDownBotSemanticKernel.Primitives.Chains;
using LockedDownBotSemanticKernel.Skills.Functions.FunctionCalling;
using Newtonsoft.Json.Schema;

namespace LockedDownBotSemanticKernel.Primitives;

public class SkillJsonSchemaExtractor
{
    public static void Test()
    {
        var schema = new SkillJsonSchemaExtractor().GenerateFor(new ExtractInformationToCallFunction.Function());
    }
    
    public JsonSchema GenerateFor<TIn, TOut>(IChainableSkill<TIn, TOut> fn)
    {
        var schemaGen = new JsonSchemaGenerator();
        var schema = schemaGen.Generate(typeof(TIn));
        return schema;
    }
}