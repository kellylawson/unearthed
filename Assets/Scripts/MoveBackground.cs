using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveBackground : MonoBehaviour
{
    Vector2 startPosition;
    private Renderer spriteRenderer;
    [SerializeField] private float parallaxEffectX = 0f;
    [SerializeField] private float parallaxEffectY = 0f;

    void Start()
    {
        startPosition = transform.position;
        spriteRenderer = GetComponent<Renderer>();
    }

    void FixedUpdate()
    {
        float distanceX = Camera.main.transform.position.x * parallaxEffectX;
        float distanceY = Camera.main.transform.position.y * parallaxEffectY;
        transform.position = new Vector3(startPosition.x + distanceX, startPosition.y + distanceY, transform.position.z);
    }
}
