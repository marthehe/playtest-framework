namespace PlayTest.Core.TestData;

using Demo.PlayPlatform.Sessions;

public class SessionBuilder
{
    private Guid _id = Guid.NewGuid();
    private Guid _playerId = Guid.NewGuid();
    private string _gameTitle = "Test Game";
    private DateTime _startedAt = DateTime.UtcNow;
    private DateTime? _endedAt;
    private SessionStatus _status = SessionStatus.Active;

    public SessionBuilder WithId(Guid id) { _id = id; return this; }
    public SessionBuilder WithPlayerId(Guid id) { _playerId = id; return this; }
    public SessionBuilder WithGameTitle(string title) { _gameTitle = title; return this; }
    public SessionBuilder WithStartedAt(DateTime dt) { _startedAt = dt; return this; }
    public SessionBuilder Completed(DateTime? endedAt = null)
    {
        _endedAt = endedAt ?? _startedAt.AddHours(2);
        _status = SessionStatus.Completed;
        return this;
    }
    public SessionBuilder TimedOut()
    {
        _endedAt = _startedAt.AddHours(8);
        _status = SessionStatus.TimedOut;
        return this;
    }

    public GameSession Build() => new(_id, _playerId, _gameTitle, _startedAt, _endedAt, _status);

    public static implicit operator GameSession(SessionBuilder b) => b.Build();
}
