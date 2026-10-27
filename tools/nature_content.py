"""Three natural-event finds, drawn directly as native sixteen-pixel icons."""
ARTIFACTS = [
    ("frozen_leaf", "valley", 2, 1, 700, "", "leaf", "Отпечаток спящего листа", "Sleeping Leaf Imprint",
     "Редкая находка после первого снега. В тонком льду сохранился золотой лист. Подходит для витрины, архива Гюнтера или продажи.",
     "A rare find after the first snow. A golden leaf rests inside thin ice. Display it, donate it to Gunther's archive, or sell it.",
     "На обороте процарапано: «До весны». Кто-то хотел сохранить последний лист своего сада — и зима сдержала обещание за него.",
     "Someone scratched 'Until spring' on the back. They wanted to preserve their garden's last leaf, and winter kept the promise for them."),
    ("tide_calendar", "sea", 2, 1, 850, "", "calendar", "Календарь четырёх приливов", "Calendar of Four Tides",
     "Редкая находка на берегу при смене сезона. Четыре цветные вставки отмечают годовой круг. Подходит для витрины, архива Гюнтера или продажи.",
     "A rare coastal find at the turn of a season. Four colored inlays mark the year's cycle. Display it, donate it to Gunther's archive, or sell it.",
     "Моряки отмечали здесь первый тёплый ветер, длинный день, красные листья и тихую воду перед снегом. Домой возвращались по этому календарю, а не по звёздам.",
     "Sailors marked the first warm wind, the longest day, red leaves and still water before snow. This calendar, rather than the stars, told them when to come home."),
    ("star_glass", "dwarf", 3, 1, 1600, "", "glass", "Звёздное стекло", "Star Glass",
     "Эпическая находка в земле возле нового метеорита на ферме. В чёрном стекле мерцает холодный свет. Подходит для витрины, архива Гюнтера или продажи.",
     "An epic find in the soil beside a new meteorite on the farm. A cold light glimmers in dark glass. Display it, donate it to Gunther's archive, or sell it.",
     "Гномьи мастера называли такие осколки «окнами без стены». Если долго смотреть в глубину, кажется, будто далёкий огонёк медленно приближается.",
     "Dwarven makers called such shards 'windows without walls'. Gaze into its depths and a distant light seems to draw slowly closer."),
]

GRIDS = {
    "frozen_leaf": """
        .....######
        ...##CwwTC#
        ..#CwwTTTC#
        .#CwTTT##TC#
        .#CTT#hl#TC#
        #CwT#hlm#TT#
        #CT#hlm#TTC#
        #CT#lm#TTTC#
        #CT#m#TTTwC#
        .#CT#TTTTC#
        .#CTTTTTC#
        ..#CTTTC#
        ...#CCC#
        ....###
    """,
    "tide_calendar": """
        ...########
        ..#hlllllmb#
        .#h#GG#hh#mb#
        #m#Ggv#hmh#bb#
        #m#gvv#mmh#bb#
        #m#####mmh#bb#
        #m#TT#hl#h#bb#
        #m#Tt#lm#e#bb#
        #m#tt#####ebb#
        #m#ttt#beb#bb#
        #m#Ttt#bhb#bb#
        .#b#tt#bb#bs#
        ..#bbbbbbbb#
        ...########
    """,
    "star_glass": """
        .......###
        ......#APu#
        .....#APpu#
        ....#APwpu#
        ...#APwwwpu#
        ..#APuPwPuu#
        .#APpuuPpuux#
        #APpuuuuuuux#
        #APpuuuxxxax#
        #Apuuxxxaaax#
        .#puxxaaAxx#
        ..#xxaAxxx#
        ...#axxxx#
        ....#####
    """,
}

