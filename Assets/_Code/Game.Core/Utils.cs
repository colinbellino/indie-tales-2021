using Cysharp.Threading.Tasks;
using UnityEngine;
using Game.Inputs;
using UnityEngine.Tilemaps;
using System;

namespace Game.Core
{
	public static class Utils
	{
		public static bool IsDevBuild()
		{
#if UNITY_EDITOR
			return true;
#endif

#pragma warning disable 162
			return false;
#pragma warning restore 162
		}

		public static Vector3 GetMouseWorldPosition(GameControls controls, Camera camera)
		{
			var mousePosition = controls.Gameplay.MousePosition.ReadValue<Vector2>();
			var mouseWorldPosition = camera.ScreenToWorldPoint(mousePosition);
			mouseWorldPosition.z = 0f;
			return mouseWorldPosition;
		}

		public static ParticleSystem SpawnEffect(ParticleSystem effectPrefab, Vector3 position)
		{
			return GameObject.Instantiate(effectPrefab, position, Quaternion.identity);
		}
	}
}
