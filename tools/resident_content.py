"""Original personal stories, useful keepsakes and their native 16px source grids."""
ARTIFACTS = [
    ("reader_seal", "dwarf", 2, 1, 1100, "fragments", "seal", "Печать внимательного читателя", "Careful Reader's Seal",
     "ПКМ: +8 гномьих фрагментов раз в день. Подарок за историю Пенни. Общий заряд для всех копий у одного игрока.",
     "Right-click: +8 dwarven fragments once a day. A keepsake from Penny's story. All copies share one charge per player.",
     "Пенни переписала рядом с гномьим знаком слово «попробуй». Теперь оно встречается на каждой странице, где раньше стояло «неправильно».",
     "Penny wrote 'try again' beside the dwarven mark. It now appears on every page that used to say 'wrong'."),
    ("garden_locket", "valley", 2, 1, 1100, "energy", "locket", "Медальон первого цветника", "First Garden Locket",
     "ПКМ: восстанавливает до 60 энергии раз в день. Подарок за историю Эвелин. При полной энергии заряд сохраняется.",
     "Right-click: restore up to 60 energy once a day. A keepsake from Evelyn's story. Full energy preserves the charge.",
     "За стеклом лежит засушенный лепесток. Эвелин уверяет, что цветы лучше растут, если пообещать кому-нибудь показать их весной.",
     "A pressed petal rests behind the glass. Evelyn says flowers grow better when you promise someone you'll show them in spring."),
    ("harbor_whistle", "sea", 2, 1, 1100, "compass", "whistle", "Свисток тихой гавани", "Quiet Harbour Whistle",
     "ПКМ на участке: точно подсвечивает ближайшую находку раз в день. Подарок за историю Вилли. Без доступной находки заряд сохраняется.",
     "Right-click at a dig site: reveal the nearest find once a day. A keepsake from Willy's story. No available find preserves the charge.",
     "Два коротких, один длинный. Вилли говорит, что этот сигнал означает «я здесь», а иногда — всё, что нужно услышать человеку на берегу.",
     "Two short notes, one long. Willy says the signal means 'I'm here', which is sometimes all someone waiting ashore needs to hear."),
]

GRIDS = {
    "reader_seal": """
        ..............
        ....######
        ...#hllllmb#
        ..#hm####mbb#
        .#hm#wwww#bb#
        .#hm#w#wB#bb#
        .#hm#w##B#bb#
        .#hm#wwBB#bb#
        ..#mb####bb#
        ...#bbbbbb#
        ....##bb##
        ...#Tt##tT#
        ...#Tt#.#t#
        ....##...#
    """,
    "garden_locket": """
        ....######
        ...#hlmmbb#
        ....#b##b#
        ....##..##
        ...########
        ..#hlllllmb#
        .#hm#CCC#mbb#
        .#m#CwppC#bb#
        .#m#CpppC#bb#
        .#m#CGgpC#bb#
        .#m#CvggC#bb#
        ..#b#CCC#bb#
        ...#bbbbbb#
        ....######
    """,
    "harbor_whistle": """
        ..........###
        .........#hl#
        ........#hmb#
        .......#hmb#
        ..#####hmb#
        .#hlllmmmb#
        #hm#CT#mmmb#
        #hm#Tt#mbbb#
        #hmm##mbbb#
        .#mmmmmbb#
        ..#bbbb##
        ...####
        ..............
        ..............
    """,
}

