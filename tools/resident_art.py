"""Presentation sheet only; all in-game artwork is drawn natively at 16x16."""
from PIL import Image, ImageDraw
from art_preview import font
from pixel_art import render_icon


def generate(root):
    preview=Image.new("RGB",(1040,350),"#e8dbbe")
    draw=ImageDraw.Draw(preview)
    draw.text((28,18),"Истории жителей · памятные реликвии 16×16",font=font(24),fill="#281b26")
    entries=(("reader_seal","Пенни · печать читателя","+8 гномьих фрагментов в день"),
             ("garden_locket","Эвелин · медальон цветника","До 60 энергии в день"),
             ("harbor_whistle","Вилли · свисток гавани","Подсветка находки раз в день"))
    for i,(name,label,effect) in enumerate(entries):
        center=175+i*345
        sprite=render_icon(name)
        big=sprite.resize((144,144),Image.Resampling.NEAREST)
        preview.paste(big,(center-72,82),big)
        preview.paste(sprite,(center+80,216),sprite)
        for text,y,size in ((label,265,18),(effect,302,16)):
            text_font=font(size)
            draw.text((center-draw.textlength(text,font=text_font)/2,y),text,font=text_font,fill="#281b26")
    preview.save(root / "previews/resident-stories.png")
