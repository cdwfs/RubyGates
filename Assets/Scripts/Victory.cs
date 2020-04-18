using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class Victory : MonoBehaviour
{
    public string nextLevelName;

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
        SceneManager.LoadScene(nextLevelName);
    }
}
