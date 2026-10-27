"""Review-only enlargement. Game sprites are already rendered at native size."""
from PIL import Image, ImageDraw
from art_preview import font
from pixel_art import render_icon


def generate(root):
    preview = Image.new("RGB", (1040,340), "#e8dbbe")
    draw = ImageDraw.Draw(preview)
    draw.text((28,18), "Природные события · 16×16 · чёткий тёмный контур", font=font(24), fill="#281b26")
    names = (("frozen_leaf","Отпечаток спящего листа"),("tide_calendar","Календарь четырёх приливов"),("star_glass","Звёздное стекло"))
    for i,(name,label) in enumerate(names):
        center = 175+i*345
        sprite = render_icon(name)
        big = sprite.resize((144,144),Image.Resampling.NEAREST)
        preview.paste(big,(center-72,90),big)
        preview.paste(sprite,(center+80,220),sprite)
        label_font = font(18)
        draw.text((center-draw.textlength(label,font=label_font)/2,270),label,font=label_font,fill="#281b26")
    preview.save(root / "previews/nature-finds.png")
