Project Fight Arena - README

English (Turkish explanation below :))
-------

Overview

Project Fight Arena is a small, host-based LAN multiplayer arena shooter built with Unity and Unity Netcode (Netcode for GameObjects). The game uses a host-authoritative model where one instance acts as server (host) and others connect as clients. The core gameplay revolves around collecting items, using skills (like magnet and armor), and fighting other players in short arena rounds. This game was made in 1 week as a weekly project at the university.

How to play

- Start a Host to run the server and a client locally, or run separate builds on different machines on the same LAN.
- Move your player with standard controls and pick up collectables to gain ammo, armor, or temporary abilities (for example: magnet).
- Some items are instant (armor, magnet pickup) while others increment the ammo/collectable count.

Project layout (relevant parts)

- Assets/: Unity asset folder used at runtime and authoring.
- Assets/Scripts/Character/: Player related scripts including inventory, combat, and input handlers (e.g., `Player.Inventory.cs`, `Player.Combat.cs`, `PlayerInputHandler.cs`).
- Assets/Scripts/Core/Pooling/: Network object pooling and spawner code (e.g. `NetworkObjectPool.cs`, the project's ObjectSpawner logic).
- Assets/Scripts/Utilities/Debug.cs: Centralized debug wrapper used across the project for consistent logging and easy toggling of debug output.

Networking model

- Host-based LAN: a host instance is authoritative for gameplay state. The game uses Unity Netcode NetworkVariables and ServerRpc calls to synchronize important state (health, collectable counts, powerups).
- Pickup flow: clients request pickups via a ServerRpc which validates and applies the pickup on the server-side to maintain authority. This prevents cheating and ensures consistent state.

Optimization techniques used and developer notes

1) Network object pooling (ObjectSpawner / NetworkObjectPool)
- Instead of Instantiating/Destroying networked prefabs frequently, the project uses a pooled approach: objects are reused which reduces GC pressure and network spawn/despawn spikes.
- Relevant file: `Assets/Scripts/Core/Pooling/NetworkObjectPool.cs` (see repository Assets for implementation and usage examples).

2) Minimized and aggregated network traffic
- Use NetworkVariables for frequently-read small state (like health and collectableCount) and ServerRpc only for important events (pickup requests, special ability spawns).
- Example: `collectableCount` is a NetworkVariable<int> so UI updates only when the value changes; pickup is requested with a ServerRpc (`RequestPickupServerRpc`) and validated server-side.

3) Centralized debug wrapper (Utilities.Debug)
- `Assets/Scripts/Utilities/Debug.cs` wraps UnityEngine.Debug and allows toggling, filtering, or formatting logs consistently. By centralizing logging you can disable or reduce log volume easily for builds which helps performance.
- Recommendation: keep detailed logs for development and reduce log verbosity for release builds (or gated by a runtime flag) to avoid CPU/time spent logging.

4) Collision and detection optimizations
- Prefer physics overlap queries (Physics.OverlapSphere with LayerMask) for AoE-like skills (magnet) instead of per-frame scans over all objects.
- Use LayerMask to limit checks only to relevant layers so the magnet only sees the intended collectable objects.
- Avoid expensive GetComponent calls in hot loops — cache component references where possible.

5) UI and owner-only updates
- Only the owner updates their UI (e.g., ammo text) when NetworkVariable changes. This reduces unnecessary UI work for non-owning clients.

Magnet skill design notes (how it should behave)

- The magnet is a temporary effect spawned server-side (host-authoritative). When active, it attracts collectables of the player's allowed types (`player.collectableTypes`) within a configurable radius.
- Attraction should only affect collectable objects on a specific LayerMask (e.g., `CollectableLayer`) for performance and correctness.
- Authority and pickup handling:
  - The server should be the authority for moving/despawning collectables. If attraction movement is simulated locally on a client for visual responsiveness, the client should still request pickup via RPC to the server and the server must validate and finalize the pickup.
  - For simple games, performing attraction and then calling the same pickup ServerRpc used by collisions is acceptable. For smoother visuals consider client-side interpolation and server reconciliation.

Implementation hints (files to inspect)

- `Assets/Scripts/Character/Player.Inventory.cs` — shows the pickup RPC flow (`RequestPickupServerRpc`) and how collectableCount and special flags (hasArmor, hasMagnet) are applied. This is the authoritative path to apply pickups.
- `Assets/Scripts/Core/Pooling/NetworkObjectPool.cs` — pooling and spawn/despawn helpers used throughout the project for collectables and skills (magnet spawn/spawn timing).
- `Assets/Scripts/Utilities/Debug.cs` — helpful when debugging networked behavior because you can easily control log output levels.

Common pitfalls / debugging tips

- Owner vs server logic: UI updates should be gated with `IsOwner` checks. Server-only application of state should use ServerRpc/NetworkVariable to avoid divergent state.
- LayerMask usage: Ensure collectables and magnet queries are on the same layer and that physics queries use an explicit LayerMask to avoid extra costs.
- RPC flow: When a client is not the server, pickups must be requested via a ServerRpc. The server must validate the network object id, type and distance before granting a pickup.
- Pooled objects: After calling `.ReturnToPool()` ensure that the object's NetworkObject is properly despawned on the server and recycled.

Repository references

- Main repository: https://github.com/karayilann/Project-Fight-Arena
- Assets folder reference: https://github.com/karayilann/Project-Fight-Arena/tree/main/FightArene/Assets

Turkish (Türkçe)
-----------------

Genel Bakış

