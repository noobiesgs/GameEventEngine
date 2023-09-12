using Cysharp.Threading.Tasks;

namespace GameEventEngine.Actions;

public class ActionState
{
    internal ActionState(Func<GameAction, GameAction, bool> abortParentsPredicate)
    {
        Type = ActionStateType.AbortParents;
        AbortParentsPredicate = abortParentsPredicate;
    }

    internal ActionState(UniTask task)
    {
        Type = ActionStateType.WaitTask;
        Task = task;
    }

    internal ActionState(ActionStateType type)
    {
        Type = type;
    }

    public ActionStateType Type { get; }
    public Func<GameAction, GameAction, bool>? AbortParentsPredicate { get; }
    public UniTask? Task { get; }

    public static readonly ActionState Commit = new(ActionStateType.None);
    public static readonly ActionState AbortParent = AbortParents(1);
    public static readonly ActionState AbortSibling = new(ActionStateType.AbortSiblings);

    public static ActionState WaitTask(UniTask task)
    {
        return new ActionState(task);
    }

    public static ActionState AbortParents(int deep)
    {
        return new ActionState((current, parent) => current.Level - parent.Level <= deep);
    }
}

public enum ActionStateType : byte
{
    None,
    WaitTask,
    AbortParents,
    AbortSiblings
}