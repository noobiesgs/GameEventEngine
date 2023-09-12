using System.Diagnostics;
using Cysharp.Threading.Tasks;
using GameEventEngine.Triggers;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;

namespace GameEventEngine.Actions;

public class GameActionScheduler : ITransientDependency
{
    private const ushort MaximumActionLevel = 40960;

    private GameAction? _current;
    private readonly ILogger<GameActionScheduler> _log;

    public GameActionScheduler(ILogger<GameActionScheduler> log)
    {
        _log = log;
    }

    internal GameAction? CurrentAction
    {
        get => _current;
        set
        {
            if (_current == value) return;
            _log.LogInformation("Current Action: {currentAction}, Level: {level}", value?.ToString() ?? "NULL, ", value?.Level.ToString() ?? " -- ");
            _current = value;
        }
    }

    internal void Schedule(IEnumerator<ActionState> routine, string name, bool interruptible = true)
    {
        var action = new GameAction(routine, name, CurrentAction, interruptible);
        CurrentAction!.SubActions.Enqueue(action);
    }

    internal void Schedule(IEnumerator<ActionState> routine, GameEvent gameEvent, bool interruptible = true)
    {
        var action = new GameAction(routine, gameEvent, CurrentAction, interruptible);
        CurrentAction!.SubActions.Enqueue(action);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="rootTrigger"></param>
    /// <param name="gameEvent"></param>
    /// <param name="args"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public UniTask RunAsync(Trigger rootTrigger, GameEvent gameEvent, GameEventArgs args)
    {
        if (CurrentAction?.IsCompleted != false)
        {
            CurrentAction = new GameAction(rootTrigger.Run(gameEvent, args), gameEvent, null, false);
            return RunAsync();
        }

        throw new InvalidOperationException("There is currently an action being executed");
    }

    private async UniTask RunAsync()
    {
        do
        {
            Debug.Assert(CurrentAction != null);

            if (CurrentAction.Level > MaximumActionLevel)
            {
                CurrentAction.Abort();
                var parent = CurrentAction.Parent;
                while (parent != null)
                {
                    parent.Abort();
                    parent = parent.Parent;
                }
                break;
            }

            if (CurrentAction is { IsCompleted: false, SubActions.Count: > 0 })
            {
                CurrentAction = CurrentAction.SubActions.Dequeue();
                continue;
            }

            if (CurrentAction is { IsCompleted: true, Parent: { } })
            {
                CurrentAction = CurrentAction.Parent;
                continue;
            }

            var moveNext = CurrentAction.MoveNext();
            if (!moveNext)
            {
                var parent = FindFirstAvailableParent(CurrentAction);
                if (parent != null)
                {
                    CurrentAction = parent;
                }
                continue;
            }

            var state = CurrentAction.Current;
            if (state != null)
            {
                switch (state.Type)
                {
                    case ActionStateType.None:
                        break;
                    case ActionStateType.WaitTask:
                        await state.Task!.Value;
                        break;
                    case ActionStateType.AbortParents:
                        {
                            var parents = GetParents(CurrentAction);
                            foreach (var gameAction in parents.Where(gameAction => state.AbortParentsPredicate!(CurrentAction, gameAction)))
                            {
                                gameAction.Abort();
                            }
                            break;
                        }
                    case ActionStateType.AbortSiblings:
                        {
                            CurrentAction.Parent?.SubActions.Clear();
                            break;
                        }
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        } while (!CurrentAction.IsCompleted);

        CurrentAction = null;
    }

    private static IEnumerable<GameAction> GetParents(GameAction current)
    {
        var parent = current.Parent;
        while (parent != null)
        {
            yield return parent;
            parent = parent.Parent;
        }
    }

    private static GameAction? FindFirstAvailableParent(GameAction current)
    {
        var parent = current.Parent;
        while (parent != null)
        {
            if (!parent.IsCompleted)
            {
                break;
            }
            parent = parent.Parent;
        }
        return parent;
    }
}