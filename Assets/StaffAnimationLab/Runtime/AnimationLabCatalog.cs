using System;
using UnityEngine;
namespace Staff.AnimationLab {
[CreateAssetMenu(menuName="STAFF/Animation Lab Catalog")]
public sealed class AnimationLabCatalog : ScriptableObject {
 [Serializable] public class Entry { public string title, category, subcategory, source, context; public string catalogId; public string[] additionalSubcategories; public AnimationClip clip; public string Context => string.IsNullOrEmpty(context) ? category : context; }
 public Entry[] entries;
 public GameObject[] characters;
}
}
