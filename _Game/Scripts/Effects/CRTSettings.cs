using UnityEngine;
namespace CoComputar
{
    [CreateAssetMenu(menuName = "Co-computar/CRT")]
    public sealed class CRTSettings : ScriptableObject
    {
        public Material material;
        [System.NonSerialized] public float sepiaAmount,glitchAmount;
        public void UpdateStory(){if(material)material.SetVector("_Light",new Vector4(noise,glow,sepiaAmount,glitchAmount));}
        [Range(0,1)] public float scanline = .18f, curvature = .055f, vignette = .22f, aberration = .0012f, noise = .018f, glow = .16f;
        public void Apply(float intensity,float bend)
        {
            if (!material) return;
            material.SetFloat("_Intensity", intensity);
            material.SetVector("_Shape", new Vector4(scanline, curvature*Mathf.Clamp01(bend), vignette, aberration));
            UpdateStory();
        }
    }
}
