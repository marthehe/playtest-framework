namespace Demo.PlayPlatform.Exceptions;

/// <summary>
/// Thrown when a requested entity cannot be found.
/// </summary>
public class EntityNotFoundException : Exception
{
    public string EntityType { get; }
    public string Identifier { get; }

    public EntityNotFoundException(string entityType, string identifier)
        : base($"{entityType} with identifier '{identifier}' was not found.")
    {
        EntityType = entityType;
        Identifier = identifier;
    }

    public EntityNotFoundException(string entityType, Guid id)
        : this(entityType, id.ToString())
    {
    }
}

/// <summary>
/// Thrown when a domain rule or business invariant is violated.
/// </summary>
public class DomainRuleException : Exception
{
    public string Rule { get; }

    public DomainRuleException(string rule, string message)
        : base(message)
    {
        Rule = rule;
    }
}

/// <summary>
/// Thrown when an entity already exists and duplicates are not allowed.
/// </summary>
public class DuplicateEntityException : DomainRuleException
{
    public DuplicateEntityException(string entityType, string identifier)
        : base("NoDuplicates", $"{entityType} with identifier '{identifier}' already exists.")
    {
    }
}
