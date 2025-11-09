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

    [Header("Debug")]
    public bool showHotspotGizmo = true;
    public Color gizmoColor = Color.red;
    public float gizmoSize = 0.1f;

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

        // Convert hotspot into normalized (0–1) texture space
        float normX = hotspot.x / customCursor.width;
        float normY = hotspot.y / customCursor.height;

        // Draw a simple reference box to visualize hotspot direction
        Gizmos.color = gizmoColor;
        Vector3 pos = transform.position;

        // Draw gizmo relative to GameObject for debugging reference
        Gizmos.DrawWireCube(pos, new Vector3(0.5f, 0.5f, 0.01f));
        Gizmos.DrawSphere(pos + new Vector3(normX - 0.5f, normY - 0.5f, 0), gizmoSize);
    }
}
