using System.Collections.Generic;
using UnityEngine;

public class WeaponManager : MonoBehaviour
{
    [Header("무기 프리팹 (0=Fork, 1=Spoon, 2=ChopStick)")]
    public List<GameObject> weaponPrefabs;

    [Header("장착 소켓 (Player 아래 빈 오브젝트)")]
    public Transform weaponSocket;

    [Header("입력/옵션")]
    public bool wrapAround = true;
    public float switchCooldown = 0.2f;
    public int defaultIndex = 0;

    [Header("Stage Rules (optional)")]
    public bool useStageRules = false;
    public int stage1Index = 0;
    public int stage2Index = 0;
    public int stage3Index = 0;
    
    [Header("스테이지별 무기별 데미지 (0=Fork, 1=Spoon, 2=ChopStick)")]
    [Tooltip("기본 1데미지, 강화 무기 3데미지")]
    public float[] stage1WeaponDamage = { 3f, 1f, 1f };   // 1스테이지: 포크 강화
    public float[] stage2WeaponDamage = { 3f, 1f, 1f };   // 2스테이지: 포크 강화
    public float[] stage3WeaponDamage = { 1f, 3f, 1f };   // 3스테이지: 숟가락 강화
    public float[] stage4WeaponDamage = { 1f, 3f, 1f };   // 4스테이지: 숟가락 강화 (보스룸은 BossTrigger에서 젓가락으로 변경)

    int currentIndex = -1;
    GameObject currentGO;
    float nextSwitchTime;
    int currentStage = 1;

    void Start()
    {
        if (!weaponSocket)
        {
            Debug.LogError("[WeaponManager] weaponSocket 지정 필요");
            enabled = false; return;
        }

        Equip(Mathf.Clamp(defaultIndex, 0, (weaponPrefabs?.Count ?? 1) - 1));
    }

    void Update()
    {
        if (Time.time < nextSwitchTime) return;

        if (Input.GetKeyDown(KeyCode.Alpha1)) { Equip(0); return; }
        if (Input.GetKeyDown(KeyCode.Alpha2)) { Equip(1); return; }
        if (Input.GetKeyDown(KeyCode.Alpha3)) { Equip(2); return; }

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll > 0.01f) Next();
        else if (scroll < -0.01f) Prev();
    }

    public void ApplyStageRules(int stage)
    {
        currentStage = stage;
        Debug.Log($"[WeaponManager] 스테이지 {stage} 규칙 적용");
        
        // 현재 장착된 무기에 데미지 적용
        ApplyDamageToCurrentWeapon();
        
        if (!useStageRules) return;

        int idx = defaultIndex;
        switch (stage)
        {
            case 1: idx = stage1Index; break;
            case 2: idx = stage2Index; break;
            case 3: idx = stage3Index; break;
            case 4: idx = stage3Index; break;  // 4스테이지도 처리
            default: idx = defaultIndex; break;
        }

        Equip(Mathf.Clamp(idx, 0, (weaponPrefabs?.Count ?? 1) - 1));
    }
    
    void ApplyDamageToCurrentWeapon()
    {
        if (!currentGO || currentIndex < 0) return;
        
        float damage = GetWeaponDamageForStage(currentStage, currentIndex);
        var gun = currentGO.GetComponent<Gun>();
        if (gun) gun.SetStageDamage(damage);
    }
    
    float GetWeaponDamageForStage(int stage, int weaponIndex)
    {
        float[] damages;
        switch (stage)
        {
            case 1: damages = stage1WeaponDamage; break;
            case 2: damages = stage2WeaponDamage; break;
            case 3: damages = stage3WeaponDamage; break;
            case 4: damages = stage4WeaponDamage; break;
            default: damages = stage1WeaponDamage; break;
        }
        
        if (damages == null || weaponIndex < 0 || weaponIndex >= damages.Length)
            return 5f; // 기본값
        
        return damages[weaponIndex];
    }

    public void Next()
    {
        int n = weaponPrefabs?.Count ?? 0; if (n == 0) return;
        int i = currentIndex + 1; if (i >= n) i = wrapAround ? 0 : n - 1;
        Equip(i);
    }
    public void Prev()
    {
        int n = weaponPrefabs?.Count ?? 0; if (n == 0) return;
        int i = currentIndex - 1; if (i < 0) i = wrapAround ? n - 1 : 0;
        Equip(i);
    }

    void Equip(int idx)
    {
        if (weaponPrefabs == null || idx < 0 || idx >= weaponPrefabs.Count) return;
        if (idx == currentIndex) return;

        if (currentGO) { Destroy(currentGO); currentGO = null; }

        var prefab = weaponPrefabs[idx];
        if (!prefab) { Debug.LogError("[WeaponManager] 프리팹 비어있음"); return; }

        currentGO = Instantiate(prefab, weaponSocket);
        currentGO.transform.localPosition = Vector3.zero;
        currentGO.transform.localRotation = Quaternion.identity;
        currentGO.transform.localScale = Vector3.one;

        currentIndex = idx;

        // ⭐️ 여기 추가: 장착 아이콘 갱신
        UIManager.Instance?.SetWeaponIconActive(idx);

        var mount = currentGO.GetComponent<WeaponMount>();
        if (mount)
        {
            currentGO.transform.localPosition += (Vector3)mount.localOffset;
            var e = currentGO.transform.localEulerAngles; e.z += mount.localZRotation;
            currentGO.transform.localEulerAngles = e;
        }

        var gun = currentGO.GetComponent<Gun>();
        if (gun)
        {
            // 스테이지별 무기별 데미지 적용
            float damage = GetWeaponDamageForStage(currentStage, idx);
            gun.SetStageDamage(damage);
            
            UIManager.Instance?.RegisterGun(gun);
        }

        nextSwitchTime = Time.time + switchCooldown;
    }
}
