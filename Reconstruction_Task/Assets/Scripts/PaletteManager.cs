using System.Collections.Generic;
using UnityEngine;

public class PaletteManager : MonoBehaviour
{
    public static PaletteManager Instance { get; private set; }

    [Header("Setup")]
    public List<PlaceableItemDefinition> availableItems = new List<PlaceableItemDefinition>();
    public PaletteSlotUI slotPrefab;
    public Transform slotContainer; // "Content" object of a Vertical Layout Group inside a ScrollView

    [Header("Placement")]
    public Transform spawnPoint;        // empty GameObject placed in the scene - new objects spawn at its position
    public LayerMask placedObjectLayer; // layer newly spawned objects get moved to
    public SelectionManager selectionManager; // lets palette icons select their already-placed instance

    [Header("Spawn Offset")]
    [Tooltip("Distance between each ring of fallback positions if the spawn point is occupied.")]
    public float spawnOffsetStep = 1f;
    [Tooltip("Radius used to check whether a position is already occupied by a placed object.")]
    public float spawnCheckRadius = 0.5f;
    [Tooltip("How many rings of offset positions to try around the spawn point before giving up and stacking anyway.")]
    public int maxSpawnSearchRings = 6;

    private readonly Dictionary<PlaceableItemDefinition, int> remaining = new Dictionary<PlaceableItemDefinition, int>();
    private readonly Dictionary<PlaceableItemDefinition, PaletteSlotUI> slots = new Dictionary<PlaceableItemDefinition, PaletteSlotUI>();
    private readonly Dictionary<PlaceableItemDefinition, List<PlaceableObject>> spawnedInstances = new Dictionary<PlaceableItemDefinition, List<PlaceableObject>>();

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        RebuildPalette();
    }

    // Call this again at runtime if you ever want to change availableItems dynamically.
    public void RebuildPalette()
    {
        foreach (Transform child in slotContainer)
            Destroy(child.gameObject);
        slots.Clear();
        remaining.Clear();
        spawnedInstances.Clear();

        foreach (var item in availableItems)
        {
            remaining[item] = item.initialCount;
            CreateSlot(item);
        }
    }

    void CreateSlot(PlaceableItemDefinition item)
    {
        var slot = Instantiate(slotPrefab, slotContainer);
        slot.Setup(item, this, selectionManager);
        slot.Refresh(remaining[item]);
        slots[item] = slot;
    }

    public int GetRemaining(PlaceableItemDefinition item) =>
        remaining.TryGetValue(item, out var count) ? count : 0;

    public bool TryConsume(PlaceableItemDefinition item)
    {
        if (!remaining.ContainsKey(item)) return false;
        if (remaining[item] == 0) return false; // out of stock (unlimited items are stored as -1 and never blocked)
        if (remaining[item] > 0) remaining[item]--;
        if (slots.TryGetValue(item, out var slot)) slot.Refresh(remaining[item]);
        return true;
    }

    // Called when a placed object is deleted - increments its count back and
    // stops tracking that instance.
    public void ReturnItem(PlaceableObject obj)
    {
        if (obj == null || obj.sourceDefinition == null) return;
        var item = obj.sourceDefinition;

        if (spawnedInstances.TryGetValue(item, out var list))
            list.Remove(obj);

        if (!remaining.ContainsKey(item)) return;
        if (remaining[item] >= 0) remaining[item]++;
        if (slots.TryGetValue(item, out var slot)) slot.Refresh(remaining[item]);
    }

    // Returns the currently placed instance of this item, if any - used by the
    // palette icon's click-to-select. If duplicates ever exist, returns the first one.
    public PlaceableObject GetSpawnedInstance(PlaceableItemDefinition item)
    {
        if (spawnedInstances.TryGetValue(item, out var list) && list.Count > 0)
            return list[0];
        return null;
    }

    public GameObject SpawnAtSpawnPoint(PlaceableItemDefinition item)
    {
        if (spawnPoint == null)
        {
            Debug.LogWarning("PaletteManager: no spawnPoint assigned - can't place the item.");
            return null;
        }
        Vector3 position = FindFreeSpawnPosition(spawnPoint.position);
        return SpawnAt(item, position);
    }

    // Tries the spawn point first, then searches outward in rings on the XZ plane
    // for the nearest position that isn't already occupied by a placed object.
    Vector3 FindFreeSpawnPosition(Vector3 origin)
    {
        if (!IsOccupied(origin)) return origin;

        for (int ring = 1; ring <= maxSpawnSearchRings; ring++)
        {
            float radius = ring * spawnOffsetStep;
            int pointsInRing = ring * 8;

            for (int i = 0; i < pointsInRing; i++)
            {
                float angle = (360f / pointsInRing) * i * Mathf.Deg2Rad;
                Vector3 candidate = origin + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;
                if (!IsOccupied(candidate)) return candidate;
            }
        }

        return origin; // gave up - spawn on top rather than not spawning at all
    }

    bool IsOccupied(Vector3 position)
    {
        return Physics.CheckSphere(position, spawnCheckRadius, placedObjectLayer);
    }

    public GameObject SpawnAt(PlaceableItemDefinition item, Vector3 worldPos)
    {
        var instance = Instantiate(item.prefab, worldPos, Quaternion.identity);
        SetLayerRecursively(instance, LayerMaskToLayer(placedObjectLayer));

        var placeable = instance.GetComponent<PlaceableObject>();
        if (placeable == null) placeable = instance.AddComponent<PlaceableObject>();
        placeable.sourceDefinition = item;

        if (!spawnedInstances.TryGetValue(item, out var list))
        {
            list = new List<PlaceableObject>();
            spawnedInstances[item] = list;
        }
        list.Add(placeable);

        // Make sure it's selectable even if the prefab has no collider.
        if (instance.GetComponentInChildren<Collider>() == null)
        {
            var box = instance.AddComponent<BoxCollider>();
            var rend = instance.GetComponentInChildren<Renderer>();
            if (rend != null)
            {
                box.center = instance.transform.InverseTransformPoint(rend.bounds.center);
                box.size = rend.bounds.size;
            }
        }
        return instance;
    }

    static void SetLayerRecursively(GameObject go, int layer)
    {
        if (layer < 0) return;
        go.layer = layer;
        foreach (Transform child in go.transform)
            SetLayerRecursively(child.gameObject, layer);
    }

    static int LayerMaskToLayer(LayerMask mask)
    {
        int layerNumber = 0;
        int layerValue = mask.value;
        while (layerValue > 1)
        {
            layerValue >>= 1;
            layerNumber++;
        }
        return layerNumber;
    }
}
