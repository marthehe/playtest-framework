namespace PlayTest.Core.TestData;

using Demo.PlayPlatform.Achievements;

public class AchievementBuilder
{
    private Guid _id = Guid.NewGuid();
    private string _key = "first_win";
    private string _title = "First Victory";
    private string _description = "Win your first game";
    private int _points = 10;

    public AchievementBuilder WithId(Guid id) { _id = id; return this; }
    public AchievementBuilder WithKey(string key) { _key = key; return this; }
    public AchievementBuilder WithTitle(string title) { _title = title; return this; }
    public AchievementBuilder WithDescription(string desc) { _description = desc; return this; }
    public AchievementBuilder WithPoints(int pts) { _points = pts; return this; }

    public Achievement Build() => new(_id, _key, _title, _description, _points);

    public static implicit operator Achievement(AchievementBuilder b) => b.Build();
}
