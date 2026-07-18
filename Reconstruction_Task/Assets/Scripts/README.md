# Runtime Object Placement System - Setup

## 1. Create three layers
Edit → Project Settings → Tags and Layers → add:
- `Ground` — your floor/table/placement surface
- `Placeable` — objects that have been placed in the scene
- `Gizmo` — the move/scale handles

Assign your floor/table object to the `Ground` layer. It needs a Collider
(Mesh Collider or Box Collider) so raycasts can hit it.

## 2. Build the right-side palette UI
1. Create a Canvas (Screen Space – Overlay) if you don't have one.
2. Add a Panel, anchored to the right edge (anchor min `(1,0)`, max `(1,1)`,
   pivot `(1, 0.5)`, fixed width e.g. 200px).
3. Inside it, add a Scroll View (Vertical) — or just a Vertical Layout Group
   + Content Size Fitter if you don't expect it to overflow the screen.
4. The Scroll View's `Content` object is what you'll assign as `slotContainer`
   on `PaletteManager`.

## 3. Build the palette slot prefab
1. Create an empty GameObject with a `RectTransform` and a `CanvasGroup`.
2. Add a child `Image` (the icon) and a child `Text` (the remaining count).
3. Attach `PaletteSlotUI`, and wire up the `icon` and `countLabel` fields.
4. Drag it into your Project window to turn it into a prefab, then delete
   the instance from the scene.

## 4. Create your item definitions
Right-click in the Project window → Create → Placement System →
Placeable Item, once per object you want placeable. Assign its prefab,
icon, and `initialCount` (`-1` = unlimited).

## 5. Wire up the manager objects
- **PaletteManager** (on an empty GameObject): assign `availableItems`,
  `slotPrefab`, `slotContainer`, `groundLayer` = `Ground`,
  `placedObjectLayer` = `Placeable`.
- **TransformGizmo** (on an empty GameObject, can start inactive): assign
  `gizmoLayer` = `Gizmo`.
- **SelectionManager** (on an empty GameObject): assign `mainCamera`,
  `placedObjectLayer` = `Placeable`, `gizmo` = the TransformGizmo object,
  `palette` = the PaletteManager object.

Make sure Main Camera's Culling Mask includes `Ground`, `Placeable`, and
`Gizmo` (default "Everything" is fine).

## How it behaves
- Drag an icon from the right-hand palette onto the scene → it spawns where
  you dropped it, and the palette count ticks down. Items with `-1` count
  never run out; items that hit `0` grey out and stop being draggable.
- Click a placed object → gizmo appears at its position.
- **W** = move handles, **R** = scale handles (same shortcuts as the Unity
  editor). Drag a red/green/blue handle to move or scale along that axis.
- **Delete** / **Backspace** while something is selected → removes it and
  returns one unit to its palette slot.
- Click empty space → deselects.

## Notes / easy extensions
- Movement and scaling are currently along world axes, not the object's
  local rotation — matches Unity's default (non-local) gizmo mode. If you
  need local-space handles, swap `Vector3.right/up/forward` in
  `TransformGizmo.CreateHandle` for `Target.right/up/forward` and rebuild
  the handles when the target changes.
- There's no rotate handle yet since you only asked for scale + move +
  delete — the same `GizmoHandle` pattern (axis + drag-plane projection)
  extends to rotation if you need it later.
- `PaletteSlotUI` uses legacy `UnityEngine.UI.Text` — swap for `TMP_Text`
  if your project uses TextMeshPro (just change the field type and the one
  `.text` assignment in `Refresh()`).