RU = {
    "residents.tab": "Жители",
    "residents.title": "У каждой вещи — голос",
    "residents.title.penny": "Азбука под пылью",
    "residents.title.evelyn": "Обещание первой весны",
    "residents.title.willy": "Два коротких, один длинный",
    "residents.site.penny": "Потерянный урок",
    "residents.site.evelyn": "Забытый цветник",
    "residents.site.willy": "Сигнал с берега",
    "residents.discovered": "У находки появилась личная история. {{name}} поможет её узнать — вкладка «Жители» в книге.",
    "residents.site-found": "{{name}}: новый тайник · {{location}}. Две находки, без срока.",
    "residents.recovered": "Записи найдены! +{{fragments}} фрагментов. Вернись к жителю: {{name}}.",
    "residents.shared": "История рассказана: {{name}}. +{{friendship}} дружбы, {{experience}} опыта. Реликвия ждёт в книге.",
    "residents.requirements": "Для разговора подойди к нужному жителю на расстояние до 4 клеток и открой «Жители». Спящий или участвующий в событии персонаж сейчас недоступен.",
    "residents.status": "История жителя",
    "residents.action.start": "Показать запись о находке",
    "residents.action.share": "Обсудить найденную историю",
    "residents.action.claim": "Забрать памятную реликвию",
    "residents.read": "Перечитать записи",
    "residents.return": "Вернуться к цели",
    "residents.no-space": "Пока нет свободного места для двух находок. Освободи землю указанной локации и вернись следующим утром или перезагрузи сохранение. Зацепка не пропадёт.",
    "residents.active": "Тайник: {{location}}. Осталось находок: {{count}}. Поиск — {{key}}, затем мотыгой по подсветке. Срок не ограничен.",
    "residents.reward": "Памятная реликвия: {{artifact}}. За тайник дополнительно {{fragments}} фрагментов; за финальный разговор — {{friendship}} дружбы и {{experience}} опыта. Реликвия ждёт свободного места в книге и выдаётся один раз.",
    "residents.stage.0": "Сначала восстанови «{{artifact}}». История откроется автоматически, даже если находка потом продана, выставлена или передана в архив. Старые открытия тоже засчитываются.",
    "residents.stage.1": "{{name}} может узнать знак на находке. Подойди к жителю, открой эту страницу и покажи запись. Сам предмет отдавать или носить с собой не нужно.",
    "residents.stage.2": "Первая расшифровка сохранена. Обследуй {{location}} и найди две части истории. Фрагменты записей хранятся в книге; для них не нужен рюкзак.",
    "residents.stage.3": "Обе части истории найдены. {{name}} ждёт рассказа: подойди и обсуди записи через кнопку ниже. Находки остаются в архиве книги.",
    "residents.stage.4": "История рассказана, дружба и опыт уже начислены. Забери памятную реликвию, когда в рюкзаке появится место. Ещё раз разговаривать для получения не нужно.",
    "residents.stage.5": "История завершена. Разговоры и найденная записка остаются в архиве этой страницы. Памятная реликвия доступна в коллекции своего направления.",
    "residents.goal.penny": "Пенни узнает в табличке ученика рунописца знакомые попытки исправить ошибку. Её зацепка ведёт к городским тропинкам.",
    "residents.goal.evelyn": "Ласточка на медной броши напоминает Эвелин о весеннем обещании. Зацепка ведёт на лесные дорожки, к следам старого цветника.",
    "residents.goal.willy": "Глиняный свисток моряка хранит простой береговой сигнал. Вилли поможет найти его продолжение на западной стороне пляжа, без ремонта моста.",
    "residents.artifact-hint.penny": "Восстанови табличку ученика рунописца и пройди историю Пенни во вкладке «Жители».",
    "residents.artifact-hint.evelyn": "Восстанови медную брошь с ласточкой и пройди историю Эвелин во вкладке «Жители».",
    "residents.artifact-hint.willy": "Восстанови глиняный свисток моряка и пройди историю Вилли во вкладке «Жители».",
    "residents.dialogue.penny.start": "Можно посмотреть твои записи? В рунах я не специалист… Но вот эти повторения узнаю. Так ученик пробует написать знак, а потом сердится и стирает всё сразу.#$b#Про усы наставника я лучше не буду читать детям. Хотя Винсенту наверняка понравилось бы.#$b#Между строк есть рисунок дорожки и двух плоских камней. Похоже на старые городские тропинки. Проверь их прибором: вдруг там сохранилось продолжение урока?",
    "residents.dialogue.penny.share": "На первой пластинке один и тот же знак вырезан семь раз. На второй… «Ещё раз. Уже лучше». Значит, наставник всё-таки был терпеливым.#$b#Знаешь, я сохраню копию для занятий. Иногда ребёнку нужно увидеть, что даже чьи-то древние учителя разрешали ошибаться.#$b#А эту печать возьми себе. Будем ставить её рядом с маленькими открытиями. Не за правильный ответ — за внимательность.",
    "residents.note.penny": "Две учебные пластинки. На первой семь неровных знаков; на второй рукой наставника: «Ещё раз. Уже лучше». У края дорисованы маленькие усы, аккуратно обведённые рамкой.",
    "residents.dialogue.evelyn.start": "Ох, какая ласточка… Весной такие брошки дарили с обещанием вернуться. Я помню, как мы с подругой ждали птиц у дороги.#$b#За лесной тропинкой у нас был крохотный цветник. Мы прятали там жестяные записки о том, что хотим посадить на следующий год. Совсем как взрослые садовницы.#$b#Может быть, земля сохранила хоть одну. Посмотри на лесных дорожках своим прибором. Только не торопись: хорошая история, как и сад, умеет ждать.",
    "residents.dialogue.evelyn.share": "«Весной посадим ещё. Даже если не все взойдут». Надо же… Я ведь забыла эти слова, а всё равно жила по ним.#$b#Здесь не было редких цветов. Были две девочки, горсть семян и очень серьёзное обещание снова встретиться весной. Этого оказалось достаточно.#$b#Я положила в медальон лепесток из моего нынешнего сада. Возьми. Когда устанешь, вспомни: начинать новый цветник можно в любом возрасте.",
    "residents.note.evelyn": "Две жестяные створки с детским рисунком ласточки и цветника. Внутри прижата бумага: «Весной посадим ещё. Даже если не все взойдут». Подписи стёрлись, но обещание сохранилось.",
    "residents.dialogue.willy.start": "Рыбка-свисток? Ха! Настоящая береговая вещица. Не для шторма — чтобы с суши позвать лодку, когда голос теряется в ветре.#$b#Два коротких, один длинный. Мой отец отвечал так, когда я ждал его с уловом. Я всегда свистел слишком рано и потом краснел.#$b#У старых береговых тропинок на западном пляже оставляли памятки с такими сигналами. Попробуй свой прибор. Мост к лужам тебе не понадобится.",
    "residents.dialogue.willy.share": "Вот она, памятка. «Не свисти, пока не увидишь парус». Хороший совет… жаль, мне его не давали.#$b#А с другой стороны: «Если ждёшь — дай знать, что ты здесь». Пожалуй, этот важнее. Человек в лодке тоже рад услышать знакомый звук.#$b#Я почистил для тебя старый латунный свисток. Два коротких, один длинный. Пусть помогает находить дорогу к тому, что ждёт в земле.",
    "residents.note.willy": "Обломки береговой памятки с рисунками лодки и рыбки. На одной стороне: «Не свисти, пока не увидишь парус». На другой: «Если ждёшь — дай знать, что ты здесь». Ни слова о кладах, только о возвращении домой.",
}

