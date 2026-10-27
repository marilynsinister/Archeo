"""Reproducible original 16px sprites, bilingual text, and the artifact catalog."""
import json
from pathlib import Path
from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
DATA = [
    # id, culture, rarity, cost, price, effect, shape, Russian name, English name, Russian effect, English effect, Russian lore, English lore
    ("settler_cup", "valley", 0, 20, 140, "", "cup", "Чашка первого поселенца", "Settler's Cup", "Можно поставить на стол или передать в музейный архив.", "Display on a table or donate to the research archive.", "На дне выцарапано: «На новом месте чай всё равно пахнет домом». Гюнтер считает, что чашку привезли в долину ещё до строительства железной дороги.", "Scratched into the base: 'Tea still smells like home, even here.' Gunther believes this cup predates the valley's railway."),
    ("copper_brooch", "valley", 0, 24, 180, "", "brooch", "Медная брошь с ласточкой", "Copper Swallow Brooch", "Коллекционная древность. Можно поставить на стол.", "A collectible antiquity. Can be displayed on a table.", "Такими брошами обменивались люди, обещавшие вернуться весной. У этой ласточки одно крыло отполировано большим пальцем.", "People exchanged these brooches as a promise to return in spring. One wing has been polished smooth by a thumb."),
    ("keeper_lantern", "valley", 1, 30, 320, "lamp", "lantern", "Фонарь забытого смотрителя", "Forgotten Keeper's Lantern", "Поставь дома или на улице: фонарь даёт тёплый свет.", "Place indoors or outdoors for a warm light.", "Когда в лесу ещё не было тропинок, смотритель зажигал этот фонарь для тех, кто возвращался домой слишком поздно. Стекло до сих пор хранит запах сосновой смолы.", "Before the forest had paths, a keeper lit this lantern for those returning home late. The glass still smells of pine resin."),
    ("herbalist_flask", "valley", 2, 45, 650, "energy", "flask", "Фляга лесной травницы", "Woodland Herbalist's Flask", "ПКМ с предметом в руке: восстанавливает 60 энергии. Один раз в день.", "Right-click while holding: restore 60 energy. Once per day.", "Внутри всегда остаётся глоток прохладного настоя. Рецепт на пробке почти стёрся, но рядом с ним нарисован улыбающийся листок.", "There is always a sip of cool herbal tea inside. The recipe on the stopper is faded, but a smiling leaf is still visible."),
    ("harvest_seal", "valley", 2, 50, 750, "fragments", "seal", "Печать старого землемера", "Old Surveyor's Seal", "ПКМ: даёт 8 фрагментов наследия долины. Один раз в день.", "Right-click: gain 8 valley fragments. Once per day.", "Прикоснувшись к земле, печать проявляет линии давно исчезнувших межей. По ним можно найти следы прежних поселений.", "Touching soil with this seal reveals long-forgotten field boundaries, leading to traces of the valley's early settlements."),
    ("memory_musicbox", "valley", 3, 70, 1400, "heal", "musicbox", "Шкатулка добрых воспоминаний", "Music Box of Kind Memories", "ПКМ: восстанавливает 40 здоровья. Один раз в день.", "Right-click: restore 40 health. Once per day.", "Мелодия каждый раз немного другая. Слушателю кажется, что её напевал кто-то очень близкий, когда за окном шёл дождь.", "The melody changes a little each time. It sounds like something a loved one once hummed while rain tapped at the window."),
    ("forest_heart", "valley", 4, 100, 3000, "lamp", "heart", "Сердце первого леса", "Heart of the First Forest", "Древняя светящаяся реликвия. Можно поставить дома или на ферме.", "An ancient glowing relic. Place it in your home or on the farm.", "В янтарной капле застыл лист дерева, которое росло здесь задолго до первого дома. Даже зимой рядом с ним свет кажется по-летнему тёплым.", "An amber droplet holds a leaf from a tree older than the first house in the valley. Even in winter, its light feels like summer."),
    ("brass_gear", "dwarf", 0, 20, 150, "", "gear", "Шестерня старой подъёмной машины", "Ancient Lift Gear", "Коллекционная древность. Можно поставить на стол.", "A collectible antiquity. Can be displayed on a table.", "На каждом зубце выбита крошечная руна. Похоже, гномьи механики нумеровали детали, чтобы ремонтировать машины даже в полной темноте.", "A tiny rune marks every tooth. Dwarven mechanics seem to have numbered their parts to repair machines in total darkness."),
    ("rune_tablet", "dwarf", 0, 25, 190, "", "tablet", "Табличка ученика рунописца", "Apprentice Runecarver's Tablet", "Коллекционная древность. Можно поставить на стол.", "A collectible antiquity. Can be displayed on a table.", "Последняя строка переведена как «больше не буду рисовать усы наставнику». Предыдущие строки старательно соскоблены.", "The final line reads 'I will stop drawing a moustache on my teacher.' The earlier lines were carefully scraped away."),
    ("miner_goblet", "dwarf", 1, 32, 360, "", "cup", "Кубок подгорного мастера", "Underhill Master's Goblet", "Украшение для стола и экспонат музейного архива.", "A table ornament and a research archive exhibit.", "Кубок тяжёлый, но идеально сбалансирован. По ободу идёт надпись: «Хорошая работа начинается после хорошего обеда».", "Heavy, yet perfectly balanced. Its rim reads: 'Good work begins after a good lunch.'"),
    ("dwarf_thermos", "dwarf", 2, 45, 700, "coffee", "thermos", "Гномий термос", "Dwarven Thermos", "ПКМ: даёт чашку тройного эспрессо. Один раз в день.", "Right-click: receive a Triple Shot Espresso. Once per day.", "У крышки три положения: «горячо», «очень горячо» и «перерыв окончен». Последнее положение, к счастью, заклинило.", "The lid has three settings: 'hot', 'very hot', and 'break time is over'. Fortunately, the last setting is stuck."),
    ("resonant_lens", "dwarf", 2, 50, 800, "survey", "lens", "Резонансная линза", "Resonant Lens", "ПКМ на раскопках: сразу показывает точную точку находки. Один раз в день.", "Right-click at a dig site: reveal an exact find. Once per day.", "Сквозь стекло видны слабые отголоски ударов молота. Линза помогает заметить места, где когда-то работали чьи-то руки.", "Through the glass, faint echoes of hammer strikes are visible. The lens reveals places once shaped by working hands."),
    ("clockwork_mole", "dwarf", 3, 75, 1600, "survey", "mole", "Заводной землекоп", "Clockwork Digger", "ПКМ на раскопках: указывает точную точку находки. Один раз в день.", "Right-click at a dig site: pinpoint a find. Once per day.", "Крошечный латунный зверёк упрямо поворачивает нос к зарытым древностям. На его животе выгравировано: «Копай с любопытством».", "This tiny brass animal stubbornly turns its nose toward buried antiquities. Its belly is engraved: 'Dig with curiosity.'"),
    ("mountain_heart", "dwarf", 4, 105, 3400, "fragments", "crystal", "Память глубокой горы", "Memory of the Deep Mountain", "ПКМ: даёт 8 гномьих фрагментов. Один раз в день.", "Right-click: gain 8 dwarven fragments. Once per day.", "Кристалл запоминает форму вещей, которые лежали рядом с ним. В его глубине на мгновение возникают мосты и лестницы подземного города.", "This crystal remembers the shape of objects once placed beside it. Bridges and stairways of an underground city flicker in its depths."),
    ("clay_whistle", "sea", 0, 20, 140, "", "whistle", "Глиняный свисток моряка", "Sailor's Clay Whistle", "Коллекционная древность. Можно поставить на стол.", "A collectible antiquity. Can be displayed on a table.", "Свисток вылеплен в форме рыбы. Возможно, его подарили ребёнку перед долгим плаванием. Отверстие забито мелким белым песком.", "Shaped like a fish, perhaps a gift to a child before a long voyage. Fine white sand fills the opening."),
    ("sailor_buckle", "sea", 0, 24, 180, "", "buckle", "Пряжка с затонувшей шхуны", "Sunken Schooner Buckle", "Коллекционная древность. Можно поставить на стол.", "A collectible antiquity. Can be displayed on a table.", "Под слоем соли обнаружилось изображение трёх звёзд. В портовых книгах Гюнтер нашёл судно с таким же названием, но без даты возвращения.", "Beneath the salt is an image of three stars. Gunther found a ship of the same name in the harbour records, without a return date."),
    ("shell_comb", "sea", 1, 30, 330, "", "comb", "Перламутровый гребень", "Mother-of-Pearl Comb", "Украшение для стола и экспонат музейного архива.", "A table ornament and a research archive exhibit.", "Мастер вырезал каждый зубец по изгибу раковины. Вечером гребень отбрасывает на стену блики, похожие на далёкие паруса.", "Every tooth follows the shell's curve. At dusk, the comb casts reflections resembling distant sails."),
    ("tide_compass", "sea", 2, 45, 700, "compass", "compass", "Компас приливов", "Tide Compass", "ПКМ на раскопках: показывает точную точку находки. Один раз в день.", "Right-click at a dig site: reveal an exact find. Once per day.", "Стрелка не знает севера. Она ищет вещи, по которым кто-то когда-то очень скучал. Моряки считали это полезнее обычного компаса.", "The needle knows nothing of north. It seeks things someone once missed dearly. Sailors considered that more useful."),
    ("pearl_casket", "sea", 2, 52, 850, "pearl", "pearl", "Ларец солёных жемчужин", "Salt-Pearl Casket", "ПКМ: даёт 3 ракушки. Один раз в день.", "Right-click: receive 3 clams. Once per day.", "Изнутри доносится шум маленькой волны. Каждое утро в ларце появляются дары далёкого берега, хотя крышка всю ночь закрыта.", "A tiny wave can be heard inside. Every morning, gifts from a distant shore appear despite the lid being closed all night."),
    ("moon_shell", "sea", 3, 72, 1500, "energy", "shell", "Раковина лунного прилива", "Moon-Tide Shell", "ПКМ: восстанавливает 60 энергии. Один раз в день.", "Right-click: restore 60 energy. Once per day.", "Внутри слышно море, которого нет ни на одной карте. Если слушать достаточно долго, дыхание становится ровным, а усталость отступает.", "Inside is the sea from no known map. Listening makes your breathing steady and lets weariness drift away."),
    ("abyss_beacon", "sea", 4, 100, 3200, "survey", "beacon", "Маяк забытых глубин", "Beacon of Forgotten Depths", "ПКМ на раскопках: показывает точную точку находки. Один раз в день.", "Right-click at a dig site: reveal an exact find. Once per day.", "Этот огонь освещал путь там, где никогда не светило солнце. На основании вырезана карта побережья, которое море давно забрало себе.", "This light guided travellers where the sun never shone. Its base bears a map of a coastline long since claimed by the sea."),
]

