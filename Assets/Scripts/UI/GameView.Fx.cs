using DG.Tweening;
using UnityEngine;

namespace CardHome
{
    // Эффекты из Art/Fx: короткие вспышки поверх доски и карт. Только DOTween, без Update.
    // Нет картинки — эффекта просто нет, игра от этого не ломается.
    public partial class GameView
    {
        static Sprite FxArt(string name) => ArtStore.Get("Fx/" + name, null);

        // Вспышка в точке parent: появляется с подскоком, поднимается на rise·size и тает.
        void Burst(RectTransform parent, string name, Vector2 pos, float size, float rise = 0.35f, float life = 0.9f)
        {
            var sprite = FxArt(name);
            if (sprite == null || parent == null) return;
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
        }

        // Вспышка над гостем на доске.
        void BurstAtGuest(string name, float scale = 0.9f, float rise = 0.35f) =>
            Burst(gridRoot, name, guestToken.anchoredPosition + new Vector2(0, L.Cell * 0.15f), L.Cell * scale, rise);

        static bool IsGhost(GuestDef g) => g.Id == "Ghost" || g.Id == "GhostLord";
    }
}
