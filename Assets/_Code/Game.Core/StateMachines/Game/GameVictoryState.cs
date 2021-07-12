using Cysharp.Threading.Tasks;

namespace Game.Core.StateMachines.Game
{
	public class GameVictoryState : BaseGameState
	{
		public GameVictoryState(GameFSM fsm, GameSingleton game) : base(fsm, game) { }

		public override async UniTask Enter()
		{
			await base.Enter();

			var victoryLevel = 0;
			if (_state.Score > 5)
			{
				victoryLevel = 1;
			}
			if (_state.Score > 10)
			{
				victoryLevel = 2;
			}
			if (_state.Score > 20)
			{
				victoryLevel = 3;
			}

			var text = $"Thanks for playing! This is where the ending was supposed to go but we ran out of time...\nYour score was {_state.Score}.";
			await _ui.ShowVictory(text);

			_ui.VictoryButton1.onClick.AddListener(Restart);
			_ui.VictoryButton2.onClick.AddListener(Quit);

			_ = _audioPlayer.StopMusic(5f);
		}

		public override async UniTask Exit()
		{
			await base.Exit();

			await _ui.HideVictory();

			_ui.VictoryButton1.onClick.RemoveListener(Restart);
			_ui.VictoryButton2.onClick.RemoveListener(Quit);
		}

		private void Restart()
		{
			_fsm.Fire(GameFSM.Triggers.Retry);
		}

		private void Quit()
		{
			_fsm.Fire(GameFSM.Triggers.Quit);
		}
	}
}
