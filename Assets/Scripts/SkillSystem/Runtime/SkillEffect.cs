using System.Collections.Generic;
using UnityEngine;
using Jinhyeong_GeneratedEnums;
using Jinhyeong_Managers;
using Jinhyeong_Common;

namespace Jinhyeong_SkillSystem
{
    /// <summary>발사된 스킬 GO의 런타임 동작 컴포넌트. 모션(Instant/Linear/Arc/Curve), 히트 디스패치(Single/AoE/Beam/Chain/Death), 디스폰 규칙을 보유.</summary>
    public class SkillEffect : BaseBehaviour
    {
        public enum MotionMode
        {
            Instant,
            Linear,
            Arc,
            Curve,
        }

        public string PoolKey = PoolManager.KeyEmpty;

        private CompiledSkill _c;
        private SkillObject _caster;
        private ESkillTeam _enemyTeam;
        private MotionMode _mode;

        private Vector3 _moveDir;
        private float _moveSpeed;
        private float _maxDistance;
        private Vector3 _startPos;

        private Vector3 _arcStart;
        private Vector3 _arcEnd;
        private float _arcDuration;
        private float _arcHeight;
        private float _curveAmplitude;

        private Vector3 _prevPos;

        private float _spawnTime;
        private bool _despawning;

        private readonly Dictionary<Damageable, float> _lastHitTime = new Dictionary<Damageable, float>(16);

        private int _pulseCount;

        private bool _oneShotDone;

        private static readonly List<Damageable> _scanBuf = new List<Damageable>(64);

        public void InitInstant(CompiledSkill c, SkillContext ctx)
        {
            CommonInit(c, ctx, MotionMode.Instant);
        }

        public void InitLinear(CompiledSkill c, SkillContext ctx, Vector3 dir, float speed, float maxDist)
        {
            CommonInit(c, ctx, MotionMode.Linear);
            _moveDir = dir.sqrMagnitude > 0.0001f ? dir.normalized : transform.forward;
            _moveSpeed = speed;
            _maxDistance = maxDist;
        }

        public void InitArc(CompiledSkill c, SkillContext ctx, Vector3 startPos, Vector3 endPos, float speed, float arcHeight)
        {
            CommonInit(c, ctx, MotionMode.Arc);
            _arcStart = startPos;
            _arcEnd = endPos;
            _arcHeight = arcHeight;
            float dist = Vector3.Distance(startPos, endPos);
            _arcDuration = speed > 0.01f ? dist / speed : 1f;
        }

        public void InitCurve(CompiledSkill c, SkillContext ctx, Vector3 startPos, Vector3 endPos, float speed, float amplitude)
        {
            CommonInit(c, ctx, MotionMode.Curve);
            _arcStart = startPos;
            _arcEnd = endPos;
            _curveAmplitude = amplitude;
            float dist = Vector3.Distance(startPos, endPos);
            _arcDuration = speed > 0.01f ? dist / speed : 1f;
        }

        private void CommonInit(CompiledSkill c, SkillContext ctx, MotionMode mode)
        {
            _c = c;
            _caster = ctx.Caster;
            _enemyTeam = SkillTeamUtil.Opposite(_caster != null ? _caster.Team : ESkillTeam.Friend);
            _mode = mode;
            _spawnTime = Time.time;
            _startPos = transform.position;
            _prevPos = transform.position;
            _despawning = false;
            _pulseCount = 0;
            _oneShotDone = false;
            _lastHitTime.Clear();
            ApplyHitShapeScale();
        }

        private void ApplyHitShapeScale()
        {
            if (_c == null || _c.HitNode == null)
                return;
            float radius = _c.HitNode.GetFloat(ESkillParamKey.Radius, _c.LevelData, 0f);
            if (radius > 0f)
            {
                transform.localScale = Vector3.one * (radius * 2f);
            }
        }

        private void Update()
        {
            if (_c == null || _despawning)
                return;

            switch (_mode)
            {
                case MotionMode.Instant:
                    break;

                case MotionMode.Linear:
                    if (StepLinear())
                        return;
                    break;

                case MotionMode.Arc:
                    if (StepArc(arc: true))
                        return;
                    break;

                case MotionMode.Curve:
                    if (StepArc(arc: false))
                        return;
                    break;
            }

            ProcessHit(immediate: false, seedTargets: null);
            _prevPos = transform.position;
            CheckTimedDespawn();
        }

        private bool StepLinear()
        {
            transform.position += _moveDir * _moveSpeed * Time.deltaTime;
            if ((transform.position - _startPos).sqrMagnitude >= _maxDistance * _maxDistance)
            {
                Despawn();
                return true;
            }
            return false;
        }

