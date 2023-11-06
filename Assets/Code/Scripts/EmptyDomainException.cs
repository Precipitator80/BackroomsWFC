using System;

public class EmptyDomainException : Exception
{
    public EmptyDomainException()
    {
    }

    public EmptyDomainException(string message)
        : base(message)
    {
    }

    public EmptyDomainException(string message, Exception inner)
        : base(message, inner)
    {
    }
}