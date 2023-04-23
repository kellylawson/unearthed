using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Dissolver : MonoBehaviour
{
    [SerializeField] float fade = 1f;
    bool isDissolving = false;
    Action dissolvedCallback;

    // Update is called once per frame
    void Update()
    {
        if (isDissolving)
        {
            fade -= Time.deltaTime;

            if (fade <= 0f)
            {
                fade = 0f;
                isDissolving = false;
                if (dissolvedCallback != null)
                {
                    dissolvedCallback();
                }
            }

            SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>();
            foreach (SpriteRenderer renderer in renderers)
            {
                renderer.material?.SetFloat("_Fade", fade);
            }
        }
    }

    public void Dissolve(Action callback)
    {
        isDissolving = true;
        dissolvedCallback = callback;
    }
}