EN = {
    "residents.tab": "Residents",
    "residents.title": "Every object has a voice",
    "residents.title.penny": "An Alphabet Beneath the Dust",
    "residents.title.evelyn": "The First Spring's Promise",
    "residents.title.willy": "Two Short Notes, One Long",
    "residents.site.penny": "The Lost Lesson",
    "residents.site.evelyn": "The Forgotten Garden",
    "residents.site.willy": "A Signal from Shore",
    "residents.discovered": "A find has a personal story. {{name}} can help; see the journal's Residents tab.",
    "residents.site-found": "{{name}}: a new cache at {{location}}. Two finds, no deadline.",
    "residents.recovered": "Notes recovered! +{{fragments}} fragments. Return to {{name}}.",
    "residents.shared": "Story shared with {{name}}. +{{friendship}} friendship, {{experience}} experience. A relic awaits in the journal.",
    "residents.requirements": "Approach the resident within 4 tiles and open Residents to talk. Sleeping residents and ongoing events are unavailable.",
    "residents.status": "Resident's story",
    "residents.action.start": "Show the discovery notes",
    "residents.action.share": "Discuss the recovered story",
    "residents.action.claim": "Claim the keepsake relic",
    "residents.read": "Reread the notes",
    "residents.return": "Return to the objective",
    "residents.no-space": "No safe room for two finds yet. Clear ground at the destination and return next morning or reload. This lead will not expire.",
    "residents.active": "Cache: {{location}}. Finds remaining: {{count}}. Survey with {{key}}, then hoe the highlighted tiles. No deadline.",
    "residents.reward": "Keepsake: {{artifact}}. Cache bonus: {{fragments}} fragments; final conversation: {{friendship}} friendship and {{experience}} experience. The relic waits for backpack space in the journal and can be claimed once.",
    "residents.stage.0": "First restore the {{artifact}}. Its story opens automatically, even if you later sell, display or donate the find. Earlier discoveries count too.",
    "residents.stage.1": "{{name}} may recognise a mark on this find. Approach the resident, open this page and show your notes. You do not need to carry or hand over the object.",
    "residents.stage.2": "The first interpretation is archived. Survey {{location}} and recover two pieces of the story. Notes stay in the journal without using backpack space.",
    "residents.stage.3": "Both pieces recovered. {{name}} is waiting to hear the story: approach and discuss the notes using the button below. Your findings stay in the journal archive.",
    "residents.stage.4": "Story shared; friendship and experience already awarded. Claim the keepsake when you have backpack space. No further conversation is required to collect it.",
    "residents.stage.5": "Story complete. Conversations and the recovered note remain in this page's archive. The keepsake appears in its culture's collection.",
    "residents.goal.penny": "Penny recognises familiar attempts to fix a mistake on the apprentice's tablet. Her lead points to the town's paths.",
    "residents.goal.evelyn": "The swallow brooch reminds Evelyn of a springtime promise. Her lead follows forest paths to an old garden's traces.",
    "residents.goal.willy": "The sailor's clay whistle holds a simple signal from shore. Willy can help find its continuation on the western beach without repairing the bridge.",
    "residents.artifact-hint.penny": "Restore the Apprentice Runecarver's Tablet and follow Penny's story in the Residents tab.",
    "residents.artifact-hint.evelyn": "Restore the Copper Swallow Brooch and follow Evelyn's story in the Residents tab.",
    "residents.artifact-hint.willy": "Restore the Sailor's Clay Whistle and follow Willy's story in the Residents tab.",
    "residents.dialogue.penny.start": "May I see your notes? Runes aren't my specialty... but I recognise these repetitions. A pupil tries to write a symbol, gets frustrated, and rubs everything out.#$b#I might skip the bit about the teacher's moustache when reading to the children. Though Vincent would probably love it.#$b#Between the lines there's a path and two flat stones. It looks like the old town paths. Try your instrument there. Perhaps the rest of the lesson survived?",
    "residents.dialogue.penny.share": "The first tablet has the same symbol carved seven times. The second says... 'Once more. Better already.' So the teacher was patient after all.#$b#I'll keep a copy for our lessons. Sometimes a child needs to see that even an ancient teacher allowed mistakes.#$b#Take this seal. We can put it beside little discoveries. For paying attention, rather than getting the answer right.",
    "residents.note.penny": "Two practice tablets. Seven uneven symbols cover the first; the teacher wrote 'Once more. Better already.' on the second. A small moustache in the margin has been carefully framed.",
    "residents.dialogue.evelyn.start": "Oh, that little swallow... People gave those brooches with a promise to return in spring. I remember waiting for the birds by the road with my friend.#$b#We had a tiny garden beyond the forest path. We hid tin notes there about what we'd plant next year, like very grown-up gardeners.#$b#Perhaps the soil kept one. Try your instrument along the forest paths. Take your time. A good story, like a garden, knows how to wait.",
    "residents.dialogue.evelyn.share": "'We'll plant more in spring. Even if they don't all grow.' My goodness... I'd forgotten those words, yet I kept living by them.#$b#There weren't any rare flowers. Just two girls, a handful of seeds and a very serious promise to meet again in spring. It was enough.#$b#I put a petal from my garden today in this locket. Take it. When you're tired, remember: you can start a new garden at any age.",
    "residents.note.evelyn": "Two tin covers show a child's drawing of a swallow and a garden. A paper inside reads: 'We'll plant more in spring. Even if they don't all grow.' The names faded; the promise survived.",
    "residents.dialogue.willy.start": "A little fish whistle? Ha! A proper shore thing. You'd use it to call a boat when your voice was lost in the wind, though never in a storm.#$b#Two short notes, one long. My father answered that way when I waited for his catch. I always whistled too early, then went red as a lobster.#$b#They left signal notes near the old paths on the western beach. Try your instrument there. You won't need the bridge to the pools.",
    "residents.dialogue.willy.share": "There's the note. 'Don't whistle until you see the sail.' Good advice... wish someone had told me.#$b#The other side says: 'If you're waiting, let them know you're here.' That one's more important. Someone in a boat is glad to hear a familiar sound too.#$b#I cleaned an old brass whistle for you. Two short notes, one long. May it guide you to the things waiting in the soil.",
    "residents.note.willy": "Pieces of a shore signal guide show a boat and a fish. One side reads 'Don't whistle until you see the sail.' The other: 'If you're waiting, let them know you're here.' Nothing about treasure; only coming home.",
}
