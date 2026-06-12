using UnityEngine;
using ADCREA.Dungeon;
using ADCREA.Enemies;

namespace ADCREA.Weapons
{
    /// <summary>
    /// Fires whatever the inventory has equipped, Enter-the-Gungeon style.
    ///
    /// Everything flows from the WeaponInstance's effective stats, so upgrades change
    /// behaviour without any special cases here:
    ///  - Fire modes: semi-auto clicks, full-auto hold, melee swings.
    ///  - Spread: each projectile deviates up to half the inaccuracy cone either side of
    ///    the cursor; the revolver's cone shrinks back to zero over a second of not firing.
    ///  - Crits: rolled per shot. Chance above 100% spills into "double crit" (4x damage),
    ///    the shotgun trades crit damage for double pellets, the revolver doubles its
    ///    crit chance on the cylinder's last round, the broadsword crits in a full circle.
    ///  - Reloads: R or an empty magazine starts one; the shotgun loads shell by shell
    ///    and firing mid-reload keeps whatever was already chambered.
    /// </summary>
    [RequireComponent(typeof(WeaponInventory))]
    public class WeaponController : MonoBehaviour
    {
        public KeyCode reloadKey = KeyCode.R;

        private WeaponInventory _inventory;
        private WeaponAimDisplay _display;
        private Camera _viewCamera;
        private System.Random _rng;

        private WeaponInstance _activeWeapon;   // Tracked to detect Q/E swaps mid-reload.
        private float _cooldownTimer;
        private float _cooldownDuration;        // Remembered so the meter can show progress.
        private float _timeSinceLastShot = 99f; // Large start: the first shot is always fully settled.

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
        private const float FlashDuration = 0.18f;
        private const float HitscanMaxDistance = 40f;

        public bool IsReloading
        {
            get { return _reloading; }
        }

        /// <summary>0..1 progress of the running reload (per shell for the shotgun).</summary>
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
                // The click that picks a menu card is usually still held when the game
                // unfreezes; without this gate an automatic weapon would fire it.
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
                // Swapping weapons drops the reload in progress; the magazine keeps
                // whatever was already loaded because that state lives on the instance.
                _activeWeapon = weapon;
                CancelReload();
                _cooldownTimer = 0f;
                _timeSinceLastShot = 99f;
            }

            if (weapon == null)
            {
                _display.Hide();
                return;
            }

