using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveBackground : MonoBehaviour
{
    float startPosition;
    private Renderer spriteRenderer;
    [SerializeField] private float parallaxEffect;

    void Start()
    {
        startPosition = transform.position.x;
        spriteRenderer = GetComponent<Renderer>();
    }

    void FixedUpdate()
    {
        float distance = Camera.main.transform.position.x * parallaxEffect;
        // Debug.Log($"Background Layer Order {spriteRenderer?.sortingOrder} Camera X: {followCamera?.transform.position.x}, distance: {distance}, start position: {startPosition}");
        transform.position = new Vector3(startPosition + distance, transform.position.y, transform.position.z);
    }
}
