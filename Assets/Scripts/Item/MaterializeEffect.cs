using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MaterializeEffect : MonoBehaviour
{
    private static readonly int DissolveValue = Shader.PropertyToID("_DissolveValue");
    
    [Header("Dissolve")]
    [SerializeField] private Material dissolveMaterial;
    [SerializeField] private float duration = 1.2f;
    [SerializeField] private AnimationCurve curve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    private readonly List<RendererCache> cache = new();

    private struct RendererCache
    {
        public Renderer renderer;
        public Material[] originals;
        public Material[] dissolves;
    }
    
    public void Init(Material mat, float dur)
    {
        dissolveMaterial = mat;
        duration = dur;
    }

    public void Play()
    {
        foreach (var r in GetComponentsInChildren<Renderer>())
        {
            var originals = r.sharedMaterials;
            var dissolves = new Material[originals.Length];
            for (int i = 0; i < originals.Length; i++)
            {
                dissolves[i] = new Material(dissolveMaterial);
                if (originals[i] != null && originals[i].HasProperty("_BaseMap"))
                    dissolves[i].SetTexture("_BaseMap", originals[i].GetTexture("_BaseMap"));
                if (originals[i] != null && originals[i].HasProperty("_BaseColor"))
                    dissolves[i].SetColor("_BaseColor", originals[i].GetColor("_BaseColor"));
                dissolves[i].SetFloat(DissolveValue, 1f);
            }
            r.materials = dissolves;
            cache.Add(new RendererCache { renderer = r, originals = originals,  dissolves = dissolves });
        }

        StartCoroutine(Animate());
    }

    private IEnumerator Animate()
    {
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            float v = 1f - curve.Evaluate(Mathf.Clamp01(t));
            foreach (var c in cache)
            {
                foreach (var m in c.dissolves)
                {
                    if(m != null) m.SetFloat(DissolveValue, v);
                }
            }
            yield return null;
        }

        foreach (var c in cache)
        {
            if (c.renderer)  
                c.renderer.materials = c.originals;
            foreach (var m in c.dissolves)
                if (!m) Destroy(m);
        }

        cache.Clear();
        Destroy(this);
    }
}
