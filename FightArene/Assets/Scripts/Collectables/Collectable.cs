using System;
using UnityEngine;
using Unity.Netcode;
using Debug = Utilities.Debug;
using System.Threading;
using Cysharp.Threading.Tasks;

public class Collectable : PooledNetworkObject
{
    public PoolObjectType type;
    public float groundStayDuration = 3f;

    private bool _grounded = false;
    private CancellationTokenSource _groundCts;

    public override void OnSpawnFromPool()
    {
        gameObject.SetActive(true);
        _grounded = false;

        if (_groundCts != null)
        {
            _groundCts.Cancel();
            _groundCts.Dispose();
            _groundCts = null;
        }
    }

    public override void OnReturnToPool()
    {
        if (_groundCts != null)
        {
            _groundCts.Cancel();
            _groundCts.Dispose();
            _groundCts = null;
        }

        _grounded = false;
        gameObject.SetActive(false);
    }

    private void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.CompareTag("Ground") && !_grounded)
        {
            Debug.Log("Collectable hit the ground, starting ground hit timer...");
            _grounded = true;

            _groundCts = new CancellationTokenSource();
            HandleGroundHitAsync(_groundCts.Token).Forget();
        }
    }

    private async UniTaskVoid HandleGroundHitAsync(CancellationToken token)
    {
        try
        {
            await UniTask.Delay((int)(groundStayDuration * 1000), cancellationToken: token);

            if (token.IsCancellationRequested) return;

            if (!IsSpawned)
            {
                Debug.Log("Collectable is no longer spawned, skipping ground hit handling");
                return;
            }

            if (IsServer)
            {
                ReturnToPool();
            }
            else
            {
                NotifyGroundHitServerRpc();
            }
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            if (_groundCts != null)
            {
                _groundCts.Dispose();
                _groundCts = null;
            }
        }
    }


    [ServerRpc(RequireOwnership = false)]
    private void NotifyGroundHitServerRpc(ServerRpcParams rpcParams = default)
    {
        if (IsServer)
        {
            ReturnToPool();
        }
    }
}