// csharp

using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Unity.Netcode;
using UnityEngine.UI;
using UnityEngine;

namespace Character
{
    public partial class Player
    {
        private NetworkVariable<bool> hasArmor = new NetworkVariable<bool>(false);
        private NetworkVariable<bool> hasMagnet = new NetworkVariable<bool>(false);
        
        private Image armorImage => PlayerRequirements.Instance.armorImage;
        private Image armorTimer => PlayerRequirements.Instance.armorTimer;
        private Image armorBackground => PlayerRequirements.Instance.armorBackground;
        
        private Image magnetImage => PlayerRequirements.Instance.magnetImage;
        private Image magnetTimer => PlayerRequirements.Instance.magnetTimer;
        private Image magnetBackground => PlayerRequirements.Instance.magnetBackground;
        
        private int _selectedSkillIndex = 0;
        
        
        private readonly Dictionary<Image, CancellationTokenSource> _backgroundFlashCts = new();

        private void StartBackgroundFlash(Image img)
        {
            if (img == null) return;

            if (_backgroundFlashCts.TryGetValue(img, out var oldCts))
            {
                oldCts.Cancel();
                oldCts.Dispose();
            }

            var cts = new CancellationTokenSource();
            _backgroundFlashCts[img] = cts;
            FlashBackgroundAsync(img, 0.12f, 0.12f, cts.Token).Forget();
        }

        private async UniTaskVoid FlashBackgroundAsync(Image img, float toDuration, float backDuration, CancellationToken ct)
        {
            try
            {
                if (img == null) return;

                Color start = img.color;
                float a = start.a;
                Color peak = new Color(1f, 1f, 1f, a);

                float t = 0f;
                while (t < toDuration)
                {
                    ct.ThrowIfCancellationRequested();
                    t += Time.deltaTime;
                    img.color = Color.Lerp(start, peak, Mathf.Clamp01(t / toDuration));
                    await UniTask.Yield(ct);
                }

                t = 0f;
                while (t < backDuration)
                {
                    ct.ThrowIfCancellationRequested();
                    t += Time.deltaTime;
                    img.color = Color.Lerp(peak, start, Mathf.Clamp01(t / backDuration));
                    await UniTask.Yield(ct);
                }

                img.color = start;
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                if (_backgroundFlashCts.TryGetValue(img, out var stored))
                {
                    stored.Dispose();
                    _backgroundFlashCts.Remove(img);
                }
            }
        }

        private void HandleArmorSelected()
        {
            _selectedSkillIndex = 0;
            Debug.Log("Armor skill selected");
            StartBackgroundFlash(armorBackground);
        }

        private void HandleMagnetSelected()
        {
            _selectedSkillIndex = 1;
            Debug.Log("Magnet skill selected");
            StartBackgroundFlash(magnetBackground);
        }
    }
}
