using Cysharp.Threading.Tasks;
using GameEventEngine.Actions;
using GameEventEngine.Triggers;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;

namespace GameEventEngine.Games;

public class Game : ISingletonDependency
{
    public Game(GameActionScheduler actionScheduler, ILogger<Game> logger)
    {
        ActionScheduler = actionScheduler;
        Logger = logger;
        _triggers = new Dictionary<GameEvent, List<Trigger>>();
        Players = Enumerable.Range(0, 4).Select(_ => new Player()).ToList();
    }

    public GameActionScheduler ActionScheduler { get; }

    public List<Player> Players { get; }

    public ILogger<Game> Logger { get; }

    private readonly Dictionary<GameEvent, List<Trigger>> _triggers;

    public void RegisterTrigger(GameEvent gameEvent, Trigger trigger)
    {
        if (!_triggers.ContainsKey(gameEvent))
        {
            _triggers[gameEvent] = new List<Trigger>();
        }
        _triggers[gameEvent].Add(trigger);
    }

    public ActionState Emit(GameEvent gameEvent, GameEventArgs args)
    {
        if (!_triggers.ContainsKey(gameEvent)) return ActionState.Commit;

        var triggers = new List<Trigger>(_triggers[gameEvent]);
        if (triggers.Count == 0) return ActionState.Commit;

        foreach (var t in triggers)
        {
            ActionScheduler.Schedule(t.Run(gameEvent, args), gameEvent);
        }

        return ActionState.Commit;
    }

    public ActionState DoDamage(Player source, Player target, int magnitude)
    {
        IEnumerator<ActionState> DoDamageCoroutine(Player s, Player t, int m)
        {
            var args = new DamageEventArgs(this)
            {
                Magnitude = m,
                Source = s
            };
            args.Targets.Add(t);

            ActionScheduler.Schedule(DoDamagePreparation(args), nameof(DoDamagePreparation));
            yield return ActionState.Commit;

            if (ActionScheduler.CurrentAction!.AbortedSubActions.Contains(nameof(DoDamagePreparation)))
            {
                yield return Emit(GameEvent.DamageComputingFinished, args);
                yield break;
            }

            args.Targets[0].Health -= args.Magnitude;
            args.Game.Logger.LogInformation("Player Inflicted: {magnitude}", args.Magnitude);

            yield return Emit(GameEvent.AfterHealthChanged, args);
            yield return Emit(GameEvent.AfterDamageCaused, args);
            yield return Emit(GameEvent.AfterDamageInflicted, args);
            yield return Emit(GameEvent.DamageComputingFinished, args);
        }

        IEnumerator<ActionState> DoDamagePreparation(DamageEventArgs args)
        {
            yield return Emit(GameEvent.DamageSourceConfirmed, args);
            yield return Emit(GameEvent.DamageElementConfirmed, args);

            yield return Emit(GameEvent.BeforeDamageComputing, args);
            yield return Emit(GameEvent.DamageComputingStarted, args);
            yield return Emit(GameEvent.DamageCaused, args);
            yield return Emit(GameEvent.DamageInflicted, args);

            if (args.Magnitude == 0)
            {
                yield return ActionState.AbortParent;
                yield break;
            }

            var healthChangedArgs = new HealthChangedEventArgs(args.Game, args);
            yield return Emit(GameEvent.BeforeHealthChanged, healthChangedArgs);
            args.Magnitude = -healthChangedArgs.Delta;
        }

        ActionScheduler.Schedule(DoDamageCoroutine(source, target, magnitude), nameof(DoDamage));

        return ActionState.Commit;
    }

