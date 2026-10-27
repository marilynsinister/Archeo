"""Native furniture pixel art. Each coordinate is one source pixel; no game asset is resized."""
from PIL import Image, ImageDraw, ImageFont
from pixel_art import PALETTE, rgba, render_icon


def furniture(name):
    image = Image.new("RGBA", (16 if name == "mechanical_clock" else 32, 32))
    draw = ImageDraw.Draw(image)
    def rect(box, fill, outline=None):
        draw.rectangle(box, fill=rgba(PALETTE[fill]), outline=rgba(PALETTE[outline]) if outline else None)
    def line(points, color):
        draw.line(points, fill=rgba(PALETTE[color]), width=1)
    if name == "gallery_case":
        rect((1, 5, 30, 28), "e", "#")
        rect((2, 6, 29, 23), "m", "#")
        rect((3, 7, 28, 22), "d", "#")
        rect((4, 8, 27, 19), "x")
        line([(4, 8), (27, 8)], "c")
        line([(4, 9), (4, 18)], "c")
        line([(5, 10), (7, 8)], "C")
        line([(24, 10), (26, 8)], "c")
        rect((3, 20, 28, 23), "b", "#")
        for x in (5, 14, 23):
            rect((x, 21, x + 3, 22), "h")
            rect((x, 25, x + 3, 26), "e")
        line([(2, 24), (29, 24)], "h")
        for x in (3, 25):
            rect((x, 28, x + 3, 30), "e", "#")
        rect((0, 3, 31, 5), "e", "#")
        line([(2, 4), (29, 4)], "h")
    elif name == "ancient_map":
        rect((1, 1, 30, 30), "e", "#")
        rect((2, 2, 29, 29), "m", "#")
        rect((4, 4, 27, 27), "h", "#")
        rect((5, 5, 26, 26), "l")
        line([(7, 24), (8, 19), (11, 17), (12, 12), (16, 11), (19, 7)], "c")
        line([(8, 24), (9, 19), (12, 18), (13, 12), (17, 12), (20, 7)], "t")
        for x, y in ((8, 8), (10, 7), (7, 12), (22, 17), (23, 20), (20, 21)):
            rect((x, y, x + 1, y + 2), "g")
            rect((x, y, x + 1, y), "v")
        line([(7, 17), (11, 15), (17, 16), (22, 12)], "b")
        rect((16, 15, 18, 17), "r", "#")
        line([(23, 6), (23, 10)], "e")
        line([(21, 8), (25, 8)], "e")
        for x, y in ((3, 3), (28, 3), (3, 28), (28, 28)):
            rect((x, y, x, y), "l")
    elif name == "mechanical_clock":
        rect((3, 2, 12, 29), "e", "#")
        rect((2, 3, 13, 14), "m", "#")
        rect((3, 4, 12, 13), "b", "#")
        rect((4, 5, 11, 12), "h", "#")
        rect((5, 6, 10, 11), "l")
        line([(8, 6), (8, 9), (6, 9)], "#")
        rect((5, 16, 10, 26), "d", "#")
        line([(8, 17), (8, 23)], "h")
        rect((7, 23, 9, 25), "m", "#")
        line([(4, 16), (4, 27)], "b")
        line([(11, 16), (11, 27)], "b")
        rect((2, 28, 13, 30), "b", "#")
        rect((5, 0, 10, 2), "e", "#")
        line([(6, 1), (9, 1)], "h")
    elif name == "sea_mosaic":
        rect((1, 1, 30, 30), "d", "#")
        rect((2, 2, 29, 29), "m", "#")
        rect((4, 4, 27, 27), "d", "#")
        for y in range(5, 27, 3):
            for x in range(5, 27, 3):
                rect((x, y, x + 1, y + 1), "t" if (x + y) % 2 else "c")
        line([(16, 7), (16, 20)], "#")
        for y in range(8, 17):
            line([(17, y), (17 + (y - 8) // 2, y)], "w")
            if y > 10: line([(15, y), (15 - (y - 10) // 2, y)], "C")
        line([(10, 19), (21, 19), (19, 22), (13, 22), (10, 19)], "#")
        line([(12, 20), (19, 20)], "h")
        for x in (7, 15, 23):
            line([(x - 2, 25), (x, 24), (x + 2, 25)], "C")
        for x in range(6, 27, 5):
            rect((x, 2, x + 1, 2), "l")
            rect((x, 29, x + 1, 29), "b")
    else:
        raise ValueError(name)
    return image


def generate(root):
    names = ("gallery_case", "ancient_map", "mechanical_clock", "sea_mosaic")
    images = {name: furniture(name) for name in names}
    for name, image in images.items():
        image.save(root / "assets" / f"{name}.png")
    # This board is only a preview. Native exported PNGs above are never resized.
    preview = Image.new("RGB", (1024, 430), "#f4e2b8")
    draw = ImageDraw.Draw(preview)
    font = ImageFont.truetype("C:/Windows/Fonts/arial.ttf", 20)
    titles = ("Витрина на три экспоната", "Карта старой долины", "Подгорные часы", "Мозаика мореплавателей")
    for index, (name, title) in enumerate(zip(names, titles)):
        tile = images[name]
        if name == "gallery_case":
            # The game uses integer 2x artifact icons inside an integer 4x case.
            sample = tile.resize((128, 128), Image.Resampling.NEAREST)
            for i, artifact in enumerate(("settler_cup", "copper_brooch", "herbalist_flask")):
                sample.alpha_composite(render_icon(artifact).resize((32, 32), Image.Resampling.NEAREST), (12 + i * 36, 44))
            shown = sample.resize((256, 256), Image.Resampling.NEAREST)
        else:
            shown = tile.resize((tile.width * 7, 224), Image.Resampling.NEAREST)
        center = index * 256 + 128
        preview.paste(shown, (center - shown.width // 2, 78), shown)
        # Split long labels intentionally to keep the preview readable.
        words = title.split()
        first = " ".join(words[:-1]); last = words[-1]
        for row, text in enumerate((first, last)):
            box = draw.textbbox((0, 0), text, font=font)
            draw.text((center - (box[2] - box[0]) // 2, 350 + row * 25), text, font=font, fill="#472f23")
    draw.text((28, 20), "Личный музей · нативные пиксельные спрайты", font=font, fill="#472f23")
    preview.save(root / "previews" / "museum.png")
