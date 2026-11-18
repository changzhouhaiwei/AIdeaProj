using UnityEngine;

public class ParticleController : MonoBehaviour
{
    private ParticleSystem[] particleSystems;

    private void Awake()
    {
        // 获取节点下所有的粒子系统组件
        particleSystems = GetComponentsInChildren<ParticleSystem>(true);

        foreach (ParticleSystem ps in particleSystems)
        {
            var mainModule = ps.main;
            mainModule.playOnAwake = false;
            ps.Stop();
        }
    }

    public void Play()
    {
        foreach (ParticleSystem ps in particleSystems)
        {
            ps.Play();
        }
    }

    public void Stop()
    {
        foreach (ParticleSystem ps in particleSystems)
        {
            ps.Stop();
        }
    }
}