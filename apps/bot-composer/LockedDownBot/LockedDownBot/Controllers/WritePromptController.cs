using System;
using System.ComponentModel;
using System.Linq;
using LockedDownBotSemanticKernel.Primitives.Chains;
using LockedDownBotSemanticKernel.Skills.EnterpriseSearch;
using LockedDownBotSemanticKernel.Skills.Foundational.ExtractKeyTerms;
using LockedDownBotSemanticKernel.Skills.Foundational.GetEmbeddings;
using LockedDownBotSemanticKernel.Skills.Foundational.SummariseContent;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace LockedDownBot.Controllers;

[ApiController]
[Route("api/writeprompt")]
public class WritePromptController : Controller
{
    public string Get()
    {
        var skills = new Type[]
        {
            typeof(SummariseContentFunction.Function),
            typeof(CognitiveSearchVectorIndexFunction.Function),
            typeof(GetEmbeddingsFunction.Function),
            typeof(ExtractKeyTermsFunction.Function),
        };

        var skillsDefinition = from skill in skills
            let skillInterface = skill.GetInterfaces()
                .Single(x => x.IsConstructedGenericType && x.GetGenericTypeDefinition() == typeof(IChainableSkill<,>))
                .GenericTypeArguments
            let input = skillInterface[0]
            let output = skillInterface[1]
            select new
            {
                skillName = skill.FullName,
                description = ((DescriptionAttribute)TypeDescriptor.GetAttributes(skill)[typeof(DescriptionAttribute)]!)
                    .Description,
                input = new
                {
                    type = input.FullName,
                    parameters = TypeDescriptor.GetProperties(input).OfType<PropertyDescriptor>().Select(x => new
                    {
                        name = x.Name,
                        type = x.PropertyType.Name,
                        description = ((DescriptionAttribute)x.Attributes[typeof(DescriptionAttribute)])!.Description
                    })
                },
                output = new
                {
                    type = output.FullName,
                    parameters = TypeDescriptor.GetProperties(output).OfType<PropertyDescriptor>().Select(x => new
                    {
                        name = x.Name,
                        type = x.PropertyType.Name,
                        description = ((DescriptionAttribute)x.Attributes[typeof(DescriptionAttribute)])!.Description
                    })
                }
            };

        var userInput = new
        {
            parameters = new
            {
                Context = "You are a bank teller",
                UserInput = "What interest rate is on your transaction account?"
            }
        };

        var possiblePlan = new
        {
            skills = new[]
            {
                new
                {
                    skill = typeof(ExtractKeyTermsFunction.Function).FullName,
                    executionOrder = 1,
                    inputs = (object)new
                    {
                        UserInput = "inputs:UserInput",
                        Context = "inputs:Context",
                    },
                    outputs = new string[] { "1:outputs:KeyTermsString", "1:outputs:KeyTerms" }
                },
                // new
                // {
                //     skill = typeof(GetEmbeddingsFunction.Function).FullName,
                //     skillIndex = 2,
                //     inputs = (object)new
                //     {
                //         Content = "1:outputs:KeyTermsString"
                //     }
                // },
                // new
                // {
                //     skill = typeof(CognitiveSearchVectorIndexFunction.Function).FullName,
                //     skillIndex = 3,
                //     inputs = (object)new
                //     {
                //         Content = "2:outputs:SearchText",
                //         Embeddings = "2:outputs:SearchText",
                //     }
                // },
                // new
                // {
                //     skill = typeof(SummariseContentFunction.Function).FullName,
                //     skillIndex = 4,
                //     inputs = (object)new
                //     {
                //         Context = "1:inputs:Context",
                //         OriginalAsk = "1:inputs:Context",
                //         Content = "3:outputs:UserInput"
                //     }
                // },
            }
        };

        var prompt = """
Given a goal, and initial input, you must output a plan how to achieve the goal given possible skills.
The skills are defined as JSON and the plan must be VALID JSON.

All inputs to a SKILL must be populated.
All inputs in the plan MUST be either original INPUTS or OUTPUTS from the preceding skill.
All inputs into a SKILL that reference OUTPUTS must be the SAME type.

EXAMPLE
-------
SKILLS:

""" + JsonConvert.SerializeObject(skillsDefinition, Formatting.Indented) + """


INPUT:

""" + JsonConvert.SerializeObject(userInput, Formatting.Indented) + """


PLAN OUTPUT FORMAT:

""" + JsonConvert.SerializeObject(possiblePlan, Formatting.Indented) + """



GOAL
----
The user wants to know the interest rate on transaction accounts.
""";

        return prompt;
    }
}