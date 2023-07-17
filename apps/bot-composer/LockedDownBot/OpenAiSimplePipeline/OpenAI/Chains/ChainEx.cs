namespace OpenAiSimplePipeline.OpenAI.Chains;

public static class PromptEx
{
    public static IChainableCall<TResponse> NoOp<TResponse>(
        this TResponse output)
    {
        return new NoOpChainableCall<TResponse>(output);
    }

    public static IChainableCall<
            Either<
                IChainableCall<TFalseOutput>,
                IChainableCall<TTrueOutput>>>
        ThenIf<TOutput, TFalseOutput, TTrueOutput>(
            this IChainableCall<TOutput> prompt,
            Func<TOutput, bool> predicate,
            Func<TOutput, IChainableCall<TFalseOutput>> falsePrompt,
            Func<TOutput, IChainableCall<TTrueOutput>> truePrompt
        ) where TFalseOutput : class where TTrueOutput : class
    {
        return new EitherChainableCall<TOutput, TFalseOutput, TTrueOutput>(prompt, predicate, falsePrompt, truePrompt);
    }


    public static IChainableCall<TOutput> ThenIf<TOutput>(
        this IChainableCall<TOutput> prompt,
        Func<TOutput, bool> predicate,
        Func<TOutput, IChainableCall<TOutput>> truePrompt
    )
    {
        return new ChainableCall<TOutput>(prompt, predicate, truePrompt);
    }


    public static IChainableCall<TOutput> Then<TInput, TOutput>(
        this IChainableCall<TInput> prompt,
        Func<TInput, IChainableCall<TOutput>> truePrompt
    )
    {
        return new ChainableCallChangeOutput<TInput, TOutput>(prompt, truePrompt);
    }
}