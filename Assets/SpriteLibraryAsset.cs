using UnityEngine;
using System;

// This attribute allows the class to show up in the Inspector
[Serializable]
public class SpriteSet
{
    public string setName;
    public Sprite[] sprites;
}

[CreateAssetMenu(fileName = "NewSpriteLibrary", menuName = "ScriptableObjects/SpriteLibrary")]
public class SpriteLibraryAsset : ScriptableObject
{
    // We use an array or list to hold the 4 sets
    // You can also hardcode 4 variables if the count will never change
    public SpriteSet[] spriteSets = new SpriteSet[4];
}