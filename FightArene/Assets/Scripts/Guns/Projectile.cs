using Unity.Netcode;
using UnityEngine;
using Cysharp.Threading.Tasks;
using PrimeTween;

public class Projectile : NetworkBehaviour
{
    [Header("Projectile Settings")]
    [SerializeField] private float damage = 10f;
    [SerializeField] private float range = 50f;
    [SerializeField] private float projectileSpeed = 20f;
    [SerializeField] private float arcHeight = 5f; // Parabolik yay yüksekliği
    [SerializeField] private LayerMask hitLayers;
    
    [Header("Visual")]
    [SerializeField] private GameObject impactEffect;
    [SerializeField] private TrailRenderer trailRenderer;
    
    private Vector3 startPosition;
    private Vector3 targetPosition;
    private float travelDistance;
    private bool isInitialized = false;
    private Tween movementTween;
    private Vector3 lastCheckPosition;
    
    /// <summary>
    /// Gun scriptinden çağrılacak initialize fonksiyonu
    /// </summary>
    public void Initialize(Vector3 startPos, Vector3 targetPos, float damageAmount, float maxRange)
    {
        if (!IsServer && !IsHost)
            return;
            
        startPosition = startPos;
        targetPosition = targetPos;
        damage = damageAmount;
        range = maxRange;
        
        transform.position = startPosition;
        
        // Hedef mesafesini hesapla
        travelDistance = Vector3.Distance(startPosition, targetPosition);
        
        // Range kontrolü
        if (travelDistance > range)
        {
            // Hedef çok uzaksa, range'e göre yeni hedef belirle
            Vector3 direction = (targetPosition - startPosition).normalized;
            targetPosition = startPosition + direction * range;
            travelDistance = range;
        }
        
        isInitialized = true;
        
        // Parabolik atışı başlat
        LaunchProjectile().Forget();
    }
    
    private async UniTaskVoid LaunchProjectile()
    {
        if (!IsServer && !IsHost)
            return;
            
        // Uçuş süresi hesapla
        float duration = travelDistance / projectileSpeed;
        
        // Parabolik yol için ara noktalar oluştur
        Vector3 midPoint = (startPosition + targetPosition) / 2f;
        midPoint.y += arcHeight; // Y ekseninde yükseklik ekle
        
        // PrimeTween ile parabolik hareket
        var sequence = Sequence.Create();
        
        // Ana hareket tweeni - Bezier curve ile parabolik yol
        movementTween = Tween.Custom(0f, 1f, duration, onValueChange: t =>
        {
            if (this == null || gameObject == null)
                return;
                
            // Quadratic Bezier curve ile parabolik yol
            Vector3 position = CalculateBezierPoint(t, startPosition, midPoint, targetPosition);
            transform.position = position;
            
            // Projektili hareket yönüne doğru döndür
            if (t < 0.99f)
            {
                Vector3 nextPos = CalculateBezierPoint(t + 0.01f, startPosition, midPoint, targetPosition);
                Vector3 direction = (nextPos - position).normalized;
                if (direction != Vector3.zero)
                {
                    transform.rotation = Quaternion.LookRotation(direction);
                }
            }
            
            CheckCollisionAlongPath(position);
        });
        
        await movementTween.ToUniTask();
        
        if (this != null && gameObject != null)
        {
            DeactivateProjectile();
        }
    }
    
    private Vector3 CalculateBezierPoint(float t, Vector3 p0, Vector3 p1, Vector3 p2)
    {
        // Quadratic Bezier curve formülü
        float u = 1 - t;
        float tt = t * t;
        float uu = u * u;
        
        Vector3 point = uu * p0; // (1-t)^2 * P0
        point += 2 * u * t * p1; // 2(1-t)t * P1
        point += tt * p2; // t^2 * P2
        
        return point;
    }
    
    private void CheckCollisionAlongPath(Vector3 currentPosition)
    {
        if (lastCheckPosition == Vector3.zero)
        {
            lastCheckPosition = currentPosition;
            return;
        }
        
        Vector3 direction = currentPosition - lastCheckPosition;
        float distance = direction.magnitude;
        
        if (distance > 0.01f)
        {
            if (Physics.Raycast(lastCheckPosition, direction.normalized, out RaycastHit hit, distance, hitLayers))
            {
                OnHit(hit);
            }
        }
        
        lastCheckPosition = currentPosition;
    }
    
    private void OnHit(RaycastHit hit)
    {
        if (!IsServer && !IsHost)
            return;
            
        if (hit.collider.TryGetComponent<IDamageable>(out var damageable))
        {
            damageable.TakeDamage(damage);
        }
        
        if (impactEffect != null)
        {
            EffectManager.Instance.ShowImpactEffectClientRpc(hit.point, hit.normal);
        }
        
        DeactivateProjectile();
    }
    
   
    
    private void DeactivateProjectile()
    {
        if (!IsServer && !IsHost)
            return;
            
        if (movementTween.isAlive)
        {
            movementTween.Complete();
        }
        
        if (trailRenderer != null)
        {
            trailRenderer.Clear();
        }
        
        if (NetworkObject != null && NetworkObject.IsSpawned)
        {
            NetworkObject.Despawn();
        }
        
        Destroy(gameObject);
    }
    
    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        
        if (movementTween.isAlive)
        {
            movementTween.Stop();
        }
    }
    
    public float GetDamage() => damage;
    public float GetRange() => range;
    public void SetDamage(float value) => damage = value;
    public void SetRange(float value) => range = value;
}

