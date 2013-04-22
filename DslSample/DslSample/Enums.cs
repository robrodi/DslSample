namespace DslSample
{
    public enum Keywords
    {
        When,
        And,
        Or
    }

    public enum Condition
    {
        Is,
        Equal,
        Eq,
        NotEqual,
        Neq,
        Greater,
        GreaterThan,
        Gt,
        GreaterThanOrEqual,
        Gte,
        Less,
        LessThan,
        Lt,
        LessThanOrEqual,
        Lte

    }

    public enum EventCriteria
    {
        None,
        Id,
        Count
    }

    public enum InputCriteria
    {
        None,
        Mode,
        Variant,
    }

    public enum Operation
    {
        Add,
        Subtract
    }
    public enum Mode
    {
        Alpha = 1,
        Bravo,
        Charlie,
        Delta
    }

    public enum Variant
    {
        Echo,
        Foxtrot,
        Golf,
        Hotel
    }
}

