namespace LockedDownBotSemanticKernel.Primitives.Chains;

public static class ChainEx
{
    public static IChainableSkill<TInput, TResponse> NoOp<TInput, TResponse>(this TResponse output)
    {
        return new NoOpChainableCall<TInput, TResponse>(output);
    }

    public static IChainableSkill<TInput,
            Either<
                IChainableSkill<TFalseInput, TFalseOutput>,
                IChainableSkill<TTrueInput, TTrueOutput>>>
        ThenEither<TInput, TOutput, TFalseInput, TFalseOutput, TTrueInput, TTrueOutput>(
            this IChainableSkill<TInput, TOutput> prompt,
            Func<TOutput, bool> predicate,
            Func<ISkillResolver, IChainableSkill<TFalseInput, TFalseOutput>> falsePrompt,
            Func<ISkillResolver, IChainableSkill<TTrueInput, TTrueOutput>> truePrompt
        ) 
        where TFalseOutput : class
        where TTrueOutput : class

    {
        return new EitherChainableCall<TInput, TOutput, TFalseInput, TFalseOutput, TTrueInput, TTrueOutput>(
            prompt,
            predicate,
            falsePrompt,
            truePrompt);
    }

    public static IChainableSkill<TInput, TOutput> ThenIf<TInput, TOutput>(
        this IChainableSkill<TInput, TOutput> prompt,
        Func<TOutput, bool> predicate,
        Func<ISkillResolver, IChainableSkill<TOutput, TOutput>> truePrompt
    )
    {
        return new ChainableIfCall<TInput, TOutput, TOutput>(
            prompt,
            predicate,
            (r, s) => s,
            truePrompt);
    }

    public static IChainableSkill<TInput, TOutput> ThenIf<TInput, TOutput>(
        this IChainableSkill<TInput, TOutput> prompt,
        Func<TOutput, bool> predicate,
        Func<ISkillResolver, IChainableSkill<TInput, TOutput>> truePrompt
    )
    {
        return new ChainableIfCall<TInput, TInput, TOutput>(
            prompt,
            predicate,
            (r, s) => r,
            truePrompt);
    }

    public static IChainableSkill<TInput, TOutput> ThenIf<TInput, TInput2, TOutput>(
        this IChainableSkill<TInput, TOutput> prompt,
        Func<TOutput, bool> predicate,
        Func<TInput, TOutput, TInput2> inputFactory,
        Func<ISkillResolver, IChainableSkill<TInput2, TOutput>> truePrompt
    )
    {
        return new ChainableIfCall<TInput, TInput2, TOutput>(
            prompt,
            predicate,
            inputFactory,
            truePrompt);
    }

    public static IChainableSkill<TInput, TOutput2> Then<TInput, TOutput, TInput2, TOutput2>(
        this IChainableSkill<TInput, TOutput> startSkill,
        Func<TInput, TOutput, TInput2> inputFactory,
        Func<ISkillResolver, IChainableSkill<TInput2, TOutput2>> transform
    )
    {
        return new ChainableCall<TInput, TOutput, TInput2, TOutput2>(
            startSkill, inputFactory, transform);
    }
}