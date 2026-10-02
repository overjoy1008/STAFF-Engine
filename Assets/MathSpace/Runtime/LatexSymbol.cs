using UnityEngine;

namespace Staff.MathSpace
{
    /// <summary>Provenance for a world-space glyph compiled with real LaTeX/Manim.</summary>
    [DisallowMultipleComponent]
    public sealed class LatexSymbol : MonoBehaviour
    {
        [SerializeField] string formula = "O";
        [SerializeField] Texture2D renderedTexture;
        [SerializeField] string sourceAsset = "Assets/MathSpace/Mathematics/Origin/symbol.tex";
        public string Formula => formula;
        public Texture2D RenderedTexture => renderedTexture;
        public string SourceAsset => sourceAsset;
        public void SetSource(string value, Texture2D texture, string path)
        {
            formula = value;
            renderedTexture = texture;
            sourceAsset = path;
        }
    }
}
