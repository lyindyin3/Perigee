using DG.Tweening;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Perigee
{
    public class AbilitySystem : MonoBehaviour
    {
        public const int SlotA = 0;
        public const int SlotB = 1;
        const int SlotCount = 2;

        [SerializeField] GameConfig config;
        [SerializeField] GameLoop loop;
        [SerializeField] EnemySystem enemies;
        [SerializeField] Transform fxRoot;

        readonly AbilityId[] slots = new AbilityId[SlotCount];
        readonly bool[] filled = new bool[SlotCount];
        // таймеры по номеру AbilityId; длительность и перезарядка запоминаются в момент нажатия
        float[] elapsed;
        float[] duration;
        float[] cooldown;
        // предложение на экране выбора
        AbilityId[] offer;
        int offerCount;
        int offerSlot;
        SpriteRenderer pulseRing;
        InputAction[] keys;

        public int OfferCount => offerCount;

        void Awake()
        {
            int n = config.abilities.Length;
            elapsed = new float[n];
            duration = new float[n];
            cooldown = new float[n];
            offer = new AbilityId[n];

            // толщина задаётся в мировых единицах, поэтому в текстуре она делится на радиус волны
            Sprite ring = ShapeFactory.Arc(360f, config.pulseFxWidth / config.pulseRadius);
            pulseRing = ShapeFactory.NewRenderer("Pulse", fxRoot, ring, config.cream, config.sortFx, config.spriteMaterial);
            pulseRing.enabled = false;

            // по номеру слота: Q = слот A, E = слот B
            keys = new[] { new InputAction(binding: "<Keyboard>/q"), new InputAction(binding: "<Keyboard>/e") };
            for (int i = 0; i < keys.Length; i++) keys[i].Enable();
        }

        void OnDestroy()
        {
            for (int i = 0; i < keys.Length; i++) keys[i].Dispose();
        }

        void Update()
        {
            for (int i = 0; i < keys.Length; i++)
                if (keys[i].WasPressedThisFrame()) Press(i);
        }

        public void ResetRun()
        {
            // слот A остаётся с прошлого забега: Retry экран выбора пропускает
            filled[SlotB] = false;
            System.Array.Clear(elapsed, 0, elapsed.Length);
            System.Array.Clear(duration, 0, duration.Length);
            System.Array.Clear(cooldown, 0, cooldown.Length);
        }

        public void Tick(float dt)
        {
            for (int i = 0; i < elapsed.Length; i++) elapsed[i] += dt;
        }

        // вызывают кнопки способностей, номер слота задан в сцене
        public void Press(int slot)
        {
            if (loop.State != GameState.Playing || !filled[slot]) return;

            AbilityId id = slots[slot];
            int a = (int)id;
            // во время эффекта и перезарядки нажатие ничего не делает
            if (elapsed[a] < cooldown[a] || elapsed[a] < duration[a]) return;

            AbilityDef def = Def(id);
            elapsed[a] = 0f;
            duration[a] = def.duration * loop.Stats.abilityDurationMul;
            cooldown[a] = def.cooldown * loop.Stats.abilityCooldownMul;
            if (id == AbilityId.Pulse) Pulse();
        }

        public bool Filled(int slot)
        {
            return filled[slot];
        }

        public AbilityId Id(int slot)
        {
            return slots[slot];
        }

        // доля прошедшей перезарядки, 1 = готова
        public float Charge(int slot)
        {
            int a = (int)slots[slot];
            return elapsed[a] >= cooldown[a] ? 1f : elapsed[a] / cooldown[a];
        }

        public bool Active(AbilityId id)
        {
            return elapsed[(int)id] < duration[(int)id];
        }

        // секунды с момента нажатия
        public float Elapsed(AbilityId id)
        {
            return elapsed[(int)id];
        }

        // РОСТ: плавный вход и выход за growScaleTime, 0..1
        public float GrowBlend
        {
            get
            {
                int a = (int)AbilityId.Grow;
                return Mathf.Clamp01(Mathf.Min(elapsed[a], duration[a] - elapsed[a]) / config.growScaleTime);
            }
        }

        // есть ли способность с длительностью: условие карточки ABILITY_LONG
        public bool HasTimed
        {
            get
            {
                for (int i = 0; i < SlotCount; i++)
                    if (filled[i] && Def(slots[i]).duration > 0f) return true;
                return false;
            }
        }

        public AbilityDef Offer(int i)
        {
            return Def(offer[i]);
        }

        // слот A: на выбор все способности
        public void OfferAll()
        {
            offerSlot = SlotA;
            offerCount = config.abilities.Length;
            for (int i = 0; i < offerCount; i++) offer[i] = config.abilities[i].id;
        }

        // слот B: случайные из ещё не взятых
        public void OfferRandom(Rng rng)
        {
            offerSlot = SlotB;
            int n = 0;
            for (int i = 0; i < config.abilities.Length; i++)
            {
                AbilityId id = config.abilities[i].id;
                if (!Owns(id)) offer[n++] = id;
            }

            offerCount = Mathf.Min(config.abilityChoices, n);
            for (int k = 0; k < offerCount; k++)
            {
                // частичная тасовка: на место k встаёт случайная из ещё не выбранных
                int pick = rng.Range(k, n);
                AbilityId id = offer[pick];
                offer[pick] = offer[k];
                offer[k] = id;
            }
        }

        public void Pick(int i)
        {
            slots[offerSlot] = offer[i];
            filled[offerSlot] = true;
        }

        bool Owns(AbilityId id)
        {
            for (int i = 0; i < SlotCount; i++)
                if (filled[i] && slots[i] == id) return true;
            return false;
        }

        void Pulse()
        {
            enemies.Pulse(config.pulseRadius, config.pulseDamage, config.pulsePush);

            Transform ring = pulseRing.transform;
            ring.localScale = Vector3.zero;
            pulseRing.color = config.cream;
            pulseRing.enabled = true;
            ring.DOScale(new Vector3(config.pulseRadius, config.pulseRadius, 1f), config.pulseTime).SetLink(pulseRing.gameObject);
            pulseRing.DOFade(0f, config.pulseTime).SetEase(Ease.InQuad).SetLink(pulseRing.gameObject);
        }

        AbilityDef Def(AbilityId id)
        {
            AbilityDef[] defs = config.abilities;
            for (int i = 0; i < defs.Length; i++)
                if (defs[i].id == id) return defs[i];
            return null;
        }
    }
}
