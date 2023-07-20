namespace LockedDownBotSemanticKernel.Primitives.Chains;

public class Either<T1, T2> 
    where T1: class
    where T2: class
{
    public T1? ItemFalse { get; }
    public T2? ItemTrue { get; }

    public bool Result { get; }

    public Either(bool b, T1? itemFalse = null, T2? item = null)
    {
        Result = b;
        ItemFalse = itemFalse;
        ItemTrue = ItemTrue;
    }
    
    public static Either<T1, T2> True(T2 trueItem)
    {
        return new Either<T1, T2>(true, null, trueItem);
    }

    public static Either<T1, T2> False(T1 falseItem)
    {
        return new Either<T1, T2>(false, falseItem);
    }
}