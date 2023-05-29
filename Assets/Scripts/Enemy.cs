using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Dissolver))]
[RequireComponent(typeof(Animator))]
public class Enemy : MonoBehaviour
{
    [Header("Damage")]
    [SerializeField] int maxHealth = 100;
    [SerializeField] ParticleSystem attackEffect;

    int currentHealth;
    Animator animator;
    protected bool tookDamage = false;
    protected Vector2 damageDirection = Vector2.left;
    protected bool dead = false;
    ParticleSystem[] damageEffects;

    // Start is called before the first frame update
    protected void Start()
    {
        currentHealth = maxHealth;
        animator = GetComponent<Animator>();
        damageEffects = GetComponentsInChildren<ParticleSystem>();
    }

    public void TakeDamage(int damage, float direction)
    {
        currentHealth -= damage;
        tookDamage = true;
        //attackEffect.Play();
        PlayDamageEffects();
        damageDirection = direction > 0 ? Vector2.right : Vector2.left;
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        dead = true;
        this.GetComponent<Dissolver>()?.Dissolve(() =>
        {
            Destroy(this.gameObject);
        });
    }

    private void PlayDamageEffects()
    {
        foreach (ParticleSystem effect in damageEffects)
        {
            if (effect.tag == "Damage Effect")
            {
                effect.Play();
            }
        }
    }
}
