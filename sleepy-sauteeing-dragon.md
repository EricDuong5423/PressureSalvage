# Kế hoạch FULL CODE: Shop + Inventory hotbar (Lethal Company) + gear cầm tay

## Context
- Bỏ `enum GearType`: thêm bình thuốc/xẻng/súng sẽ phải sửa enum + switch (vi phạm OCP) → **polymorphism + data-driven**. Hành vi item nằm trên component prefab qua interface `IUsable`, không switch theo loại.
- Bỏ "đèn đầu": đèn là **đồ cầm tay chiếm 1 ô hotbar + tốn weight** → người chơi đánh đổi (đèn an toàn vs ô loot), kiểu Lethal Company.
- Mọi đồ player mang (đèn/súng/xẻng/thuốc/loot) đều là item chạy qua hotbar và **tranh slot**.
- Dissolve: user đã sửa xong → bỏ khỏi plan.

**Tích hợp với code hiện có (đã đọc):**
- `GameProgressionManager`: có `Credits`, `AddEarnings(int)`, `OnStateChanged`, settle/day loop → thêm `TrySpend`.
- `OxygenSystem.Update` line 56 đọc `interact.CarriedWeightKg` → giữ nguyên, chỉ đổi `CarriedWeightKg` trỏ về `Inventory.TotalWeight`.
- `DiveExit.RiseAndExit` đang gọi `_sellZone.SellAll()` → đổi sang `Inventory.SellSalvage()` (loot giờ nằm trong hotbar, không thả ở SellZone nữa).
- `CarryItem` chỉ bị `SellZone`/`PlayerInteract`/`bowl.prefab` dùng; không còn OneHand/TwoHand → refactor an toàn.
- `worldPrefab` của 1 item = **chính prefab CarryItem** của nó (đồ nằm dưới đất). Cầm = Instantiate rồi tắt physics + parent vào tay; Thả = Instantiate bật physics + set lại Value.

---

# A. ITEM CORE

## A1. `Assets/Scripts/Item/ItemData.cs` (sửa — full)
```csharp
using UnityEngine;

[CreateAssetMenu(fileName = "New Item Data", menuName = "Game Data/Item Data")]
public class ItemData : ScriptableObject
{
    [Header("Identity")]
    public string itemName;
    public ItemRank rank;
    public Sprite icon;                 // icon ô hotbar

    [Header("Economy")]
    public int minValue;
    public int maxValue;

    [Header("Physics")]
    public float weightKg = 1f;

    [Header("World")]
    public GameObject worldPrefab;      // prefab CarryItem trong thế giới (spawn lúc cầm + lúc thả)

    [Header("Properties")]
    public bool canBreak = false;
    public bool isQuest = false;        // quest item: không bán, giữ qua scene
}

public enum ItemRank { F, D, C, B, A, S }
```

## A2. `Assets/Scripts/Item/IUsable.cs` (mới)
```csharp
using UnityEngine;

// Gắn lên component của prefab cầm tay. Thêm item mới = tạo prefab + 1 component implement IUsable.
public interface IUsable
{
    void OnEquip(GameObject holder);   // khi món thành ô active (cầm lên tay)
    void OnUnequip();                  // khi đổi ô / cất đi
    void Use();                        // khi bấm phím Use lúc đang cầm
}
```

## A3. `Assets/Scripts/Item/CarryItem.cs` (sửa — full, bỏ ICarryable/velocity-follow)
```csharp
using UnityEngine;
using Random = UnityEngine.Random;

[RequireComponent(typeof(Rigidbody))]
public class CarryItem : Interactable
{
    [Header("Item data")]
    public ItemData data;

    public int Value { get; private set; }
    private bool rolled;

    private void Start()
    {
        if (!rolled && data != null)
            Value = Random.Range(data.minValue, data.maxValue + 1);
    }

    // Giữ Value khi thả lại từ hotbar (không roll lại)
    public void SetValue(int v) { Value = v; rolled = true; }

    protected override void Interact() { }   // nhặt do PlayerHotbar xử lý
}
```
> Xoá `Assets/Scripts/Item/ICarryable.cs` (không còn ai dùng sau refactor).

