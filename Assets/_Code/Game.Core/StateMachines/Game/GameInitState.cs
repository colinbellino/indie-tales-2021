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

			_ui.PauseButton1.onClick.AddListener(ToggleSounds);
			_ui.PauseButton2.onClick.AddListener(ToggleMusic);
			_ui.PauseButton3.onClick.AddListener(QuitGame);
			_ui.PauseButton4.onClick.AddListener(ToggleAssistMode);

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

		private void ToggleSounds()
		{
			var isOn = _state.CurrentSoundVolume == _state.InitialSoundVolume;
			_state.CurrentSoundVolume = isOn ? 0f : _state.InitialSoundVolume;
			_audioPlayer.SetSoundVolume(_state.CurrentSoundVolume);
			_ui.PauseButton1.GetComponentInChildren<TMPro.TMP_Text>().text = "Sound: " + (isOn ? "OFF" : "ON");
		}

		private void ToggleMusic()
		{
			var isOn = _state.CurrentMusicVolume == _state.InitialMusicVolume;
			_state.CurrentMusicVolume = isOn ? 0f : _state.InitialMusicVolume;
			_audioPlayer.SetMusicVolume(_state.CurrentMusicVolume);
			_ui.PauseButton2.GetComponentInChildren<TMPro.TMP_Text>().text = "Music: " + (isOn ? "OFF" : "ON");
		}

		private void ToggleAssistMode()
		{
			_state.AssistMode = !_state.AssistMode;
			_ui.PauseButton4.GetComponentInChildren<TMPro.TMP_Text>().text = "Assist mode: " + (!_state.AssistMode ? "OFF" : "ON");
		}

		private void QuitGame()
		{
#if UNITY_EDITOR
			UnityEditor.EditorApplication.isPlaying = false;
#else
			UnityEngine.Application.Quit();
#endif
		}
	}
}
