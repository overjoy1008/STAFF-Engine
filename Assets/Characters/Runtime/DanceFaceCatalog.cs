using System;
using UnityEngine;
namespace Staff.Characters
{
    public sealed class DanceFaceCatalog : ScriptableObject
    {
        [Serializable] public class Entry { public Avatar avatar; public TextAsset samples; }
        public Entry[] characters;
    }
}
