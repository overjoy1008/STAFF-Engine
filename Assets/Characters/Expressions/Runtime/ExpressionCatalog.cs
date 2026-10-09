using System;
using UnityEngine;
namespace Staff.Characters {
 public sealed class ExpressionCatalog : ScriptableObject {
  [Serializable] public class Overlay { public Mesh source; public Mesh blush; }
  [Serializable] public class Character { public string id; public Avatar avatar; public Overlay[] overlays; }
  public TextAsset json;
  public Material blushMaterial;
  public Character[] characters;
 }
}
