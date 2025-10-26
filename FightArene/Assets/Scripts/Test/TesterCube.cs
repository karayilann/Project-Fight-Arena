using UnityEngine;

public class TesterCube : MonoBehaviour,IDamageable
{
    public float health = 100f;

    public void TakeDamage(float damage)
    {
        health -= damage;
        Debug.Log($"TesterCube took {damage} damage. Current health: {health}");
    }
}