        private bool StepArc(bool arc)
        {
            float t = (Time.time - _spawnTime) / Mathf.Max(0.0001f, _arcDuration);
            if (t >= 1f)
            {
                transform.position = _arcEnd;
                Despawn();
                return true;
            }

            Vector3 baseLine = Vector3.Lerp(_arcStart, _arcEnd, t);
            if (arc)
            {
                baseLine.y += _arcHeight * 4f * t * (1f - t);
            }
            else
            {
                Vector3 forward = (_arcEnd - _arcStart).normalized;
                Vector3 right = Vector3.Cross(Vector3.up, forward);
                if (right.sqrMagnitude < 0.0001f)
                {
                    right = Vector3.Cross(Vector3.forward, forward);
                }
                right.Normalize();
                float lateral = Mathf.Sin(t * Mathf.PI * 2f) * _curveAmplitude;
                baseLine += right * lateral;
            }
            transform.position = baseLine;
            return false;
        }

        public void TryImmediateHit(IList<Damageable> seedTargets)
        {
            ProcessHit(immediate: true, seedTargets: seedTargets);
        }

        private void ProcessHit(bool immediate, IList<Damageable> seedTargets)
        {
            if (_c.HitNode == null)
                return;
            switch (_c.HitNode.NodeType)
            {
                case ESkillNodeType.SingleHit:
                    ProcessSingle(seedTargets);
                    break;
                case ESkillNodeType.AoEHit:
                    ProcessAoE(seedTargets);
                    break;
                case ESkillNodeType.BeamHit:
                    if (immediate == false)
                        ProcessBeamTick();
                    break;
                case ESkillNodeType.ChainLightningHit:
                    if (_oneShotDone == false)
                        ProcessBounce(seedTargets);
                    break;
                case ESkillNodeType.DeathChainHit:
                    ProcessExplode(seedTargets);
                    break;
            }
        }

        private void ProcessSingle(IList<Damageable> seedTargets)
        {
            float radius = _c.HitNode.GetFloat(ESkillParamKey.Radius, _c.LevelData, 0.5f);
            float damage = _c.HitNode.GetFloat(ESkillParamKey.Damage, _c.LevelData, 1f);

            Damageable d = seedTargets != null && seedTargets.Count > 0
                ? seedTargets[0]
                : FindNearestEnemy(radius);

            if (d == null || d.IsAlive == false)
                return;
            if (_lastHitTime.ContainsKey(d))
                return;

            DealDamageTo(d, damage);
            _lastHitTime[d] = Time.time;
            BumpPulse();
        }

        private void ProcessAoE(IList<Damageable> seedTargets)
        {
            float radius = _c.HitNode.GetFloat(ESkillParamKey.Radius, _c.LevelData, 0.5f);
            float damage = _c.HitNode.GetFloat(ESkillParamKey.Damage, _c.LevelData, 1f);
            int maxPerPulse = _c.HitNode.GetInt(ESkillParamKey.MaxPerTarget, null, int.MaxValue);

            int hits = 0;
            if (seedTargets != null)
            {
                for (int i = 0; i < seedTargets.Count; i++)
                {
                    if (TryDamageOnce(seedTargets[i], damage))
                        hits++;
                    if (hits >= maxPerPulse)
                        break;
                }
            }
            if (hits < maxPerPulse)
            {
                GatherEnemiesInSweep(_prevPos, transform.position, radius, _scanBuf);
                for (int i = 0; i < _scanBuf.Count && hits < maxPerPulse; i++)
                {
                    if (TryDamageOnce(_scanBuf[i], damage))
                        hits++;
                }
            }

            if (hits > 0)
            {
                BumpPulse();
            }
        }

        private void ProcessBeamTick()
        {
            float length = _c.HitNode.GetFloat(ESkillParamKey.Length, _c.LevelData, 5f);
            float width = _c.HitNode.GetFloat(ESkillParamKey.Width, _c.LevelData, 1f);
            float interval = _c.HitNode.GetFloat(ESkillParamKey.DamageInterval, null, 0.5f);
            int maxPerTarget = _c.HitNode.GetInt(ESkillParamKey.MaxPerTarget, null, int.MaxValue);
            float damage = _c.HitNode.GetFloat(ESkillParamKey.Damage, _c.LevelData, 1f);

            Vector3 origin = transform.position;
            Vector3 forward = transform.forward;
            float halfWidth = width * 0.5f;
            float lengthSq = length * length;

            int anyTickThisFrame = 0;
            IReadOnlyList<Damageable> all = Damageable.All;
            for (int i = 0; i < all.Count; i++)
            {
                Damageable d = all[i];
                if (d == null || d.IsAlive == false)
                    continue;
                if (d.Team != _enemyTeam)
                    continue;

                Vector3 to = d.transform.position - origin;
                float along = Vector3.Dot(to, forward);
                if (along < 0f || along * along > lengthSq)
                    continue;
                Vector3 perp = to - forward * along;
                if (perp.sqrMagnitude > halfWidth * halfWidth)
                    continue;

                if (_lastHitTime.TryGetValue(d, out float lastT))
                {
                    if (Time.time - lastT < interval)
                        continue;
                }
                if (CountHitsOn(d) >= maxPerTarget)
                    continue;

                DealDamageTo(d, damage);
                _lastHitTime[d] = Time.time;
                anyTickThisFrame++;
            }

            if (anyTickThisFrame > 0)
            {
                BumpPulse();
            }
        }