## A4. `Assets/Scripts/Item/Flashlight.cs` (mới — gear đầu tiên, implement IUsable)
```csharp
using UnityEngine;

// Gắn lên prefab đèn pin (cùng prefab CarryItem). Có child Light (spot).
public class Flashlight : MonoBehaviour, IUsable
{
    [SerializeField] private Light beam;     // child spot light
    private bool on;

    private void Awake() { if (beam) beam.enabled = false; }

    public void OnEquip(GameObject holder) { if (beam) beam.enabled = on; }
    public void OnUnequip() { if (beam) beam.enabled = false; }
    public void Use()                        // bấm phím Use để bật/tắt
    {
        on = !on;
        if (beam) beam.enabled = on;
    }
}
```

---

# B. INVENTORY HOTBAR

## B1. `Assets/Scripts/Item/Inventory.cs` (mới — singleton persist)
```csharp
using System;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    public static Inventory Instance { get; private set; }

    [Serializable]
    public class Slot
    {
        public ItemData data;
        public int value;
        public bool Empty => data == null;
        public void Clear() { data = null; value = 0; }
    }

    [SerializeField] private int capacity = 1;     // = PlayerLoadout.slotCount
    public Slot[] slots;
    public int activeIndex { get; private set; }

    public event Action OnChanged;

    public int Capacity => slots != null ? slots.Length : 0;
    public Slot Active => (slots != null && slots.Length > 0) ? slots[activeIndex] : null;

    public float TotalWeight
    {
        get
        {
            float w = 0f;
            if (slots != null)
                foreach (var s in slots) if (!s.Empty) w += s.data.weightKg;
            return w;
        }
    }

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        EnsureSize(capacity);
    }

    private void EnsureSize(int n)
    {
        n = Mathf.Max(1, n);
        var old = slots;
        slots = new Slot[n];
        for (int i = 0; i < n; i++)
            slots[i] = (old != null && i < old.Length && old[i] != null) ? old[i] : new Slot();
        if (activeIndex >= n) activeIndex = n - 1;
    }

    public void SetCapacity(int n)
    {
        if (Capacity == n) return;
        EnsureSize(n);
        OnChanged?.Invoke();
    }

    // Vào ô trống đầu tiên. Full → false.
    public bool TryAdd(ItemData d, int value)
    {
        for (int i = 0; i < slots.Length; i++)
            if (slots[i].Empty)
            {
                slots[i].data = d; slots[i].value = value;
                OnChanged?.Invoke();
                return true;
            }
        return false;
    }

    public void SetActive(int i)
    {
        if (i < 0 || i >= slots.Length || i == activeIndex) return;
        activeIndex = i;
        OnChanged?.Invoke();
    }

    public void ClearActive()
    {
        if (Active == null) return;
        Active.Clear();
        OnChanged?.Invoke();
    }

    // Bán mọi ô không phải quest → cộng Credits. Quest item ở lại.
    public void SellSalvage()
    {
        int total = 0;
        foreach (var s in slots)
            if (!s.Empty && !s.data.isQuest) { total += s.value; s.Clear(); }
        if (total > 0) GameProgressionManager.Instance?.AddEarnings(total);
        OnChanged?.Invoke();
    }

    public bool Contains(ItemData d)
    {
        foreach (var s in slots) if (!s.Empty && s.data == d) return true;
        return false;
    }
}
```

