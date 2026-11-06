# «Дом на ночь» — промт для иконки игры

Нужны два файла: квадратная иконка (приложение, favicon, Unity Player Settings) и обложка для itch.io.
Текст на картинке не просим — генераторы плохо пишут кириллицу; название при желании добавим сами нашим шрифтом.

## ПРОМТ (копировать целиком)

```
Create a game icon for my cozy-spooky fantasy card game "A House for the Night" (Russian: «Дом на ночь»).
In the game, the player builds a small guesthouse room by room from cards, and at night guests knock at the door:
travelers, scholars, a friendly ghost, but also a sneaky thief and a wolf.

STYLE (same as the rest of the game):
- Hand-painted 2D cartoon, modern fantasy card-game / cozy mobile style.
- Bold clean shapes, THICK dark brown outlines (#2b1a10), soft cel shading, light from the top-left.
- Palette: deep night blue and purple (#141b33, #223056, #4a4670, #6b3fa0) contrasted with warm golden window light
  (#ffc861, #ffb834, #f59a2c), wood browns (#9a6438, #4a2e20), a touch of moss green (#4f8a3a).
- NO text, NO letters, NO numbers, NO logos, NO watermarks.

COMPOSITION:
- One clear hero object that reads at 48×48 px: a small crooked two-storey wooden guesthouse with a purple shingle roof,
  a chimney with a curl of smoke and BIG glowing warm-yellow windows; a round wooden front door, slightly open,
  warm light spilling onto the doorstep.
- Behind it: a big pale full moon and a deep blue-purple night sky.
- Small storytelling details (must not clutter the silhouette): a tiny friendly ghost peeking from the attic window,
  a lantern hanging by the door, two glowing wolf eyes in a bush at the bottom corner.
- Two or three playing cards fanned at the bottom edge in front of the house, shown from the back:
  dark wood back with a golden frame and a small crescent-moon door medallion (this is our card back).
- Strong silhouette, high contrast between the warm house and the cold night, the house fills ~70% of the frame.

FILES:
1) Icon.png — 1024×1024, square, full-bleed background (no transparency), important content inside the central 80%
   (platforms round the corners and crop the edges).
2) Cover.png — 630×500 (itch.io cover), the same scene composed wider: house slightly left of center,
   moon top-right, cards bottom-left, calm dark sky on the right third (space for the title added later).
Also give a small 64×64 preview of Icon.png so I can check it still reads at small size.
```

## Куда положить

| Файл | Куда | Зачем |
|---|---|---|
| `Icon.png` | `Assets/Icon.png` → Project Settings → Player → Default Icon | иконка сборки и вкладки браузера |
| `Icon.png` (уменьшенная до 32×32 / 192×192) | `Assets/WebGLTemplates/CardHome/` | favicon в шаблоне WebGL |
| `Cover.png` | страница игры на itch.io → Cover image | обложка в каталоге itch |
