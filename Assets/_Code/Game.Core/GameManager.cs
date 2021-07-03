using Game.Core.StateMachines.Game;
using Game.Inputs;
using UnityEngine;
using UnityEngine.Assertions;

namespace Game.Core
{
	public class GameManager : MonoBehaviour
	{
		public static GameSingleton Game { get; private set; }

		private void Start()
		{
			var musicAudioSource = GameObject.Find("Music Audio Source").GetComponent<AudioSource>();
			var config = Resources.Load<GameConfig>("Game Config");
			var camera = Camera.main;
			var ui = FindObjectOfType<GameUI>();
			var cameraRig = FindObjectOfType<CameraRig>();

			Assert.IsNotNull(config);
			Assert.IsNotNull(musicAudioSource);
			Assert.IsNotNull(camera);
			Assert.IsNotNull(ui);
			Assert.IsNotNull(cameraRig);

			Game = new GameSingleton();
			Game.Config = config;
			Game.Controls = new GameControls();
			Game.CameraRig = cameraRig;
			Game.UI = ui;
			Game.State = new GameState();
			Game.AudioPlayer = new AudioPlayer(config, musicAudioSource);
			Game.GameFSM = new GameFSM(config.DebugFSM, Game);

			Game.UI.Inject(Game);
			Game.GameFSM.Start();
		}

		private void Update()
		{
			Game?.GameFSM.Tick();
		}
	}
}
