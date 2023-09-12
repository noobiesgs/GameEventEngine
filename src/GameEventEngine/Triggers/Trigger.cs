using GameEventEngine.Actions;

namespace GameEventEngine.Triggers;

public class Trigger
{
    public virtual IEnumerator<ActionState> Run(GameEvent gameEvent, GameEventArgs gameEventArgs)
    {
        yield break;
    }
}