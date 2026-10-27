"""Effects and review sheets; the relic icons themselves are hand-authored in unusual_content.py."""
from PIL import Image, ImageDraw, ImageFont
from pixel_art import PALETTE, rgba, render_icon


def effects():
    image = Image.new("RGBA", (32,16))
    draw = ImageDraw.Draw(image)
    def rect(box,fill,outline=None):
        draw.rectangle(box,fill=rgba(PALETTE[fill]),outline=rgba(PALETTE[outline]) if outline else None)
    rect((6,3,9,7),"T","#")
    rect((5,8,9,10),"t","#")
    rect((5,12,8,13),"C","#")
    rect((21,4,26,11),"T","#")
    rect((23,2,24,13),"C","#")
    rect((19,7,28,8),"T","#")
    rect((23,6,24,9),"w")
    # Reapply outer ink on this native grid, so the spectral icon still has a crisp outline.
    mask=image.getchannel("A")
    for y in range(1,15):
        for x in range(1,31):
            if mask.getpixel((x,y)) and any(not mask.getpixel((x+dx,y+dy)) for dx,dy in ((-1,0),(1,0),(0,-1),(0,1))):
                image.putpixel((x,y),rgba(PALETTE["#"]))
    return image


def generate(root):
    effects().save(root / "assets/past_tracks.png")
    preview=Image.new("RGB",(1080,340),"#f4e2b8")
    draw=ImageDraw.Draw(preview)
    font=ImageFont.truetype("C:/Windows/Fonts/arial.ttf",20)
    draw.text((28,20),"Необычные реликвии · исходники 16×16, просмотр без сглаживания",font=font,fill="#472f23")
    for i,(name,label) in enumerate((("past_lens","Лупа прошлого"),("garden_flute","Флейта сада"),("keeper_hourglass","Песочные часы"),("pocket_observatory","Обсерватория"))):
        sprite=render_icon(name)
        large=sprite.resize((144,144),Image.Resampling.NEAREST)
        center=135+i*270
        preview.paste(large,(center-72,95),large)
        box=draw.textbbox((0,0),label,font=font)
        draw.text((center-(box[2]-box[0])//2,275),label,font=font,fill="#472f23")
        preview.paste(sprite,(center+80,230),sprite)
    preview.save(root / "previews/unusual-relics.png")
