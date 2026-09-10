namespace PlayTest.Core.TestData;

using Demo.PlayPlatform.Players;

public class PlayerBuilder
{
    private Guid _id = Guid.NewGuid();
    private string _username = $"player_{Guid.NewGuid():N}"[..20];
    private string _displayName = "Test Player";
    private DateTime _createdAt = DateTime.UtcNow;
    private PlayerStatus _status = PlayerStatus.Active;

    public PlayerBuilder WithId(Guid id) { _id = id; return this; }
    public PlayerBuilder WithUsername(string username) { _username = username; return this; }
    public PlayerBuilder WithDisplayName(string name) { _displayName = name; return this; }
    public PlayerBuilder WithCreatedAt(DateTime dt) { _createdAt = dt; return this; }
    public PlayerBuilder WithStatus(PlayerStatus status) { _status = status; return this; }
    public PlayerBuilder Suspended() => WithStatus(PlayerStatus.Suspended);
    public PlayerBuilder Banned() => WithStatus(PlayerStatus.Banned);

    public Player Build() => new(_id, _username, _displayName, _createdAt, _status);

    public static implicit operator Player(PlayerBuilder b) => b.Build();
}
