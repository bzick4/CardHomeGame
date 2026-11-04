# «Дом на ночь» — промт для всех изображений

Документ из двух частей:
1. **Промт** (на английском) — скопируй его целиком в чат с генератором картинок.
2. **Информация для реализации** — куда класть файлы, что уже подключено в коде, что подключим после.

---

## ЧАСТЬ 1. ПРОМТ (копировать целиком)

```
You are the art director and sprite artist for my mobile card game made in Unity. Create ALL game sprites listed below in ONE consistent art style, export each as a separate PNG with the EXACT file name given, and pack everything into one downloadable ZIP archive with the EXACT folder structure given. If you cannot do everything in one answer, work folder by folder (one ZIP per folder, same structure) and tell me which folder is next.

=== THE GAME ===
Title: "A House for the Night" (Russian: «Дом на ночь»). Mobile card strategy (CCG + roguelike), portrait and landscape.
By day the player builds a guesthouse on a 5×5 wooden board from room cards (kitchen, bedroom, library, traps…). Room doors must connect.
At night an opponent plays guest cards one by one onto the ENTRANCE: friendly travelers, scholars, ghosts — and threats: a thief who goes for the treasury, a wolf who goes for the kitchen. Happy guests pay gold, unhappy ones hurt reputation (hearts). Traps catch thieves and wolves.
Campaign: 10 houses (levels) unlocked one by one on a map. Roguelike mode "Wandering": a branching map with houses, fairs, campfires, events, dangerous houses and a boss — the Ghost Lord.
Mood: cozy, magical, a little spooky night. Warm light from windows against deep blue-purple night.

=== GLOBAL STYLE (apply to EVERY image) ===
- Hand-painted 2D cartoon illustration in the style of a modern fantasy collectible card game / cozy mobile game.
- Bold clean shapes, THICK dark brown outlines (#2b1a10), soft cel shading, slight top-left light, gentle gradients.
- Palette: night blues and purples (#141b33, #223056, #4a4670, #6b3fa0) contrasted with warm gold/orange light (#ffc861, #ffb834, #f59a2c), wood browns (#9a6438, #4a2e20), parchment cream (#f3d79b, #f7ecd2), grass greens (#4f8a3a, #8fb35a).
- Slightly exaggerated whimsical proportions, storybook feel, crisp edges, high detail but readable at small mobile size.
- NO text, NO letters, NO numbers, NO logos, NO watermarks anywhere (I add all text in the game).
- Objects, icons, characters, frames: TRANSPARENT background (PNG with alpha), centered, with ~6% empty padding around, no drop shadow outside the object unless stated.
- Backgrounds: full-bleed, no transparency.
- Same lighting, outline thickness and palette across all files so they look like one set.

=== FILE RULES ===
- PNG, sRGB, names in Latin EXACTLY as written (case-sensitive), no spaces.
- Root folder of the ZIP: Art/  (plus Audio/ is NOT needed).
- Sizes are given per folder; keep exact aspect ratios.

=== FOLDERS AND FILES ===

---------- Art/Rooms/  (room icons drawn on the board tiles and room cards) ----------
256×256, transparent. One clear object per room, viewed from the front/slightly above, centered, fills ~80% of the canvas.
Hall.png — a sturdy wooden front door with a golden knob (entrance hall)
Corridor.png — a long red carpet runner with gold trim
Crossroad.png — a cross-shaped red carpet with a golden diamond in the middle
Kitchen.png — a steaming cooking pot on a stove with a ladle
Dining.png — a dinner plate with a roast, fork and knife
Bedroom.png — a cozy bed with a blue blanket and a white pillow
Bath.png — a white bathtub with soap foam and golden feet
Library.png — a short shelf of colorful books
Fireplace.png — a stone fireplace with a warm fire and logs
Cellar.png — a wooden wine barrel with iron hoops
Trap.png — an open steel bear trap with sharp teeth
Treasury.png — an open treasure chest overflowing with gold coins
Nursery.png — a cute teddy bear
Lounge.png — a green velvet sofa with a golden cushion

---------- Art/Tiles/  (full room tiles for the 5×5 board, top-down cutaway like a dollhouse) ----------
512×512, transparent corners. A square room seen from above: walls around the edge, floor and furniture inside matching the room, NO doors drawn (doors are added by the game), warm interior light.
Hall.png, Corridor.png, Crossroad.png, Kitchen.png, Dining.png, Bedroom.png, Bath.png, Library.png, Fireplace.png, Cellar.png, Trap.png, Treasury.png, Nursery.png, Lounge.png
Plus:
Empty.png — an empty dirt/grass building plot with a faint dashed outline
Broken.png — transparent overlay of cracks, broken planks and dust to lay over any room

---------- Art/Cards/  (guest card art + card details) ----------
Characters, 512×512, transparent, FULL BODY standing, facing the viewer 3/4, feet at the bottom edge:
Traveler.png — weary traveler: wide-brimmed brown hat with red band, green cloak, big backpack with bedroll, walking stick
Scholar.png — bookish scholar: purple robe, square academic cap with gold tassel, round glasses, white beard, holding a red book
Ghost.png — friendly white bedsheet ghost with big dark eyes, floating, small chain
Thief.png — sneaky hooded thief: dark green hood and cloak, face mask, two daggers, a glowing green potion on the belt (ENEMY)
Wolf.png — cartoon grey wolf standing on hind legs in a torn red vest, yellow eyes, small fangs (ENEMY)
Inspector.png — strict inspector: grey suit, bowler hat, monocle, mustache, clipboard
Couple.png — two lovers holding hands: a girl in a pink dress and a guy in a blue coat, a small heart above them
Drunkard.png — jolly drunkard: big belly, red nose, green bandana, beer mug
GhostLord.png — the BOSS Ghost Lord: big pale-teal ghost with a golden crown and a purple royal cape, glowing red eyes, purple magic flames (ENEMY BOSS)
Card details:
Backdrop.png — 512×640, NO transparency: the landscape behind every guest on a card — meadow with a winding path, snowy mountains, a small castle, a few houses, dusk sky (calm, no characters)
Banner.png — 512×128, transparent: horizontal parchment ribbon shaped like an arrow pointing right (for the name)
Sun.png — 256×256, transparent: a round golden sun badge with rays (stat badge)
Moon.png — 256×256, transparent: a round pale full-moon badge with craters (stat badge)
RoleFriendly.png — 256×256, transparent: a small house with a heart (friendly guest badge)
RoleThreat.png — 256×256, transparent: two crossed daggers (threat badge)
RoleSpecial.png — 256×256, transparent: a golden star (special guest badge)
RoleBoss.png — 256×256, transparent: a golden crown (boss badge)
Back.png — 512×720, NO transparency: default back side of a guest card — dark blue field, golden ornamental border, a medallion with a wooden door and a crescent moon in the center
Lock.png — 256×256, transparent: a golden padlock
Star.png — 256×256, transparent: a shiny golden star
StarEmpty.png — 256×256, transparent: the same star shape, empty, dark brown
Cloud.png — 512×300, transparent: a fluffy cream cartoon cloud (covers locked places on maps)
MapPaper.png — 768×1008, NO transparency: plain aged parchment with very faint hand-drawn hills, trees and a river (calm, low contrast, no icons)

---------- Art/Guests/  (round tokens that walk on the board at night) ----------
256×256, transparent: the HEAD/BUST of each guest inside a round colored token with a thick outline (like a board-game pawn seen from above):
Traveler.png, Scholar.png, Ghost.png, Thief.png, Wolf.png, Inspector.png, Couple.png, Drunkard.png, GhostLord.png

---------- Art/CardFrames/  (frames of OUR cards) ----------
RoomCard.png — 560×730, transparent center: frame of a room card: carved wooden border, a window at the top for the room art, a dark ribbon area for the name below it, a parchment panel at the bottom for the description. Neutral light-grey wood so the game can tint it by room color.
GuestCard.png — 600×840, transparent center: frame of a guest card: wooden border, big art window, an arrow-ribbon for the name at the top, a round badge slot in the top-left, a parchment panel at the bottom, round slots in both bottom corners for the sun and moon badges.
Upgrade frames laid OVER a room card when it is upgraded (same 560×730, transparent center, only the metal border with ornaments, getting richer each level):
Upgrade1.png — bronze
Upgrade2.png — silver
Upgrade3.png — gold
Upgrade4.png — diamond (ice-blue crystals sticking out)
Upgrade5.png — ruby (gold with red gems)
Upgrade6.png — emerald (gold with green gems and curls)
Upgrade7.png — epic (purple metal, a small crown on top)
Upgrade8.png — legendary (orange-gold with flame tongues on the sides)

---------- Art/Backs/  (card backs — collectible) ----------
Themes, 512×720, NO transparency, the art of the back WITHOUT outer frame (the frame is separate). Every theme has the same central medallion: a wooden door with a keyhole and a crescent moon. Theme decoration reflects the house:
Theme0.png — Hostel: light wooden planks, small golden keys
Theme1.png — Manor: deep blue velvet with a diamond lattice and a small coat of arms
Theme2.png — Tavern: dark red with a fireplace glow at the bottom and two beer mugs
Theme3.png — Castle: grey stone brick wall
Theme4.png — Haunted Mansion: purple fog, candles in the corners, faint ghost faces
Theme5.png — Inn: green with hanging lanterns and horseshoes
Theme6.png — Monastery: colorful stained glass
Theme7.png — Hunting Lodge: brown fur texture with antlers at the top
Theme8.png — Spa: blue water waves and bubbles
Theme9.png — Palace: ivory with golden sun rays and a crown
Theme10.png — Wanderer (default): dark wood with tiny stars
Frames, 560×790, transparent center (fits around a 512×720 back), one per difficulty circle, each richer than the previous:
Frame0.png — plain wood with iron nails
Frame1.png — bronze corners
Frame2.png — silver with small gems
Frame3.png — gold with diamonds at top and bottom
Frame4.png — diamond: ice-blue crystals sticking out
Frame5.png — ruby: gold with red gems on the sides
Frame6.png — emerald: gold with green gems and curls
Frame7.png — epic: purple metal with a crown on top
Frame8.png — legendary: orange-gold with flame tongues

---------- Art/Houses/  (house icons for level select) ----------
256×256, transparent: a small cozy cartoon building in its own style:
Hostel.png — simple wooden hostel with a sign
Manor.png — elegant two-storey manor
Tavern.png — tavern with a hanging mug sign and smoke
Castle.png — small stone castle with towers
Haunted.png — crooked spooky mansion with a ghost in a window
Inn.png — roadside inn with a stable and lanterns
Monastery.png — stone monastery with a bell tower
Lodge.png — log hunting lodge with antlers over the door
Spa.png — bathhouse with steam and a little pool
Palace.png — golden royal palace with domes

---------- Art/Locations/  (in-game background behind the board, one per house) ----------
For each house TWO files: portrait 1080×1920 and landscape 1920×1080, NO transparency. Night scene of the surroundings of that house; the CENTER must stay calm and darker (the wooden board with rooms is placed there); detail goes to the edges and the sky.
Hostel.png / HostelWide.png — country road, fields, a small wooden hostel far away, fireflies
Manor.png / ManorWide.png — a garden with hedges, a fountain and a manor in the distance
Tavern.png / TavernWide.png — a village square with lanterns, wolves' eyes in the dark woods at the edge
Castle.png / CastleWide.png — castle courtyard, stone walls, torches, a guard tower
Haunted.png / HauntedWide.png — misty graveyard garden, dead trees, purple fog, floating ghost lights
Inn.png / InnWide.png — a busy crossroads with a signpost, carts and lanterns
Monastery.png / MonasteryWide.png — mountain monastery with stained glass windows, candles, snow
Lodge.png / LodgeWide.png — deep pine forest, snow, a frozen lake, wolf howling on a rock
Spa.png / SpaWide.png — hot springs with steam, rocks, lanterns, a waterfall
Palace.png / PalaceWide.png — royal palace gardens, golden gates, fireworks in the sky

---------- Art/Board/  (the game field) ----------
Board.png — 1024×1024, transparent outside: the wooden building board — thick planks with a carved border, iron corners, slightly worn (a 5×5 grid of tiles will be placed on it; do NOT draw the grid)
Slot.png — 256×256, transparent: an empty square building slot, a slightly darker dented area with a faint dashed outline
Porch.png — 400×520, transparent: the ENTRANCE in front of the house where guest cards are laid: a doormat on wooden porch steps with a lantern
Door.png — 128×64, transparent: a small light wooden doorway piece (the opening between two rooms)
GuestLantern.png — 512×512, transparent: a soft round warm light glow (lantern light around a walking guest)

---------- Art/Campaign/  (level select map for the 10 houses) ----------
MapBackground.png — 1080×1920, NO transparency: a vertical night fantasy world map seen from above — a winding road from bottom to top through meadows, a river with bridges and a waterfall, forests, a windmill, villages, a castle on a hill at the top, a full moon, purple clouds. IMPORTANT: NO markers, NO icons, NO pedestals, NO numbers — the game places 10 house markers on top.
MapBackgroundWide.png — 1920×1080, the same world map in landscape (road goes left to right).
Node.png — 256×256, transparent: a round stone platform/pedestal on which a house icon stands
NodeCurrent.png — 256×256, transparent: the same platform glowing gold (the house to play now)
NodeLocked.png — 256×256, transparent: the same platform covered by a small cloud with a padlock

---------- Art/Wander/  (roguelike "Wandering" map) ----------
MapBackground.png — 1080×1350, NO transparency: a parchment treasure-map scroll between two thick wooden log rollers at top and bottom, a hanging lantern on the left, a red wax seal with a crown in the bottom-right corner, a compass rose top-right; inside — a night landscape (hills, pines, river, bridge, waterfall, castle at the top, moon). IMPORTANT: NO paths, NO dotted trails, NO icons, NO pedestals.
MapBackgroundWide.png — 1920×1080, the same scroll in landscape.
Pedestal.png — 256×256, transparent: an empty round grey stone pedestal with a small golden plaque with a star on the front
Map markers (256×256, transparent, the stone pedestal WITH the object on top):
NodeHouse.png — a cozy little house with glowing windows
NodeFair.png — a big shiny gold coin
NodeRest.png — a campfire with logs
NodeEvent.png — a purple banner with a white question mark on a wooden frame (the question mark is the only allowed symbol)
NodeElite.png — two crossed steel swords inside a glass dome
NodeBoss.png — the Ghost Lord with a crown on a larger ornate purple pedestal with purple flames
Screens of the points (1080×1920 + Wide 1920×1080 each, NO transparency, calm center for UI):
Fair.png / FairWide.png — a night fair: colorful tents, lanterns, a merchant stall with cards and coins
Rest.png / RestWide.png — a cozy campfire in a forest clearing under the stars, a bedroll, a tree stump
Event.png / EventWide.png — a mysterious forest crossroads with a signpost and fog

---------- Art/Events/  (illustrations for random events, 768×768, NO transparency, square scene) ----------
Event0.png — a soaked traveler knocking at a gate at night in the rain
Event1.png — an abandoned cellar with a glowing barrel and rats' eyes
Event2.png — a wandering carpenter with a toolbox next to a cart
Event3.png — a street magician with three thimbles on a barrel
Event4.png — an old witch at a well holding a wrapped bundle
Event5.png — a burnt-down house, smoking ruins
Event6.png — a smiling innkeeper inviting to a fire, a recipe scroll on the table
Event7.png — a ghost looking out of an old ornate mirror

---------- Art/Menu/  (main menu) ----------
Background.png — 1080×1920, NO transparency: night; a crooked two-storey cozy guesthouse with glowing windows and chimney smoke; a ghost peeking from the attic window; a hooded thief hiding behind a pine; wolf eyes in the bushes; a castle on a hill; full moon; a winding path where a small traveler walks toward the house. Keep the top-left sky calm (title) and the bottom-left calm (buttons).
BackgroundWide.png — 1920×1080, the same scene in landscape: moon top-left, traveler in the center, house on the right; keep the top-center sky and the left side calm.
Logo.png — 1024×512, transparent: a wooden hanging signboard on two ropes with an EMPTY plank (no letters — the title is added by the game), small nails, a lantern hanging on one side
Gear.png, Compass.png, Home.png, Play.png, Back.png, Flag.png, Sound.png, Book.png, Deck.png — 256×256, transparent, simple bold icons: settings gear, compass, small house, green round play button, back arrow, white surrender flag, speaker, blue book, stack of cards
Chest.png, Campfire.png, Swords.png, EventBanner.png — 256×256, transparent: treasure chest, campfire, crossed swords, purple banner (no symbol)

---------- Art/UI/  (interface pieces, white/grey so the game can tint them) ----------
Button.png — 256×128, transparent: a chunky rounded rectangular button with a thick dark outline, a lighter top bevel and a darker bottom lip, NEUTRAL WHITE/LIGHT GREY (tinted by the game: green, orange, purple, wood)
Panel.png — 256×256, transparent: a wooden panel with a thick dark outline and bevel (for windows and plaques)
Parchment.png — 256×256, transparent: a parchment sheet with slightly torn edges
Plank.png — 512×128, transparent: a single wooden plank sign
Heart.png — 128×128: a red cartoon heart (reputation)
Coin.png — 128×128: a gold coin (gold)
Card.png — 128×128: a small card icon (deck counter)
Victory.png — 1024×600, transparent: a victory crest — winged shield with a blue ribbon and three golden stars, NO text
Defeat.png — 1024×600, transparent: a defeat crest — a broken wooden shield with crossed axes and a red ribbon, NO text
Moon.png — 512×512, transparent: a big glowing full moon
Glow.png — 256×256, transparent: a soft white round glow (for highlights)
Spark.png — 128×128, transparent: a 4-pointed white sparkle

---------- Art/Shop/  (bonuses, shop and rewards, 256×256, transparent) ----------
Reroll.png — three cards being shuffled with a curved arrow
ExtraAction.png — an hourglass with a plus sign
RemoveCard.png — a card being torn in half
UpgradeCard.png — a card with an upward golden arrow and sparkles
Repair.png — a hammer and nails over a wooden plank
Reward.png — a gift box with a ribbon
Heirloom.png — an old golden key on a velvet cushion (inherited card)

---------- Art/ (root) ----------
Coin.png — 128×128, transparent: gold coin (flying coins in the night summary)
Crack.png — 256×256, transparent: dark crack lines only (overlay for a broken room)

=== OUTPUT ===
1) Show a small preview contact sheet for each folder.
2) Give me ONE ZIP named CardHome_Art.zip with the top folder Art/ and exactly the subfolders and file names above (Art/Rooms, Art/Tiles, Art/Cards, Art/Guests, Art/CardFrames, Art/Backs, Art/Houses, Art/Locations, Art/Board, Art/Campaign, Art/Wander, Art/Events, Art/Menu, Art/UI, Art/Shop, and the 2 root files).
3) Add a README.txt inside the ZIP listing every file with its size.
If the full set is too large for one answer, do it in this order and give a ZIP after each step: Cards → Guests → Rooms → Tiles → Board → CardFrames → Backs → Houses → Campaign → Wander → Events → Locations → Menu → UI → Shop → root files.
```

