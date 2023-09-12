using GameEventEngine.Triggers;

namespace GameEventEngine.Actions;

public sealed class GameAction
{
    private IEnumerator<ActionState>? _routine;
    private readonly bool _interruptible;

    public GameAction(IEnumerator<ActionState> routine, string name, GameAction? parent = null, bool interruptible = true)
    {
        _routine = routine ?? throw new ArgumentNullException(nameof(routine));
        _interruptible = interruptible;
        Parent = parent;
        Name = name;
        SubActions = new Queue<GameAction>();
        Level = (parent?.Level ?? -1) + 1;
        _abortedSubActions = new List<string>();
    }

    public GameAction(IEnumerator<ActionState> routine, GameEvent gameEvent, GameAction? parent = null, bool interruptible = true) : this(routine, gameEvent.Name, parent, interruptible)
    {
        GameEvent = gameEvent ?? throw new ArgumentNullException(nameof(gameEvent));
    }

    public GameAction? Parent { get; }

    public GameEvent? GameEvent { get; }

    public string Name { get; }

    internal readonly Queue<GameAction> SubActions;

    private readonly List<string> _abortedSubActions;

    public List<string> AbortedSubActions => new(_abortedSubActions);

    internal int Level { get; }

    internal bool MoveNext()
    {
        if (_routine != null)
        {
            var canMoveNext = _routine.MoveNext();
            if (!canMoveNext)
            {
                _routine = null;
                IsCompleted = true;
            }
            return canMoveNext;
        }

        return false;
    }

    internal bool IsCompleted { get; private set; }

    internal void Abort()
    {
        if (!_interruptible) { return; }
        _routine = null;
        IsCompleted = true;
        SubActions.Clear();
        Parent?._abortedSubActions.Add(Name);
    }

    internal ActionState? Current => _routine?.Current;

    public override string ToString()
    {
        var name = Name;
        var parent = Parent;
        while (parent != null)
        {
            name = $"{parent.Name}->{name}";
            parent = parent.Parent;
        }
        return name;
    }
}