using Microsoft.Extensions.Hosting;
using Volo.Abp;

namespace GameEventEngine;

public class GameEventEngineHostedService : IHostedService
{
    private readonly IAbpApplicationWithExternalServiceProvider _abpApplication;
    private readonly GameDemoService _gameDemoService;

    public GameEventEngineHostedService(IAbpApplicationWithExternalServiceProvider abpApplication, GameDemoService gameDemoService)
    {
        _abpApplication = abpApplication;
        _gameDemoService = gameDemoService;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        return _gameDemoService.RunAsync();
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await _abpApplication.ShutdownAsync();
    }
}