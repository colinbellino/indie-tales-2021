using Game.Core.StateMachines.Game;
using Game.Inputs;

namespace Game.Core
{
	public class GameSingleton
	{
		public GameConfig Config;
		public GameUI UI;
		public CameraRig CameraRig;
		public GameControls Controls;
		public GameState State;
		public AudioPlayer AudioPlayer;
		public GameFSM GameFSM;
	}
}