            Vector2 aim = AimDirection();
            _display.Show(weapon.Definition, aim);

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
            // Only true automatics repeat while held; semi-auto guns and melee swings
            // demand a click each, so neither can be spammed by parking the button.
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
                // The shotgun may interrupt its shell-by-shell reload to fire what it has;
                // magazine reloads block the trigger until they finish.
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
            // Attack speed 0 marks the uncapped revolver: clicking IS the rate limit.
            if (attacksPerSecond > 0f)
            {
                _cooldownTimer = 1f / attacksPerSecond;
                _cooldownDuration = _cooldownTimer;
            }
        }

        // ---------------------------------------------------------- cooldown meter

        // The thin white line floating over the player's head: empty right after a
        // shot, full when the trigger is ready again. Hidden entirely while ready so
        // the screen stays clean between fights.
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

            // Sub-tenth-of-a-second cooldowns flicker more than they inform.
            bool visible = _cooldownTimer > 0f && _cooldownDuration > 0.1f && _activeWeapon != null;
            _cooldownBack.enabled = visible;
            _cooldownFill.enabled = visible;
            if (!visible)
            {
                return;
            }

            float progress = 1f - Mathf.Clamp01(_cooldownTimer / _cooldownDuration);
            float width = CooldownMeterWidth * progress;
            // Grows from the left edge: the fill's centre shifts right as it widens.
            _cooldownFill.transform.localScale = new Vector3(width, 0.07f, 1f);
            _cooldownFill.transform.localPosition = new Vector3(
                -(CooldownMeterWidth - width) * 0.5f, 1.25f, 0f);
        }

        // ------------------------------------------------------------------ shooting

        private void FireShot(WeaponInstance weapon, Vector2 aim)
        {
            WeaponDefinition def = weapon.Definition;

            float critChance = weapon.EffectiveCritChance();
            // Revolver quirk: the last round in the cylinder crits twice as often.
            if (def.DoubleCritOnLastShot && weapon.AmmoInMagazine == 1)
            {
                critChance *= 2f;
            }
            int critTier = RollCritTier(critChance);

            int pellets = def.PelletsPerShot;
            float damage = weapon.EffectiveDamage();

            if (critTier > 0 && def.CritPelletsPerShot > 0)
            {
                // Shotgun crits never raise damage - per spec the entire crit effect
                // is the doubled pellet count, on double crits included.
                pellets = def.CritPelletsPerShot;
            }
            else
            {
                damage *= CritDamageMultiplier(critTier);
            }

            Vector3 muzzle = _display.MuzzlePosition(transform.position, aim, def);
            float spread = CurrentSpreadDegrees(weapon);

            // Muzzle flash in the weapon's colour - cheap square shards, one burst per
            // trigger pull regardless of pellet count.
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

            // An empty magazine reloads on its own - waiting for a dead-click adds
            // nothing but frustration at this scope.
            if (weapon.AmmoInMagazine <= 0)
            {
                StartReload(weapon);
            }
        }

        private void FireHitscan(WeaponDefinition def, Vector3 muzzle, Vector2 aim, float damage)
        {
            Vector3 endPoint = muzzle + (Vector3)(aim * HitscanMaxDistance);
            int enemiesHit = 0;
            int maxEnemies = def.PierceCount + 1; // Piercing through 1 enemy means hitting 2.

            // RaycastAll returns hits sorted by distance, so walking the array in order
            // is walking along the bullet's path.
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

                // Anything solid that is not an enemy is a wall - the shot ends here.
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
                // Full cone right after a shot, narrowing linearly to perfect accuracy
                // once BloomRecoverySeconds have passed without firing.
                float recovery = Mathf.Clamp01(_timeSinceLastShot / weapon.Definition.BloomRecoverySeconds);
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
            // The cone is centred on the cursor: half the spread to either side.
            return ((float)_rng.NextDouble() - 0.5f) * spreadDegrees;
        }

        private static Vector2 RotateByDegrees(Vector2 vector, float degrees)
        {
            float radians = degrees * Mathf.Deg2Rad;
            float cos = Mathf.Cos(radians);
            float sin = Mathf.Sin(radians);
            return new Vector2(vector.x * cos - vector.y * sin, vector.x * sin + vector.y * cos);
        }

        /// <summary>
        /// 0 = normal, 1 = crit (2x), 2 = double crit (4x). Chance above 100% guarantees
        /// the crit and rolls the overflow as the double-crit chance, per project spec
        /// (130% = always crit, 30% of shots quadruple).
        /// </summary>
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

        // ------------------------------------------------------------------ melee

        private void MeleeSwing(WeaponInstance weapon, Vector2 aim)
        {
            WeaponDefinition def = weapon.Definition;

            int critTier = RollCritTier(weapon.EffectiveCritChance());
            float damage = weapon.EffectiveDamage() * CritDamageMultiplier(critTier);
            bool fullCircle = critTier > 0 && def.CritHitsFullCircle;

            // Range is measured from the player's edge; the body radius makes a 1-unit
            // sword feel like 1 unit of blade instead of vanishing inside the collider.
            // Kept tight on purpose - a generous swing radius made the sword feel like
            // it hit things it visibly should not have.
            float radius = 0.6f + def.MeleeRange;

            ShowSwingFlash(aim, radius, def.MeleeArcDegrees, fullCircle, def.Tint);

            // The small extra accepts enemies whose centre is just outside the arc
            // but whose body is inside it.
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, radius + 0.25f);
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

        // ------------------------------------------------------------------ reloading

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
                // One shell at a time; the timer restarts until the tube is full.
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

        // ------------------------------------------------------------------ aiming

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

            // For an orthographic camera the z component is the distance from the camera
            // plane; the camera's own height lands the point on the z = 0 gameplay plane.
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

        // ------------------------------------------------------------------ swing visual

        // One flash object reused forever: swings happen far too often to allocate and
        // destroy a GameObject each time.
        private void CreateSwingFlash()
        {
            var flashObject = new GameObject("MeleeSwingFlash");
            _swingFlash = flashObject.AddComponent<SpriteRenderer>();
            _swingFlash.sprite = RuntimeSprites.SolidSquare();
            _swingFlash.sortingOrder = 5;
            _swingFlash.enabled = false;
        }

        /// <summary>
        /// Arms the swing animation: a blade-shaped quad that UpdateSwingFlash sweeps
        /// across the weapon's arc (the full circle on a crit). The damage was already
        /// applied instantly when the swing started - the sweep is pure presentation,
        /// so animation timing can be tuned without touching combat balance.
        /// </summary>
        private void ShowSwingFlash(Vector2 aim, float radius, float arcDegrees, bool fullCircle, Color tint)
        {
            float aimDegrees = Mathf.Atan2(aim.y, aim.x) * Mathf.Rad2Deg;
            float halfArc = arcDegrees * 0.5f;
            if (fullCircle)
            {
                halfArc = 180f;
            }
            _sweepStartDegrees = aimDegrees - halfArc;
            _sweepEndDegrees = aimDegrees + halfArc;
            _sweepRadius = radius;

            // A thin blade rather than a filled wedge: watching it travel communicates
            // the 90-degree coverage better than a static block ever did.
            _swingFlash.transform.localScale = new Vector3(radius * 0.95f, 0.3f, 1f);

            Color color = tint;
            color.a = 0.55f;
            _swingFlash.color = color;
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

            // Anchored to the live player position so the blade stays in hand even
            // when the swing happens mid-run.
            float progress = 1f - _flashTimer / FlashDuration;
            float angle = Mathf.Lerp(_sweepStartDegrees, _sweepEndDegrees, progress);
            float radians = angle * Mathf.Deg2Rad;
            var direction = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));

            _swingFlash.transform.position = transform.position + (Vector3)(direction * (_sweepRadius * 0.55f));
            _swingFlash.transform.rotation = Quaternion.Euler(0f, 0f, angle);

            // Full strength for most of the sweep, quick fade right at the end.
            Color color = _swingFlash.color;
            color.a = 0.55f * Mathf.Clamp01(_flashTimer / (FlashDuration * 0.35f));
            _swingFlash.color = color;
        }
    }
}
