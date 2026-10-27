"""Four hand-authored 16x16 story icons and a native 16x32 placed bell."""
from textwrap import dedent
from PIL import Image, ImageDraw, ImageFont
from pixel_art import PALETTE, rgba, render_icon

GRIDS = {
    "port_record": """
        ...#########
        ..#wllllllh#
        .#whbbbbhlh#
        .#wlllllllh#
        .#wlbllbllh#
        .#wlllllllh#
        .#wlbbbbblh#
        .#wlllllllh#
        .#wlbblllbh#
        .#wlllllllh#
        .#wlhbhlblh#
        .#hhhhhhhhb#
        ..#########
        ..............
    """,
    "sealed_bottle": """
        .....####
        ....#hmbb#
        ....#ebbs#
        .....#td#
        ....#CTtd#
        ...#CTTttd#
        ..#CwTTTttd#
        ..#CT#lh#td#
        ..#TT#hl#td#
        ..#Tt#bh#td#
        ..#tt#hb#td#
        ..#tddTTtdd#
        ...#ddddds#
        ....######
    """,
    "captain_letter": """
        ..............
        ..##########
        .#wllllllllh#
        .#wlbbbbbbhh#
        .#wlllllllhh#
        .#wllbllblhh#
        .#wlllllllhh#
        .#wlbbbbllhh#
        .#wlllll###h#
        .#wlbbb#Rrr##
        .#wllll#rbr##
        .#hhhhh#rrs##
        ..##########
        ..............
    """,
    "ship_bell": """
        ...########
        ..#hlmmmmmb#
        ..#m#bbb##b#
        ..#m##h#.#b#
        ..#m#hlm##b#
        ..#m#hlmb#b#
        ..#m#hmmmbb#
        ..#m#mmmbbb#
        ..#m######b#
        ..#m#.#b#bb#
        ..#m#..##bb#
        ..#m######b#
        .#hmmmmmmmmb#
        ..##########
    """,
}


def icon(name):
    image = Image.new("RGBA", (16,16))
    rows = dedent(GRIDS[name]).strip("\n").splitlines()
    assert len(rows) == 14 and all(len(row) <= 14 for row in rows), name
    for y, row in enumerate(rows, 1):
        for x, token in enumerate(row, 1):
            image.putpixel((x,y), rgba(PALETTE[token]))
    # Finish the dark one-pixel outline on the original grid, without resampling.
    mask = image.getchannel("A")
    for y in range(1,15):
        for x in range(1,15):
            if mask.getpixel((x,y)) and any(not mask.getpixel((x+dx,y+dy)) for dx,dy in ((-1,0),(1,0),(0,-1),(0,1))):
                image.putpixel((x,y), rgba(PALETTE["#"]))
    return image


def placed_bell():
    image = Image.new("RGBA", (16,32))
    draw = ImageDraw.Draw(image)
    def rect(box, fill, outline=None):
        draw.rectangle(box, fill=rgba(PALETTE[fill]), outline=rgba(PALETTE[outline]) if outline else None)
    def line(points, color): draw.line(points, fill=rgba(PALETTE[color]), width=1)
    # Timber frame, brass fitting, patina, and three tiny stars on the lip.
    rect((1,3,3,29), "b", "#")
    rect((12,3,14,29), "e", "#")
    rect((0,1,15,4), "b", "#")
    line([(2,2),(13,2)], "h")
    rect((6,4,9,7), "m", "#")
    draw.polygon([(7,6),(8,6),(8,8),(10,10),(10,12),(11,14),(11,15),(12,16),(12,19),(3,19),(3,16),(4,15),(4,14),(5,12),(5,10),(7,8)], fill=rgba(PALETTE["#"]))
    draw.polygon([(7,9),(8,9),(9,11),(9,13),(10,15),(10,16),(11,17),(11,18),(4,18),(4,17),(5,16),(5,15),(6,13),(6,11)], fill=rgba(PALETTE["m"]))
    line([(7,10),(6,13),(6,15)], "l")
    line([(9,13),(10,16),(11,17)], "b")
    line([(4,18),(11,18)], "b")
    for x in (5,7,9): rect((x,17,x,17), "l")
    rect((7,19,8,22), "b", "#")
    rect((6,22,9,24), "m", "#")
    rect((0,28,15,30), "e", "#")
    line([(2,29),(13,29)], "h")
    line([(2,6),(2,26)], "h")
    line([(13,6),(13,26)], "b")
    return image


def generate(root):
    atlas = Image.new("RGBA", (64,16))
    for slot, name in enumerate(GRIDS):
        sprite = icon(name)
        atlas.paste(sprite, (slot * 16,0))
    atlas.save(root / "assets/shipwreck.png")
    bell = placed_bell()
    bell.save(root / "assets/ship_bell.png")
    # Preview enlargements only; the actual assets above use their native pixel sizes.
    preview = Image.new("RGB", (1080,410), "#f4e2b8")
    draw = ImageDraw.Draw(preview)
    font = ImageFont.truetype("C:/Windows/Fonts/arial.ttf", 21)
    draw.text((28,20), "Письма с затонувшей шхуны · нативная пиксельная графика", font=font, fill="#472f23")
    names = ("Пряжка", "Записи", "Бутылка", "Письмо", "Колокол")
    images = (render_icon("sailor_buckle"), icon("port_record"), icon("sealed_bottle"), icon("captain_letter"), bell)
    for i, (name, source) in enumerate(zip(names,images)):
        size = (source.width * 6, source.height * 6)
        large = source.resize(size, Image.Resampling.NEAREST)
        center = i * 210 + 120
        preview.paste(large, (center - large.width // 2, 210 - large.height // 2), large)
        box = draw.textbbox((0,0),name,font=font)
        draw.text((center - (box[2]-box[0])//2,345),name,font=font,fill="#472f23")
    preview.save(root / "previews/shipwreck.png")
