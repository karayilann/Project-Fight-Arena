using Unity.Netcode;
using UnityEngine;
using Cysharp.Threading.Tasks;
using PrimeTween;
using Debug = Utilities.Debug;

public class Projectile : NetworkBehaviour
{
    [Header("Projectile Settings")]
    [SerializeField] private float damage = 10f;
    [SerializeField] private float range = 50f;
    [SerializeField] private float projectileSpeed = 20f;
    [SerializeField] private float arcHeight = 5f;

    [Header("Visual")]
    [SerializeField] private GameObject impactEffect;
    [SerializeField] private TrailRenderer trailRenderer;

    private Vector3 _startPosition;
    private Vector3 _targetPosition;
    private float _travelDistance;
    private bool _isInitialized;
    private Tween _movementTween;
    private Vector3 _lastCheckPosition;
    private bool _hitOccurred;


    public void Initialize(Vector3 startPos, Vector3 targetPos)
    {
        if (!IsServer && !IsHost)
            return;

        _startPosition = startPos;
        _targetPosition = targetPos;

        transform.position = _startPosition;

        _travelDistance = Vector3.Distance(_startPosition, _targetPosition);

        if (_travelDistance > range)
        {
            Vector3 direction = (_targetPosition - _startPosition).normalized;
            _targetPosition = _startPosition + direction * range;
            _travelDistance = range;
        }

        _isInitialized = true;
        _hitOccurred = false;

        LaunchProjectile().Forget();
    }

    private async UniTaskVoid LaunchProjectile()
    {
        if (!IsServer && !IsHost)
            return;

        float duration = _travelDistance / projectileSpeed;

        Vector3 midPoint = (_startPosition + _targetPosition) / 2f;
        midPoint.y += arcHeight;

        _movementTween = Tween.Custom(0f, 1f, duration, onValueChange: t =>
        {
            if (this == null || gameObject == null || _hitOccurred)
                return;

            Vector3 position = CalculateBezierPoint(t, _startPosition, midPoint, _targetPosition);
            transform.position = position;

            if (t < 0.99f)
            {
                Vector3 nextPos = CalculateBezierPoint(t + 0.01f, _startPosition, midPoint, _targetPosition);
                Vector3 direction = (nextPos - position).normalized;
                if (direction != Vector3.zero)
                {
                    transform.rotation = Quaternion.LookRotation(direction);
                }
            }
        });

        await _movementTween.ToUniTask();

        if (this != null && gameObject != null && !_hitOccurred)
        {
            DeactivateProjectile();
        }
    }

    private Vector3 CalculateBezierPoint(float t, Vector3 p0, Vector3 p1, Vector3 p2)
    {
        float u = 1 - t;
        float tt = t * t;
        float uu = u * u;

        Vector3 point = uu * p0;
        point += 2 * u * t * p1;
        point += tt * p2;

        return point;
    }

    private void OnCollisionEnter(Collision other)
    {
        if (_hitOccurred)
            return;

        if (other.collider.TryGetComponent<IDamageable>(out var damageable))
        {
            damageable.TakeDamage(damage);
            Debug.Log("Projectile hit " + other.collider.name + " for " + damage + " damage.");
        }

        if (impactEffect != null)
        {
            EffectManager.Instance.ShowImpactEffectClientRpc(other.contacts[0].point, other.contacts[0].normal);
        }

        DeactivateProjectile();
    }
    
    private void DeactivateProjectile()
    {
        if (!IsServer && !IsHost)
            return;

        if (_movementTween.isAlive)
        {
            _movementTween.Stop();
        }

        if (trailRenderer != null)
        {
            trailRenderer.Clear();
        }

        if (NetworkObject != null && NetworkObject.IsSpawned)
        {
            NetworkObject.Despawn(false);
            Destroy(gameObject, 0.1f);
        }
        else if(gameObject != null)
        {
            Destroy(gameObject);
        }
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

        if (_movementTween.isAlive)
        {
            _movementTween.Stop();
        }
    }
}