---

## ЧАСТЬ 2. ИНФОРМАЦИЯ ДЛЯ РЕАЛИЗАЦИИ

### Куда класть
Распакуй архив так, чтобы получилось **`Assets/Resources/Art/...`** (папка `Art` из архива ложится прямо в `Assets/Resources/`).
Названия папок и файлов менять нельзя — игра ищет их по имени. Чтобы заменить картинку руками, просто перезапиши файл с тем же именем.

**Настройка импорта в Unity** (выдели все PNG в папке → Inspector):
- Texture Type: **Sprite (2D and UI)**
- Non-Power of 2: **None** (иначе исказятся пропорции)
- Generate Mip Maps: выкл.
- Max Size: 2048 для фонов, 512–1024 для остального.

### Что подхватится сразу (код уже умеет)
Если файл есть — используется он, если нет — рисуется кодом, как сейчас. Можно добавлять по одной картинке.

| Папка | Файлы | Где в игре |
|---|---|---|
| `Art/Rooms/` | 14 комнат (`Kitchen.png`…) | значки комнат на доске и картах |
| `Art/Guests/` | 9 гостей (`Traveler.png`…) | фишки гостей, ночью ходят по дому |
| `Art/Houses/` | 10 домов (`Hostel.png`…) | значки домов на карте выбора и в странствии |
| `Art/Cards/` | 9 персонажей + `Backdrop`, `Banner`, `Sun`, `Moon`, `Role*`, `Back`, `Lock`, `Star`, `StarEmpty`, `Cloud`, `MapPaper` | карты гостей, звёзды, замки, пергамент карты |
| `Art/Backs/` | `Theme0…10`, `Frame0…8` | рубашки (коллекция, колоды) |
| `Art/Menu/` | `Background`, `BackgroundWide` + значки | главное меню |
| `Art/Wander/` | `MapBackground`, `Pedestal`, `Node*` | карта странствия |
| `Art/` | `Coin`, `Crack` | летящие монеты, трещины сломанной комнаты |

