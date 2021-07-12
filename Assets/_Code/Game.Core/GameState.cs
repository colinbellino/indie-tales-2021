using UnityEngine;

namespace Game.Core
{
	public class GameState
	{
		public float InitialMusicVolume;
		public float CurrentMusicVolume;
		public float InitialSoundVolume;
		public float CurrentSoundVolume;
		public Entity Player;
		public Entity[] Birds;
		public Unity.Mathematics.Random Random;
		public Transform[] RopeTargets;
		public GameObject[] Ropes;
		public ShuffleBag<Vector3> BirdSpots;
		public int Score;
		public int BirdDoneCount;
		public bool Running;
		public bool AssistMode;
		public float StepSoundTimestamp;
	}
}
