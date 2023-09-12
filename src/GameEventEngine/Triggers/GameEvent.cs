using System.Data;

namespace GameEventEngine.Triggers;

public class GameEvent
{
    public GameEvent(string name)
    {
        Name = name;
        if (DefinedGameEventMap.ContainsKey(name))
        {
            throw new DuplicateNameException(nameof(name));
        }
    }

    public string Name { get; }

    static GameEvent()
    {
        DoPlayer = new GameEvent(nameof(DoPlayer));
        GameStart = new GameEvent(nameof(GameStart));

        DamageSourceConfirmed = new GameEvent(nameof(DamageSourceConfirmed));
        DamageElementConfirmed = new GameEvent(nameof(DamageElementConfirmed));
        BeforeDamageComputing = new GameEvent(nameof(BeforeDamageComputing));
        DamageComputingStarted = new GameEvent(nameof(DamageComputingStarted));
        DamageCaused = new GameEvent(nameof(DamageCaused));
        DamageInflicted = new GameEvent(nameof(DamageInflicted));
        BeforeHealthChanged = new GameEvent(nameof(BeforeHealthChanged));
        AfterHealthChanged = new GameEvent(nameof(AfterHealthChanged));
        AfterDamageCaused = new GameEvent(nameof(AfterDamageCaused));
        AfterDamageInflicted = new GameEvent(nameof(AfterDamageInflicted));
        DamageComputingFinished = new GameEvent(nameof(DamageComputingFinished));
    }

    private static readonly Dictionary<string, GameEvent> DefinedGameEventMap = new();

    public static List<GameEvent> DefinedGameEvents => new(DefinedGameEventMap.Values);

    public static readonly GameEvent DoPlayer;
    public static readonly GameEvent GameStart;

    public static readonly GameEvent DamageSourceConfirmed;
    public static readonly GameEvent DamageElementConfirmed;
    public static readonly GameEvent BeforeDamageComputing;
    public static readonly GameEvent DamageComputingStarted;
    public static readonly GameEvent DamageCaused;
    public static readonly GameEvent DamageInflicted;
    public static readonly GameEvent BeforeHealthChanged;
    public static readonly GameEvent AfterHealthChanged;
    public static readonly GameEvent AfterDamageCaused;
    public static readonly GameEvent AfterDamageInflicted;
    public static readonly GameEvent DamageComputingFinished;
}