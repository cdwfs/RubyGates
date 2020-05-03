using Unity.Assertions;
using UnityEngine;

[DisallowMultipleComponent]
public class Victory : MonoBehaviour
{
    private ParticleSystem _particles;
    // Start is called before the first frame update
    void Start()
    {
        _particles = GetComponent<ParticleSystem>();
    }

    public void DoVictoryDance()
    {
        _particles.Play();
        
        var uiMgr = FindObjectOfType<UIManager>();
        Assert.IsNotNull(uiMgr);
        uiMgr.SetVictoryPanelActive(true);
    }
}
