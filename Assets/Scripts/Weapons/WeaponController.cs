using UnityEngine;
using ADCREA.Dungeon;
using ADCREA.Enemies;

namespace ADCREA.Weapons
{

    [RequireComponent(typeof(WeaponInventory))]
    public class WeaponController : MonoBehaviour
    {
        public KeyCode reloadKey = KeyCode.R;

        private WeaponInventory _inventory;
        private WeaponAimDisplay _display;
        private Camera _viewCamera;
        private System.Random _rng;

        private WeaponInstance _activeWeapon;
        private float _cooldownTimer;
        private float _cooldownDuration;
        private float _timeSinceLastShot = 99f;

        private SpriteRenderer _cooldownBack;
        private SpriteRenderer _cooldownFill;
        private const float CooldownMeterWidth = 1.1f;

        private bool _reloading;
        private float _reloadTimer;
        private float _reloadDuration;
        private bool _waitingForFireRelease;

        private SpriteRenderer _swingFlash;
        private float _flashTimer;
        private float _sweepStartDegrees;
        private float _sweepEndDegrees;
        private float _sweepRadius;
        private float _swingBaseAlpha = 0.55f;
        private const float FlashDuration = 0.18f;

        private const float SweepReachMultiplier = 1.5f;
        private const float HitscanMaxDistance = 40f;

        public bool IsReloading
        {
            get { return _reloading; }
        }

        public float ReloadProgress01
        {
            get
            {
                if (!_reloading || _reloadDuration <= 0f)
                {
                    return 0f;
                }
                return 1f - Mathf.Clamp01(_reloadTimer / _reloadDuration);
            }
        }

        private void Awake()
        {
            _inventory = GetComponent<WeaponInventory>();
            _display = WeaponAimDisplay.Create(transform);
            _rng = new System.Random();
            CreateSwingFlash();
            CreateCooldownMeter();
        }

        private void Update()
        {
            UpdateSwingFlash();
            UpdateCooldownMeter();

            if (!GameSession.IsPlaying)
            {

                _waitingForFireRelease = true;
                return;
            }

            if (_waitingForFireRelease && !Input.GetMouseButton(0))
            {
                _waitingForFireRelease = false;
            }

            WeaponInstance weapon = _inventory.Equipped;
            if (weapon != _activeWeapon)
            {

                _activeWeapon = weapon;
                CancelReload();
                _cooldownTimer = 0f;
                _timeSinceLastShot = 99f;
                if (_swingFlash != null)
                {

                    _swingFlash.enabled = false;
                }
            }

            if (weapon == null)
            {
                _display.Hide();
                return;
            }

            Vector2 aim = AimDirection();

            if (weapon.IsMelee && _swingFlash != null && _swingFlash.enabled)
            {
                _display.Hide();
            }
            else
            {
                _display.Show(weapon.Definition, aim);
            }

            _cooldownTimer -= Time.deltaTime;
            _timeSinceLastShot += Time.deltaTime;

            UpdateReload(weapon);

            if (Input.GetKeyDown(reloadKey))
            {
                StartReload(weapon);
            }

            if (!_waitingForFireRelease && FirePressed(weapon.Definition.Mode) && _cooldownTimer <= 0f)
            {
                TryAttack(weapon, aim);
            }
        }

        private bool FirePressed(FireMode mode)
        {

            if (mode == FireMode.Automatic)
            {
                return Input.GetMouseButton(0);
            }
            return Input.GetMouseButtonDown(0);
        }

        private void TryAttack(WeaponInstance weapon, Vector2 aim)
        {
            if (weapon.IsMelee)
            {
                MeleeSwing(weapon, aim);
                ApplyCooldown(weapon);
                return;
            }

            if (_reloading)
            {

                if (weapon.Definition.IncrementalReload && weapon.AmmoInMagazine > 0)
                {
                    CancelReload();
                }
                else
                {
                    return;
                }
            }

            if (weapon.AmmoInMagazine <= 0)
            {
                StartReload(weapon);
                return;
            }

            FireShot(weapon, aim);
            ApplyCooldown(weapon);
        }

