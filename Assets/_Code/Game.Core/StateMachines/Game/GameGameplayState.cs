using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Core.StateMachines.Game
{
	public class GameGameplayState : BaseGameState
	{
		private bool _confirmWasPressedThisFrame;
		private bool _cancelWasPressedThisFrame;

		public GameGameplayState(GameFSM fsm, GameSingleton game) : base(fsm, game) { }

		public override async UniTask Enter()
		{
			await base.Enter();

			var playerPrefab = Resources.Load<Entity>("Player");
			_state.Player = GameObject.Instantiate(playerPrefab, GameObject.Find("Player Spawn").transform.position, Quaternion.identity);

			_state.Random = new Unity.Mathematics.Random();
			_state.Random.InitState((uint)UnityEngine.Random.Range(0, int.MaxValue));

			_state.CurrentRopeIndex = 0;

			var birdPrefab = Resources.Load<Entity>("Bird");
			_state.Birds = new Entity[10];

			for (int birdIndex = 0; birdIndex < _state.Birds.Length; birdIndex++)
			{
				var bird = GameObject.Instantiate(birdPrefab);
				bird.transform.position = new Vector3(99, 99);
				bird.SpawnTimestamp = Time.time + 2f * birdIndex;
				_state.Birds[birdIndex] = bird;
			}

			var ropePrefab = Resources.Load<GameObject>("Rope");
			var ropes = GameObject.FindGameObjectsWithTag("Rope Spawn");
			_state.Ropes = new GameObject[ropes.Length];
			for (int i = 0; i < ropes.Length; i++)
			{
				var rope = GameObject.Instantiate(ropePrefab, ropes[i].transform.position, Quaternion.identity);
				_state.Ropes[i] = rope;
			}

			_state.RopeTargets = new Transform[_state.Ropes.Length];

			// _state.RopeTargets[0] = _state.Player.transform;

			_ui.ShowDebug();
			_ = _ui.FadeOut();

			_controls.Gameplay.Enable();
			_controls.Gameplay.Confirm.started += ConfirmStarted;
			_controls.Gameplay.Cancel.started += CancelStarted;
		}

		public override void Tick()
		{
			base.Tick();

			if (_state.Player != null)
			{
				HandleInput(_state.Player);
			}

			for (int i = 0; i < _state.Ropes.Length; i++)
			{
				var end = _state.Ropes[i].transform.GetChild(_state.Ropes[i].transform.childCount - 1);
				if (_state.RopeTargets[i] == null)
				{
					continue;
				}

				end.position = _state.RopeTargets[i].position;
			}

			foreach (var entity in _state.Birds)
			{
				if (entity.ControlledByAI == false)
				{
					continue;
				}

				switch (entity.AIState)
				{
					case AIStates.WaitingToSpawn:
						{
							if (Time.time > entity.SpawnTimestamp)
							{
								// FIXME: Find a point on the ground instead of a totally random point
								var randomPosition = new Vector3(
									_state.Random.NextFloat(-12f, 12f),
									_state.Random.NextFloat(0f, 6f)
								);
								entity.MoveDestination = randomPosition;
								entity.StartMoveTimestamp = Time.time;
								entity.transform.position = new Vector3(0, 17);
								// UnityEngine.Debug.Log("moving to " + entity.MoveDestination);
								entity.AIState = AIStates.MovingToDestination;
							}
						}
						break;

					case AIStates.MovingToDestination:
						{
							if (entity.Moving == false)
							{
								entity.transform.DOMove(entity.MoveDestination, 1 / entity.MoveSpeed).SetEase(Ease.OutExpo);
								entity.Moving = true;
							}

							var distance = Vector3.Distance(entity.transform.position, entity.MoveDestination);
							if (distance < 0.1f)
							{
								entity.MoveDestination = Vector3.zero;
								entity.FleeTimestamp = Time.time + 10f;
								entity.Moving = false;
								entity.AIState = AIStates.Idle;
							}
						}
						break;

					case AIStates.Idle:
						{
							var hits = Physics2D.OverlapCircleAll(entity.transform.position, entity.DetectionRadius);
							foreach (var hit in hits)
							{
								if (hit.transform.gameObject.CompareTag("Player"))
								{
									var timestamp = Time.time + entity.FleeDelay;
									if (timestamp < entity.FleeTimestamp)
									{
										entity.FleeTimestamp = timestamp;
									}

									break;
								}
							}

							if (Time.time > entity.FleeTimestamp)
							{
								// TODO: Trigger detection anim
								// TODO: Move away from the player and top
								entity.MoveDestination = new Vector3(entity.transform.position.x, 17);
								entity.AIState = AIStates.Fleeing;
							}
						}
						break;

					case AIStates.Fleeing:
						{
							if (entity.Moving == false)
							{
								entity.transform.DOMove(entity.MoveDestination, 1 / entity.MoveSpeed).SetEase(Ease.InExpo);
								entity.Moving = true;
							}
						}
						break;
				}

			}

			if (Keyboard.current.f1Key.wasPressedThisFrame)
			{
				_fsm.Fire(GameFSM.Triggers.NextLevel);
			}

			if (Keyboard.current.f2Key.wasPressedThisFrame)
			{
				_fsm.Fire(GameFSM.Triggers.Lost);
			}

			_confirmWasPressedThisFrame = false;
			_cancelWasPressedThisFrame = false;
		}

		public override async UniTask Exit()
		{
			await base.Exit();

			_controls.Gameplay.Disable();
			_controls.Gameplay.Confirm.started -= ConfirmStarted;
			_controls.Gameplay.Cancel.started -= CancelStarted;

			await _ui.FadeIn(Color.white);

			foreach (var rope in _state.Ropes)
			{
				GameObject.Destroy(rope.gameObject);
			}
			foreach (var bird in _state.Birds)
			{
				GameObject.Destroy(bird.gameObject);
			}

			GameObject.Destroy(_state.Player.gameObject);
		}

		private void ConfirmStarted(InputAction.CallbackContext context) => _confirmWasPressedThisFrame = true;

		private void CancelStarted(InputAction.CallbackContext context) => _cancelWasPressedThisFrame = true;

		private void HandleInput(Entity entity)
		{
			var moveInput = _controls.Gameplay.Move.ReadValue<Vector2>();

			if (entity.Controller.isGrounded)
			{
				entity.Velocity.y = 0;
			}

			var hits = Physics2D.OverlapCircleAll(entity.transform.position, entity.DetectionRadius);

			foreach (var hit in hits)
			{
				if (hit.CompareTag("Killbox"))
				{
					UnityEngine.Debug.Log("Player killed");
					// TODO: Trigger death effect
					_fsm.Fire(GameFSM.Triggers.Lost);
					break;
				}
			}

			if (Time.time >= entity.AnimationTimestamp)
			{
				if (_cancelWasPressedThisFrame)
				{
					foreach (var hit in hits)
					{
						if (hit.CompareTag("Bird"))
						{
							UnityEngine.Debug.Log("attach to " + hit.transform);
							_state.RopeTargets[_state.CurrentRopeIndex] = hit.transform;

							if (_state.CurrentRopeIndex < _state.Ropes.Length - 1)
							{
								_state.CurrentRopeIndex += 1;
								// _state.RopeTargets[_state.CurrentRopeIndex] = _state.Player.transform;
							}

							break;
						}
					}
				}

				if (moveInput.x > 0f)
				{
					entity.NormalizedHorizontalSpeed = 1;
					if (entity.transform.localScale.x < 0f)
					{
						entity.transform.localScale = new Vector3(-entity.transform.localScale.x, entity.transform.localScale.y, entity.transform.localScale.z);
					}

					if (entity.Controller.isGrounded)
					{
						entity.Animator?.Play(Animator.StringToHash("Run"));
						// if (Time.time > _stepSoundTimestamp)
						// {
						//     var clip = _config.FootstepClips[UnityEngine.Random.Range(0, _config.FootstepClips.Length)];
						//     entity.AudioSource.clip = clip;
						//     entity.AudioSource.Play();
						//     _stepSoundTimestamp = Time.time + 0.2f;
						// }
					}
				}
				else if (moveInput.x < 0f)
				{
					entity.NormalizedHorizontalSpeed = -1;
					if (entity.transform.localScale.x > 0f)
					{
						entity.transform.localScale = new Vector3(-entity.transform.localScale.x, entity.transform.localScale.y, entity.transform.localScale.z);
					}

					if (entity.Controller.isGrounded)
					{
						entity.Animator?.Play(Animator.StringToHash("Run"));
						// if (Time.time > _stepSoundTimestamp)
						// {
						//     var clip = _config.FootstepClips[UnityEngine.Random.Range(0, _config.FootstepClips.Length)];
						//     entity.AudioSource.clip = clip;
						//     entity.AudioSource.Play();
						//     _stepSoundTimestamp = Time.time + 0.2f;
						// }
					}
				}
				else
				{
					entity.NormalizedHorizontalSpeed = 0;

					if (entity.Controller.isGrounded)
					{
						entity.Animator?.Play(Animator.StringToHash("Idle"));
					}
				}

				// JUMP or Go down a platform
				if (_confirmWasPressedThisFrame && entity.Controller.isGrounded && moveInput.y >= 0f)
				{
					entity.Velocity.y = Mathf.Sqrt(2f * entity.JumpHeight * -entity.Gravity);
					// _audioPlayer.PlaySoundEffect(_config.JumpClip, entity.transform.position, 0.4f);
					entity.Animator?.Play(Animator.StringToHash("Jump"));
				}

				// apply horizontal speed smoothing it. dont really do this with Lerp. Use SmoothDamp or something that provides more control
				var smoothedMovementFactor = entity.Controller.isGrounded ? entity.GroundDamping : entity.InAirDamping; // how fast do we change direction?
				entity.Velocity.x = Mathf.Lerp(entity.Velocity.x, entity.NormalizedHorizontalSpeed * entity.MoveSpeed, Time.deltaTime * smoothedMovementFactor);
			}
			else
			{
				entity.Velocity.x = 0;
			}

			if (entity.Velocity.y < 0)
			{
				entity.Animator?.Play(Animator.StringToHash("Fall"));
			}

			// apply gravity before moving
			entity.Velocity.y += entity.Gravity * Time.deltaTime;

			// if holding down bump up our movement amount and turn off one way platform detection for a frame.
			// this lets us jump down through one way platforms
			if (entity.Controller.isGrounded && _confirmWasPressedThisFrame && moveInput.y < 0f)
			{
				entity.Controller.fallingThroughPlatformTimestamp = Time.time + 0.2f;
			}

			if (entity.Velocity.y < 0 && Time.time < entity.Controller.fallingThroughPlatformTimestamp)
			{
				// entity.Velocity.y *= entity.Gravity * Time.deltaTime;
				entity.Controller.ignoreOneWayPlatformsThisFrame = true;
			}

			entity.Controller.move(entity.Velocity * Time.deltaTime);

			// grab our current entity.Velocity to use as a base for all calculations
			entity.Velocity = entity.Controller.velocity;
		}
	}
}