RU = {
    "nature.tab": "Природа",
    "nature.title": "Следы природных событий",
    "nature.name.snow": "Под первым снегом",
    "nature.name.season": "Берег нового сезона",
    "nature.name.meteor": "След упавшей звезды",
    "nature.discovered": "Новые раскопки: {{name}} · {{location}}. Подробности в книге, вкладка «Природа».",
    "nature.completed": "{{name}}: +{{fragments}} фрагментов, {{experience}} опыта и деталь за участок!",
    "nature.reward-waiting": "Новая находка ждёт в книге, вкладка «Природа». Освободи место и забери её.",
    "nature.status": "Природное событие",
    "nature.waiting": "Ждём нового природного события.",
    "nature.tomorrow": "Зацепка откроется следующим утром.",
    "nature.queued": "Зацепка ждёт свободной доступной земли. Осталось дней: {{days}}. Исходный срок не продлевается.",
    "nature.guide.snow": "Первый замеченный снежный день каждого года оставляет зацепку на следующее утро. Три находки появятся в лесу, глуши или городе на {{days}} дн. Если мод установлен в снежный день, он тоже считается первым наблюдением.",
    "nature.guide.season": "Первый день нового сезона открывает три находки на пляже на {{days}} дн. Нужно встретить смену сезона с включённым модом: установка сразу первого числа не создаёт событие задним числом.",
    "nature.guide.meteor": "Новый метеорит на ферме открывает три находки в радиусе 8 клеток от места падения на {{days}} дн. Уже стоявшие метеориты при первом наблюдении не учитываются. Сам камень не нужно разбивать. Если вокруг всё занято, освободи обычную землю и перезагрузи сохранение до окончания срока.",
    "nature.rules": "Первое завершение каждого вида даёт уникальный артефакт; он хранится в книге до получения. Повторные участки дают фрагменты, опыт и древнюю деталь. Снег и берег: +12 фрагментов и 45 опыта; метеорит: +18 и 75. Все три вида могут существовать одновременно. Точки обходят воду, посевы и постройки. Песочные часы продлевают участок один раз.",
    "nature.disabled": "Новые участки отключены в настройках. Наблюдения продолжаются; старые участки и награды доступны до своего срока. События за время отключения не накапливаются.",
    "nature.artifact-hint.snow": "Снег бережно укрыл что-то забытое. Заверши раскопки «Под первым снегом»; зацепка приходит на утро после первого замеченного снежного дня года.",
    "nature.artifact-hint.season": "На берегу меняется не только погода. Заверши раскопки «Берег нового сезона» после смены сезона.",
    "nature.artifact-hint.meteor": "Упавшая звезда оставила след в земле фермы. Заверши раскопки возле нового метеорита.",
}
EN = {
    "nature.tab": "Nature",
    "nature.title": "Traces of natural events",
    "nature.name.snow": "Beneath the First Snow",
    "nature.name.season": "Shore of a New Season",
    "nature.name.meteor": "Trace of a Fallen Star",
    "nature.discovered": "New dig: {{name}} · {{location}}. See the journal's Nature tab.",
    "nature.completed": "{{name}}: +{{fragments}} fragments, {{experience}} experience and the site keystone!",
    "nature.reward-waiting": "A new find awaits in the journal's Nature tab. Make room and claim it.",
    "nature.status": "Natural event",
    "nature.waiting": "Waiting for a new natural event.",
    "nature.tomorrow": "This lead opens the next morning.",
    "nature.queued": "This lead awaits free, accessible ground. Days remaining: {{days}}. Its original deadline is unchanged.",
    "nature.guide.snow": "The first observed snowy day of each year leaves a lead for the next morning. Three finds appear in the forest, backwoods or town for {{days}} days. Installing on a snowy day also counts as the first observation.",
    "nature.guide.season": "The first day of a new season opens three beach finds for {{days}} days. The mod must observe the season change; installing on the first day does not create a retroactive event.",
    "nature.guide.meteor": "A new farm meteorite opens three finds within 8 tiles of the impact for {{days}} days. Meteorites present at the first observation are ignored. You do not need to break the rock. If the ground is occupied, clear ordinary soil and reload before the lead expires.",
    "nature.rules": "The first completion of each kind gives a unique artifact, stored in the journal until claimed. Repeat sites grant fragments, experience and a keystone. Snow and shore: +12 fragments and 45 experience; meteor: +18 and 75. All three kinds can coexist. Finds avoid water, crops and buildings. The hourglass can extend a site once.",
    "nature.disabled": "New sites are disabled. Observations continue; existing sites and rewards remain available until their own deadlines. Events during disabled days do not accumulate.",
    "nature.artifact-hint.snow": "Snow covered something forgotten. Finish Beneath the First Snow; its lead opens the morning after the year's first observed snowy day.",
    "nature.artifact-hint.season": "More than the weather changes on the shore. Finish Shore of a New Season after a season change.",
    "nature.artifact-hint.meteor": "A fallen star left traces in the farm's soil. Finish the dig beside a new meteorite.",
}