## B2. `Assets/Scripts/Player/PlayerController/PlayerHotbar.cs` (mới — trên Player)
```csharp
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerHotbar : MonoBehaviour
{
    [SerializeField] private Transform holdPoint;          // điểm cầm trên tay (child camera)
    [SerializeField] private Vector3 dropOffset = new Vector3(0f, 0f, 1.2f);
    [SerializeField] private Key useKey = Key.F;           // phím dùng đồ (đèn...)

    private GameObject heldGO;
    private IUsable heldUsable;

    private Inventory Inv => Inventory.Instance;

    private void Start()
    {
        if (Inv != null && PlayerLoadout.Instance != null)
            Inv.SetCapacity(PlayerLoadout.Instance.slotCount);
        RefreshHeld();                                     // spawn lại món active (đồ qua scene)
    }

    private void Update()
    {
        var kb = Keyboard.current;
        if (kb == null || Inv == null) return;

        // Phím số 1..N đổi ô active
        for (int i = 0; i < Inv.Capacity && i < 9; i++)
        {
            var k = Key.Digit1 + i;                        // Digit1..Digit9 liên tiếp
            if (kb[k].wasPressedThisFrame) { Inv.SetActive(i); RefreshHeld(); break; }
        }

        // Phím Use
        if (kb[useKey].wasPressedThisFrame) heldUsable?.Use();
    }

    // Dựng lại GameObject cầm trên tay theo ô active
    public void RefreshHeld()
    {
        heldUsable?.OnUnequip();
        if (heldGO != null) Destroy(heldGO);
        heldGO = null; heldUsable = null;

        var slot = Inv != null ? Inv.Active : null;
        if (slot == null || slot.Empty || slot.data.worldPrefab == null || holdPoint == null) return;

        heldGO = Instantiate(slot.data.worldPrefab, holdPoint);
        heldGO.transform.localPosition = Vector3.zero;
        heldGO.transform.localRotation = Quaternion.identity;
        ConfigureHeld(heldGO);

        heldUsable = heldGO.GetComponent<IUsable>();
        heldUsable?.OnEquip(gameObject);
    }

    private static void ConfigureHeld(GameObject go)
    {
        if (go.TryGetComponent(out Rigidbody rb)) { rb.isKinematic = true; rb.detectCollisions = false; }
        foreach (var c in go.GetComponentsInChildren<Collider>()) c.enabled = false;
        var uw = go.GetComponent<UnderwaterBody>(); if (uw) uw.enabled = false;
    }

    // Nhặt: gọi từ PlayerInteract khi raycast trúng CarryItem
    public bool TryPickup(CarryItem item)
    {
        if (Inv == null || item == null || item.data == null) return false;
        if (!Inv.TryAdd(item.data, item.Value)) return false;   // full
        bool wasActiveEmpty = Inv.Active != null && Inv.Active.data == item.data;
        Destroy(item.gameObject);
        RefreshHeld();
        return true;
    }

    // Thả ô active ra thế giới
    public void DropActive()
    {
        var slot = Inv != null ? Inv.Active : null;
        if (slot == null || slot.Empty || slot.data.worldPrefab == null) return;

        Vector3 pos = holdPoint != null ? holdPoint.TransformPoint(dropOffset)
                                        : transform.position + transform.forward * 1.2f;
        var go = Instantiate(slot.data.worldPrefab, pos, Quaternion.identity);
        if (go.TryGetComponent(out CarryItem ci)) ci.SetValue(slot.value);

        Inv.ClearActive();
        RefreshHeld();
    }
}
```

