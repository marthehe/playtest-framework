namespace Demo.PlayPlatform.Players;

/// <summary>
/// Represents a registered player on the platform.
/// </summary>
public record Player(
    Guid Id,
    string Username,
    string DisplayName,
    DateTime CreatedAt,
    PlayerStatus Status)
{
    /// <summary>
    /// Creates a new player with Active status and a generated identifier.
    /// </summary>
    public static Player Create(string username, string displayName)
        => new(Guid.NewGuid(), username, displayName, DateTime.UtcNow, PlayerStatus.Active);
}

/// <summary>
/// Lifecycle states for a player account.
/// </summary>
public enum PlayerStatus
{
    Active,
    Suspended,
    Banned,
    Deactivated
}
