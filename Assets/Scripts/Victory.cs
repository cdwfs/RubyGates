using UnityEngine;
using UnityEngine.Rendering;

[DisallowMultipleComponent]
public class Victory : MonoBehaviour
{
    public CanvasRenderer victoryPanel;
    private ParticleSystem _particles;
    // Start is called before the first frame update
    void Start()
    {
        _particles = GetComponent<ParticleSystem>();
    }

    public void DoVictoryDance()
    {
        _particles.Play();
        victoryPanel.gameObject.SetActive(true);
    }
}
