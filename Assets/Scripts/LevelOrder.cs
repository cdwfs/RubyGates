using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[CreateAssetMenu(fileName = "Data", menuName = "ScriptableObjects/LevelOrder", order = 1)]
public class LevelOrder : ScriptableObject
{
    public string[] levels;
}
