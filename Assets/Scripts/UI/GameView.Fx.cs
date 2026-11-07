using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace CardHome
{
    // Эффекты из Art/Fx: короткие вспышки поверх доски и карт. Только DOTween, без Update.
    // Нет картинки — эффекта просто нет, игра от этого не ломается.
    public partial class GameView
    {
        static Sprite FxArt(string name) => ArtStore.Get("Fx/" + name, null);

        // Вспышка в точке parent: появляется с подскоком, поднимается на rise·size и тает. Возвращает картинку (или null).
        Image Burst(RectTransform parent, string name, Vector2 pos, float size, float rise = 0.35f, float life = 0.9f)
        {
            var sprite = FxArt(name);
            if (sprite == null || parent == null) return null;
            var img = UIKit.Image("Fx" + name, parent, Color.white, sprite);
            img.raycastTarget = false;
            img.preserveAspect = true;
            var rt = img.rectTransform;
            rt.sizeDelta = new Vector2(size, size);
            rt.anchoredPosition = pos;
            rt.SetAsLastSibling();

            DOTween.Sequence()
                .Append(rt.DOScale(1, 0.22f).From(0.3f).SetEase(Ease.OutBack))
                .Join(rt.DOAnchorPosY(size * rise, life).SetRelative().SetEase(Ease.OutCubic))
                .Insert(life * 0.55f, img.DOFade(0, life * 0.45f))
                .SetLink(img.gameObject)
                .OnComplete(() => Destroy(img.gameObject));
            return img;
        }

        // Вспышка над гостем на доске.
        void BurstAtGuest(string name, float scale = 0.9f, float rise = 0.35f) =>
            Burst(gridRoot, name, guestToken.anchoredPosition + new Vector2(0, L.Cell * 0.15f), L.Cell * scale, rise);

        static bool IsGhost(GuestDef g) => g.Id == "Ghost" || g.Id == "GhostLord";

        // ───────── Золото ночью ─────────
        // Ночью счётчик показывает shownGold; монеты летят в него сразу, а goldGoal — куда он докрутится.
        // Само золото игра начисляет после обхода гостя; это только показ, итоги ночи не меняются.
        int goldGoal;

        void CountGold(float dur = 0.4f)
        {
            if (!goldHeld) return;
            DOTween.Kill("goldCount");
            DOTween.To(() => shownGold, x => { shownGold = x; UpdateStats(); }, goldGoal, dur)
                .SetId("goldCount").SetEase(Ease.OutQuad).SetLink(gameObject)
                .OnComplete(() => Juice.Punch(statGold.Root, 0.2f, 0.24f));
        }

        // Монеты из точки доски летят дугой в счётчик золота (3–5 штук вразброс), счётчик докручивается.
        void CoinsToCounter(Vector2 boardPos, int amount, float delay = 0.15f)
        {
            if (amount <= 0 || !goldHeld || statGold == null || fxLayer == null) return;
            goldGoal = Mathf.Max(0, goldGoal + amount);
            int count = Mathf.Clamp(amount, 3, 5);
            var start = gridRoot.TransformPoint(boardPos);
            float size = L.Cell * 0.32f;
            for (int i = 0; i < count; i++)
            {
                bool last = i == count - 1;
                DOVirtual.DelayedCall(delay + i * 0.05f, () =>
                {
                    var coin = UIKit.Image("Coin", fxLayer, Color.white, Icons.Coin);
                    coin.raycastTarget = false;
                    coin.preserveAspect = true;
                    var rt = coin.rectTransform;
                    rt.sizeDelta = new Vector2(size, size);
                    rt.position = start;
                    rt.anchoredPosition += Random.insideUnitCircle * L.Cell * 0.3f;
                    Juice.FlyTo(rt, statGold.Root, 0.55f, () =>
                    {
                        Sfx.Coin();
                        if (last) CountGold();
                    });
                }).SetLink(gameObject);
            }
        }

        // Монеты вылетают из счётчика к краю экрана — золото украли.
        void CoinsFromCounter(int amount)
        {
            if (amount <= 0 || !goldHeld || statGold == null || fxLayer == null) return;
            goldGoal = Mathf.Max(0, goldGoal - amount);
            CountGold(0.5f);
            int count = Mathf.Clamp(amount, 3, 5);
            var src = statGold.Root;
            var start = src.TransformPoint(src.rect.center);
            float size = L.Cell * 0.32f;
            var r = fxLayer.rect;
            for (int i = 0; i < count; i++)
            {
                var coin = UIKit.Image("Coin", fxLayer, Color.white, Icons.Coin);
                coin.raycastTarget = false;
                coin.preserveAspect = true;
                var rt = coin.rectTransform;
                rt.sizeDelta = new Vector2(size, size);
                rt.position = start;
                // К ближнему боковому краю и чуть вниз, вразлёт.
                var end = new Vector2(r.xMax + size, rt.anchoredPosition.y - r.height * Random.Range(0.05f, 0.3f));
                DOTween.Sequence().SetLink(coin.gameObject)
                    .AppendInterval(i * 0.06f)
                    .Append(rt.DOJumpAnchorPos(end, L.Cell * 0.6f, 1, 0.6f).SetEase(Ease.InQuad))
                    .Join(rt.DORotate(new Vector3(0, 0, -360), 0.6f, RotateMode.FastBeyond360))
                    .Insert(i * 0.06f + 0.35f, coin.DOFade(0, 0.25f))
                    .OnComplete(() => Destroy(coin.gameObject));
            }
        }

        // ───────── Главные моменты ночи ─────────

        // Ловушка поймала вора или волка: стоп-кадр → удар по клетке → щит → «+N» → монеты в копилку.
        void PlayCatch(Vector2Int cell, int reward)
        {
            Sfx.Trap();
            Juice.HitStop(0.08f);
            var tile = tiles[cell.x, cell.y].Root;
            Juice.Shake(tile, 20, 0.35f, 28);
            BurstAtGuest("Shield", 1.0f, 0.15f);
            BurstAtGuest("Dust", 1.1f, 0.05f);
            Juice.Shake(guestVisual, 12, 0.4f, 30);
            if (reward > 0)
            {
                Juice.FloatText(gridRoot, CellPos(cell) + new Vector2(0, L.Cell * 0.45f), "+" + reward, Gold, UiArt.Icon("Gold"), L.StatsFont + 10);
                CoinsToCounter(CellPos(cell), reward, 0.3f);
            }
        }

        // Вор дошёл до сокровищницы: дом вздрагивает, клетка вспыхивает красным, золото улетает.
        void PlayRobbery(Vector2Int cell, int gold)
        {
            Sfx.Break();
            Juice.ScreenShake(12, 0.4f);
            tiles[cell.x, cell.y].FlashBad();
            if (gold < 0)
            {
                Juice.FloatText(gridRoot, CellPos(cell) + new Vector2(0, L.Cell * 0.45f), "−" + (-gold), Bad, UiArt.Icon("Gold"), L.StatsFont + 10);
                CoinsFromCounter(-gold);
            }
        }

        // Гость ушёл недовольным: сердца репутации дрожат и мигают, «−1» с сердцем.
        void PlayUnhappy(Vector2 boardPos, int reputation)
        {
            if (reputation < 0)
                Juice.FloatText(gridRoot, boardPos + new Vector2(0, L.Cell * 0.4f), "−" + (-reputation), Bad, UiArt.Icon("Heart"), L.StatsFont + 6);
            if (statHearts == null) return;
            Juice.Shake(statHearts.Root, 12, 0.4f);
            foreach (var img in statHearts.Root.GetComponentsInChildren<Image>())
                Juice.Flicker(img, 3, 0.45f);
        }
    }
}
