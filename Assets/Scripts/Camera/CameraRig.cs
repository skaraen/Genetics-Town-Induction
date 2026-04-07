using System;
using UnityEngine;

public class CameraRig : MonoBehaviour
{
    bool focusing;
    Vector3 targetCamPos = Vector3.zero;
    Vector3 targetCamFocus = Vector3.zero;
    private Vector3 straightFocus, leftFocus;
    private Transform refTransform;
    private Vector3 camVelocity = Vector3.zero;

    void OnEnable()
    {
        focusing = false;
    }

    public void InitRig(Transform refTransform, float xOffset, float yOffset, float zOffset)
    {
        targetCamPos = transform.position;

        this.refTransform = refTransform;
        straightFocus = new Vector3(0, 0, -zOffset);
        leftFocus = new Vector3(-xOffset, 0, -zOffset);
    }

    public void MoveToLeft()
    {
        targetCamPos = leftFocus;
        targetCamFocus = new Vector3(leftFocus.x, leftFocus.y, 20);
        focusing = true;
    }

    public void MoveStraight()
    {
        targetCamPos = straightFocus;
        targetCamFocus = new Vector3(straightFocus.x, straightFocus.y, 20);
        focusing = true;
    }

    private void OffsetFocusFromLeft(float camDistance, double theta, out float xOffset, out float yOffset)
    {
        double phi = Math.Atan(Math.Abs(transform.position.y / transform.position.x));

        double distance = camDistance * Math.Sin(theta);
        float k = (float) Math.Tan(theta + phi);

        yOffset = (float) (distance / Math.Sqrt(1 + (k * k)));
        xOffset = yOffset * k;
    }

    private void OffsetFocusFromRight(float camDistance, double theta, out float xOffset, out float yOffset)
    {
        double alpha = theta + (Math.PI / 4);
        xOffset = (float) (camDistance * Math.Sin(theta) * Math.Cos(alpha));
        yOffset = (float) (camDistance * Math.Sin(theta) * Math.Sin(alpha));
    }

    private Vector3 GetOffset(float camDistance)
    {
        double theta = Math.Atan(Math.Abs(leftFocus.x / leftFocus.z));
        Vector3 offset;

        // OffsetFocusFromLeft(camDistance, theta, out float xOffset, out float yOffset);
        // offset = (yOffset * refTransform.forward) - (xOffset * refTransform.right);
        OffsetFocusFromRight(camDistance, theta, out float xOffset, out float yOffset);
        offset = - (yOffset * refTransform.forward) - (xOffset * refTransform.right);

        return offset;
    }

    public void FocusOnComponent(Transform obj)
    {
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        Bounds bounds = renderers[0].bounds;

        foreach (Renderer r in renderers)
            bounds.Encapsulate(r.bounds);

        FocusOnBounds(bounds);
    }

    public void FocusOnRenderer(Renderer renderer)
    {
        Bounds bounds = renderer.bounds;

        FocusOnBounds(bounds);
    }

    void FocusOnBounds(Bounds bounds)
    {
        Vector3 center = bounds.center;
        Debug.Log($"Bounds: {center}");

        float camDistance = bounds.size.magnitude * 1.5f;
        float camCoords = (float)(camDistance / Math.Sqrt(2));

        // targetCamPos = center - refTransform.forward * camCoords - refTransform.right * camCoords;
        targetCamPos = center - refTransform.forward * camCoords + refTransform.right * camCoords;

        targetCamFocus = center + GetOffset(camDistance);
        focusing = true;
    }

    void LateUpdate()
    {
        if (!focusing) return;

        transform.position = Vector3.SmoothDamp(transform.position, targetCamPos, ref camVelocity, 0.3f);

        Quaternion targetRot = Quaternion.LookRotation(targetCamFocus - transform.position);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 5f);

        float posError = Vector3.Distance(transform.position, targetCamPos);
        float rotError = Quaternion.Angle(transform.rotation, targetRot);

        if (posError < 0.01f && rotError < 0.5f)
        {
            transform.position = targetCamPos;
            transform.rotation = targetRot;
            focusing = false;
        }
    }
}
