using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Utilities
{
    public static class UITweenExtensions
    {
        public static async UniTask TweenColorAsync(this Image img, Color target, float duration, CancellationToken ct = default, bool useUnscaledTime = false)
        {
            if (img == null) return;
            if (duration <= 0f)
            {
                img.color = target;
                return;
            }

            Color start = img.color;
            float elapsed = 0f;

            try
            {
                while (elapsed < duration)
                {
                    ct.ThrowIfCancellationRequested();
                    float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                    elapsed += dt;
                    float t = Mathf.Clamp01(elapsed / duration);
                    img.color = Color.Lerp(start, target, t);
                    await UniTask.Yield(ct);
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }

            img.color = target;
        }
    
    
    
    }
}