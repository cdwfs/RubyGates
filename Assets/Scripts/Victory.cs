using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Victory : MonoBehaviour
{
    public int nextSceneIndex;

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
        SceneManager.LoadScene(nextSceneIndex);
    }
}
