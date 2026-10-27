"""Weather expeditions, their two collectible relics, and native pixel art."""
ARTIFACTS = [
    ("rain_pitcher", "valley", 2, 1, 700, "water", "pitcher", "Кувшин первого дождя", "Pitcher of the First Rain",
     "ПКМ: поливает до 9 вспаханных клеток перед игроком. Один раз в день; без сухой земли заряд сохраняется.",
     "Right-click: water up to 9 tilled tiles in front of you. Once per day; no charge is spent without dry soil.",
     "Под слоем лесного ила сохранился рисунок маленькой тучи. Каждое утро на дне кувшина снова собирается дождь, хотя его давно никто не наполнял.",
     "A tiny cloud was painted beneath the forest silt. Rain gathers in the pitcher every morning, though nobody has filled it for years."),
    ("storm_fork", "dwarf", 3, 1, 1300, "resonance", "fork", "Грозовой камертон", "Storm Tuning Fork",
     "ПКМ: на 30 секунд раскрывает все находки раскопок в радиусе 8 клеток. Один раз в день; копай подсвеченные клетки мотыгой.",
     "Right-click: reveal all dig-site finds within 8 tiles for 30 seconds. Once per day; hoe the highlighted tiles.",
     "Молния обнажила инструмент у обугленных корней. Между зубцами дрожит тихий аккорд: говорят, подгорные мастера слышали в нём отклик забытых вещей.",
     "Lightning exposed this instrument beneath charred roots. A quiet chord trembles between its prongs; underhill makers heard forgotten objects answer it.")
]

GRIDS = {
    "rain_pitcher": """
        ....######
        ...#lwwllh#
        ....#Cctd#
        ...#Cwwctd#
        ..#CwwCCtd###
        .#CCwwwctd###
        .#CwwwCtdd#.#
        .#CwCttddt#.#
        .#CwtdTddt###
        .#cctTtdtd#
        ..#cttdttd#
        ..#dddddds#
        ...#bbbbe#
        ....#####
    """,
    "storm_fork": """
        ..##....##
        .#lh#..#lh#
        .#hm#..#mb#
        .#hm#..#mb#
        .#hm#..#mb#
        .#hm#..#mb#
        .#hm#..#mb#
        ..#hm##mb#
        ...#hmbb#
        ....#mmb#
        ....#Ttd#
        ....#Ttd#
        ....#mmb#
        .....###
    """,
}

RU = {
    "weather.tab": "Погода",
    "weather.title": "Следы непогоды",
    "weather.guide": "Утром после дождливого дня земля может открыть старую стоянку. После грозы появляются обугленные корни с остатками подгорного механизма.\n\nОсобый участок содержит 3 находки и существует {{days}} дня. Прибор сначала ведёт к нему, затем к обычным раскопкам.\n\nПервое завершение каждого типа даёт новую реликвию. Последующие — дополнительные фрагменты и ключевую деталь.\n\nМежду появлением участков проходит не меньше {{cooldown}} дней. Незаконченные участки остаются до своего срока. Награды ждут в книге без ограничения времени.",
    "weather.disabled": "Создание новых погодных участков выключено в config.json. Существующие можно закончить.",
    "weather.lead": "Зацепка «{{name}}» ждёт безопасного места на карте. До окончания срока: {{days}} дн.",
    "weather.name.rain": "Размытая стоянка",
    "weather.name.storm": "Обугленные корни",
    "weather.discovered": "Новые раскопки: {{name}} — {{location}}. Подробности во вкладке «Погода».",
    "weather.completed": "{{name}}: исследование завершено! +12 фрагментов и опыт.",
    "weather.reward-waiting": "Новая реликвия ждёт во вкладке «Погода» — забери её из дневника.",
    "weather.claim": "Забрать реликвию",
    "weather.status": "Находка после непогоды",
    "weather.reward-hint.rain": "Неизвестная реликвия дождя",
    "weather.reward-hint.storm": "Неизвестная реликвия грозы",
    "weather.waiting.rain": "Новая стоянка может открыться утром после дождя.",
    "weather.waiting.storm": "Следы корней появляются утром после грозы.",
    "weather.active": "{{location}} · находок: {{count}} · осталось дней: {{days}}",
    "weather.active-short": "{{location}} · {{count}} находки · {{days}} дн.",
    "weather.record": "Исследовано участков: {{count}}",
    "weather.artifact-hint.rain": "Эта реликвия скрыта в размытой стоянке. Дождись дождливого дня, проверь вкладку «Погода» следующим утром и исследуй все три находки. Награда останется в книге до получения.",
    "weather.artifact-hint.storm": "Эта реликвия скрыта под обугленными корнями. Утром после грозы проверь вкладку «Погода» и исследуй все три находки. Фрагменты обычных проектов для неё не нужны.",
    "weather.no-soil": "Перед тобой нет сухой вспаханной земли. Заряд кувшина сохранён.",
    "weather.watered": "Древний кувшин полил клеток: {{count}}.",
    "weather.no-resonance": "В радиусе 8 клеток нет доступных находок раскопок. Заряд камертона сохранён.",
    "weather.resonance": "Откликнулись находки: {{count}}! Голубые клетки можно копать 30 секунд."
}

EN = {
    "weather.tab": "Weather",
    "weather.title": "Traces of the Weather",
    "weather.guide": "The morning after a rainy day may reveal an old encampment. After a thunderstorm, charred roots can expose an underhill mechanism.\n\nA special site contains 3 finds and lasts {{days}} days. Your instrument prioritizes it before ordinary digs.\n\nThe first completion of each type grants a new relic. Repeats give extra fragments and a keystone.\n\nAt least {{cooldown}} days separate new sites. Unfinished sites remain until their expiry. Unclaimed rewards wait in the journal indefinitely.",
    "weather.disabled": "New weather sites are disabled in config.json. Existing sites can still be completed.",
    "weather.lead": "The lead '{{name}}' is waiting for a safe map position. Days remaining: {{days}}.",
    "weather.name.rain": "Washed-Out Encampment",
    "weather.name.storm": "Charred Roots",
    "weather.discovered": "New dig site: {{name}} - {{location}}. See the Weather tab.",
    "weather.completed": "{{name}} completed! +12 fragments and research experience.",
    "weather.reward-waiting": "A new relic is waiting in your journal's Weather tab.",
    "weather.claim": "Collect relic",
    "weather.status": "A weather discovery",
    "weather.reward-hint.rain": "An unknown relic of rain",
    "weather.reward-hint.storm": "An unknown relic of storms",
    "weather.waiting.rain": "An encampment may open the morning after rain.",
    "weather.waiting.storm": "The roots can be uncovered the morning after a thunderstorm.",
    "weather.active": "{{location}} · finds: {{count}} · days left: {{days}}",
    "weather.active-short": "{{location}} · {{count}} finds · {{days}} days",
    "weather.record": "Sites researched: {{count}}",
    "weather.artifact-hint.rain": "This relic lies in a washed-out encampment. Wait for a rainy day, check the Weather tab the following morning, and excavate all three finds. Your reward will wait in the book.",
    "weather.artifact-hint.storm": "This relic lies beneath charred roots. Check the Weather tab the morning after a storm and excavate all three finds. Ordinary research fragments are not needed.",
    "weather.no-soil": "No dry tilled soil is in front of you. The pitcher's charge is saved.",
    "weather.watered": "The ancient pitcher watered {{count}} tiles.",
    "weather.no-resonance": "No available dig-site finds within 8 tiles. The fork's charge is saved.",
    "weather.resonance": "{{count}} finds resonated! Hoe the blue tiles within 30 seconds."
}
