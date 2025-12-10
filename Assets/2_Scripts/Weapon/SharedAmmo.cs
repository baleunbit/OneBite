using System;
using UnityEngine;

public class SharedAmmo : MonoBehaviour
{
    [Header("°ø¿ë Åº¾à")]
    public int maxAmmo = 6;
    public int currentAmmo = 6;

    public event Action<int, int> OnAmmoChanged;

    void OnEnable() { Notify(); }

    public bool CanFire => currentAmmo > 0;

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

    void Notify() => OnAmmoChanged?.Invoke(currentAmmo, maxAmmo);
}
