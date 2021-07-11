using Prime31;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Core
{

	public class Entity : MonoBehaviour
	{
		[SerializeField] public Rigidbody2D Rigidbody;
		[SerializeField] public CharacterController2D Controller;
		[SerializeField] public Animator Animator;
		[SerializeField] public AudioSource AudioSource;

		[SerializeField] public float Gravity = -25f;
		[SerializeField] public float MoveSpeed = 8f;
		[SerializeField] public float GroundDamping = 20f;
		[SerializeField] public float InAirDamping = 5f;
		[SerializeField] public float JumpHeight = 3f;
		[SerializeField] public float DetectionRadius = 3f;
		[SerializeField] public float FleeDelay = 0.5f;

		[SerializeField] public bool ControlledByAI;

		[HideInInspector] public RaycastHit2D LastControllerColliderHit;
		[HideInInspector] public Vector3 Velocity;
		[HideInInspector] public float NormalizedHorizontalSpeed = 0;
		[HideInInspector] public float AnimationTimestamp;
		[HideInInspector] public Vector3 MoveDestination;
		[HideInInspector] public float SpawnTimestamp;
		[HideInInspector] public float FleeTimestamp;
		[HideInInspector] public float StartMoveTimestamp;
		[HideInInspector] public bool Moving;
		[HideInInspector] public AIStates AIState;
	}

	public enum AIStates
	{
		WaitingToSpawn,
		MovingToDestination,
		Idle,
		Fleeing,
	}
}
