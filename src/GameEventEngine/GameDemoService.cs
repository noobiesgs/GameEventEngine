using GameEventEngine.Games;
using Volo.Abp.DependencyInjection;

namespace GameEventEngine
{
    public class GameDemoService : ITransientDependency
    {
        private readonly Game _game;

        public GameDemoService(Game game)
        {
            _game = game;
        }

        public async Task RunAsync()
        {
            _game.InitTriggers();
            await _game.RunAsync();
        }
    }
}