Project Fight Arena, Unity ve Unity Netcode ile yazılmış, host-tabanlı bir LAN çok oyunculu arena nişancı oyunudur. Oyun host-authoritative modelini kullanır: bir oyun örneği sunucu (host) olarak davranır, diğerleri istemci olarak bağlanır. Oyunun ana oynanışı, nesneleri toplamak, yetenekler (magnet, armor vb.) kullanmak ve kısa arena maçlarında diğer oyuncularla savaşmaktır. Bu oyun üniversitede haftalık oyun projesi olarak 1 haftada yapılmıştır

Nasıl oynanır

- Host başlatarak hem server hem client'ı tek makinede çalıştırabilir veya farklı makinelerdeki build'leri aynı LAN'da çalıştırabilirsiniz.
- Oyuncunuzu hareket ettirin, collectable toplamak için etkileşime girin; bazı eşyalar anında etkinleşir (armor, magnet), bazıları ise sayıyı (ammo/collectable) artırır.

Proje yapısı (ilgili kısımlar)

- Assets/: Unity varlıkları ve scriptler.
- Assets/Scripts/Character/: Inventory, Combat, Input handler gibi oyuncu ile ilgili scriptler (`Player.Inventory.cs`, `Player.Combat.cs`, `PlayerInputHandler.cs`).
- Assets/Scripts/Core/Pooling/: Network object pooling/spawner kodu (`NetworkObjectPool.cs`, ObjectSpawner mantığı burada yer alır).
- Assets/Scripts/Utilities/Debug.cs: Proje genelinde kullanılan merkezi debug wrapper.

Ağ modeli

- Host-based LAN: Host instance oyun durumunda otoritedir. Unity Netcode NetworkVariables ve ServerRpc'lar ile önemli durumlar (can, ammo, powerup'lar) senkronize edilir.
- Pickup akışı: İstemciler pickup için ServerRpc gönderir; sunucu doğrulama yapar ve pickup'ı uygular — bu hileleri önler ve tutarlı durum sağlar.

Kullanılan optimizasyon teknikleri ve notlar

1) Network nesne havuzu (ObjectSpawner / NetworkObjectPool)
- Ağ nesnelerini sık sık instantiate/destroy etmek yerine havuzlama kullanılır; bu GC baskısını ve spawn/despawn anındaki ağ yükünü azaltır.
- İlgili dosya: `Assets/Scripts/Core/Pooling/NetworkObjectPool.cs`.

2) Ağ trafiğini azaltma
- Sık okunan küçük durumlar NetworkVariable ile paylaşılır; önemli olaylar için sadece gerekli RPC'lar çağrılır.
- Örnek: `collectableCount` bir NetworkVariable<int> olduğu için UI yalnızca değişikliklerde güncellenir; pickup isteği ServerRpc ile yapılır ve sunucuda doğrulanır.

3) Merkezi debug (Utilities.Debug)
- `Assets/Scripts/Utilities/Debug.cs`, UnityEngine.Debug üzerinde sarmalayıcı sağlar ve loglama hassasiyetini kolayca değiştirmenize olanak tanır. Geliştirme sırasında loglar açık, release'te azaltılabilir.

4) Çarpışma ve algılama optimizasyonları
- AoE benzeri yetenekler (magnet) için Physics.OverlapSphere + LayerMask tercih edin. Sadece ilgili katmanları taramak maliyeti düşürür.
- Hot-path'lerde GetComponent çağrılarını önlemek için referanslar önbelleğe alınmalı.

5) UI ve owner-only güncellemeler
- Sadece owner, NetworkVariable değişikliklerinde UI'yı güncellemelidir (ör: cephane/health UI), bu da fazladan iş yapılmasını önler.

Magnet yeteneği için tasarım notları

- Magnet, server tarafında (host authoritative) oluşturulacak geçici bir etkidir. Aktif olduğunda oyuncunun izin verilen türdeki (`player.collectableTypes`) collectable'larını belirli bir yarıçap içinden çeker.
- Yalnızca belirli bir LayerMask üzerinde çalışmalı (ör: `CollectableLayer`) — performans ve doğruluk için önemlidir.
- Yetki akışı:
  - Sunucu, collectable'ların hareketi ve despawn'ı konusunda otoritedir. Eğer istemcide görsel olarak çekme simülasyonu yapılırsa bile pickup yetkilendirmesi ServerRpc ile sunucuda yapılmalıdır.

İncelenmesi gereken dosyalar

- `Assets/Scripts/Character/Player.Inventory.cs` — pickup RPC akışı (`RequestPickupServerRpc`) ve `collectableCount`/hasArmor/hasMagnet gibi durumların uygulandığı yer.
- `Assets/Scripts/Core/Pooling/NetworkObjectPool.cs` — pooling ve spawn/despawn yardımcıları.
- `Assets/Scripts/Utilities/Debug.cs` — ağ davranışlarını debug ederken faydalı merkezi loglama.

Hata ayıklama / yaygın tuzaklar

- Owner vs Server mantığı: UI güncellemeleri `IsOwner` ile korunmalı. Sunucu tarafında durumu uygulamak için ServerRpc/NetworkVariable kullanılmalı.
- LayerMask: Collectable ve magnet sorgularının doğru layerda olduğundan ve Physics sorgularının LayerMask kullandığından emin olun.
- RPC akışı: İstemci server değilse pickup için ServerRpc göndermelidir; sunucu netId, tür ve mesafeyi doğrulamalıdır.
- Havuzlanan nesneler: `.ReturnToPool()` çağırıldığında, nesnenin NetworkObject'inin sunucuda doğru bir şekilde despawn edildiğinden emin olun.

Useful links / Faydalı linkler

- Repo: https://github.com/karayilann/Project-Fight-Arena
- Assets: https://github.com/karayilann/Project-Fight-Arena/tree/main/FightArene/Assets