## B3. `Assets/Scripts/Player/PlayerController/PlayerInteract.cs` (sửa — full, bỏ single-carry)
```csharp
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteract : MonoBehaviour
{
    private Camera cam;
    [Header("Raycast")]
    [SerializeField] private float distance = 3f;
    [SerializeField] private LayerMask mask;
    private MeshRenderer lastRenderer;

    private PlayerUI playerUI;
    private InputManager inputManager;
    private PlayerHotbar hotbar;
    private RaycastHit hitInfo;

    private MaterialPropertyBlock propBlock;

    // Weight → oxy giờ lấy từ Inventory (OxygenSystem không phải đổi)
    public float CarriedWeightKg => Inventory.Instance != null ? Inventory.Instance.TotalWeight : 0f;

    void Start()
    {
        cam = GetComponent<PlayerLook>().cam;
        playerUI = GetComponent<PlayerUI>();
        inputManager = GetComponent<InputManager>();
        hotbar = GetComponent<PlayerHotbar>();
        propBlock = new MaterialPropertyBlock();

        inputManager.OnFoot.Interact.performed += Interact;
        inputManager.OnFoot.Drop.performed += Drop;
    }

    private void OnDestroy()
    {
        if (inputManager == null) return;
        inputManager.OnFoot.Interact.performed -= Interact;
        inputManager.OnFoot.Drop.performed -= Drop;
    }

    private void Drop(InputAction.CallbackContext _) => hotbar?.DropActive();

    private void Interact(InputAction.CallbackContext _)
    {
        if (hitInfo.collider == null) return;

        if (hitInfo.collider.TryGetComponent(out Interactable interactable))
            interactable.BaseInteract();
        else if (hitInfo.collider.TryGetComponent(out CarryItem item))
            hotbar?.TryPickup(item);
    }

    void Update()
    {
        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        if (!Physics.Raycast(ray, out hitInfo, distance, mask))
        {
            ClearOutline();
            playerUI.UpdateText(string.Empty);
            return;
        }

        MeshRenderer rend = hitInfo.collider.GetComponent<MeshRenderer>();
        HandleOutline(rend);
        HandleInteractionUI(hitInfo.collider);
    }

    private void HandleInteractionUI(Collider hit)
    {
        if (hit.TryGetComponent(out Interactable interactable))
            playerUI.UpdateText(interactable.promptMessage);
        else if (hit.TryGetComponent(out CarryItem item) && item.data != null)
        {
            bool full = Inventory.Instance != null && Inventory.Instance.TryWouldBeFull();
            playerUI.UpdateText(full ? "Inventory full" : $"Pick up {item.data.itemName}");
        }
        else playerUI.UpdateText(string.Empty);
    }

    private void HandleOutline(MeshRenderer rend)
    {
        if (rend == lastRenderer) return;
        ClearOutline();
        if (rend != null) { SetOutline(rend, 1.05f); lastRenderer = rend; }
    }

    private void ClearOutline()
    {
        if (lastRenderer != null) { SetOutline(lastRenderer, 0f); lastRenderer = null; }
    }

    private void SetOutline(MeshRenderer r, float v)
    {
        r.GetPropertyBlock(propBlock, 1);
        propBlock.SetFloat("_Scale", v);
        r.SetPropertyBlock(propBlock, 1);
    }
}
```
> Thêm helper nhỏ vào `Inventory`:
> ```csharp
> public bool TryWouldBeFull() { foreach (var s in slots) if (s.Empty) return false; return true; }
> ```

## B4. `Assets/Scripts/Player/PlayerView/HotbarUI.cs` (mới — UI thanh ô)
```csharp
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HotbarUI : MonoBehaviour
{
    [System.Serializable]
    public class Cell { public GameObject root; public Image icon; public Image highlight; public TMP_Text label; }

    [SerializeField] private Cell[] cells;      // tạo sẵn N ô trong Canvas

    private void OnEnable()
    {
        if (Inventory.Instance != null) Inventory.Instance.OnChanged += Refresh;
        Refresh();
    }
    private void OnDisable()
    {
        if (Inventory.Instance != null) Inventory.Instance.OnChanged -= Refresh;
    }

    private void Refresh()
    {
        var inv = Inventory.Instance;
        for (int i = 0; i < cells.Length; i++)
        {
            bool used = inv != null && i < inv.Capacity;
            if (cells[i].root) cells[i].root.SetActive(used);
            if (!used) continue;

            var slot = inv.slots[i];
            if (cells[i].icon)
            {
                cells[i].icon.enabled = !slot.Empty;
                cells[i].icon.sprite = slot.Empty ? null : slot.data.icon;
            }
            if (cells[i].highlight) cells[i].highlight.enabled = (i == inv.activeIndex);
            if (cells[i].label) cells[i].label.text = (i + 1).ToString();
        }
    }
}
```

---

# C. SHOP + UPGRADE (polymorphic, không enum)

