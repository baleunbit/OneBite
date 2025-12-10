using System.Collections;
using UnityEngine;

public class Gun : MonoBehaviour
{
    public Transform Crosshair;

    [Header("탄창(개별 탄약 모드용)")]
    public int maxAmmo = 6;
    public float reloadTime = 2f;

    [Header("연사 속도")]
    public float fireRate = 0.2f;

    [Header("데미지/관통")]
    [SerializeField] float Damage = 5f;
    [SerializeField] int Pierce = 1;
    float stageDamageOverride = -1f;  // 스테이지별 데미지 오버라이드

    [Header("발사체")]
    [SerializeField] GameObject bulletPrefab;

    [Header("탄약 모드")]
    public bool useSharedAmmo = true;
    public SharedAmmo sharedAmmo;                 // Player 등에 붙은 SharedAmmo

    Player player;
    int currentAmmo;                              // 개별 모드에서만 사용
    bool isReloading;
    bool deathHandled;
    float baseDamage;
    float nextFireTime;

    void Awake()
    {
        baseDamage = Damage;  // ✔ 인스턴스 생성 후 초기화 가능
        if (useSharedAmmo && !sharedAmmo)
        {
            // 부모나 씬에서 자동으로 찾아서 연결
            sharedAmmo = GetComponentInParent<SharedAmmo>()
                      ?? FindFirstObjectByType<SharedAmmo>();
        }

        // 시작 탄수 초기화
        currentAmmo = useSharedAmmo
            ? (sharedAmmo ? sharedAmmo.currentAmmo : maxAmmo)
            : maxAmmo;
    }

    void Start()
    {
        if (!Crosshair)
        {
            Crosshair = GameObject.FindWithTag("Crosshair")?.transform
                     ?? GameObject.Find("Crosshair")?.transform;
        }
        var pObj = GameObject.FindGameObjectWithTag("Player");
        if (pObj) player = pObj.GetComponent<Player>();

        UIManager.Instance?.RegisterGun(this);
        UIManager.Instance?.UpdateAmmoText(GetCurrentAmmo(), GetMaxAmmo());
    }

    void OnDisable()
    {
        UIManager.Instance?.HideReloadCircle();
    }

    void Update()
    {
        if (!player) return;

        if (player.health <= 0)
        {
            if (!deathHandled)
            {
                deathHandled = true;
            }
            return;
        }

        // -----------------------
        //  🔥 자동 장전
        // -----------------------
        if (!isReloading && GetCurrentAmmo() <= 0)
        {
            StartCoroutine(Reload());
            return;
        }

        // -----------------------
        //  레벨업 패널 열려있으면 입력 금지
        // -----------------------
        if (UIManager.Instance && UIManager.Instance.IsLevelUpPanelOpen)
            return;

        if (isReloading) return;

        // -----------------------
        //  R키 수동 장전
        // -----------------------
        if (Input.GetKeyDown(KeyCode.R) && GetCurrentAmmo() < GetMaxAmmo())
        {
            StartCoroutine(Reload());
            return;
        }

        // -----------------------
        //  공격
        // -----------------------
        if (Input.GetMouseButton(0) && Time.time >= nextFireTime)
        {
            if (GetCurrentAmmo() > 0)
            {
                Fire();
                nextFireTime = Time.time + fireRate;
            }
        }
    }

    IEnumerator Reload()
    {
        if (isReloading) yield break;
        if (GetCurrentAmmo() >= GetMaxAmmo()) yield break;

        isReloading = true;
        UIManager.Instance?.ShowReloadCircle();

        yield return new WaitForSeconds(reloadTime);

        if (useSharedAmmo && sharedAmmo)
            sharedAmmo.Refill();
        else
            currentAmmo = maxAmmo;

        UIManager.Instance?.UpdateAmmoText(GetCurrentAmmo(), GetMaxAmmo());
        UIManager.Instance?.HideReloadCircle();
        isReloading = false;
    }

    void Fire()
    {
        // 탄약 소모 (공용/개별)
        bool ok;
        if (useSharedAmmo && sharedAmmo)
            ok = sharedAmmo.TryConsume(1);
        else
        {
            if (currentAmmo <= 0) ok = false;
            else { currentAmmo--; ok = true; }
        }
        if (!ok) return;

        UIManager.Instance?.UpdateAmmoText(GetCurrentAmmo(), GetMaxAmmo());

        // 크로스헤어 안전망
        if (!Crosshair)
            Crosshair = GameObject.FindWithTag("Crosshair")?.transform
                     ?? GameObject.Find("Crosshair")?.transform;

        Vector3 origin = transform.position;
        Vector3 aim = Crosshair ? Crosshair.position : origin + transform.right;
        Vector2 dir = (aim - origin).normalized;

        float spawnOffset = GetPlayerRadius() + 0.1f;
        Vector3 spawnPos = origin + (Vector3)dir * spawnOffset;

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        Quaternion rot = Quaternion.AngleAxis(angle + 270f, Vector3.forward);

        // 🔥 강화 적용된 공격력 계산 (스테이지 데미지 + 플레이어 보너스)
        float finalDamage = GetBaseDamage() + (player ? player.weaponDamageBonus : 0f);

        var go = Instantiate(bulletPrefab, spawnPos, rot);
        var b = go.GetComponent<Bullet>();
        if (b != null)
        {
            b.Init(finalDamage, Pierce, dir);
            b.Setup(dir);
        }
        else
        {
            Debug.LogError("[Gun] Bullet prefab에 Bullet 스크립트가 없습니다.", go);
        }

        // 플레이어와 충돌 무시
        if (player)
        {
            var bulletCol = go.GetComponent<Collider2D>();
            var ownerCols = player.GetComponentsInChildren<Collider2D>(true);
            foreach (var c in ownerCols)
                if (c && bulletCol) Physics2D.IgnoreCollision(bulletCol, c, true);
        }
    }

    float GetPlayerRadius()
    {
        if (!player) return 0.3f;
        float r = 0.3f;
        var cols = player.GetComponentsInChildren<Collider2D>(true);
        foreach (var c in cols)
        {
            if (!c) continue;
            var b = c.bounds;
            r = Mathf.Max(r, Mathf.Max(b.extents.x, b.extents.y));
        }
        return r;
    }

    // ===== UIManager에서 쓰는 표준 인터페이스 =====
    public int GetCurrentAmmo()
    {
        if (useSharedAmmo && sharedAmmo) return sharedAmmo.currentAmmo;
        return currentAmmo;
    }
    public int GetMaxAmmo()
    {
        if (useSharedAmmo && sharedAmmo) return sharedAmmo.maxAmmo;
        return maxAmmo;
    }

    // (선택) 외부에서 공용탄약 주입할 때 사용
    public void SetSharedAmmo(SharedAmmo sa)
    {
        sharedAmmo = sa;
        useSharedAmmo = (sa != null);
        UIManager.Instance?.UpdateAmmoText(GetCurrentAmmo(), GetMaxAmmo());
    }
    
    // 스테이지별 데미지 설정
    public void SetStageDamage(float damage)
    {
        stageDamageOverride = damage;
    }
    
    // 현재 적용되는 기본 데미지 반환
    float GetBaseDamage()
    {
        return stageDamageOverride > 0 ? stageDamageOverride : Damage;
    }
}
