using System.Collections.Generic;
using UnityEngine;

public class GrabAndThrow : MonoBehaviour
{
    private bool isDragging = false;
    private Vector3 offset;

    private Rigidbody rb;
    private Vector3 startPosition;

    private List<Vector3> mousePositions = new List<Vector3>();
    private const int maxMousePositions = 50;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        startPosition = transform.position;
    }

    private void OnMouseDown()
    {
        isDragging = true;
        offset = transform.position - Camera.main.ScreenToWorldPoint(Input.mousePosition);

        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        // Clear the mousePositions list when dragging starts
        mousePositions.Clear();
    }

    private void OnMouseUp()
    {
        isDragging = false;
        Vector3 throwVelocity = CalculateThrowVelocity();
        rb.velocity = throwVelocity;
    }


    //The best way to address the issue of physics latency is to create a custom NetworkTransform with a custom physics-based interpolator. 
    //You can also use the Network Simulator tool to spot issues with latency.
    private Vector3 CalculateThrowVelocity()
    {
        if (mousePositions.Count < 2)
        {
            return Vector3.zero;
        }

        Vector3 dragVector = Vector3.zero;

        for (int i = 1; i < mousePositions.Count; i++)
        {
            dragVector += mousePositions[i] - mousePositions[i - 1];
        }

        // Calculate the average dragVector over the last 10 positions
        dragVector /= mousePositions.Count - 1;

        dragVector *= 300;

        return dragVector;
    }

    private void Update()
    {
        if (isDragging)
        {
            Vector3 cursorWorldPoint = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, -Camera.main.transform.position.z));
            transform.position = cursorWorldPoint;

            // Add the current mouse position to the list
            mousePositions.Add(cursorWorldPoint);

            // Remove old positions if the list exceeds the maximum count
            if (mousePositions.Count > maxMousePositions)
            {
                mousePositions.RemoveAt(0);
            }
        }
    }

    public void ResetCube()
    {
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        transform.position = startPosition;
    }
}