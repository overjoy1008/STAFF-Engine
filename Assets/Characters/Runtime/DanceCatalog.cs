using System;
using UnityEngine;
namespace Staff.Characters
{
    [CreateAssetMenu(menuName="STAFF/Dance Catalog")]
    public sealed class DanceCatalog : ScriptableObject
    {
        [Serializable] public sealed class Entry
        {
            public string id, title;
            public AnimationClip clip;
            public AudioClip music;
            public float audioStart;
        }
        public Entry[] dances = Array.Empty<Entry>();
    }
}
