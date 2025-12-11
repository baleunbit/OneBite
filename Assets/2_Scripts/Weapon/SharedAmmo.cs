using System;
using System.Collections;
using UnityEngine;

public class SharedAmmo : MonoBehaviour
{
    [Header("공용 탄창")]
    public int maxAmmo = 6;
    public int currentAmmo = 6;
    public float reloadTime = 2f;

    public event Action<int, int> OnAmmoChanged;
    
    // 재장전 상태 (무기가 바뀌어도 유지)
    public bool IsReloading { get; private set; }
    float reloadEndTime;
    Coroutine reloadCoroutine;

    void OnEnable() { Notify(); }

    public bool CanFire => currentAmmo > 0 && !IsReloading;

    public bool TryConsume(int amount)
    {
        if (currentAmmo < amount) return false;
        currentAmmo -= amount;
        Notify();
        return true;
    }

    public void Refill()
    {
        currentAmmo = maxAmmo;
        Notify();
    }
    
    // 재장전 시작 (무기가 바뀌어도 타이머 유지)
    public void StartReload(float customReloadTime = -1f)
    {
        if (IsReloading) return;
        if (currentAmmo >= maxAmmo) return;
        
        float time = customReloadTime > 0 ? customReloadTime : reloadTime;
        reloadEndTime = Time.time + time;
        
        if (reloadCoroutine != null) StopCoroutine(reloadCoroutine);
        reloadCoroutine = StartCoroutine(ReloadCoroutine(time));
    }
    
    IEnumerator ReloadCoroutine(float time)
    {
        IsReloading = true;
        UIManager.Instance?.ShowReloadCircle();
        
        yield return new WaitForSeconds(time);
        
        Refill();
        IsReloading = false;
        UIManager.Instance?.HideReloadCircle();
        UIManager.Instance?.UpdateAmmoText(currentAmmo, maxAmmo);
    }
    
    // 남은 재장전 시간 반환
    public float GetRemainingReloadTime()
    {
        if (!IsReloading) return 0f;
        return Mathf.Max(0f, reloadEndTime - Time.time);
    }

    void Notify() => OnAmmoChanged?.Invoke(currentAmmo, maxAmmo);
}