        private void ApplyCooldown(WeaponInstance weapon)
        {
            float attacksPerSecond = weapon.EffectiveAttackSpeed();

            if (attacksPerSecond > 0f)
            {
                _cooldownTimer = 1f / attacksPerSecond;
                _cooldownDuration = _cooldownTimer;
            }
        }

        private void CreateCooldownMeter()
        {
            var backObject = new GameObject("CooldownMeterBack");
            backObject.transform.SetParent(transform, false);
            backObject.transform.localPosition = new Vector3(0f, 1.25f, 0f);
            backObject.transform.localScale = new Vector3(CooldownMeterWidth, 0.09f, 1f);
            _cooldownBack = backObject.AddComponent<SpriteRenderer>();
            _cooldownBack.sprite = RuntimeSprites.SolidSquare();
            _cooldownBack.color = new Color(0f, 0f, 0f, 0.55f);
            _cooldownBack.sortingOrder = 7;
            _cooldownBack.enabled = false;

            var fillObject = new GameObject("CooldownMeterFill");
            fillObject.transform.SetParent(transform, false);
            _cooldownFill = fillObject.AddComponent<SpriteRenderer>();
            _cooldownFill.sprite = RuntimeSprites.SolidSquare();
            _cooldownFill.color = new Color(1f, 1f, 1f, 0.9f);
            _cooldownFill.sortingOrder = 8;
            _cooldownFill.enabled = false;
        }

        private void UpdateCooldownMeter()
        {
            if (_cooldownBack == null)
            {
                return;
            }

            bool visible = _cooldownTimer > 0f && _cooldownDuration > 0.1f && _activeWeapon != null;
            _cooldownBack.enabled = visible;
            _cooldownFill.enabled = visible;
            if (!visible)
            {
                return;
            }

            float progress = 1f - Mathf.Clamp01(_cooldownTimer / _cooldownDuration);
            float width = CooldownMeterWidth * progress;

            _cooldownFill.transform.localScale = new Vector3(width, 0.07f, 1f);
            _cooldownFill.transform.localPosition = new Vector3(
                -(CooldownMeterWidth - width) * 0.5f, 1.25f, 0f);
        }

        private void FireShot(WeaponInstance weapon, Vector2 aim)
        {
            WeaponDefinition def = weapon.Definition;

            float critChance = weapon.EffectiveCritChance();

            if (def.DoubleCritOnLastShot && weapon.AmmoInMagazine == 1)
            {
                critChance *= 2f;
            }
            int critTier = RollCritTier(critChance);

            int pellets = def.PelletsPerShot;
            float damage = weapon.EffectiveDamage();

            if (critTier > 0 && def.CritPelletsPerShot > 0)
            {

                pellets = def.CritPelletsPerShot;
            }
            else
            {
                damage *= EffectiveCritMultiplier(weapon, critTier);
            }

            Vector3 muzzle = _display.MuzzlePosition(transform.position, aim, def);
            float spread = CurrentSpreadDegrees(weapon);

            ParticleBurst.Spawn(muzzle, aim, def.Tint, 6, 5f);

            if (def.Hitscan)
            {
                FireHitscan(def, muzzle, aim, damage);
            }
            else
            {
                float pelletSize = 0.35f;
                if (pellets > 1)
                {
                    pelletSize = 0.22f;
                }
                for (int i = 0; i < pellets; i++)
                {
                    Vector2 direction = RotateByDegrees(aim, RandomSpreadAngle(spread));
                    Projectile.Fire(muzzle, direction, weapon.EffectiveProjectileVelocity(),
                        damage, def.Tint, pelletSize);
                }
            }

            weapon.AmmoInMagazine--;
            _timeSinceLastShot = 0f;

            if (weapon.AmmoInMagazine <= 0)
            {
                StartReload(weapon);
            }
        }