RU = {
 "culture.valley":"Наследие долины", "culture.dwarf":"Гномьи механизмы", "culture.sea":"Морские тайны",
 "rarity.0":"Обычный", "rarity.1":"Необычный", "rarity.2":"Редкий", "rarity.3":"Эпический", "rarity.4":"Легендарный",
 "item.journal.name":"Полевой дневник", "item.journal.description":"Истории, раскопки и восстановление артефактов. ПКМ, чтобы открыть. Горячая клавиша настраивается в config.json.",
 "item.surveyor.name":"Археологический прибор", "item.surveyor.description":"ПКМ на раскопках: направление и расстояние до находки. Подойди ближе и выкопай её мотыгой.",
 "mail.title":"Забытые истории долины", "mail.welcome":"Дорогой исследователь!^В земле долины скрывается немало историй. Я подготовил для тебя полевой дневник и записи о первых раскопках.^Нажми O, чтобы открыть дневник, и K для обследования местности. Клавиши можно изменить в config.json. Копии дневника и прибора можно получить на первой странице книги.^Приноси восстановленные древности в музей: через дневник мы зарегистрируем их в отдельном исследовательском архиве.^Буду с интересом следить за твоими открытиями!^^   — Гюнтер",
 "message.welcome":"Гюнтер прислал письмо! Дневник: {{journal}}. Поиск: {{survey}}.",
 "message.inventory-full":"Освободи место в рюкзаке — предметы и фрагменты останутся у тебя.",
 "message.restored":"Артефакт восстановлен: {{name}}!", "message.bring-artifact":"Положи этот артефакт в рюкзак.",
 "message.donated":"{{name}} принят в архив! Награда: деньги и ключевая деталь.",
 "message.site-complete":"Участок исследован! Получена ключевая деталь. Новые участки появляются утром.",
 "message.fragments":"+{{amount}} фрагментов: {{culture}}", "message.keystone":"Найдена ключевая деталь: {{culture}}!",
 "survey.no-site":"Здесь нет активных раскопок. Посмотри участки в дневнике ({{key}}).",
 "survey.here":"Находка под ногами! Выкопай подсвеченную клетку мотыгой.", "survey.near":"Зелёный сигнал: близко, {{direction}}.",
 "survey.medium":"Жёлтый сигнал: неподалёку, {{direction}}.", "survey.far":"Красный сигнал: далеко, {{direction}}.",
 "direction.e":"на восток", "direction.se":"на юго-восток", "direction.s":"на юг", "direction.sw":"на юго-запад", "direction.w":"на запад", "direction.nw":"на северо-запад", "direction.n":"на север", "direction.ne":"на северо-восток", "direction.here":"здесь",
 "hud.site":"{{culture}} · находок: {{count}} · поиск: {{key}}",
 "journal.title":"Археология · Тайны долины", "journal.counter":"Коллекция: {{count}}/{{total}}   ·   Уровень исследователя: {{level}}",
 "journal.overview":"Полевые записи", "journal.field-guide":"С чего начать", "journal.expeditions":"Доступные раскопки",
 "journal.guide":"1. Выбери участок на соседней странице.\n\n2. На месте нажимай {{survey}}. Стрелка и цвет сигнала помогут найти путь.\n\n3. Подойди к находке и обследуй ещё раз. Выкопай золотую клетку мотыгой.\n\n4. В разделах книги собирай артефакты из фрагментов.\n\nКлавиша дневника: {{journal}}.",
 "journal.tools":"Получить дневник и прибор", "journal.site":"Осталось находок: {{count}} · дней: {{days}}",
 "journal.no-sites":"Все доступные участки исследованы. Новые записи появятся утром.",
 "journal.museum":"В архиве музея: {{count}}. Для передачи принеси артефакт в музей и открой его страницу.",
 "journal.footer":"Поиск: {{key}} · Вкладки: ← → · Выбор: ↑ ↓ · Текст: колёсико · Закрыть: Esc",
 "journal.fragments":"Фрагменты: {{count}}   ·   Детали: {{keys}}", "journal.unknown":"Неизвестная древность",
 "journal.donated":"В музейном архиве", "journal.restored":"Восстановлен", "journal.current":"Текущий проект", "journal.undiscovered":"Ещё не открыт",
 "journal.lore-locked":"Восстанови артефакт, чтобы прочитать его историю.",
 "journal.hidden-hint":"Следы этой древности могут встретиться в направлении «{{culture}}». Продолжай исследования: новые проекты открываются после восстановления текущего. Легендарные проекты доступны после семи восстановлений этого направления.",
 "journal.progress":"Собрано: {{count}} / {{cost}}", "journal.key-on":"[x] Ключевая деталь ({{count}})", "journal.key-off":"[ ] Ключевая деталь ({{count}})",
 "journal.key-help":"Одна ключевая деталь заменяет до 12 фрагментов. Расходуется при восстановлении.",
 "journal.restore":"Восстановить артефакт", "journal.donate":"Передать в архив музея", "journal.use":"Использовать реликвию",
 "journal.display":"Поставь артефакт на стол или принеси его в музейный архив.",
 "relic.used":"Реликвия уже использована сегодня. Её сила вернётся утром.", "relic.full-energy":"Энергия уже восстановлена. Заряд реликвии сохранён.",
 "relic.full-health":"Здоровье уже восстановлено. Заряд реликвии сохранён.", "relic.energy":"Тёплая сила реликвии восстанавливает 60 энергии.",
 "relic.heal":"Знакомая мелодия восстанавливает 40 здоровья.", "relic.travel":"Возьми реликвию на активные раскопки. Заряд сохранён.",
 "relic.reveal":"Реликвия указала точное место. Выкопай золотую клетку мотыгой!", "relic.coffee":"В термосе нашёлся горячий тройной эспрессо!", "relic.shells":"В ларце появились три морские ракушки."
}
EN = {
 "culture.valley":"Valley Heritage", "culture.dwarf":"Dwarven Mechanisms", "culture.sea":"Secrets of the Sea",
 "rarity.0":"Common", "rarity.1":"Uncommon", "rarity.2":"Rare", "rarity.3":"Epic", "rarity.4":"Legendary",
 "item.journal.name":"Field Journal", "item.journal.description":"Stories, dig sites, and artifact restoration. Right-click to open. Configure shortcuts in config.json.",
 "item.surveyor.name":"Survey Instrument", "item.surveyor.description":"Right-click at a dig site to locate finds. Survey nearby, then dig the highlighted tile with a hoe.",
 "mail.title":"The valley's forgotten stories", "mail.welcome":"Dear explorer,^The valley's soil holds many stories. I have prepared a field journal and notes on your first dig sites.^Press O for your journal and K to survey. Shortcuts can be changed in config.json. You can collect a journal and instrument from the first page.^Bring restored artifacts to the museum and register them in our separate research archive through the journal.^I look forward to your discoveries!^^   - Gunther",
 "message.welcome":"A letter from Gunther! Journal: {{journal}}. Survey: {{survey}}.",
 "message.inventory-full":"Make room in your backpack. Your items and fragments are safe.",
 "message.restored":"Artifact restored: {{name}}!", "message.bring-artifact":"Put this artifact in your backpack.",
 "message.donated":"{{name}} archived! Reward: gold and a keystone.",
 "message.site-complete":"Site completed! You received a keystone. New sites appear in the morning.",
 "message.fragments":"+{{amount}} fragments: {{culture}}", "message.keystone":"Found a keystone: {{culture}}!",
 "survey.no-site":"No active dig site here. Check your journal ({{key}}).", "survey.here":"A find is right here! Hoe the highlighted tile.",
 "survey.near":"Green signal: close, {{direction}}.", "survey.medium":"Yellow signal: nearby, {{direction}}.", "survey.far":"Red signal: far away, {{direction}}.",
 "direction.e":"east", "direction.se":"southeast", "direction.s":"south", "direction.sw":"southwest", "direction.w":"west", "direction.nw":"northwest", "direction.n":"north", "direction.ne":"northeast", "direction.here":"here",
 "hud.site":"{{culture}} · finds: {{count}} · survey: {{key}}",
 "journal.title":"Archaeology · Secrets of the Valley", "journal.counter":"Collection: {{count}}/{{total}}   ·   Explorer level: {{level}}",
 "journal.overview":"Field Notes", "journal.field-guide":"Getting started", "journal.expeditions":"Active dig sites",
 "journal.guide":"1. Choose a site on the opposite page.\n\n2. Press {{survey}} at the location. Follow the arrow and signal colour.\n\n3. Survey again when close. Dig the golden tile with your hoe.\n\n4. Open a culture tab to restore artifacts from fragments.\n\nJournal shortcut: {{journal}}.",
 "journal.tools":"Collect journal and instrument", "journal.site":"Finds left: {{count}} · days: {{days}}", "journal.no-sites":"All available sites are complete. New notes arrive in the morning.",
 "journal.museum":"Museum archive: {{count}}. Bring an artifact to the museum and open its page to donate.",
 "journal.footer":"Survey: {{key}} · Tabs: ← → · Select: ↑ ↓ · Scroll text: wheel · Close: Esc",
 "journal.fragments":"Fragments: {{count}}   ·   Keystones: {{keys}}", "journal.unknown":"Unknown antiquity",
 "journal.donated":"In the museum archive", "journal.restored":"Restored", "journal.current":"Current project", "journal.undiscovered":"Undiscovered",
 "journal.lore-locked":"Restore this artifact to uncover its story.",
 "journal.hidden-hint":"Traces of this artifact belong to {{culture}}. Complete your current project to uncover new ones. Legendary projects unlock after seven restorations in this field.",
 "journal.progress":"Collected: {{count}} / {{cost}}", "journal.key-on":"[x] Use keystone ({{count}})", "journal.key-off":"[ ] Use keystone ({{count}})",
 "journal.key-help":"One keystone replaces up to 12 fragments. Consumed only when restoring.", "journal.restore":"Restore artifact", "journal.donate":"Donate to museum archive", "journal.use":"Use relic",
 "journal.display":"Display on a table, or bring it to the museum archive.", "relic.used":"Already used today. The relic's power returns tomorrow.",
 "relic.full-energy":"Energy is already full. Your charge is saved.", "relic.full-health":"Health is already full. Your charge is saved.",
 "relic.energy":"The relic restores 60 energy.", "relic.heal":"A familiar melody restores 40 health.", "relic.travel":"Bring this relic to an active dig site. Your charge is saved.",
 "relic.reveal":"The relic revealed a find! Hoe the golden tile.", "relic.coffee":"The thermos held a hot Triple Shot Espresso!", "relic.shells":"Three clams appeared inside the casket."
}

