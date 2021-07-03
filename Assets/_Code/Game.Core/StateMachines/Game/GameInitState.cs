using Cysharp.Threading.Tasks;
using UnityEngine;
using static Game.Core.Utils;

namespace Game.Core.StateMachines.Game
{
	public class GameInitState : BaseGameState
	{
		public GameInitState(GameFSM fsm, GameSingleton game) : base(fsm, game) { }

		public override async UniTask Enter()
		{
			await base.Enter();

			_audioPlayer.SetMusicVolume(_config.MusicVolume);
			_audioPlayer.SetSoundVolume(_config.SoundVolume);

			_state.InitialMusicVolume = _state.CurrentMusicVolume = _config.MusicVolume;
			_state.InitialSoundVolume = _state.CurrentSoundVolume = _config.SoundVolume;

			Time.timeScale = 1f;

			if (IsDevBuild())
			{
				_ui.ShowDebug();

				if (_config.LockFPS > 0)
				{
					Debug.Log($"Locking FPS to {_config.LockFPS}");
					Application.targetFrameRate = _config.LockFPS;
					QualitySettings.vSyncCount = 1;
				}
				else
				{
					Application.targetFrameRate = 999;
					QualitySettings.vSyncCount = 0;
				}
			}

			_fsm.Fire(GameFSM.Triggers.Done);
		}
	}
}
