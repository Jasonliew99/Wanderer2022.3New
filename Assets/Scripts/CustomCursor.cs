using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class CustomCursor : MonoBehaviour
{
    [Header("Cursor Settings")]
    public Texture2D customCursor;
    public Vector2 hotspot = Vector2.zero;   // (0,0) = bottom-left
    public CursorMode cursorMode = CursorMode.Auto;

    [Header("Debug Gizmo Settings")]
    public bool showHotspotGizmo = true;
    public Color gizmoColor = Color.red;
    [Tooltip("Overall gizmo size in world space (bigger = more visible in Scene view)")]
    public float gizmoBaseSize = 0.25f;
    [Tooltip("Additional visual size for hotspot indicator (relative to base size)")]
    public float hotspotVisualScale = 1.0f;

    private void Start()
    {
        ApplyCursor();
    }

    private void OnValidate()
    {
        ApplyCursor();
    }

    private void ApplyCursor()
    {
        if (customCursor != null)
        {
            Cursor.SetCursor(customCursor, hotspot, cursorMode);
            Cursor.visible = true;
        }
    }

    private void OnDrawGizmos()
    {
        if (!showHotspotGizmo || customCursor == null)
            return;

        // Calculate hotspot position relative to texture size
        float normX = hotspot.x / customCursor.width;
        float normY = hotspot.y / customCursor.height;

        // Base position (centered on object)
        Vector3 pos = transform.position;

        // Scale gizmo size based on Scene camera distance for visibility
        float distanceScale = 1f;
#if UNITY_EDITOR
        if (UnityEditor.SceneView.lastActiveSceneView != null)
        {
            Camera cam = UnityEditor.SceneView.lastActiveSceneView.camera;
            if (cam != null)
                distanceScale = Vector3.Distance(cam.transform.position, pos) * 0.05f;
        }
#endif

        float finalSize = gizmoBaseSize * distanceScale;

        Gizmos.color = gizmoColor;

        // Draw reference box
        Gizmos.DrawWireCube(pos, Vector3.one * finalSize);

        // Draw hotspot dot (moves based on hotspot values)
        Vector3 hotspotPos = pos + new Vector3(
            (normX - 0.5f) * finalSize,
            (normY - 0.5f) * finalSize,
            0
        );

        Gizmos.DrawSphere(hotspotPos, finalSize * 0.2f * hotspotVisualScale);
    }
}