# Source art is a hand-authored grid. Each source cell writes one pixel directly.
from pixel_art import render_icon, render_lamp
from art_preview import make_previews
from workshop_text import RU as WORKSHOP_RU, EN as WORKSHOP_EN
from workshop_art import generate as generate_workshop
from gallery_art import generate as generate_gallery
from gallery_content import RU as GALLERY_RU, EN as GALLERY_EN
from shipwreck_content import RU as SHIPWRECK_RU, EN as SHIPWRECK_EN
from shipwreck_art import generate as generate_shipwreck
from unusual_art import generate as generate_unusual
from unusual_content import ARTIFACTS as UNUSUAL_ARTIFACTS, RU as UNUSUAL_RU, EN as UNUSUAL_EN
from weather_content import ARTIFACTS as WEATHER_ARTIFACTS, RU as WEATHER_RU, EN as WEATHER_EN
from nature_content import ARTIFACTS as NATURE_ARTIFACTS, RU as NATURE_RU, EN as NATURE_EN
from nature_art import generate as generate_nature
from resident_content import ARTIFACTS as RESIDENT_ARTIFACTS, RU as RESIDENT_RU, EN as RESIDENT_EN
from resident_art import generate as generate_residents
from field_content import RU as FIELD_RU, EN as FIELD_EN