## C1. `Assets/Scripts/Shop/UpgradeData.cs` (mới — base + 2 subclass)
```csharp
using UnityEngine;

public abstract class UpgradeData : ScriptableObject
{
    public abstract bool CanApply(PlayerLoadout l);
    public abstract void Apply(PlayerLoadout l);
}

[CreateAssetMenu(menuName = "Game Data/Upgrade/Oxygen Tank")]
public class OxygenTankUpgrade : UpgradeData
{
    public int maxTier = 3;
    public override bool CanApply(PlayerLoadout l) => l.oxygenTankTier < maxTier;
    public override void Apply(PlayerLoadout l) => l.oxygenTankTier++;
}

[CreateAssetMenu(menuName = "Game Data/Upgrade/Slot Count")]
public class SlotCountUpgrade : UpgradeData
{
    public int maxSlots = 4;
    public override bool CanApply(PlayerLoadout l) => l.slotCount < maxSlots;
    public override void Apply(PlayerLoadout l) => l.slotCount++;
}
```

## C2. `Assets/Scripts/Shop/ShopOffer.cs` (mới — base + 2 subclass)
```csharp
using UnityEngine;

public abstract class ShopOffer : ScriptableObject
{
    public string displayName;
    public Sprite icon;
    [TextArea] public string description;
    public int cost;

    public abstract bool CanBuy();      // chưa sở hữu / chưa max
    public abstract void Purchase();    // ShopManager đã TrySpend xong mới gọi
}

[CreateAssetMenu(menuName = "Game Data/Shop/Item Offer")]
public class ItemOffer : ShopOffer
{
    public ItemData item;
    public bool consumable;             // true: mua bao nhiêu lần cũng được, vào thẳng hotbar

    public override bool CanBuy()
    {
        if (consumable) return true;
        var l = PlayerLoadout.Instance;
        return l != null && !l.ownedGear.Contains(item);   // gear vĩnh viễn: chặn trùng
    }

    public override void Purchase()
    {
        if (consumable) { Inventory.Instance?.TryAdd(item, Random.Range(item.minValue, item.maxValue + 1)); return; }
        PlayerLoadout.Instance?.ownedGear.Add(item);        // gear → kệ sub
    }
}

[CreateAssetMenu(menuName = "Game Data/Shop/Upgrade Offer")]
public class UpgradeOffer : ShopOffer
{
    public UpgradeData upgrade;
    public override bool CanBuy() => PlayerLoadout.Instance != null && upgrade.CanApply(PlayerLoadout.Instance);
    public override void Purchase() => upgrade.Apply(PlayerLoadout.Instance);
}
```

## C3. `Assets/Scripts/Core/GameProgressionManager.cs` (sửa — thêm 1 method)
```csharp
public bool TrySpend(int cost)
{
    if (cost <= 0) return true;
    if (Credits < cost) return false;
    Credits -= cost;
    OnStateChanged?.Invoke();
    return true;
}
```

## C4. `Assets/Scripts/Shop/ShopUI.cs` (mới)
```csharp
using System.Collections.Generic;
using UnityEngine;

public class ShopUI : MonoBehaviour
{
    [SerializeField] private ShopOffer[] offers;
    [SerializeField] private ShopOfferButton buttonPrefab;   // UI prefab 1 dòng
    [SerializeField] private Transform listContainer;

    private readonly List<ShopOfferButton> spawned = new();

    public void Build()
    {
        foreach (var b in spawned) if (b) Destroy(b.gameObject);
        spawned.Clear();
        foreach (var offer in offers)
        {
            var btn = Instantiate(buttonPrefab, listContainer);
            btn.Setup(offer, this);
            spawned.Add(btn);
        }
    }

    public void Buy(ShopOffer offer)
    {
        var g = GameProgressionManager.Instance;
        if (offer.CanBuy() && g != null && g.TrySpend(offer.cost))
        {
            offer.Purchase();
            Refresh();
        }
    }

    public void Refresh() { foreach (var b in spawned) if (b) b.Refresh(); }
}
```

