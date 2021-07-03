using Cysharp.Threading.Tasks;
using UnityEditor;

namespace Game.Core.StateMachines.Game
{
	public class GameQuitState : BaseGameState
	{
		public GameQuitState(GameFSM fsm, GameSingleton game) : base(fsm, game) { }

		public override async UniTask Enter()
		{
			await base.Enter();

#if UNITY_EDITOR
			EditorApplication.isPlaying = false;
#else
			UnityEngine.Application.Quit();
#endif
		}
	}
}