        private void FireHitscan(WeaponDefinition def, Vector3 muzzle, Vector2 aim, float damage)
        {
            Vector3 endPoint = muzzle + (Vector3)(aim * HitscanMaxDistance);
            int enemiesHit = 0;
            int maxEnemies = def.PierceCount + 1;

            RaycastHit2D[] hits = Physics2D.RaycastAll(muzzle, aim, HitscanMaxDistance);
            for (int i = 0; i < hits.Length; i++)
            {
                Collider2D collider = hits[i].collider;
                if (collider.isTrigger)
                {
                    continue;
                }
                if (collider.CompareTag("Player"))
                {
                    continue;
                }

                EnemyHealth enemy = collider.GetComponentInParent<EnemyHealth>();
                if (enemy != null)
                {
                    enemy.TakeDamage(damage);
                    enemiesHit++;
                    if (enemiesHit >= maxEnemies)
                    {
                        endPoint = hits[i].point;
                        break;
                    }
                    continue;
                }

                endPoint = hits[i].point;
                break;
            }

            BulletTracer.Spawn(muzzle, endPoint, def.Tint);
        }

        private float CurrentSpreadDegrees(WeaponInstance weapon)
        {
            float spread = weapon.EffectiveInaccuracy();
            if (weapon.Definition.BloomRecovery)
            {

                float recovery = Mathf.Clamp01(_timeSinceLastShot / weapon.EffectiveBloomRecoverySeconds());
                spread *= 1f - recovery;
            }
            return spread;
        }

        private float RandomSpreadAngle(float spreadDegrees)
        {
            if (spreadDegrees <= 0f)
            {
                return 0f;
            }

            return ((float)_rng.NextDouble() - 0.5f) * spreadDegrees;
        }

        private static Vector2 RotateByDegrees(Vector2 vector, float degrees)
        {
            float radians = degrees * Mathf.Deg2Rad;
            float cos = Mathf.Cos(radians);
            float sin = Mathf.Sin(radians);
            return new Vector2(vector.x * cos - vector.y * sin, vector.x * sin + vector.y * cos);
        }

        private int RollCritTier(float chance)
        {
            double roll = _rng.NextDouble();
            if (chance >= 1f)
            {
                if (roll < chance - 1f)
                {
                    return 2;
                }
                return 1;
            }
            if (roll < chance)
            {
                return 1;
            }
            return 0;
        }

        private static float CritDamageMultiplier(int critTier)
        {
            if (critTier >= 2)
            {
                return 4f;
            }
            if (critTier == 1)
            {
                return 2f;
            }
            return 1f;
        }

        private static float EffectiveCritMultiplier(WeaponInstance weapon, int critTier)
        {
            float multiplier = CritDamageMultiplier(critTier);
            if (critTier > 0)
            {
                multiplier *= 1f + weapon.CritDamageBonus;
            }
            return multiplier;
        }

        private void MeleeSwing(WeaponInstance weapon, Vector2 aim)
        {
            WeaponDefinition def = weapon.Definition;

            int critTier = RollCritTier(weapon.EffectiveCritChance());
            float damage = weapon.EffectiveDamage() * EffectiveCritMultiplier(weapon, critTier);
            bool fullCircle = critTier > 0 && def.CritHitsFullCircle;

            float radius = 0.6f + def.MeleeRange;

            ShowSwingFlash(def, aim, radius, fullCircle);

            _display.Hide();

            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, radius + 0.75f);
            for (int i = 0; i < hits.Length; i++)
            {
                EnemyHealth enemy = hits[i].GetComponentInParent<EnemyHealth>();
                if (enemy == null)
                {
                    continue;
                }

                if (!fullCircle)
                {
                    Vector2 toEnemy = enemy.transform.position - transform.position;
                    if (Vector2.Angle(aim, toEnemy) > def.MeleeArcDegrees * 0.5f)
                    {
                        continue;
                    }
                }

                enemy.TakeDamage(damage);
            }

            _timeSinceLastShot = 0f;
        }

        private void StartReload(WeaponInstance weapon)
        {
            if (_reloading || weapon.IsMelee)
            {
                return;
            }
            if (weapon.AmmoInMagazine >= weapon.Definition.MagazineSize)
            {
                return;
            }

            _reloading = true;
            _reloadDuration = weapon.EffectiveReloadTime();
            _reloadTimer = _reloadDuration;
        }

