using UnityEngine;

namespace Staff.MathSpace
{
    [ExecuteAlways, DisallowMultipleComponent]
    public sealed class MonochromeMode : MonoBehaviour
    {
        [SerializeField] bool inverted;
        [SerializeField] Camera sceneCamera;
        [SerializeField] Material normalSkybox;
        [SerializeField] Material invertedSkybox;
        [SerializeField] Renderer[] planes;
        [SerializeField] Renderer[] symbols;
        MaterialPropertyBlock properties;
        public bool Inverted => inverted;

        public void Configure(Camera camera, Material normal, Material inverse, Renderer[] geometry, Renderer[] glyphs)
        {
            sceneCamera = camera;
            normalSkybox = normal;
            invertedSkybox = inverse;
            planes = geometry;
            symbols = glyphs;
            Apply();
        }

        public void SetInverted(bool value) { inverted = value; Apply(); }
        public void Toggle() => SetInverted(!inverted);
        void OnEnable() => Apply();
        void OnValidate() => Apply();

        public void Apply()
        {
            if (!sceneCamera || !normalSkybox || !invertedSkybox) return;
            RenderSettings.skybox = inverted ? invertedSkybox : normalSkybox;
            sceneCamera.backgroundColor = inverted ? Color.white : Color.black;
            properties ??= new MaterialPropertyBlock();
            if (planes != null) foreach (var renderer in planes)
            {
                if (!renderer) continue;
                renderer.GetPropertyBlock(properties);
                properties.SetFloat("_Inverted", inverted ? 1 : 0);
                renderer.SetPropertyBlock(properties);
                properties.Clear();
            }
            if (symbols != null) foreach (var renderer in symbols)
            {
                if (!renderer) continue;
                renderer.GetPropertyBlock(properties);
                // Tint the actual LaTeX texture, never replace its glyph with a font.
                properties.SetColor("_BaseColor", inverted ? Color.black : Color.white);
                renderer.SetPropertyBlock(properties);
                properties.Clear();
            }
        }
    }
}
