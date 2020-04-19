using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class Victory : MonoBehaviour
{
    public LevelOrder levelOrder;

    private ParticleSystem _particles;
    // Start is called before the first frame update
    void Start()
    {
        _particles = GetComponent<ParticleSystem>();
    }

    public void DoVictoryDance()
    {
        StartCoroutine(VictoryCoroutine());
    }

    IEnumerator VictoryCoroutine() {
        _particles.Play();
        yield return new WaitForSeconds(_particles.main.duration + 2.0f);
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
