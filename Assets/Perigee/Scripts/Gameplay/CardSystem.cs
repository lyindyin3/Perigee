using DG.Tweening;
using UnityEngine;

namespace Perigee
{
    public class CardSystem : MonoBehaviour
    {
        // резонансу нужна пара спутников
        const int ResonancePair = 2;

        [SerializeField] GameConfig config;
        [SerializeField] GameLoop loop;
        [SerializeField] SatelliteSystem satellites;
        [SerializeField] EnemySystem enemies;
        [SerializeField] AbilitySystem abilities;
        [SerializeField] CardPickView view;
        [SerializeField] AbilityPickView abilityView;
        [SerializeField] Transform fxRoot;

        SpriteRenderer beaconRing;
        Rng cardRng;
        // индексы в config.cards
        int[] eligible;
        int[] offer;
        int offerCount;
        int screens;
        float beaconTimer;

        public int OfferCount => offerCount;

        void Awake()
        {
            eligible = new int[config.cards.Length];
            offer = new int[config.cardChoices];
            // толщина задаётся в мировых единицах, поэтому в текстуре она делится на радиус волны
            Sprite ring = ShapeFactory.Arc(360f, config.beaconFxWidth / config.beaconRadius);
            beaconRing = ShapeFactory.NewRenderer("Beacon", fxRoot, ring, config.cream, config.sortFx, config.spriteMaterial);
            beaconRing.enabled = false;
        }

        public void ResetRun(int seed)
        {
            cardRng = new Rng(seed);
            offerCount = 0;
            screens = 0;
            beaconTimer = 0f;
        }

        public int Need(int level)
        {
            return config.xpBase + config.xpPerLevel * level;
        }

        public CardDef Offer(int slot)
        {
            return config.cards[offer[slot]];
        }

        public void Tick(float dt)
        {
            if (loop.Stats.beacons > 0)
            {
                beaconTimer += dt;
                if (beaconTimer >= config.beaconInterval)
                {
                    beaconTimer -= config.beaconInterval;
                    enemies.Blast(config.beaconRadius, config.beaconDamage);
                    BeaconFx();
                }
            }

            // за шаг открывается один экран: остаток опыта переносится, следующий экран откроется после выбора
            RunState run = loop.Run;
            if (run.xp >= Need(run.level)) LevelUp();
        }

        public void Pick(int slot)
        {
            int index = offer[slot];
            CardDef card = config.cards[index];
            loop.Run.cardStacks[index]++;
            loop.RecomputeStats();
            // STAR_PLATING лечит на столько же, на сколько поднимает максимум
            if (card.plus.stat == StatId.StarMaxHp) loop.Run.hp = Mathf.Min(loop.Run.hp + card.plus.value, loop.Stats.starMaxHp);
        }

        void LevelUp()
        {
            RunState run = loop.Run;
            run.xp -= Need(run.level);
            run.level++;

            // это повышение уровня открывает выбор второй способности вместо карточек
            if (run.level == config.abilityPickLevel)
            {
                abilities.OfferRandom(cardRng);
                loop.SetState(GameState.CardPick);
                abilityView.Show();
                return;
            }

            Roll();
            // все карточки набраны до предела: уровень растёт без экрана выбора
            if (offerCount == 0) return;

            screens++;
            loop.SetState(GameState.CardPick);
            view.Show();
        }

        void Roll()
        {
            CardDef[] cards = config.cards;
            int n = 0;
            int forced = -1;
            for (int i = 0; i < cards.Length; i++)
            {
                if (!Eligible(i)) continue;
                // первый экран забега всегда предлагает ADD_SAT, пока спутников меньше максимума
                if (screens == 0 && cards[i].id == CardId.AddSat && satellites.Count < config.maxSatellites) forced = i;
                eligible[n++] = i;
            }

            offerCount = Mathf.Min(offer.Length, n);
            bool forcedMissing = forced >= 0;
            for (int k = 0; k < offerCount; k++)
            {
                // частичная тасовка: на место k встаёт случайная из ещё не выбранных
                int pick = cardRng.Range(k, n);
                int index = eligible[pick];
                eligible[pick] = eligible[k];
                eligible[k] = index;
                offer[k] = index;
                if (index == forced) forcedMissing = false;
            }
            if (forcedMissing) offer[cardRng.Range(0, offerCount)] = forced;
        }

        bool Eligible(int i)
        {
            CardDef card = config.cards[i];
            if (loop.Run.cardStacks[i] >= card.maxStacks) return false;
            if (card.id == CardId.Resonator && satellites.Count < ResonancePair) return false;
            // ABILITY_LONG нужна способность с длительностью
            if (card.id == CardId.AbilityLong && !abilities.HasTimed) return false;
            return true;
        }

        void BeaconFx()
        {
            Transform ring = beaconRing.transform;
            ring.localScale = Vector3.zero;
            beaconRing.color = config.cream;
            beaconRing.enabled = true;
            ring.DOScale(new Vector3(config.beaconRadius, config.beaconRadius, 1f), config.beaconFxTime).SetLink(beaconRing.gameObject);
            beaconRing.DOFade(0f, config.beaconFxTime).SetLink(beaconRing.gameObject);
        }
    }
}