## C5. `Assets/Scripts/Shop/ShopOfferButton.cs` (mới — 1 dòng item)
```csharp
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopOfferButton : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text nameText, costText, descText;
    [SerializeField] private Button buyButton;

    private ShopOffer offer;
    private ShopUI owner;

    public void Setup(ShopOffer o, ShopUI ui)
    {
        offer = o; owner = ui;
        if (iconImage) iconImage.sprite = o.icon;
        if (nameText) nameText.text = o.displayName;
        if (costText) costText.text = $"{o.cost}₡";
        if (descText) descText.text = o.description;
        if (buyButton) { buyButton.onClick.RemoveAllListeners(); buyButton.onClick.AddListener(() => owner.Buy(offer)); }
        Refresh();
    }

    public void Refresh()
    {
        if (buyButton == null) return;
        var g = GameProgressionManager.Instance;
        buyButton.interactable = offer.CanBuy() && g != null && g.Credits >= offer.cost;
    }
}
```

## C6. `Assets/Scripts/Interactables/ShopTerminal.cs` (mới — tái dùng pattern MapSelectComputer)
```csharp
using UnityEngine;
using UnityEngine.InputSystem;

public class ShopTerminal : Interactable
{
    [SerializeField] private GameObject shopCanvas;
    [SerializeField] private Canvas screenCanvas;
    [SerializeField] private ShopUI ui;
    private bool open;

    protected override void Interact() => SetOpen(!open);
    public void Close() => SetOpen(false);

    private void Update()
    {
        if (open && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            SetOpen(false);
    }

    private void SetOpen(bool o)
    {
        open = o;
        shopCanvas.SetActive(o);
        Cursor.lockState = o ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = o;
        var im = FindObjectOfType<InputManager>();
        if (im) im.ControlEnabled = !o;

        if (o)
        {
            if (screenCanvas && screenCanvas.worldCamera == null) screenCanvas.worldCamera = Camera.main;
            ui.Build();
        }
    }
}
```

---

# D. PERSISTENCE & SPAWN

## D1. `Assets/Scripts/Core/PlayerLoadout.cs` (mới — singleton persist)
```csharp
using System.Collections.Generic;
using UnityEngine;

public class PlayerLoadout : MonoBehaviour
{
    public static PlayerLoadout Instance { get; private set; }

    public int oxygenTankTier = 0;          // → maxOxygen
    public int slotCount = 1;               // → số ô hotbar (carry capacity)
    public List<ItemData> ownedGear = new();// gear dùng lại (đèn/súng/xẻng) đã mua

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
}
```

## D2. `Assets/Scripts/Player/PlayerController/OxygenSystem.cs` (sửa — thêm tank tier)
Thêm field + lưu base trong Awake + 1 method:
```csharp
[SerializeField] private float oxygenPerTier = 25f;   // mỗi tier tank +25
private float baseMaxOxygen;
```
Trong `Awake()` (sau khi set currentOxygen): `baseMaxOxygen = maxOxygen;`
Thêm:
```csharp
public void ApplyTankTier(int tier)
{
    maxOxygen = baseMaxOxygen + tier * oxygenPerTier;
    currentOxygen = maxOxygen;
    OnOxygenChanged?.Invoke(currentOxygen / maxOxygen * 100f);
}
```

## D3. `Assets/Scripts/Player/PlayerController/PlayerEquipment.cs` (mới — trên Player)
```csharp
using UnityEngine;

[RequireComponent(typeof(OxygenSystem))]
public class PlayerEquipment : MonoBehaviour
{
    private void Start()
    {
        var l = PlayerLoadout.Instance;
        if (l == null) return;
        GetComponent<OxygenSystem>()?.ApplyTankTier(l.oxygenTankTier);
        Inventory.Instance?.SetCapacity(l.slotCount);
    }
}
```

