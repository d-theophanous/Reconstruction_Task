using System.Collections.Generic;
using UnityEngine;

public class PaletteManager : Singleton<PaletteManager>
{

    //public List<PlaceableItemDefinition> AllAvailableObjects = new();

    [Header("Setup")]
    public List<PlaceableItemDefinition> CurPaletteSlots = new List<PlaceableItemDefinition>();
    public List<ObjectScript> CurObjectList = new();
    public GameObject CurObjectParent;

    public List<ObjectDefinitionPair> AllObjectList = new();
    public PaletteSlotUI slotPrefab;
    public Transform slotContainer; // "Content" object of a Vertical Layout Group inside a ScrollView

    [Header("Placement")]
    public Transform spawnPoint;        // fallback position - only used if the drop's screen position can't be resolved to world space
    public LayerMask placedObjectLayer; // layer newly spawned objects get moved to
    public SelectionManager selectionManager; // lets palette icons select their already-placed instance

    [Tooltip("World Z every newly dropped object is placed at. Dragging from the palette only ever sets X/Y - depth always comes from this value.")]
    public float dropDepth = 0f;

    private readonly Dictionary<PlaceableItemDefinition, int> remaining = new Dictionary<PlaceableItemDefinition, int>();
    private readonly Dictionary<PlaceableItemDefinition, PaletteSlotUI> slots = new Dictionary<PlaceableItemDefinition, PaletteSlotUI>();
    private readonly Dictionary<PlaceableItemDefinition, List<PlaceableObject>> spawnedInstances = new Dictionary<PlaceableItemDefinition, List<PlaceableObject>>();

    private int ObjectsInSceneCount;
    private Dictionary<ObjectType, PlaceableItemDefinition> AllObjectDic = new();
    private Dictionary<PlaceableItemDefinition, ObjectType> ObjectTypeByDefinition = new();

    private bool HasShownTutorial;
    protected override void Start()
    {
        FillObjectDictionary();
    }
    private void FillObjectDictionary()
    {
        foreach (var item in AllObjectList)
        {
            AllObjectDic.Add(item.ObjectType, item.Definition);
            ObjectTypeByDefinition[item.Definition] = item.ObjectType;
        }
    }

    // Reverse lookup used when recording data for a placed instance, which only knows
    // its PlaceableItemDefinition (via PlaceableObject.sourceDefinition), not its ObjectType.
    public ObjectType GetObjectType(PlaceableItemDefinition definition) =>
        ObjectTypeByDefinition.TryGetValue(definition, out var type) ? type : default;
    public void SetUpCurrentReconstruction(List<ObjectScript> object_list, GameObject object_parent)
    {
        CurObjectList = object_list;
        LoadNewObjects(object_list);
        CurObjectParent = object_parent;
        RebuildPalette();
    }
    public void LoadNewObjects(List<ObjectScript> object_list)
    {
        CurPaletteSlots.Clear();
        foreach (var item in object_list)
        {
            CurPaletteSlots.Add(AllObjectDic[item.ObjectType]);
        }
    }
    // Call this again at runtime if you ever want to change availableItems dynamically.
    public void RebuildPalette()
    {
        ObjectsInSceneCount = 0;
        foreach (Transform child in slotContainer)
            Destroy(child.gameObject);
        slots.Clear();
        remaining.Clear();
        spawnedInstances.Clear();

        foreach (var item in CurPaletteSlots)
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

        MainManager.Instance.ConfirmButton.interactable = false;
        ObjectsInSceneCount--;
    }

    // Returns the currently placed instance of this item, if any - used by the
    // palette icon's click-to-select. If duplicates ever exist, returns the first one.
    public PlaceableObject GetSpawnedInstance(PlaceableItemDefinition item)
    {
        if (spawnedInstances.TryGetValue(item, out var list) && list.Count > 0)
            return list[0];
        return null;
    }

    // Spawns the item at the world X/Y under the given screen position (where the player
    // released the drag), at the fixed dropDepth - so only X/Y ever come from the mouse.
    public GameObject SpawnAtScreenPosition(PlaceableItemDefinition item, Vector2 screenPosition)
    {
        Camera cam = MainManager.Instance.CurCamera;
        if (cam == null)
        {
            Debug.LogWarning("PaletteManager: no active camera - can't resolve a drop position.");
            return null;
        }

        Plane dropPlane = new Plane(Vector3.forward, new Vector3(0f, 0f, dropDepth));
        Ray ray = cam.ScreenPointToRay(screenPosition);

        Vector3 worldPos;
        if (dropPlane.Raycast(ray, out float enter))
            worldPos = ray.GetPoint(enter);
        else if (spawnPoint != null)
            worldPos = spawnPoint.position; // camera looking parallel to the drop plane - rare fallback
        else
            worldPos = Vector3.zero;

        worldPos.z = dropDepth;
        return SpawnAt(item, worldPos);
    }

    public GameObject SpawnAt(PlaceableItemDefinition item, Vector3 worldPos)
    {
        var instance = Instantiate(item.prefab, worldPos, Quaternion.identity, MainManager.Instance.ObjectParent.transform);
        instance.transform.localScale = item.spawnScale;
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

        ObjectsInSceneCount++;
        if (CurPaletteSlots.Count == ObjectsInSceneCount)
            MainManager.Instance.ConfirmButton.interactable = true;

        if (!HasShownTutorial && MainManager.Instance.IsTrialRun)
        {
            HasShownTutorial = true;
            var guide = MainManager.Instance.DragObjectTutorial.GetComponent<TutorialHandGuide>();
            guide.startPoint.transform.position = MainManager.Instance.CurCamera.WorldToScreenPoint(placeable.transform.position);
            var new_end_point = new Vector3(guide.startPoint.position.x + 400,
                                        guide.startPoint.position.y,
                                        guide.startPoint.position.z);
            guide.endPoint.transform.position = new_end_point;
            MainManager.Instance.DragObjectTutorial.SetActive(true);
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
public enum ObjectType
{
    APPLE
}

[System.Serializable]
public class ObjectDefinitionPair
{
    public ObjectType ObjectType;
    public PlaceableItemDefinition Definition;
}

