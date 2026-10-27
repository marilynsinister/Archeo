"""Four research relics, their native 16px grids and bilingual field notes."""
ARTIFACTS = [
    ("past_lens", "dwarf", 2, 58, 950, "past", "lens", "Лупа прошлого", "Lens of the Past",
     "ПКМ: на 30 секунд проявляет проходимую тропу следов к ближайшей находке и древний знак над ней. Один раз в день. Без доступного следа заряд сохраняется.",
     "Right-click: reveal a walkable trail of footprints to the nearest find and its old inscription for 30 seconds. Once a day; no trail preserves the charge.",
     "В стекле видны не царапины, а крошечные тропинки. Гномий мастер подписал оправу: «Вещи помнят дорогу домой, даже когда её забывают люди».",
     "The marks in its glass are tiny paths, not scratches. A dwarven maker engraved the rim: 'Things remember the way home, even when people forget.'"),
    ("garden_flute", "valley", 3, 78, 1600, "garden", "flute", "Флейта забытого сада", "Forgotten Orchard Flute",
     "ПКМ рядом с плодовым деревом: выращивает один дополнительный плод. Раз в 7 игровых дней. Нужно взрослое здоровое дерево в сезоне плодоношения, с менее чем тремя плодами, в радиусе 5 клеток. В теплице учитываются правила дерева.",
     "Right-click near a fruit tree: grow one extra fruit, once every 7 game days. Requires a healthy mature tree in season, holding fewer than three fruits, within 5 tiles. Greenhouse season rules apply.",
     "Когда-то садовница играла на этой флейте, пока ждала писем из далёких стран. Деревья привыкли к мелодии и до сих пор отвечают ей тихим шелестом.",
     "A gardener played this flute while waiting for letters from distant lands. The trees grew used to her melody and still answer with a quiet rustle."),
    ("keeper_hourglass", "dwarf", 3, 80, 1700, "hourglass", "hourglass", "Песочные часы смотрителя", "Keeper's Hourglass",
     "ПКМ на локации раскопок: показывает остаток находок и продлевает ближайший подходящий участок на один день. Раз в день, каждый участок продлевается только один раз. Сюжетные тайники не продлеваются.",
     "Right-click at a dig location: report remaining finds and extend the nearest eligible site by one day. Once a day, once per site. Story caches cannot be extended.",
     "Песок в верхней колбе не убывает, пока кто-нибудь бережно перебирает найденные черепки. Кажется, смотритель считал незаконченные истории важнее расписания.",
     "The upper bulb never empties while someone carefully sorts their pottery shards. The keeper seems to have valued unfinished stories above timetables."),
    ("pocket_observatory", "sea", 3, 88, 1850, "stars", "observatory", "Карманная обсерватория", "Pocket Observatory",
     "ПКМ на улице после 20:00: предсказывает и открывает на следующее утро звёздную экспедицию с двумя находками. Раз в день; одновременно одна звёздная зацепка или участок. Срок — 3 дня с утра появления.",
     "Right-click outdoors after 8pm: forecast and open a two-find star expedition the next morning. Once a day; only one pending or active star expedition. Lasts 3 days from its first morning.",
     "Мореходы носили её вместо тяжёлых карт. Стрелки показывают не север, а места, где завтрашний свет первым коснётся чего-то давно забытого.",
     "Sailors carried it instead of heavy charts. Its needles point not north, but where tomorrow's light will first touch something long forgotten."),
]

GRIDS = {
    "past_lens": """
        ....######
        ...#hllllmb#
        ..#h#CTTc#mb#
        .#h#CwTTtc#bb#
        .#m#TTdtTt#bb#
        .#m#TtdTtt#bb#
        ..#b#tttd#bs#
        ...#bbbbbb##
        ....######
        .......#ms#
        ........#mb#
        .........#mb#
        ..........#b#
        ...........##
    """,
    "garden_flute": """
        ..........###
        .........#hl#
        ........#hm#
        .......#hmb#
        ......#h#b#
        .....#hmb#
        ....#h#b#
        ...#hmb#
        ..#h#b#
        .#hmvb#
        #hmvGv#
        #bvvG#
        .#bbs#
        ..###
    """,
    "keeper_hourglass": """
        ..##########
        .#hllllllhmb#
        ..#m######b#
        ..#m#CwwC#b#
        ..#m#ClhC#b#
        ..#m#ChhC#b#
        ..#m##hh##b#
        ..#m##hh##b#
        ..#m#CmhC#b#
        ..#m#CmhC#b#
        ..#m#Clhh#b#
        ..#m######b#
        .#hmmmmmmmbb#
        ..##########
    """,
    "pocket_observatory": """
        ....#######
        ...#hlmmmbb#
        ..#h#uPpu#bb#
        .#m#uPwPpu#bb#
        .#m#PwwwPp#bb#
        .#m#uPwPpu#bb#
        ..#b#upPu#bs#
        ...#bbbbbb##
        ....###b###
        ...#hllmbb#
        ..#hm#mb#bb#
        .#hm#.#b#.#b#
        .#b#..#b#..##
        ..#....#
    """,
}

