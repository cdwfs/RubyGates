using System;
using UnityEngine;

[Serializable]
public class LevelInfo
{
    public string SceneName;
    public int TargetToggleCount;
}

[CreateAssetMenu(fileName = "Data", menuName = "ScriptableObjects/LevelOrder", order = 1)]
public class LevelOrder : ScriptableObject
{
    public LevelInfo[] Levels;
}