RU.update(WORKSHOP_RU)
EN.update(WORKSHOP_EN)
DATA.extend(WEATHER_ARTIFACTS)
DATA.extend(UNUSUAL_ARTIFACTS)
DATA.extend(NATURE_ARTIFACTS)
DATA.extend(RESIDENT_ARTIFACTS)
RU.update(WEATHER_RU)
EN.update(WEATHER_EN)
RU.update(GALLERY_RU)
EN.update(GALLERY_EN)
RU.update(SHIPWRECK_RU)
EN.update(SHIPWRECK_EN)
RU.update(UNUSUAL_RU)
EN.update(UNUSUAL_EN)
RU.update(NATURE_RU)
EN.update(NATURE_EN)
RU.update(RESIDENT_RU)
EN.update(RESIDENT_EN)
RU.update(FIELD_RU)
EN.update(FIELD_EN)

atlas = Image.new("RGBA", (128, 80))
icons = []
catalog = []
icon_dir = ROOT / "assets/icons"
icon_dir.mkdir(parents=True, exist_ok=True)
for i, row in enumerate(DATA):
    aid, culture, rarity, cost, price, effect, shape, ru, en, rd, ed, rl, el = row
    sprite = i if i < 21 else i + 3  # 21..23 remain reserved for the original tools and sparkle.
    source = {"rain_pitcher": "rain", "storm_fork": "storm", "frozen_leaf": "snow", "tide_calendar": "season", "star_glass": "meteor",
              "reader_seal": "penny", "garden_locket": "evelyn", "harbor_whistle": "willy"}.get(aid, "research")
    catalog.append(dict(Id=aid, Culture=culture, Rarity=rarity, Cost=cost, Price=price, Sprite=sprite, Effect=effect, Source=source))
    RU.update({f"artifact.{aid}.name": ru, f"artifact.{aid}.description": rd, f"artifact.{aid}.lore": rl})
    EN.update({f"artifact.{aid}.name": en, f"artifact.{aid}.description": ed, f"artifact.{aid}.lore": el})
    im = render_icon(aid)
    im.save(icon_dir / f"{aid}.png")
    atlas.paste(im, (sprite % 8 * 16, sprite // 8 * 16))
    icons.append(im)

for i, aid in enumerate(("journal", "surveyor", "sparkle"), start=21):
    im = render_icon(aid)
    im.save(icon_dir / f"{aid}.png")
    atlas.paste(im, (i % 8 * 16, i // 8 * 16))

# The placed-object atlas is 2 native 16x32 tiles, as required by the game.
lamps = Image.new("RGBA", (32, 32))
for slot, aid in enumerate(("keeper_lantern", "forest_heart")):
    lamps.paste(render_lamp(aid), (slot * 16, 0))

(ROOT / "assets").mkdir(exist_ok=True)
(ROOT / "i18n").mkdir(exist_ok=True)
for filename, data in (("assets/artifacts.json", catalog), ("i18n/default.json", EN), ("i18n/ru.json", RU)):
    (ROOT / filename).write_text(json.dumps(data, ensure_ascii=False, indent=2) + chr(10), encoding="utf-8")
atlas.save(ROOT / "assets/artifacts.png")
lamps.save(ROOT / "assets/lamps.png")

# Enlargements are presentation-only; the exported game icons above are all 16x16.
make_previews(ROOT, catalog, RU, icons)
generate_workshop(ROOT)
generate_gallery(ROOT)
generate_shipwreck(ROOT)
generate_unusual(ROOT)
generate_nature(ROOT)
generate_residents(ROOT)
print(f"Generated {len(catalog)} native 16x16 artifact icons, 3 interface icons, and 2 native placed lamps.")
