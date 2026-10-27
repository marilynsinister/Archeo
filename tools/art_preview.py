"""Presentation sheets only. Game assets never pass through this code."""
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont


def font(size):
    for path in ("C:/Windows/Fonts/segoeui.ttf", "/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf"):
        if Path(path).exists():
            return ImageFont.truetype(path, size)
    return ImageFont.load_default()


def make_previews(root, catalog, names, icons):
    out = root / "previews"
    out.mkdir(exist_ok=True)
    sheet = Image.new("RGB", (1120, 2028), "#e8dbbe")
    draw = ImageDraw.Draw(sheet)
    ink = "#281b26"
    draw.text((28, 17), "Археология · иконки артефактов", fill=ink, font=font(28))
    draw.text((29, 58), "Исходники 16 × 16 px   ·   Просмотр ×6 без сглаживания   ·   Справа внизу — размер 1:1", fill="#6c5647", font=font(16))
    headings = ("НАСЛЕДИЕ ДОЛИНЫ", "ГНОМЬИ МЕХАНИЗМЫ", "МОРСКИЕ ТАЙНЫ", "ПОСЛЕ ДОЖДЯ И ГРОЗЫ", "НЕОБЫЧНЫЕ РЕЛИКВИИ", "ПРИРОДНЫЕ СОБЫТИЯ", "ИСТОРИИ ЖИТЕЛЕЙ", "ПЕСКИ КАЛИКО", "НАСЛЕДИЕ ОСТРОВА")
    groups = (range(0,7),range(7,14),range(14,21),range(21,23),range(23,27),range(27,30),range(30,33),range(33,38),range(38,43))
    for row in range(len(groups)):
        y = 104 + row * 212
        draw.text((29, y), headings[row], fill="#6c5647", font=font(14))
        for col,index in enumerate(groups[row]):
            artifact = catalog[index]
            im = icons[index]
            x, top = 28 + col * 152, y + 27
            draw.rounded_rectangle((x, top, x + 140, top + 167), radius=5, fill="#f6ecd5")
            large = im.resize((96, 96), Image.Resampling.NEAREST)
            sheet.paste(large, (x + 22, top + 5), large)
            # A second copy at its actual native size demonstrates its in-game grid.
            sheet.paste(im, (x + 118, top + 144), im)
            label = names["artifact." + artifact["Id"] + ".name"]
            lines, current = [], ""
            for word in label.split():
                candidate = (current + " " + word).strip()
                if draw.textlength(candidate, font=font(12)) > 124 and current:
                    lines.append(current)
                    current = word
                else:
                    current = candidate
            lines.append(current)
            for n, line in enumerate(lines[:3]):
                draw.text((x + 8, top + 104 + n * 16), line, fill=ink, font=font(12))
    sheet.save(out / "artifacts-16x16.png")

    # A compact comparison for review; the old atlas is a local backup only.
    before_path = root / ".work/artifacts-before/artifacts.png"
    if before_path.exists():
        before = Image.open(before_path).convert("RGBA")
        compare = Image.new("RGB", (1120, 310), "#e8dbbe")
        d = ImageDraw.Draw(compare)
        d.text((28, 14), "До / после · те же 16 × 16 пикселей", fill=ink, font=font(24))
        d.text((28, 86), "Было", fill="#6c5647", font=font(18))
        d.text((28, 212), "Стало", fill=ink, font=font(18))
        for i, index in enumerate((0, 2, 3, 6, 9, 12, 19)):
            old = before.crop((index % 8 * 16, index // 8 * 16, index % 8 * 16 + 16, index // 8 * 16 + 16))
            for source, y in ((old, 54), (icons[index], 181)):
                large = source.resize((96, 96), Image.Resampling.NEAREST)
                compare.paste(large, (140 + i * 138, y), large)
        compare.save(out / "artifacts-before-after.png")
