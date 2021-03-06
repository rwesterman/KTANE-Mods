﻿using Assets.Scripts.Missions;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(KMService))]
[RequireComponent(typeof(KMGameInfo))]
public class BetterCasePicker : MonoBehaviour
{
	void LogError(string formatting, params object[] args)
	{
		Debug.LogErrorFormat("[BetterCasePicker] " + formatting, args);
	}

	void Start()
	{
		GetComponent<KMGameInfo>().OnStateChange += delegate (KMGameInfo.State state)
		{
			if (state == KMGameInfo.State.Gameplay)
			{
				BombGenerator bombGenerator = FindObjectOfType<BombGenerator>();

				if (bombGenerator.BombPrefabOverride == null) // No point in doing anything if they aren't even going to use the ObjectPool.
				{
					GameplayState gameplayState = SceneManager.Instance.GameplayState;
					Mission mission = null;
					if (!string.IsNullOrEmpty(GameplayState.MissionToLoad))
					{
						if (GameplayState.MissionToLoad.Equals(FreeplayMissionGenerator.FREEPLAY_MISSION_ID))
						{
							mission = FreeplayMissionGenerator.Generate(GameplayState.FreeplaySettings);
						}
						else if (GameplayState.MissionToLoad.Equals(ModMission.CUSTOM_MISSION_ID))
						{
							mission = GameplayState.CustomMission;
						}
						else
						{
							mission = MissionManager.Instance.GetMission(GameplayState.MissionToLoad);
						}
					}

					if (mission == null)
					{
						LogError("Unable to find mission.");
						return;
					}

					bool frontFaceOnly = mission.GeneratorSetting.FrontFaceOnly;
					int componentCount = mission.GeneratorSetting.ComponentPools.Where(pool => pool.ModTypes == null || !(pool.ModTypes.Contains("Factory Mode") || pool.ModTypes.Contains("Multiple Bombs"))).Sum(pool => pool.Count) + 1;

					ObjectPool prefabPool = bombGenerator.BombPrefabPool;

					Dictionary<GameObject, int> bombcases = prefabPool.Objects
					.Where(gameobject => gameobject.GetComponent<KMBomb>() != null)
					.ToDictionary(gameobject => gameobject, gameobject =>
					{
						if (!frontFaceOnly)
						{
							return gameobject.GetComponent<KMBomb>().Faces.Select(face => face.Anchors.Count).Sum();
						}
						else
						{
							return gameobject.GetComponent<KMBomb>().Faces[0].Anchors.Count;
						}
					});

					bombcases.Add(prefabPool.Default, !frontFaceOnly ? 12 : 6);

					if (bombcases.Count == 0)
					{
						LogError("Unable to find any bomb cases to use.");
						return;
					}

					var validBombCases = bombcases.Where(pair => pair.Value >= componentCount);
					var minBombCases = validBombCases.Count() == 0 ? 
						bombcases.Where(pair => pair.Value == bombcases.Max(pair2 => pair2.Value)) :
						validBombCases.Where(pair => pair.Value == validBombCases.Min(pair2 => pair2.Value));

					prefabPool.Objects = minBombCases.Select(x => x.Key).ToList();
				}
			}
		};
	}
}
