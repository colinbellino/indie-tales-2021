using System.Linq;
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

		private const float BIRD_SPAWN_DELAY = 1;
		private const float BIRD_SPAWN_DELAY_MAX = 3;
		private const int MAX_SCORE = 50;

		public GameGameplayState(GameFSM fsm, GameSingleton game) : base(fsm, game) { }

		public override async UniTask Enter()
		{
			await base.Enter();

			var playerPrefab = Resources.Load<Entity>("Player");
			_state.Player = GameObject.Instantiate(playerPrefab, GameObject.Find("Player Spawn").transform.position, Quaternion.identity);

			var ropePrefab = Resources.Load<GameObject>("Rope");
			var ropes = GameObject.FindGameObjectsWithTag("Rope Spawn");
			_state.Ropes = new GameObject[ropes.Length];
			for (int i = 0; i < ropes.Length; i++)
			{
				var rope = GameObject.Instantiate(ropePrefab, ropes[i].transform.position, Quaternion.identity);
				rope.name = $"Rope {i}";
				_state.Ropes[i] = rope;
			}

			_state.RopeTargets = new Transform[_state.Ropes.Length];
			_state.RopeTargets[0] = _state.Player.transform;
			_state.Player.RopeIndex = 0;
			{
				var end = _state.Ropes[0].transform.GetChild(_state.Ropes[0].transform.childCount - 1);
				end.position = _state.RopeTargets[0].position;
			}

			await UniTask.Delay(1500); // Wait for the rope physics to calm down a little

			_state.Random = new Unity.Mathematics.Random();
			_state.Random.InitState((uint)UnityEngine.Random.Range(0, int.MaxValue));

			var birdSpots = GameObject.FindGameObjectsWithTag("Bird Spot");
			_state.BirdSpots = new ShuffleBag<Vector3>(_state.Random);
			foreach (var spot in birdSpots)
			{
				_state.BirdSpots.Add(spot.transform.position);
			}

			_state.Score = 0;
			_state.BirdDoneCount = 0;

			var birdPrefab = Resources.Load<Entity>("Bird");
			_state.Birds = new Entity[MAX_SCORE];

			var spawnTime = Time.time;
			for (int birdIndex = 0; birdIndex < _state.Birds.Length; birdIndex++)
			{
				var bird = GameObject.Instantiate(birdPrefab);
				bird.transform.position = new Vector3(99, 99);
				bird.SpawnTimestamp = spawnTime + _state.Random.NextFloat(BIRD_SPAWN_DELAY, BIRD_SPAWN_DELAY_MAX);
				_state.Birds[birdIndex] = bird;
				spawnTime = bird.SpawnTimestamp;
			}

			if (_config.Music1Clip && _audioPlayer.IsMusicPlaying() == false && _audioPlayer.IsCurrentMusic(_config.Music1Clip) == false)
			{
				_ = _audioPlayer.PlayMusic(_config.Music1Clip, true, 0.5f);
			}

			await _ui.FadeOut();

			_state.Running = true;

			_ui.ShowGameplay();

			_controls.Gameplay.Enable();
			_controls.Gameplay.Confirm.started += ConfirmStarted;
			_controls.Gameplay.Cancel.started += CancelStarted;
			_controls.Global.Enable();
		}

		public override void Tick()
		{
			base.Tick();

			if (_controls.Global.Pause.WasPerformedThisFrame())
			{
				if (Time.timeScale == 0f)
				{
					Time.timeScale = 1f;
					_state.Running = true;
					_audioPlayer.ResumeMusic();
					_ui.HidePause();

					Time.timeScale = _state.AssistMode ? 0.7f : 1f;
				}
				else
				{
					Time.timeScale = 0f;
					_state.Running = false;
					_audioPlayer.PauseMusic();
					_ui.ShowPause();
				}
			}

			if (_state.Running == false)
			{
				return;
			}

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

				end.position = _state.RopeTargets[i].position + new Vector3(0, 0.4f);
				if (_state.RopeTargets[i].CompareTag("Player"))
				{
					end.position = _state.RopeTargets[i].position;
				}
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
								entity.MoveDestination = _state.BirdSpots.Next();
								entity.StartMoveTimestamp = Time.time;
								entity.StartPosition = new Vector3(entity.MoveDestination.x > 0 ? 17 : -17, entity.MoveDestination.y + 2);
								entity.AIState = AIStates.MovingToDestination;
							}
						}
						break;

					case AIStates.MovingToDestination:
						{
							if (entity.Moving == false)
							{
								entity.transform.position = entity.StartPosition;
								entity.transform.DOMove(entity.MoveDestination, 1 / entity.MoveSpeed).SetEase(Ease.OutExpo);
								entity.Animator.Play("Fly");
								entity.SpriteRenderer.flipX = (entity.MoveDestination - entity.StartPosition).x < 0;
								entity.Moving = true;
							}

							var distance = Vector3.Distance(entity.transform.position, entity.MoveDestination);
							if (distance < 0.1f)
							{
								entity.MoveDestination = Vector3.zero;
								entity.FleeTimestamp = Time.time + entity.LeaveDelay;
								entity.Moving = false;
								entity.AIState = AIStates.Idle;
							}
						}
						break;

					case AIStates.Idle:
						{
							entity.Animator.Play("Idle");

							if (Time.time > entity.FleeTimestamp)
							{
								// TODO: Trigger detection anim
								// TODO: Move away from the player and top
								entity.StartPosition = entity.transform.position;
								entity.MoveDestination = new Vector3(entity.transform.position.x, 17);
								entity.AIState = AIStates.Fleeing;
							}
						}
						break;

					case AIStates.Fleeing:
						{
							if (entity.Moving == false)
							{
								entity.transform.DOMove(entity.MoveDestination, 1 / entity.MoveSpeed / 1.5f);
								entity.Animator.Play("Fly");
								entity.SpriteRenderer.flipX = (entity.MoveDestination - entity.StartPosition).x < 0;
								entity.Moving = true;
							}

							var distance = Vector3.Distance(entity.transform.position, entity.MoveDestination);
							if (distance < 0.1f)
							{
								entity.MoveDestination += new Vector3(0, 10f);
								entity.Moving = false;
								entity.AIState = AIStates.Captured;
							}
						}
						break;

					case AIStates.Captured:
						{
							if (entity.Moving == false)
							{
								entity.transform.DOMove(entity.MoveDestination, 1 / entity.MoveSpeed / entity.FleeSpeedMultiplier);
								entity.Moving = true;
								_state.BirdDoneCount += 1;
							}
						}
						break;
				}
			}

			if (_state.BirdDoneCount >= _state.Birds.Length)
			{
				Victory();
			}

			_confirmWasPressedThisFrame = false;
			_cancelWasPressedThisFrame = false;

			_ui.TimerText.text = (_state.Birds.Length - _state.BirdDoneCount).ToString();

			if (Utils.IsDevBuild())
			{
				if (Keyboard.current.f1Key.wasPressedThisFrame)
				{
					Victory();
				}

				if (Keyboard.current.f2Key.wasPressedThisFrame)
				{
					Defeat();
				}
			}
		}

		public override async UniTask Exit()
		{
			await base.Exit();

			_state.Running = false;

			_controls.Gameplay.Disable();
			_controls.Gameplay.Confirm.started -= ConfirmStarted;
			_controls.Gameplay.Cancel.started -= CancelStarted;
			_controls.Global.Disable();

			await _ui.FadeIn(Color.white);

			_ui.HideGameplay();

			foreach (var entity in _state.Ropes)
			{
				if (entity != null && entity.gameObject)
				{
					GameObject.Destroy(entity.gameObject);
				}
			}
			foreach (var entity in _state.Birds)
			{
				if (entity != null && entity.gameObject)
				{
					GameObject.Destroy(entity.gameObject);
				}
			}

			GameObject.Destroy(_state.Player.gameObject);
		}

		private void ConfirmStarted(InputAction.CallbackContext context) => _confirmWasPressedThisFrame = true;

		private void CancelStarted(InputAction.CallbackContext context) => _cancelWasPressedThisFrame = true;

		private void Victory()
		{
			_ = _audioPlayer.StopMusic();
			_fsm.Fire(GameFSM.Triggers.Won);
		}

		private async void Defeat()
		{
			var position = _state.Player.transform.position;

			if (_config.PlayerDeathClip)
			{
				_ = _audioPlayer.PlaySoundEffect(_config.PlayerDeathClip);
			}

			_state.Running = false;

			GameObject.Instantiate(Resources.Load("Player Death"), position, Quaternion.identity);

			await UniTask.Delay(1000);

			_state.Player.transform.position = GameObject.Find("Player Spawn").transform.position;
			_state.Player.Velocity = Vector3.zero;
			_state.Running = true;

			// _fsm.Fire(GameFSM.Triggers.Lost);
		}

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
					Defeat();
					break;
				}
			}

			if (Time.time >= entity.AnimationTimestamp)
			{
				if (_cancelWasPressedThisFrame)
				{
					foreach (var hit in hits)
					{
						if (entity.RopeIndex == -1 && hit.CompareTag("Rope Tip"))
						{
							var ropeIndex = _state.Ropes.ToList().FindIndex(rope => hit.transform.root.gameObject == rope);
							_state.RopeTargets[ropeIndex] = entity.transform;
							entity.RopeIndex = ropeIndex;
							hit.transform.gameObject.SetActive(false);

							if (_config.CaptureClip)
							{
								_ = _audioPlayer.PlaySoundEffect(_config.CaptureClip);
							}

							break;
						}

						if (entity.RopeIndex > -1 && hit.CompareTag("Bird"))
						{
							var hitEntity = hit.transform.GetComponentInChildren<Entity>();
							if (hitEntity && hitEntity.Captured == false)
							{
								_state.RopeTargets[entity.RopeIndex] = hit.transform;
								hitEntity.Captured = true;

								if (_state.Score < MAX_SCORE)
								{
									_state.Score += 1;
								}

								if (entity.RopeIndex < _state.Ropes.Length - 1)
								{
									entity.RopeIndex = -1;
								}

								entity.RopeIndex = -1;

								if (_config.CaptureClip)
								{
									_ = _audioPlayer.PlaySoundEffect(_config.CaptureClip);
								}
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
						if (Time.time > _state.StepSoundTimestamp && _config.FootstepClips.Length > 0)
						{
							var clip = _config.FootstepClips[UnityEngine.Random.Range(0, _config.FootstepClips.Length)];
							entity.AudioSource.clip = clip;
							entity.AudioSource.Play();
							_state.StepSoundTimestamp = Time.time + 0.2f;
						}
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
						if (Time.time > _state.StepSoundTimestamp && _config.FootstepClips.Length > 0)
						{
							var clip = _config.FootstepClips[UnityEngine.Random.Range(0, _config.FootstepClips.Length)];
							entity.AudioSource.clip = clip;
							entity.AudioSource.Play();
							_state.StepSoundTimestamp = Time.time + 0.2f;
						}
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