        private void ProcessBounce(IList<Damageable> seedTargets)
        {
            float jumpRange = _c.HitNode.GetFloat(ESkillParamKey.Range, _c.LevelData, 4f);
            float damage = _c.HitNode.GetFloat(ESkillParamKey.Damage, _c.LevelData, 1f);
            int maxJumps = _c.HitNode.GetInt(ESkillParamKey.MaxBounces, null, 3);

            Damageable current = seedTargets != null && seedTargets.Count > 0
                ? seedTargets[0]
                : FindNearestEnemy(jumpRange);

            if (current == null || current.IsAlive == false)
                return;

            DealDamageTo(current, damage);
            _lastHitTime[current] = Time.time;

            for (int j = 0; j < maxJumps; j++)
            {
                Damageable next = FindNearestUnchainedEnemy(current.transform.position, jumpRange);
                if (next == null)
                    break;
                DealDamageTo(next, damage);
                _lastHitTime[next] = Time.time;
                current = next;
            }

            _oneShotDone = true;
            BumpPulse();
        }

        private void ProcessExplode(IList<Damageable> seedTargets)
        {
            float radius = _c.HitNode.GetFloat(ESkillParamKey.Radius, _c.LevelData, 2f);
            float explodeRadius = _c.HitNode.GetFloat(ESkillParamKey.Range, _c.LevelData, radius);
            float damage = _c.HitNode.GetFloat(ESkillParamKey.Damage, _c.LevelData, 1f);

            Damageable primary = seedTargets != null && seedTargets.Count > 0
                ? seedTargets[0]
                : FindNearestEnemy(radius);

            if (primary == null || primary.IsAlive == false)
                return;
            if (_lastHitTime.ContainsKey(primary))
                return;

            Vector3 dyingPos = primary.transform.position;
            bool died = DealDamageTo(primary, damage);
            _lastHitTime[primary] = Time.time;

            if (died)
            {
                GatherEnemiesInRadius(dyingPos, explodeRadius, _scanBuf);
                for (int i = 0; i < _scanBuf.Count; i++)
                {
                    Damageable d = _scanBuf[i];
                    if (d == primary)
                        continue;
                    if (_lastHitTime.ContainsKey(d))
                        continue;
                    DealDamageTo(d, damage);
                    _lastHitTime[d] = Time.time;
                }
            }

            BumpPulse();
        }

        private void BumpPulse()
        {
            _pulseCount++;
            CheckHitCountDespawn();
        }

        private void CheckHitCountDespawn()
        {
            SkillNodeData ds = _c.DespawnNode;
            if (ds == null || ds.NodeType != ESkillNodeType.OnHitDespawn)
                return;
            int max = Mathf.Max(1, ds.GetInt(ESkillParamKey.Value, null, 1));
            if (_pulseCount >= max)
            {
                Despawn();
            }
        }

        private void CheckTimedDespawn()
        {
            SkillNodeData ds = _c.DespawnNode;
            if (ds == null || ds.NodeType != ESkillNodeType.DurationDespawn)
                return;
            float duration = ds.GetFloat(ESkillParamKey.Value, null, 1f);
            if (Time.time - _spawnTime >= duration)
            {
                Despawn();
            }
        }

