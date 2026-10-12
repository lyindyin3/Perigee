using DG.Tweening;
using DG.Tweening.Core;
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
        [SerializeField] BossComet boss;
        [SerializeField] AbilitySystem abilities;
        [SerializeField] CardPickView view;
        [SerializeField] AbilityPickView abilityView;
        [SerializeField] Visual beaconRing;

        Rng cardRng;
        // индексы в config.cards
        int[] eligible;
        int[] offer;
        int offerCount;
        int screens;
        float beaconTimer;
        // рост кольца МАЯКА, 0..1
        float wave;
        DOGetter<float> getWave;
        DOSetter<float> setWave;
        TweenCallback hideWave;

        public int OfferCount => offerCount;

        // «Новые карточки» за рекламу уже использованы в этом забеге
        public bool Rerolled { get; private set; }

        void Awake()
        {
            eligible = new int[config.cards.Length];
            offer = new int[config.cardChoices];
            getWave = () => wave;
            setWave = SetWave;
            hideWave = HideWave;
        }

        public void ResetRun(int seed)
        {
            cardRng = new Rng(seed);
            offerCount = 0;
            screens = 0;
            beaconTimer = 0f;
            Rerolled = false;
        }

        // другая тройка на открытом экране; первый экран забега остаётся первым
        public void Reroll()
        {
            Rerolled = true;
            Roll(screens == 1);
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
                    boss.Blast(config.beaconRadius, config.beaconDamage);
                    BeaconFx();
                }
            }
            // волна МАЯКА добила Комету: победа важнее экрана карточек
            if (loop.State != GameState.Playing) return;

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
                // свободных способностей не осталось: это повышение открывает карточки
                if (abilities.OfferCount > 0)
                {
                    loop.SetState(GameState.CardPick);
                    abilityView.Show();
                    return;
                }
            }

            Roll(screens == 0);
            // все карточки набраны до предела: уровень растёт без экрана выбора
            if (offerCount == 0) return;

            screens++;
            loop.SetState(GameState.CardPick);
            view.Show();
        }

        void Roll(bool first)
        {
            CardDef[] cards = config.cards;
            int n = 0;
            int forced = -1;
            for (int i = 0; i < cards.Length; i++)
            {
                if (!Eligible(i)) continue;
                // первый экран забега всегда предлагает ADD_SAT, пока спутников меньше максимума
                if (first && cards[i].id == CardId.AddSat && satellites.Count < config.maxSatellites) forced = i;
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
            beaconRing.gameObject.SetActive(true);
            DOTween.Kill(beaconRing);
            SetWave(0f);
            DOTween.To(getWave, setWave, 1f, config.beaconFxTime).SetTarget(beaconRing).SetLink(beaconRing.gameObject).OnComplete(hideWave);
        }

        void SetWave(float value)
        {
            wave = value;
            beaconRing.SetSizeMul(value * config.beaconRadius);
            beaconRing.SetAlpha(1f - value);
        }

        void HideWave()
        {
            beaconRing.gameObject.SetActive(false);
        }
    }
}
