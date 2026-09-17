using UnityEngine;
using UnityEngine.UI;

public class ActionPanelUI : MonoBehaviour
{
    public SelectionManager selectionManager;
    public TransformGizmo gizmo;

    [Header("Buttons")]
    //public Button xAxisButton;
    //public Button yAxisButton;
    //public Button zAxisButton;
    //public Button scaleButton;
    public Button resetButton;

    void Start()
    {
        //xAxisButton.onClick.AddListener(() => EnterMoveAxis(GizmoAxis.X));
        //yAxisButton.onClick.AddListener(() => EnterMoveAxis(GizmoAxis.Y));
        //zAxisButton.onClick.AddListener(() => EnterMoveAxis(GizmoAxis.Z));
        //scaleButton.onClick.AddListener(EnterScaleMode);
        resetButton.onClick.AddListener(() => selectionManager.DeleteSelected());

        selectionManager.OnSelectionChanged += HandleSelectionChanged;
        HandleSelectionChanged(selectionManager.Selected); // set correct initial state
    }

    void OnDestroy()
    {
        if (selectionManager != null)
            selectionManager.OnSelectionChanged -= HandleSelectionChanged;
    }

    void HandleSelectionChanged(PlaceableObject selected)
    {
        bool hasSelection = selected != null;
        //xAxisButton.interactable = hasSelection;
        //yAxisButton.interactable = hasSelection;
        //zAxisButton.interactable = hasSelection;
        //scaleButton.interactable = hasSelection;
        resetButton.interactable = hasSelection;
    }

    void EnterMoveAxis(GizmoAxis axis)
    {
        if (selectionManager.Selected == null) return;
        gizmo.SetMode(GizmoMode.Move);
        gizmo.SetActiveAxis(axis);
    }

    void EnterScaleMode()
    {
        if (selectionManager.Selected == null) return;
        gizmo.SetMode(GizmoMode.Scale);
    }
}