### Что подключу после того, как появятся картинки
Эти места сейчас рисуются кодом без загрузки файлов — пришлёшь картинки, я добавлю загрузку под эти же имена:

| Папка | Что сделаю |
|---|---|
| `Art/Locations/<Дом>.png` и `<Дом>Wide.png` | свой фон в партии для каждого дома (вместо общего неба) |
| `Art/Board/` | доска, пустые пазы, вход-крыльцо, двери, свет фонаря |
| `Art/Tiles/` | полноценные комнаты-«кукольный домик» вместо значка на цветном фоне; `Broken` — поломка |
| `Art/CardFrames/` | рамки карт комнат и гостей; `Upgrade1…8` — окантовка улучшенных карт |
| `Art/Campaign/` | карта выбора домов (как на твоём концепте), постаменты, текущий и закрытый дом |
| `Art/Wander/Fair, Rest, Event` | фоны ярмарки, привала и случая |
| `Art/Events/Event0…7` | картинка для каждого случая (номер = порядок в `WanderEvents.cs`) |
| `Art/Menu/Logo.png` | деревянная вывеска с пустой доской (название пишет игра нашим шрифтом) |
| `Art/UI/` | кнопки, панели, пергамент, сердце, монета, победа/поражение, блики |
| `Art/Shop/` | значки лавки, награды и наследства |

