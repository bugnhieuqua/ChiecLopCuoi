using UnityEngine;

/// <summary>
/// Health and destruction behavior for a base statue.
/// </summary>
public class StatueController : MonoBehaviour
{
    [SerializeField] private int maxHealth = 100;

    private Animator animator;
    private Collider2D[] colliders;
    private int currentHealth;
    private bool isDestroyed;
    private bool initialized;

    public int CurrentHealth
    {
        get
        {
            EnsureInitialized();
            return currentHealth;
        }
    }
    public bool IsDestroyed => isDestroyed;

    private void Awake()
    {
        EnsureInitialized();
    }

    private void EnsureInitialized()
    {
        if (initialized)
            return;

        animator = GetComponent<Animator>();
        colliders = GetComponents<Collider2D>();
        currentHealth = Mathf.Max(1, maxHealth);
        initialized = true;
    }

    public void TakeDamage(int dmg)
    {
        EnsureInitialized();
        if (isDestroyed || dmg <= 0)
            return;

        currentHealth = Mathf.Max(0, currentHealth - dmg);
        if (currentHealth > 0)
            return;

        isDestroyed = true;
        if (animator != null)
            animator.SetBool("IsDestroyed", true);

        foreach (Collider2D statueCollider in colliders)
        {
            if (statueCollider != null)
                statueCollider.enabled = false;
        }
    }
}