## D4. `Assets/Scripts/Interactables/SubmarineLocker.cs` (mới — chọn đồ mang đi kiểu Lethal Company)
```csharp
using UnityEngine;

// Đặt trên Submarine. Spawn pickup vật lý cho gear sở hữu mà CHƯA nằm trong hotbar.
public class SubmarineLocker : MonoBehaviour
{
    [SerializeField] private Transform[] shelfSlots;   // vị trí kệ

    private void Start() => Rebuild();

    public void Rebuild()
    {
        var loadout = PlayerLoadout.Instance;
        var inv = Inventory.Instance;
        if (loadout == null) return;

        int s = 0;
        foreach (var gear in loadout.ownedGear)
        {
            if (gear == null || gear.worldPrefab == null) continue;
            if (inv != null && inv.Contains(gear)) continue;        // đang cầm rồi → không spawn lại
            if (s >= shelfSlots.Length) break;
            Instantiate(gear.worldPrefab, shelfSlots[s].position, shelfSlots[s].rotation);
            s++;
        }
    }
}
```
> Người chơi bấm **E** nhặt gear trên kệ vào hotbar (qua `PlayerHotbar.TryPickup`) → chiếm slot + weight. Thả (Q) trên sub → rơi ra, có thể nhặt lại. → tự cân nhắc mang gì trong giới hạn slot trước khi lặn.

## D5. `Assets/Scripts/Interactables/DiveExit.cs` (sửa — 1 dòng)
Trong `RiseAndExit()` đổi:
```csharp
// if (_sellZone != null) _sellZone.SellAll();
Inventory.Instance?.SellSalvage();
GameProgressionManager.Instance?.CompleteDive();
```
(Quest item ở lại slot → qua scene. `SellZone` thành legacy, có thể gỡ khỏi cage.)

---

# Setup trong Unity (sau khi code xong)
1. **Player prefab**: add `PlayerHotbar` (gán `holdPoint` = 1 child trống dưới Camera), `PlayerEquipment`. Tạo Canvas HUD add `HotbarUI` (tạo sẵn N ô Cell).
2. **Scene nào cũng có**: 1 GameObject `Inventory`, `PlayerLoadout`, `GameProgressionManager` (đã DontDestroyOnLoad — chỉ cần ở scene khởi đầu; nếu mỗi scene tự đặt thì bản trùng tự Destroy).
3. **Flashlight**: tạo prefab CarryItem cho đèn (Rigidbody + Collider + MeshRenderer + child spot `Light` + component `Flashlight` gán `beam`). Tạo `ItemData` đèn: `worldPrefab` = prefab này, `weightKg`, `icon`. (đèn KHÔNG `isQuest`.)
4. **Loot**: mỗi ItemData gán `worldPrefab` = prefab CarryItem của nó + `icon`.
5. **Shop**: tạo các SO `ItemOffer`/`UpgradeOffer` (+`OxygenTankUpgrade`/`SlotCountUpgrade`). Đặt `ShopTerminal` + `ShopUI` (canvas Screen Space - Camera) trên Submarine, gán `offers`.
6. **SubmarineLocker** trên sub, gán `shelfSlots`.
7. **Input**: phím số 1..N + F (Use) đọc trực tiếp `Keyboard.current` — không cần sửa `.inputactions`.

# Verify
1. Mua đèn ở shop → trừ Credits, vào `ownedGear`; thiếu tiền → nút Buy mờ.
2. Về Submarine → đèn spawn trên kệ → bấm E nhặt (chiếm 1 ô); nhặt quá `slotCount` → prompt "Inventory full".
3. Bấm 1..N đổi ô → món trên tay đổi; cầm đèn bấm F → bật/tắt Light.
4. Mang nhiều đồ → oxy hao nhanh (TotalWeight qua `CarriedWeightKg`); mua SlotCountUpgrade → thêm ô; mua OxygenTankUpgrade → maxOxygen tăng (thấy lúc spawn dive).
5. Xuống dive nhặt loot → bấm lồng lên tàu → `SellSalvage` cộng Credits; quest item vẫn còn qua scene.