### Справочник имён (как в коде)
- **Комнаты:** Hall, Corridor, Crossroad, Kitchen, Dining, Bedroom, Bath, Library, Fireplace, Cellar, Trap, Treasury, Nursery, Lounge
- **Гости:** Traveler, Scholar, Ghost (друзья) · Thief, Wolf (угрозы) · Inspector, Couple, Drunkard (особые) · GhostLord (босс)
- **Дома** (порядок = уровень и номер рубашки): 0 Hostel, 1 Manor, 2 Tavern, 3 Castle, 4 Haunted, 5 Inn, 6 Monastery, 7 Lodge, 8 Spa, 9 Palace; рубашка 10 — «Странник»
- **Окантовки** (круги сложности и уровни карт): 0 Дерево, 1 Бронза, 2 Серебро, 3 Золото, 4 Алмаз, 5 Рубин, 6 Изумруд, 7 Эпичная, 8 Легендарная
- **Точки странствия:** House, Fair, Rest, Event, Elite, Boss
- **Случаи:** 0 Странник у ворот, 1 Заброшенный погреб, 2 Бродячий плотник, 3 Фокусник, 4 Ведьма у колодца, 5 Сгоревший дом, 6 Трактирщик, 7 Призрак в зеркале
- **Звуки** (не картинки, но тоже можно заменить): `Assets/Resources/Audio/` — Step, Knock, Creak, Trap, Coin, Break, Owl, Click, Plop, Fanfare, Card (.wav)

### Советы по генерации
- Генераторы плохо пишут кириллицу — поэтому во всех промтах «без текста»: надписи добавляет игра нашим шрифтом.
- Одним запросом все ~250 файлов генератор, скорее всего, не выдаст. Проси по папкам (порядок указан в конце промта) и в каждом новом запросе напоминай: «same style as previous set».
- Если генератор не умеет прозрачный фон — пусть рисует на чистом однотонном пурпурном (#ff00ff); фон уберём.
- Персонажей лучше генерировать одним листом (все 9 рядом) — так стиль будет одинаковым, потом разрежем.
