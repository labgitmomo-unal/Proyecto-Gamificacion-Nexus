using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Utilidades compartidas para interacción UI/world space del grafo.
/// </summary>
public static class GraphUiUtility
{
    /// <summary>
    /// Convierte la posición del puntero a un punto del mundo dentro del RectTransform padre.
    /// </summary>
    public static bool TryGetPointerWorldPoint(RectTransform parentRect, Canvas canvas, PointerEventData eventData, out Vector3 worldPoint)
    {
        worldPoint = default;

        if (parentRect == null || eventData == null)
            return false;

        Camera eventCamera = eventData.pressEventCamera;
        if (eventCamera == null && canvas != null)
            eventCamera = canvas.worldCamera;

        return RectTransformUtility.ScreenPointToWorldPointInRectangle(
            parentRect,
            eventData.position,
            eventCamera,
            out worldPoint);
    }
}