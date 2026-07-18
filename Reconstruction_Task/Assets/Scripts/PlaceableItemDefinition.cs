using UnityEngine;

[CreateAssetMenu(fileName = "New Placeable Item", menuName = "Placement System/Placeable Item")]
public class PlaceableItemDefinition : ScriptableObject
{
    public string itemName = "New Item";
    public Sprite icon;
    public GameObject prefab;

    [Tooltip("How many of this item can be placed in the scene. Set to -1 for unlimited.")]
    public int initialCount = 1;
}
