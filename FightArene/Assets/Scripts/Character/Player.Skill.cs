using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Unity.Netcode;
using UnityEngine.UI;
using UnityEngine;
using UITweenExtensions = Utilities.UITweenExtensions;
using Debug = Utilities.Debug;


namespace Character
{
    public partial class Player
    {
        public NetworkVariable<bool> hasArmor = new NetworkVariable<bool>(false);
        public NetworkVariable<bool> hasMagnet = new NetworkVariable<bool>(false);
        
        // UI referanslarını cached field'lar olarak tanımla
        private Image _armorImage;
        private Image _armorTimer;
        private Image _armorBackground;
        
        private Image _magnetImage;
        private Image _magnetTimer;
        private Image _magnetBackground;
        
        private bool isAvailableArmor;
        private bool isAvailableMagnet;

        private int _coolDown = 5;
        
        private int _selectedSkillIndex = 0;
        private CancellationTokenSource _magnetTweenCts;
        private CancellationTokenSource _armorTweenCts;
        
        void InitSkills()
        {
            // UI referanslarını InitUI'dan al
            if (PlayerRequirements.Instance != null)
            {
                _armorImage = PlayerRequirements.Instance.armorImage;
                _armorTimer = PlayerRequirements.Instance.armorTimer;
                _armorBackground = PlayerRequirements.Instance.armorBackground;
                _magnetImage = PlayerRequirements.Instance.magnetImage;
                _magnetTimer = PlayerRequirements.Instance.magnetTimer;
                _magnetBackground = PlayerRequirements.Instance.magnetBackground;
            }
            else
            {
                Debug.LogError("PlayerRequirements.Instance is NULL in InitSkills!");
            }
            
            hasArmor.OnValueChanged += OnArmorValueChanged;
            hasMagnet.OnValueChanged += OnMagnetValueChanged;
        }


        private void OnMagnetValueChanged(bool previousValue, bool newValue)
        {
            Debug.Log($"OnMagnetValueChanged called! IsOwner: {IsOwner}, Previous: {previousValue}, New: {newValue}");
            
            if (!IsOwner) return;
            
            Debug.Log("OnMagnetValueChanged - Owner check passed!");
            
            _magnetTweenCts?.Cancel();
            _magnetTweenCts?.Dispose();
            _magnetTweenCts = null;

            if (_magnetImage == null)
            {
                Debug.LogError("_magnetImage is NULL! PlayerRequirements UI not assigned!");
                return;
            }

            Debug.Log($"Updating magnet UI to: {newValue}");

            _magnetTweenCts = new CancellationTokenSource();

            Color target = newValue
                ? new Color(1f, 1f, 1f, 1f)
                : new Color(1f, 1f, 1f, 0.3137255f);
            
            isAvailableMagnet = newValue;
            
            UITweenExtensions.TweenColorAsync(_magnetImage, target, 0.5f, _magnetTweenCts.Token).Forget();
        }


        private void OnArmorValueChanged(bool previousValue, bool newValue)
        {
            Debug.Log($"OnArmorValueChanged called! IsOwner: {IsOwner}, Previous: {previousValue}, New: {newValue}");
            
            if (!IsOwner) return;
            
            Debug.Log("OnArmorValueChanged - Owner check passed!");
            
            _armorTweenCts?.Cancel();
            _armorTweenCts?.Dispose();
            _armorTweenCts = null;

            if (_armorImage == null)
            {
                Debug.LogError("_armorImage is NULL! PlayerRequirements UI not assigned!");
                return;
            }

            Debug.Log($"Updating armor UI to: {newValue}");

            _armorTweenCts = new CancellationTokenSource();

            Color target = newValue
                ? new Color(1f, 1f, 1f, 1f)
                : new Color(1f, 1f, 1f, 0.3137255f);
            
            isAvailableArmor = newValue;
            
            UITweenExtensions.TweenColorAsync(_armorImage, target, 0.5f, _armorTweenCts.Token).Forget();
        }


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
            if (!isAvailableArmor) return;
            _selectedSkillIndex = 0;
            Debug.Log("Armor skill selected");
            StartBackgroundFlash(_armorBackground);
        }

        private void HandleMagnetSelected()
        {
            if (!isAvailableMagnet) return;
            _selectedSkillIndex = 1;
            Debug.Log("Magnet skill selected");
            StartBackgroundFlash(_magnetBackground);
        }

                
        private async UniTaskVoid ResetSkillsAsync(int skillIndex, float cooldownDuration)
        {
            if (skillIndex == 0) isAvailableArmor = false;
            else if (skillIndex == 1) isAvailableMagnet = false;

            Image timerBg = skillIndex == 0 ? _armorTimer : _magnetTimer;
            if (timerBg != null)
            {
                timerBg.enabled = true;
                timerBg.fillAmount = 0f;
                float elapsed = 0f;

                while (elapsed < cooldownDuration)
                {
                    elapsed += Time.deltaTime;
                    timerBg.fillAmount = Mathf.Clamp01(elapsed / cooldownDuration);
                    await UniTask.Yield();
                }

                timerBg.fillAmount = 1f;
                timerBg.enabled = false;
            }
            else
            {
                await UniTask.Delay(TimeSpan.FromSeconds(cooldownDuration));
            }

            if (skillIndex == 0) isAvailableArmor = true;
            else if (skillIndex == 1) isAvailableMagnet = true;
        }

    }
}