        private void Despawn()
        {
            if (_despawning)
                return;
            _despawning = true;

            if (PoolManager.Instance != null
                && string.IsNullOrEmpty(PoolKey) == false
                && PoolKey != PoolManager.KeyEmpty)
            {
                PoolManager.Instance.Pool_Skill_Return(PoolKey, gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private bool TryDamageOnce(Damageable d, float damage)
        {
            if (d == null || d.IsAlive == false)
                return false;
            if (_lastHitTime.ContainsKey(d))
                return false;
            DealDamageTo(d, damage);
            _lastHitTime[d] = Time.time;
            return true;
        }

        private bool DealDamageTo(Damageable d, float rawDamage)
        {
            if (d == null || d.IsAlive == false)
                return false;
            float scaled = rawDamage * (_caster != null ? _caster.OutgoingDamageMultiplier : 1f);
            bool died = d.TakeDamage(scaled, _caster);
            ApplyDebuffsTo(d);
            return died;
        }

        private void ApplyDebuffsTo(Damageable target)
        {
            if (_c == null || _c.DebuffHitNodes == null)
                return;
            for (int i = 0; i < _c.DebuffHitNodes.Count; i++)
            {
                int debuffId = _c.DebuffHitNodes[i].GetInt(ESkillParamKey.DebuffId, null, 0);
                if (debuffId <= 0)
                    continue;
                SkillDebuffData data = SkillBuffRegistry.GetDebuff(debuffId);
                if (data == null)
                {
                    Debug.LogWarning($"[SkillEffect] DebuffId={debuffId} not in registry");
                    continue;
                }
                ActiveStatusEffect.ApplyDebuff(target.gameObject, data, _caster);
            }
        }

        private int CountHitsOn(Damageable d)
        {
            return _lastHitTime.ContainsKey(d) ? 1 : 0;
        }

        private Damageable FindNearestEnemy(float maxRadius)
        {
            float maxSq = maxRadius * maxRadius;
            Damageable best = null;
            float bestSq = float.MaxValue;
            IReadOnlyList<Damageable> all = Damageable.All;
            for (int i = 0; i < all.Count; i++)
            {
                Damageable d = all[i];
                if (d == null || d.IsAlive == false)
                    continue;
                if (d.Team != _enemyTeam)
                    continue;
                float sq = SweepPlanarDistSq(_prevPos, transform.position, d.transform.position);
                if (sq <= maxSq && sq < bestSq)
                {
                    bestSq = sq;
                    best = d;
                }
            }
            return best;
        }

        private Damageable FindNearestUnchainedEnemy(Vector3 from, float maxRadius)
        {
            float maxSq = maxRadius * maxRadius;
            Damageable best = null;
            float bestSq = float.MaxValue;
            IReadOnlyList<Damageable> all = Damageable.All;
            for (int i = 0; i < all.Count; i++)
            {
                Damageable d = all[i];
                if (d == null || d.IsAlive == false)
                    continue;
                if (d.Team != _enemyTeam)
                    continue;
                if (_lastHitTime.ContainsKey(d))
                    continue;
                float sq = PointPlanarDistSq(from, d.transform.position);
                if (sq <= maxSq && sq < bestSq)
                {
                    bestSq = sq;
                    best = d;
                }
            }
            return best;
        }

        private void GatherEnemiesInRadius(Vector3 origin, float radius, List<Damageable> outList)
        {
            outList.Clear();
            float rSq = radius * radius;
            IReadOnlyList<Damageable> all = Damageable.All;
            for (int i = 0; i < all.Count; i++)
            {
                Damageable d = all[i];
                if (d == null || d.IsAlive == false)
                    continue;
                if (d.Team != _enemyTeam)
                    continue;
                if (PointPlanarDistSq(d.transform.position, origin) <= rSq)
                {
                    outList.Add(d);
                }
            }
        }

        private void GatherEnemiesInSweep(Vector3 segStart, Vector3 segEnd, float radius, List<Damageable> outList)
        {
            outList.Clear();
            float rSq = radius * radius;
            IReadOnlyList<Damageable> all = Damageable.All;
            for (int i = 0; i < all.Count; i++)
            {
                Damageable d = all[i];
                if (d == null || d.IsAlive == false)
                    continue;
                if (d.Team != _enemyTeam)
                    continue;
                if (SweepPlanarDistSq(segStart, segEnd, d.transform.position) <= rSq)
                {
                    outList.Add(d);
                }
            }
        }

        private static float PointPlanarDistSq(Vector3 a, Vector3 b)
        {
            float dx = a.x - b.x;
            float dz = a.z - b.z;
            return dx * dx + dz * dz;
        }

        // XZ 평면에서 선분(segStart→segEnd)과 점(p) 사이 최단거리 제곱.
        // 빠른 발사체가 한 프레임에 적을 통과해도 잡히게 하기 위함. Y 차이는 무시한다(캐릭터/발사체 height 차이로 인한 miss 방지).
        private static float SweepPlanarDistSq(Vector3 segStart, Vector3 segEnd, Vector3 p)
        {
            float ax = segStart.x, az = segStart.z;
            float bx = segEnd.x,   bz = segEnd.z;
            float abx = bx - ax,   abz = bz - az;
            float ab2 = abx * abx + abz * abz;
            if (ab2 < 1e-6f)
            {
                float ddx = p.x - ax;
                float ddz = p.z - az;
                return ddx * ddx + ddz * ddz;
            }
            float apx = p.x - ax;
            float apz = p.z - az;
            float t = (apx * abx + apz * abz) / ab2;
            if (t < 0f)
                t = 0f;
            else if (t > 1f)
                t = 1f;
            float cx = ax + abx * t;
            float cz = az + abz * t;
            float ex = p.x - cx;
            float ez = p.z - cz;
            return ex * ex + ez * ez;
        }
    }
}
