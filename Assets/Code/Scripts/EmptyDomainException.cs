using System;

/// <summary>
/// Exception to represent a domain wipeout that can occur when pruning unsupported values during propagation.
/// </summary>
public class EmptyDomainException : Exception
{
    public EmptyDomainException() { }

    public EmptyDomainException(string message = "Domain Wipeout!") : base(message) { }

    public EmptyDomainException(string message, Exception inner) : base(message, inner) { }
}