using Zenject;
using Game.Core.Interfaces;

public class GameService : IGameService
{
    private readonly SignalBus _signalBus;
    private readonly GameConfig _gameConfig;

    [Inject]
    public GameService(SignalBus signalBus, GameConfig gameConfig)
    {
        _signalBus = signalBus;
        _gameConfig = gameConfig;
    }

    public void StartGame()
    {
        // Можна використати _gameConfig для підготовки стану гри
        _signalBus.Fire<GameStartedSignal>();
    }

    public void EndGame()
    {
        _signalBus.Fire<PlayerDiedSignal>();
    }
}
