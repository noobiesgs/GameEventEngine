using GameEventEngine.Games;

namespace GameEventEngine.Triggers;

public class GameEventArgs
{
    public GameEventArgs(Game game)
    {
        Game = game;
        Targets = new List<Player>();

    }

    public Game Game { get; }

    public List<Player> Targets { get; set; }

    public Player? Source { get; set; }


    public void CopyFrom(GameEventArgs another)
    {
        Source = another.Source;
        Targets = new List<Player>(another.Targets);
    }
}

public class DamageEventArgs : GameEventArgs
{
    public DamageEventArgs(Game game) : base(game)
    {
    }

    public int Magnitude { get; set; }
}

public class HealthChangedEventArgs : GameEventArgs
{
    public HealthChangedEventArgs(Game game) : base(game)
    {
    }

    public HealthChangedEventArgs(Game game, DamageEventArgs args) : base(game)
    {
        CopyFrom(args);
        Delta = -args.Magnitude;
    }

    public int Delta { get; set; }
}