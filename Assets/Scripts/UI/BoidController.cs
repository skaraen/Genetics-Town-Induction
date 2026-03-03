using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Simple boid controller for UI RectTransforms. Makes elements swim around
/// inside a container using flocking behavior (separation, alignment, cohesion)
/// while staying contained within the parent RectTransform bounds.
/// </summary>
public class BoidController : MonoBehaviour
{
    private static readonly List<BoidController> allBoids = new List<BoidController>();

    private RectTransform rectTransform;
    private RectTransform container;
    private Vector2 velocity;

    const float Speed = 90f;
    const float NeighborRadius = 250f;
    const float SeparationWeight = 2f;
    const float AlignmentWeight = 0.5f;
    const float CohesionWeight = 0.3f;
    const float ContainmentWeight = 5f;
    const float EdgeMargin = 60f;

    /// <summary>
    /// Sets the container this boid swims within and gives it a random initial velocity.
    /// Must be called after instantiation.
    /// </summary>
    public void Initialize(RectTransform container)
    {
        this.container = container;
        rectTransform = GetComponent<RectTransform>();
        velocity = Random.insideUnitCircle.normalized * Speed;
    }

    /// <summary> Registers this boid in the shared neighbor list used for flocking queries. </summary>
    void OnEnable() => allBoids.Add(this);
    /// <summary> Unregisters this boid so destroyed or disabled boids are excluded from flocking. </summary>
    void OnDisable() => allBoids.Remove(this);

    /// <summary>
    /// Applies boid flocking each frame: separation (avoid overlap), alignment (match neighbor
    /// velocity), and cohesion (drift toward group center), plus edge containment steering.
    /// Automatically pauses when the boid's parent is not the assigned container (e.g. during drag).
    /// </summary>
    void Update()
    {
        // Only move when parented to the boid container
        if (container == null || transform.parent != container) return;

        Vector2 pos = rectTransform.anchoredPosition;
        Vector2 sep = Vector2.zero;
        Vector2 align = Vector2.zero;
        Vector2 coh = Vector2.zero;
        int neighbors = 0;

        foreach (var other in allBoids)
        {
            if (other == this || other.transform.parent != container) continue;
            Vector2 offset = pos - other.rectTransform.anchoredPosition;
            float dist = offset.magnitude;
            if (dist > 0f && dist < NeighborRadius)
            {
                sep += offset / (dist * dist); // stronger repulsion when closer
                align += other.velocity;
                coh += other.rectTransform.anchoredPosition;
                neighbors++;
            }
        }

        Vector2 acceleration = Vector2.zero;
        if (neighbors > 0)
        {
            acceleration += sep.normalized * SeparationWeight;
            acceleration += ((align / neighbors).normalized - velocity.normalized) * AlignmentWeight;
            acceleration += ((coh / neighbors - pos).normalized) * CohesionWeight;
        }

        acceleration += GetContainmentSteer(pos);

        velocity += acceleration * Time.deltaTime * Speed;
        velocity = Vector2.ClampMagnitude(velocity, Speed);

        // Enforce a minimum speed so boids don't stall
        if (velocity.magnitude < Speed * 0.3f)
            velocity = velocity.normalized * Speed * 0.3f;

        pos += velocity * Time.deltaTime;

        // Hard clamp to bounds so nothing escapes
        Rect bounds = container.rect;
        Vector2 halfSize = rectTransform.rect.size * 0.5f;
        pos.x = Mathf.Clamp(pos.x, bounds.xMin + halfSize.x, bounds.xMax - halfSize.x);
        pos.y = Mathf.Clamp(pos.y, bounds.yMin + halfSize.y, bounds.yMax - halfSize.y);

        rectTransform.anchoredPosition = pos;
    }

    /// <summary>
    /// Returns a steering force that pushes boids away from container edges.
    /// Ramps up linearly as the boid enters the edge margin zone.
    /// </summary>
    private Vector2 GetContainmentSteer(Vector2 pos)
    {
        Rect bounds = container.rect;
        Vector2 halfSize = rectTransform.rect.size * 0.5f;
        Vector2 steer = Vector2.zero;

        float left = bounds.xMin + EdgeMargin + halfSize.x;
        float right = bounds.xMax - EdgeMargin - halfSize.x;
        float bottom = bounds.yMin + EdgeMargin + halfSize.y;
        float top = bounds.yMax - EdgeMargin - halfSize.y;

        if (pos.x < left) steer.x = (left - pos.x) / EdgeMargin;
        else if (pos.x > right) steer.x = (right - pos.x) / EdgeMargin;

        if (pos.y < bottom) steer.y = (bottom - pos.y) / EdgeMargin;
        else if (pos.y > top) steer.y = (top - pos.y) / EdgeMargin;

        return steer * ContainmentWeight;
    }
}
