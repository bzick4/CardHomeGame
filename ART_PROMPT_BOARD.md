# «Дом на ночь» — промт для поля и клеток

Доска 5×5 строится кодом: клетка 164, шаг 176 (щель 12), доска ≈ 912×912 в единицах интерфейса.
Вход — середина нижнего ряда, под ним крыльцо, куда соперник кладёт карты гостей.
Поэтому сетку на картинке НЕ рисуем: генератор не попадёт в точные клетки. Доска — только рамка и пол, клетки — отдельные картинки.

## ПРОМТ (копировать целиком)

```
You are the sprite artist for my mobile card game "A House for the Night". Create the game-board sprites below in ONE consistent style, each as a separate PNG with the EXACT file name, packed into one ZIP with the folder Art/Board/.

=== THE GAME ===
Cozy-spooky fantasy card game. By day the player builds a guesthouse on a 5×5 board from square room tiles (kitchen, bedroom, library, cellar, traps…); room doors must connect. At night guests walk from room to room with a lantern. The board is seen from straight above, like a dollhouse with the roof removed.

=== STYLE (every file) ===
- Hand-painted 2D cartoon, modern fantasy card-game / cozy mobile style, THICK dark brown outlines (#2b1a10), soft cel shading, light from the top-left.
- Palette: warm wood browns (#9a6438, #6e4526, #4a2e20), stone greys (#7c7f8c, #4f5262), lantern gold (#ffc861, #f59a2c), touches of moss green (#4f8a3a).
- Strict top-down view (90°), NO perspective, NO isometric angle — everything must tile on a square grid.
- NO text, NO numbers, NO grid lines, NO watermarks.
- Transparent background (PNG alpha) wherever stated; if you cannot do transparency, use a flat pure magenta #ff00ff background instead.
- Calm, low-contrast centers: game icons and room art are placed on top, so details go to the edges.

=== FILES (Art/Board/) ===
Board.png — 1024×1024, transparent outside the board. The foundation of the house seen from above: a square platform with a THICK carved dark-wood frame (frame width ≈ 5% of the image on each side), iron corner plates with rivets, a few moss tufts on the frame. Inside the frame: one continuous calm floor of dark warm planks, evenly lit, slightly worn. NO grid, NO rooms, NO furniture. At the middle of the bottom edge: a small wooden threshold step (the front entrance).

Slot.png — 256×256, transparent corners. An EMPTY building plot for one room, seen from above: a slightly sunken square of darker bare planks with a faint chalk dashed outline 8% inside the edge and rounded corners. Very calm, low detail, darker than the board floor.

SlotLocked.png — 256×256, same as Slot.png but covered with dusty boards nailed crosswise (a plot that can't be built on).

TileFrame.png — 256×256, transparent CENTER. The walls of one room seen from above: a thick wall rim on all four sides (plastered wall top with wooden beams and stone corner posts), wall thickness ≈ 9% of the tile, small rounded outer corners. The inner 82% is fully transparent (the room picture goes underneath). All four walls are CLOSED — doors are added separately.

Door.png — 128×64, transparent. One open doorway seen from above, to be laid over the middle of a wall: a short wooden threshold plank with two small door-frame posts and a soft warm light spilling through, horizontal orientation (the game rotates it for vertical walls).

DoorClosed.png — 128×64, transparent. The same spot with a closed wooden door seen from above (a dark plank lid with an iron ring), for walls without a passage.

Porch.png — 400×520, transparent. The ENTRANCE below the house, seen from above: wooden porch steps going down from the front door, a woven doormat in the middle (calm, empty, a guest card is placed on it), a hanging lantern on one side glowing warm, a few leaves.

Broken.png — 256×256, transparent. An overlay for a broken room: cracked floorboards, a hole, scattered splinters and dust only along cracks; the rest transparent.

Also give me one preview image (not in the ZIP list) with Board.png and a 5×5 arrangement of Slot.png / TileFrame.png on it, so I can check the style.
```

## Как это встанет в игру

| Файл | Где | Что заменит |
|---|---|---|
| `Board.png` | под всей сеткой 5×5 | нарисованную кодом коричневую доску |
| `Slot.png` | пустая клетка | тёмный квадрат-паз |
| `SlotLocked.png` | клетка, где строить нельзя (на будущее) | — |
| `TileFrame.png` | рамка-стены поверх комнаты | светлую панель «стен» |
| `Door.png` / `DoorClosed.png` | середина каждой стены | жёлтые палочки дверей |
| `Porch.png` | под прихожей | коврик с золотой рамкой «ВХОД» |
| `Broken.png` | поверх сломанной комнаты | трещины кодом |

Комнату внутри клетки предлагаю рисовать уже готовыми картинками `Art/RoomArt/<Комната>.png`
(те же, что на картах): `TileFrame` сверху даст стены, двери встанут поверх стен.
