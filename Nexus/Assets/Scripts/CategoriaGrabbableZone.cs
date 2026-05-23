using UnityEngine;

/// <summary>
/// Empty marker component attached to each CategoriaDropHandler zone.
/// Placed on a child object with a SphereCollider (Trigger) so the zone
/// is both the visual header bar AND the physical trigger volume.
///
/// The collider defines how close a button must be to that category header
/// for OnDragEnd to count as a correct or incorrect drop.
///
/// This component just marks the zone — `CategoriaDropHandler` holds the
/// categoria string; `AbductionGrabbable` checks both on release.
/// </summary>
public class CategoriaGrabbableZone : MonoBehaviour
{
}
