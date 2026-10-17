using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Perigee
{
    // Сюжет: слайды с картинкой и печатающимся текстом рассказчика. Вступление перед первым забегом, финал после первой победы в эре 8.
    public class StoryView : MonoBehaviour
    {
        [SerializeField] GameConfig config;
        [SerializeField] ArtSet art;
        [SerializeField] AudioManager sfx;
        // картинка слайда; под ней Previous держит прошлую, пока новая проявляется
        [SerializeField] Image background;
        [SerializeField] Image previous;
        [SerializeField] TMP_Text speaker;
        [SerializeField] TMP_Text text;
        // «Бесконечный режим ждёт» под текстом последнего слайда финала
        [SerializeField] TMP_Text footer;
        [SerializeField] TMP_Text nextHint;
        [SerializeField] TMP_Text skipLabel;

        bool ending;
        int slide;
        float typed;
        int lastSound;
        Action done;

        void Awake()
        {
            Loc.Changed += Localize;
            Localize();
            nextHint.DOFade(0f, config.storyHintBlink).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo).SetUpdate(true).SetLink(gameObject);
        }

        void OnDestroy()
        {
            Loc.Changed -= Localize;
        }

        void Localize()
        {
            speaker.text = Strings.Narrator;
            nextHint.text = Strings.StoryNextHint;
            skipLabel.text = Strings.StorySkip;
            footer.text = Strings.EndlessAwaits;
            if (gameObject.activeSelf) text.text = Lines[slide];
        }

        string[] Lines => ending ? Strings.StoryEnding : Strings.StoryIntro;
        SpriteSlot[] Slides => ending ? art.storyEnding : art.storyIntro;

        // onDone вызывается после последнего слайда или по «Пропустить»
        public void Show(bool isEnding, Action onDone)
        {
            ending = isEnding;
            done = onDone;
            previous.sprite = null;
            previous.enabled = false;
            gameObject.SetActive(true);
            Open(0);
        }

        void Open(int index)
        {
            slide = index;
            typed = 0f;
            lastSound = 0;
            text.text = Lines[slide];
            text.maxVisibleCharacters = 0;
            nextHint.gameObject.SetActive(false);
            footer.gameObject.SetActive(ending && slide == Lines.Length - 1);

            // прошлая картинка остаётся снизу, новая проявляется поверх и медленно приближается
            if (slide > 0)
            {
                previous.sprite = background.sprite;
                previous.enabled = true;
            }
            background.sprite = Slides[slide].sprite;
            background.DOKill();
            background.rectTransform.DOKill();
            background.color = new Color(1f, 1f, 1f, 0f);
            background.DOFade(1f, config.storyFadeTime).SetUpdate(true).SetLink(gameObject);
            background.rectTransform.localScale = Vector3.one;
            background.rectTransform.DOScale(config.storyZoom, config.storyZoomTime).SetEase(Ease.Linear).SetUpdate(true).SetLink(gameObject);
        }

        bool Typing => text.maxVisibleCharacters < text.text.Length;

        // нажатие в любом месте: во время печати строка дописывается, после — следующий слайд
        public void Tap()
        {
            if (Typing)
            {
                typed = text.text.Length;
                ShowTyped();
                return;
            }

            if (slide + 1 < Lines.Length)
            {
                sfx.Play(SfxId.StoryNext);
                Open(slide + 1);
            }
            else Finish();
        }

        public void Skip()
        {
            sfx.Play(SfxId.UiClick);
            Finish();
        }

        void Finish()
        {
            gameObject.SetActive(false);
            done();
        }

        void Update()
        {
            if (!Typing) return;
            typed += config.storyCharsPerSec * Time.unscaledDeltaTime;
            ShowTyped();
            if (text.maxVisibleCharacters - lastSound >= config.storyTypeEvery)
            {
                lastSound = text.maxVisibleCharacters;
                sfx.Play(SfxId.StoryType);
            }
        }

        void ShowTyped()
        {
            text.maxVisibleCharacters = Mathf.Min((int)typed, text.text.Length);
            nextHint.gameObject.SetActive(!Typing);
        }
    }
}
