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
        [SerializeField] BossSystem boss;
        [SerializeField] StarController star;
        [SerializeField] AbilitySystem abilities;
        [SerializeField] CardPickView view;
        [SerializeField] AbilityPickView abilityView;
        [SerializeField] Visual beaconRing;
        [SerializeField] AudioManager sfx;

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
            // карточек на экране бывает больше cardChoices (чертёж ВЫБОР), но не больше, чем есть всего
            offer = new int[config.cards.Length];
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

        // сколько бесплатных замен (чертёж ВЫБОР) осталось в этом забеге
        public int FreeRerolls => loop.Stats.freeRerolls - loop.Run.freeRerollsUsed;

        // другие карточки на открытом экране за рекламу; первый экран забега остаётся первым
        public void Reroll()
        {
            Rerolled = true;
            Roll(screens == 1);
        }

        // то же бесплатно
        public void FreeReroll()
        {
            loop.Run.freeRerollsUsed++;
            Roll(screens == 1);
        }

        public float Need(int level)
        {
            return (config.xpBase + config.xpPerLevel * level) * loop.Stats.xpNeedMul;
        }

        public CardDef Offer(int slot)
        {
            return config.cards[offer[slot]];
        }

        // сколько стаков предложенной карточки уже взято
        public int Stacks(int slot)
        {
            return loop.Run.cardStacks[offer[slot]];
        }

        public void Tick(float dt)
        {
            Stats stats = loop.Stats;
            // СОЛНЕЧНЫЙ ВЕТЕР: у звезды враги вдвое медленнее (область действует в следующем шаге врагов)
            if (stats.Has(StatId.SolarWind)) enemies.Field(Vector2.zero, config.starRadius + config.solarWindRange, 0f, config.solarWindMul, 0f);

            if (stats.beacons > 0)
            {
                beaconTimer += dt;
                // второй МАЯК укорачивает период
                float interval = config.beaconInterval * Mathf.Pow(config.beaconStackMul, loop.Stats.beacons - 1);
                if (beaconTimer >= interval)
                {
                    beaconTimer -= interval;
                    // ПРОТУБЕРАНЕЦ: волна шире, сильнее и лечит звезду
                    enemies.Blast(Vector2.zero, stats.beaconRadius, stats.beaconDamage);
                    boss.Blast(Vector2.zero, stats.beaconRadius, stats.beaconDamage);
                    if (stats.beaconHeal > 0f) star.Heal(stats.beaconHeal);
                    BeaconFx();
                    sfx.Play(SfxId.Pulse);
                }
            }
            // волна МАЯКА добила босса: победа важнее экрана карточек
            if (loop.State != GameState.Playing) return;

            // за шаг открывается один экран: остаток опыта переносится, следующий экран откроется после выбора
            RunState run = loop.Run;
            if (run.xp >= Need(run.level)) LevelUp();
        }

        public void Pick(int slot)
        {
            int index = offer[slot];
            CardDef card = config.cards[index];
            RunState run = loop.Run;
            // показанные эволюции первое место больше не занимают
            for (int k = 0; k < offerCount; k++)
                if (config.cards[offer[k]].color == CardColor.Evolution) run.evoShown |= Bit(config.cards[offer[k]].id);
            if (card.color == CardColor.Evolution)
            {
                loop.Save.Data.evolutionsTaken++;
                loop.Save.Save();
            }
            run.cardStacks[index]++;
            loop.RecomputeStats();
            // STAR_PLATING лечит на столько же, на сколько поднимает максимум (стеклянную звезду не лечит ничто); ЖАДНОСТЬ срезает HP сверх нового максимума
            float heal = card.plus.stat == StatId.StarMaxHp && !loop.Run.Has(RunModifier.GlassStar) ? card.plus.value : 0f;
            loop.Run.hp = Mathf.Min(loop.Run.hp + heal, loop.Stats.starMaxHp);
        }

        void LevelUp()
        {
            RunState run = loop.Run;
            run.xp -= Need(run.level);
            run.level++;
            sfx.Play(SfxId.LevelUp);

            // это повышение уровня открывает выбор второй способности вместо карточек; «Без способностей»: карточки
            if (run.level == config.abilityPickLevel && !run.Has(RunModifier.NoAbilities))
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
            RunState run = loop.Run;
            int n = 0;
            int forced = -1;
            int evolution = -1;
            for (int i = 0; i < cards.Length; i++)
            {
                if (!Eligible(i)) continue;
                // первый экран забега всегда предлагает ADD_SAT, пока спутников меньше максимума
                if (first && cards[i].id == CardId.AddSat) forced = i;
                // готовая эволюция в первый раз встаёт на первое место
                if (evolution < 0 && cards[i].color == CardColor.Evolution && (run.evoShown & Bit(cards[i].id)) == 0) evolution = i;
                eligible[n++] = i;
            }

            offerCount = Mathf.Min(config.cardChoices + loop.Stats.extraChoices, n);
            bool forcedMissing = forced >= 0;
            for (int k = 0; k < offerCount; k++)
            {
                // выбор по весам без возврата: на место k встаёт одна из ещё не выбранных, чем больше вес, тем чаще
                int total = 0;
                for (int j = k; j < n; j++) total += Weight(cards[eligible[j]]);
                int roll = cardRng.Range(0, total);
                int pick = k;
                while (roll >= Weight(cards[eligible[pick]])) roll -= Weight(cards[eligible[pick++]]);
                int index = eligible[pick];
                eligible[pick] = eligible[k];
                eligible[k] = index;
                offer[k] = index;
                if (index == forced) forcedMissing = false;
            }
            if (forcedMissing) offer[cardRng.Range(0, offerCount)] = forced;
            if (evolution >= 0)
            {
                // уже вытянута: меняется местами с первой, иначе встаёт на первое место
                int at = System.Array.IndexOf(offer, evolution, 0, offerCount);
                if (at > 0) offer[at] = offer[0];
                offer[0] = evolution;
            }
            Seen();
        }

        // предложенные карточки отмечаются в сохранении (Атлас показывает только их)
        void Seen()
        {
            SaveData data = loop.Save.Data;
            long seen = data.seenCards;
            for (int k = 0; k < offerCount; k++) seen |= Bit(config.cards[offer[k]].id);
            if (seen == data.seenCards) return;
            data.seenCards = seen;
            loop.Save.Save();
        }

        static long Bit(CardId id)
        {
            return 1L << (int)id;
        }

        // номер карточки в config.cards
        public int Index(CardId id)
        {
            CardDef[] cards = config.cards;
            for (int i = 0; i < cards.Length; i++)
                if (cards[i].id == id) return i;
            return -1;
        }

        int Weight(CardDef card)
        {
            if (card.color == CardColor.Evolution) return config.weightEvolution;
            if (card.era != 0) return config.weightEra;
            return card.rarity == CardRarity.Rare ? config.weightRare : config.weightCommon;
        }

        bool Eligible(int i)
        {
            CardDef card = config.cards[i];
            RunState run = loop.Run;
            // у ADD_SAT предела стаков нет: карточка идёт, пока спутников меньше максимума; в «Одиночке» её нет
            if (card.id == CardId.AddSat) return !run.Has(RunModifier.SingleSat) && 1 + loop.Stats.extraSatellites < satellites.Cap;
            if (run.cardStacks[i] >= card.maxStacks) return false;
            // эволюции нужны обе карточки-источника на максимуме
            if (card.color == CardColor.Evolution) return Maxed(card.sourceA) && Maxed(card.sourceB);
            // «Без способностей»: их карточки не нужны
            if ((card.id == CardId.AbilityHaste || card.id == CardId.AbilityLong) && run.Has(RunModifier.NoAbilities)) return false;
            // карточка эры идёт только в своей эре
            if (card.era != 0 && card.era != loop.EraNumber) return false;
            // нити нужна пара спутников
            if (card.id == CardId.SawThread && satellites.Count < ResonancePair) return false;
            // ABILITY_LONG нужна способность с длительностью
            if (card.id == CardId.AbilityLong && !abilities.HasTimed) return false;
            return true;
        }

        bool Maxed(CardId id)
        {
            int i = Index(id);
            return loop.Run.cardStacks[i] >= config.cards[i].maxStacks;
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
            beaconRing.SetSizeMul(value * loop.Stats.beaconRadius);

            beaconRing.SetAlpha(1f - value);
        }

        void HideWave()
        {
            beaconRing.gameObject.SetActive(false);
        }
    }
}
