using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    public LevelOrder levelOrder;

    public CanvasRenderer victoryPanel;
    public CanvasRenderer pausePanel;

    private Stack<CanvasRenderer> _modalPanelStack;
    public bool IsModalDialogActive => _modalPanelStack.Count > 0;
    
    private void Start()
    {
        int defaultModalPanelStackCapacity = 8;
        _modalPanelStack = new Stack<CanvasRenderer>(defaultModalPanelStackCapacity);
        // panels default to off
        pausePanel.gameObject.SetActive(false);
        victoryPanel.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (_modalPanelStack.Count == 0)
                SetPausePanelActive(true);
            else
            {
                var modalTop = _modalPanelStack.Peek();
                if (modalTop == pausePanel)
                    SetPausePanelActive(false);
            }
        }
    }

    public void SetVictoryPanelActive(bool active)
    {
        if (active == victoryPanel.gameObject.activeSelf)
            return;
        
        victoryPanel.gameObject.SetActive(active);

        if (active)
            _modalPanelStack.Push(victoryPanel);
        else
            _modalPanelStack.Pop();
    }

    public void SetPausePanelActive(bool active)
    {
        if (active == pausePanel.gameObject.activeSelf)
            return;
        
        pausePanel.gameObject.SetActive(active);

        if (active)
            _modalPanelStack.Push(pausePanel);
        else
            _modalPanelStack.Pop();
    }

    // Pause Panel Handlers
    public void OnClickResume()
    {
        if (_modalPanelStack.Count == 0 || _modalPanelStack.Peek() != pausePanel)
            return;
        SetPausePanelActive(false);
    }

    public void OnClickResetLevel()
    {
        if (_modalPanelStack.Count == 0 || _modalPanelStack.Peek() != pausePanel)
            return;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void OnClickQuit()
    {
        if (_modalPanelStack.Count == 0 || _modalPanelStack.Peek() != pausePanel)
            return;
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

    // Victory Panel Handlers
    public void OnClickReplayLevel()
    {
        if (_modalPanelStack.Count == 0 || _modalPanelStack.Peek() != victoryPanel)
            return;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void OnClickNextLevel()
    {
        if (_modalPanelStack.Count == 0 || _modalPanelStack.Peek() != victoryPanel)
            return;

        var currentSceneName = SceneManager.GetActiveScene().name;
        int currentLevelIndex = Array.FindIndex(levelOrder.Levels, l => l.SceneName == currentSceneName);
        if (currentLevelIndex == -1)
            throw new ArgumentException($"Current Scene {currentSceneName} not found in LevelOrder");
        int nextIndex = currentLevelIndex + 1;
        if (nextIndex >= levelOrder.Levels.Length)
        {
            Debug.LogError($"next level {nextIndex} is out of range [0..{levelOrder.Levels.Length - 1}]. Returning to level 0 instead.");
            nextIndex = 0;
        }
        SceneManager.LoadScene(levelOrder.Levels[nextIndex].SceneName);
    }

}
