using Cysharp.Threading.Tasks;
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

            _state.Player = GameObject.Find("Player").GetComponent<Entity>();

            _ui.ShowDebug();
            _ = _ui.FadeOut();

            _controls.Gameplay.Enable();
            _controls.Gameplay.Confirm.started += ConfirmStarted;
            _controls.Gameplay.Cancel.started += CancelStarted;
        }

        public override void Tick()
        {
            base.Tick();

            var moveInput = _controls.Gameplay.Move.ReadValue<Vector2>();

            if (_state.Player != null)
            {
                var entity = _state.Player;

                if (entity.Controller.isGrounded)
                {
                    entity.Velocity.y = 0;
                }

                if (Time.time >= entity.DigAnimationEndTimestamp)
                {
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

                        entity.DigDirection = new Vector3Int(1, 0, 0);
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

                        entity.DigDirection = new Vector3Int(-1, 0, 0);
                    }
                    else
                    {
                        entity.NormalizedHorizontalSpeed = 0;

                        if (entity.Controller.isGrounded)
                        {
                            entity.Animator?.Play(Animator.StringToHash("Idle"));
                        }

                        if (entity.transform.localScale.x > 0)
                        {
                            entity.DigDirection = new Vector3Int(1, 0, 0);
                        }
                        else
                        {
                            entity.DigDirection = new Vector3Int(-1, 0, 0);
                        }
                    }

                    // JUMP
                    if (_confirmWasPressedThisFrame && entity.Controller.isGrounded)
                    {
                        entity.Velocity.y = Mathf.Sqrt(2f * entity.JumpHeight * -entity.Gravity);
                        // _audioPlayer.PlaySoundEffect(_config.JumpClip, entity.transform.position, 0.4f);
                        entity.Animator?.Play(Animator.StringToHash("Jump"));
                    }

                    // apply horizontal speed smoothing it. dont really do this with Lerp. Use SmoothDamp or something that provides more control
                    var smoothedMovementFactor = entity.Controller.isGrounded ? entity.GroundDamping : entity.InAirDamping; // how fast do we change direction?
                    entity.Velocity.x = Mathf.Lerp(entity.Velocity.x, entity.NormalizedHorizontalSpeed * entity.RunSpeed, Time.deltaTime * smoothedMovementFactor);
                }
                else
                {
                    entity.Velocity.x = 0;
                }

                if (entity.Velocity.y < 0)
                {
                    entity.Animator?.Play(Animator.StringToHash("Fall"));
                }

                if (Time.time >= entity.StartDiggingTimestamp && entity.StartDiggingTimestamp > 0)
                {
                    entity.StartDiggingTimestamp = 0;
                    // PlayerDig(entity);
                }

                // apply gravity before moving
                entity.Velocity.y += entity.Gravity * Time.deltaTime;

                entity.Controller.move(entity.Velocity * Time.deltaTime);

                // grab our current entity.Velocity to use as a base for all calculations
                entity.Velocity = entity.Controller.velocity;
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
        }

        private void ConfirmStarted(InputAction.CallbackContext context) => _confirmWasPressedThisFrame = true;

        private void CancelStarted(InputAction.CallbackContext context) => _cancelWasPressedThisFrame = true;
    }
}