        private void UpdateReload(WeaponInstance weapon)
        {
            if (!_reloading)
            {
                return;
            }

            _reloadTimer -= Time.deltaTime;
            if (_reloadTimer > 0f)
            {
                return;
            }

            if (weapon.Definition.IncrementalReload)
            {

                weapon.AmmoInMagazine++;
                if (weapon.AmmoInMagazine >= weapon.Definition.MagazineSize)
                {
                    _reloading = false;
                }
                else
                {
                    _reloadTimer = _reloadDuration;
                }
            }
            else
            {
                weapon.AmmoInMagazine = weapon.Definition.MagazineSize;
                _reloading = false;
            }
        }

        private void CancelReload()
        {
            _reloading = false;
            _reloadTimer = 0f;
        }

        private Vector2 _lastAimDirection = Vector2.right;

        private Vector2 AimDirection()
        {
            if (_viewCamera == null)
            {
                _viewCamera = FindAnyObjectByType<Camera>();
                if (_viewCamera == null)
                {
                    return _lastAimDirection;
                }
            }

            Vector3 mouse = Input.mousePosition;
            mouse.z = -_viewCamera.transform.position.z;
            Vector3 world = _viewCamera.ScreenToWorldPoint(mouse);

            Vector2 direction = world - transform.position;
            if (direction.sqrMagnitude < 0.01f)
            {
                return _lastAimDirection;
            }

            _lastAimDirection = direction.normalized;
            return _lastAimDirection;
        }

        private void CreateSwingFlash()
        {
            var flashObject = new GameObject("MeleeSwingFlash");
            _swingFlash = flashObject.AddComponent<SpriteRenderer>();
            _swingFlash.sprite = RuntimeSprites.SolidSquare();
            _swingFlash.sortingOrder = 5;
            _swingFlash.enabled = false;
        }

        private void ShowSwingFlash(WeaponDefinition def, Vector2 aim, float radius, bool fullCircle)
        {
            float aimDegrees = Mathf.Atan2(aim.y, aim.x) * Mathf.Rad2Deg;
            float halfArc = def.MeleeArcDegrees * 0.5f;
            if (fullCircle)
            {
                halfArc = 180f;
            }
            _sweepStartDegrees = aimDegrees - halfArc;
            _sweepEndDegrees = aimDegrees + halfArc;

            float visualRadius = radius * SweepReachMultiplier;
            _sweepRadius = visualRadius;

            Sprite blade = WeaponAimDisplay.LoadSprite(def.SpriteResource);
            if (blade != null)
            {

                _swingFlash.sprite = blade;
                Vector2 worldSize = blade.bounds.size;
                float scale = worldSize.x > 0.01f ? (visualRadius * 0.95f) / worldSize.x : 1f;
                _swingFlash.transform.localScale = new Vector3(scale, scale, 1f);
                _swingFlash.color = Color.white;
                _swingBaseAlpha = 1f;
            }
            else
            {

                _swingFlash.sprite = RuntimeSprites.SolidSquare();
                _swingFlash.transform.localScale = new Vector3(visualRadius * 0.95f, 0.3f, 1f);
                Color color = def.Tint;
                color.a = 0.55f;
                _swingFlash.color = color;
                _swingBaseAlpha = 0.55f;
            }

            _swingFlash.enabled = true;
            _flashTimer = FlashDuration;

            UpdateSwingFlash();
        }

        private void UpdateSwingFlash()
        {
            if (_swingFlash == null || !_swingFlash.enabled)
            {
                return;
            }

            _flashTimer -= Time.deltaTime;
            if (_flashTimer <= 0f)
            {
                _swingFlash.enabled = false;
                return;
            }

            float progress = 1f - _flashTimer / FlashDuration;
            float angle = Mathf.Lerp(_sweepStartDegrees, _sweepEndDegrees, progress);
            float radians = angle * Mathf.Deg2Rad;
            var direction = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));

            _swingFlash.transform.position = transform.position + (Vector3)(direction * (_sweepRadius * 0.55f));
            _swingFlash.transform.rotation = Quaternion.Euler(0f, 0f, angle);

            _swingFlash.flipY = direction.x < 0f;

            Color color = _swingFlash.color;
            color.a = _swingBaseAlpha * Mathf.Clamp01(_flashTimer / (FlashDuration * 0.35f));
            _swingFlash.color = color;
        }
    }
}