    public ActionState LoseHealth(Player source, int magnitude)
    {
        var healthChangedArgs = new HealthChangedEventArgs(this) { Delta = -magnitude, Source = null };
        healthChangedArgs.Targets.Add(source);

        IEnumerator<ActionState> LoseHealthCoroutine(HealthChangedEventArgs args)
        {
            yield return Emit(GameEvent.BeforeHealthChanged, args);
            args.Targets[0].Health += args.Delta;
            args.Game.Logger.LogInformation("Player lose hp: {magnitude}", -args.Delta);
            yield return Emit(GameEvent.AfterHealthChanged, args);
        }

        ActionScheduler.Schedule(LoseHealthCoroutine(healthChangedArgs), nameof(LoseHealth));
        return ActionState.Commit;
    }

    public void InitTriggers()
    {
        RegisterTrigger(GameEvent.DoPlayer, new DoPlayerTrigger());
        RegisterTrigger(GameEvent.DamageInflicted, new ReduceDamageToZeroTrigger());
        //RegisterTrigger(GameEvent.DamageInflicted, new PreventDamageTrigger());
        RegisterTrigger(GameEvent.DamageComputingFinished, new ConfirmDamageComputingFinishedTrigger());

        //RegisterTrigger(GameEvent.BeforeDamageComputing, new JueQingSkillTrigger());
    }

    public UniTask RunAsync()
    {
        return ActionScheduler.RunAsync(new RoleGameRuleTrigger(), GameEvent.GameStart, new GameEventArgs(this));
    }

    private class RoleGameRuleTrigger : Trigger
    {
        public override IEnumerator<ActionState> Run(GameEvent gameEvent, GameEventArgs args)
        {
            for (int i = 0; i < 4; i++)
            {
                args.Game.Logger.LogInformation("Simulate deal: {i}", i);
                yield return ActionState.Commit;
            }

            args.Game.Logger.LogInformation("Start DoPlayer");

            yield return args.Game.Emit(GameEvent.DoPlayer, new GameEventArgs(args.Game));

            args.Game.Logger.LogInformation("Game over");
        }
    }

    private class DoPlayerTrigger : Trigger
    {
        public override IEnumerator<ActionState> Run(GameEvent gameEvent, GameEventArgs args)
        {
            var round = 0;

            while (round < 10)
            {
                round++;

                args.Game.Logger.LogInformation("Simulate player round: {round}", round);
                yield return ActionState.Commit;
            }

            args.Game.Logger.LogInformation("Simulate DoDamage to player");

            yield return args.Game.DoDamage(args.Game.Players[0], args.Game.Players[1], 2);

            args.Game.Logger.LogInformation("Player1 Health: {health}", args.Game.Players[1].Health);
        }
    }

    private class ReduceDamageToZeroTrigger : Trigger
    {
        public override IEnumerator<ActionState> Run(GameEvent gameEvent, GameEventArgs gameEventArgs)
        {
            var damageArgs = (DamageEventArgs)gameEventArgs;

            damageArgs.Magnitude = 0;
            gameEventArgs.Game.Logger.LogInformation("Reduce Damage To Zero");
            yield return ActionState.Commit;
        }
    }

    private class ConfirmDamageComputingFinishedTrigger : Trigger
    {
        public override IEnumerator<ActionState> Run(GameEvent gameEvent, GameEventArgs gameEventArgs)
        {
            gameEventArgs.Game.Logger.LogInformation("Confirm Damage Computing Finished");
            yield return ActionState.Commit;
        }
    }

    private class PreventDamageTrigger : Trigger
    {
        public override IEnumerator<ActionState> Run(GameEvent gameEvent, GameEventArgs gameEventArgs)
        {
            gameEventArgs.Game.Logger.LogInformation("Prevent Damage");
            yield return ActionState.AbortParent;
        }
    }

    private class JueQingSkillTrigger : Trigger
    {
        public override IEnumerator<ActionState> Run(GameEvent gameEvent, GameEventArgs gameEventArgs)
        {
            gameEventArgs.Game.Logger.LogInformation("JueQing trigger");
            yield return ActionState.AbortParents(2);
            yield return gameEventArgs.Game.LoseHealth(gameEventArgs.Targets[0], ((DamageEventArgs)gameEventArgs).Magnitude);
        }
    }
}