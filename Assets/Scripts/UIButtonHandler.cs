using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UIButtonHandler : MonoBehaviour
{
    public LevelOrder levelOrder;

    public static void OnClickQuit()
    {
        if (Application.isEditor)
        {
            Debug.Log("In standalone builds, that would quit the game.");
#if UNITY_EDITOR
            EditorApplication.ExitPlaymode();
#endif
        }
        else
        {
            Application.Quit();
        }
    }

    public void OnClickReplayLevel()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void OnClickNextLevel()
    {
        var currentSceneName = SceneManager.GetActiveScene().name;
        int currentIndex = Array.FindIndex(levelOrder.levels, l => l == currentSceneName);
        if (currentIndex == -1)
            throw new ArgumentException($"Current Scene {currentSceneName} not found in LevelOrder");
        int nextIndex = currentIndex + 1;
        if (nextIndex >= levelOrder.levels.Length)
        {
            Debug.LogError($"next level {nextIndex} is out of range [0..{levelOrder.levels.Length - 1}]. Returning to level 0 instead.");
            nextIndex = 0;
        }

        SceneManager.LoadScene(levelOrder.levels[nextIndex]);
    }

}