RU = {
    "unusual.cooldown": "Мелодия ещё отдыхает. До нового использования: {{days}} дн.",
    "unusual.ready": "Реликвия готова к использованию.",
    "unusual.ready-in": "До следующего использования: {{days}} дн. Заряд общий для всех копий у одного игрока.",
    "unusual.no-trail": "Лупа не видит проходимой тропы к этой находке. Заряд сохранён.",
    "unusual.past-revealed": "Проявились следы: {{culture}}. На участке осталось находок: {{count}}. Следы видны 30 секунд.",
    "unusual.no-tree": "В радиусе 5 клеток нужно взрослое здоровое плодовое дерево в сезоне, с менее чем тремя плодами. Заряд сохранён.",
    "unusual.garden-busy": "Сад сейчас отвечает другой флейте. Попробуй чуть позже; заряд сохранён.",
    "unusual.fruit-grown": "Сад ответил мелодии: на дереве вырос ещё один плод!",
    "unusual.no-hourglass-site": "Здесь нет подходящего участка: он должен быть активным и ещё не продлённым. Сюжетные тайники не подходят. Заряд сохранён.",
    "unusual.extended": "{{name}}: осталось находок {{count}}; теперь доступно ещё {{days}} дн. Участок продлён на один день.",
    "unusual.stars-night": "Открой обсерваторию на улице после 20:00. Заряд сохранён.",
    "unusual.stars-existing": "Сначала закончи уже предсказанную звёздную экспедицию или дождись окончания её срока. Заряд сохранён.",
    "unusual.stars-no-space": "Обсерватория пока не нашла свободного места для экспедиции. Заряд сохранён.",
    "unusual.forecast": "Завтра звёзды укажут на {{location}}. Направление: {{culture}}. Прогноз сохранён на первой странице дневника.",
    "unusual.forecast-note": "Звёздная зацепка: {{location}} · {{culture}}. Она откроется следующим утром; срок — три дня. Если её утро уже наступило, а карта занята, зацепка ждёт свободного места до исходного срока.",
    "unusual.star-site": "Звёздная экспедиция",
    "unusual.stars-opened": "Звёздная экспедиция открыта: {{location}}! Две находки; подробности на первой странице книги.",
    "unusual.stars-complete": "Звёздная экспедиция завершена! +8 фрагментов, 30 опыта и деталь за участок.",
    "unusual.page": "{{page}} / {{pages}}",
    "unusual.page-prev": "Назад",
    "unusual.page-next": "Далее",
}
EN = {
    "unusual.cooldown": "The melody is still resting. Days until ready: {{days}}.",
    "unusual.ready": "This relic is ready to use.",
    "unusual.ready-in": "Days until ready: {{days}}. All copies share one charge per player.",
    "unusual.no-trail": "The lens cannot find a walkable trail to this find. Charge preserved.",
    "unusual.past-revealed": "Old footprints appear: {{culture}}. Finds remaining: {{count}}. The trail lasts 30 seconds.",
    "unusual.no-tree": "Requires a healthy mature fruit tree in season, with fewer than three fruits, within 5 tiles. Charge preserved.",
    "unusual.garden-busy": "The orchard is answering another flute. Please try again shortly; charge preserved.",
    "unusual.fruit-grown": "The orchard answered your tune: one extra fruit has grown on the tree!",
    "unusual.no-hourglass-site": "There is no eligible active, unextended site here. Story caches cannot be extended. Charge preserved.",
    "unusual.extended": "{{name}}: {{count}} finds remain; now available for {{days}} more days. Extended by one day.",
    "unusual.stars-night": "Open the observatory outdoors after 8pm. Charge preserved.",
    "unusual.stars-existing": "Finish your predicted star expedition or wait for it to expire first. Charge preserved.",
    "unusual.stars-no-space": "The observatory cannot find free space for an expedition yet. Charge preserved.",
    "unusual.forecast": "Tomorrow the stars point to {{location}}. Culture: {{culture}}. The forecast is saved on the journal's first page.",
    "unusual.forecast-note": "Star lead: {{location}} · {{culture}}. Opens the next morning and lasts three days. If that morning has passed and the map is occupied, the lead waits for free space until its original deadline.",
    "unusual.star-site": "Star Expedition",
    "unusual.stars-opened": "A star expedition has opened at {{location}}! Two finds; see the journal's first page.",
    "unusual.stars-complete": "Star expedition complete! +8 fragments, 30 experience and the site keystone.",
    "unusual.page": "{{page}} / {{pages}}",
    "unusual.page-prev": "Back",
    "unusual.page-next": "Next",
}
