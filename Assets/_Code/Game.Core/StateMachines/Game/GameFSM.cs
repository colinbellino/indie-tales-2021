using System.Collections.Generic;
using Stateless;
using UnityEngine;
using UnityEngine.Assertions;

namespace Game.Core.StateMachines.Game
{
	public class GameFSM
	{
		public enum States { Init, Gameplay, Victory, GameOver, Quit }
		public enum Triggers { Done, Won, Lost, Retry, NextLevel, Quit }

		private readonly bool _debug;
		private readonly Dictionary<States, IState> _states;
		private readonly StateMachine<States, Triggers> _machine;
		private IState _currentState;

		public GameFSM(bool debug, GameSingleton game)
		{
			Assert.IsNotNull(game);

			_debug = debug;
			_states = new Dictionary<States, IState>
			{
				{ States.Init, new GameInitState(this, game) },
				{ States.Gameplay, new GameGameplayState(this, game) },
				{ States.Victory, new GameVictoryState(this, game) },
				{ States.GameOver, new GameOverState(this, game) },
				{ States.Quit, new GameQuitState(this, game) },
			};

			_machine = new StateMachine<States, Triggers>(States.Init);
			_machine.OnTransitioned(OnTransitioned);

			_machine.Configure(States.Init)
				.Permit(Triggers.Done, States.Gameplay);

			_machine.Configure(States.Gameplay)
				.Permit(Triggers.Won, States.Victory)
				.PermitReentry(Triggers.Lost)
				.PermitReentry(Triggers.Quit);

			_machine.Configure(States.Victory)
				.Permit(Triggers.Retry, States.Gameplay)
				.Permit(Triggers.Quit, States.Quit);

			_machine.Configure(States.GameOver)
				.Permit(Triggers.Retry, States.Gameplay)
				.Permit(Triggers.Quit, States.Quit);

			_currentState = _states[_machine.State];
		}

		public async void Start()
		{
			await _currentState.Enter();
		}

		public void Tick() => _currentState?.Tick();

		public void FixedTick() => _currentState?.FixedTick();

		public void Fire(Triggers trigger)
		{
			if (_machine.CanFire(trigger))
			{
				_machine.Fire(trigger);
			}
			else
			{
				Debug.LogWarning("Invalid transition " + _currentState + " -> " + trigger);
			}
		}

		private async void OnTransitioned(StateMachine<States, Triggers>.Transition transition)
		{
			if (_currentState != null)
			{
				await _currentState.Exit();
			}

			if (_debug)
			{
				if (_states.ContainsKey(transition.Destination) == false)
				{
					UnityEngine.Debug.LogError("Missing state class for: " + transition.Destination);
				}
			}

			_currentState = _states[transition.Destination];
			if (_debug)
			{
				UnityEngine.Debug.Log($"GameFSM: {transition.Source} -> {transition.Destination}");
			}

			await _currentState.Enter();
		}
	}
}
