"""Two original collection rewards, drawn directly on the native 32px grid."""
from PIL import Image, ImageDraw
from pixel_art import PALETTE, INK, rgba, render_icon
from art_preview import font


def reward_sprite(name):
    image = Image.new("RGBA", (32, 32))
    draw = ImageDraw.Draw(image)
    def rect(box, color, outline=None):
        draw.rectangle(box, fill=rgba(PALETTE[color]), outline=rgba(PALETTE[outline]) if outline else None)
    def line(points, color):
        draw.line(points, fill=rgba(PALETTE[color]))
    if name == "caravan_panel":
        rect((1, 2, 30, 28), "e", "#")
        rect((2, 3, 29, 27), "m", "#")
        rect((4, 5, 27, 24), "h", "#")
        rect((5, 6, 26, 23), "l")
        rect((6, 7, 25, 13), "h")
        draw.ellipse((19, 7, 23, 11), fill=rgba(PALETTE["w"]))
        line([(5, 17), (10, 14), (17, 17), (23, 13), (26, 15)], "m")
        line([(5, 22), (12, 18), (19, 21), (26, 18)], "b")
        # Two laden pack animals and a winding turquoise route.
        for x, y in ((9, 16), (18, 18)):
            rect((x, y, x+4, y+2), "e")
            rect((x+3, y-2, x+4, y), "e")
            line([(x, y+3), (x, y+4)], "s")
            line([(x+3, y+3), (x+3, y+4)], "s")
            rect((x+1, y-1, x+2, y), "r")
        line([(7, 23), (14, 22), (18, 23), (25, 21)], "t")
        for x in (3, 28):
            for y in (4, 26): rect((x, y, x, y), "w")
    elif name == "island_totem":
        # Carved greenstone turtle on a plaited wooden stand.
        rect((7, 27, 24, 30), "e", "#")
        rect((9, 25, 22, 27), "b", "#")
        rect((12, 16, 19, 25), "e", "#")
        for y in range(17, 25, 3): line([(13, y), (18, y+1)], "h")
        draw.ellipse((6, 5, 25, 21), fill=rgba(PALETTE["v"]), outline=rgba(INK))
        draw.ellipse((9, 6, 22, 19), fill=rgba(PALETTE["g"]), outline=rgba(INK))
        rect((13, 9, 18, 15), "G", "v")
        line([(10, 10), (13, 12), (11, 16)], "v")
        line([(21, 10), (18, 12), (20, 16)], "v")
        draw.ellipse((12, 1, 19, 7), fill=rgba(PALETTE["C"]), outline=rgba(INK))
        rect((14, 3, 14, 3), "#"); rect((17, 3, 17, 3), "#")
        for box in ((3, 8, 8, 12), (23, 8, 28, 12), (5, 19, 10, 22), (21, 19, 26, 22)):
            draw.ellipse(box, fill=rgba(PALETTE["t"]), outline=rgba(INK))
        line([(10, 29), (21, 29)], "h")
    else:
        raise ValueError(name)
    mask = image.getchannel("A")
    for y in range(32):
        for x in range(32):
            if mask.getpixel((x, y)) and any(not (0 <= x+dx < 32 and 0 <= y+dy < 32) or not mask.getpixel((x+dx, y+dy)) for dx,dy in ((-1,0),(1,0),(0,-1),(0,1))):
                image.putpixel((x, y), rgba(INK))
    return image


def generate(root):
    for name in ("caravan_panel", "island_totem"):
        reward_sprite(name).save(root / "assets" / f"{name}.png")
    # Enlarged artwork is for review only; game assets above stay on their native grids.
    from frontier_content import ARTIFACTS
    board = Image.new("RGB", (1000, 550), "#f4e2b8")
    draw = ImageDraw.Draw(board)
    for row, heading in enumerate(("ПЕСКИ КАЛИКО", "НАСЛЕДИЕ ОСТРОВА")):
        y = row * 270
        draw.text((24, y+14), heading, fill=INK, font=font(23))
        for col, artifact in enumerate(ARTIFACTS[row*5:row*5+5]):
            sprite = render_icon(artifact[0]).resize((80, 80), Image.Resampling.NEAREST)
            board.paste(sprite, (30+col*150, y+64), sprite)
            words=artifact[7].split(); lines=[]; current=""
            for word in words:
                candidate=(current+" "+word).strip()
                if draw.textlength(candidate, font=font(14))>135 and current:
                    lines.append(current); current=word
                else: current=candidate
            for n, line in enumerate(lines+[current]): draw.text((24+col*150,y+157+n*19),line,fill=INK,font=font(14))
        reward = reward_sprite(("caravan_panel", "island_totem")[row]).resize((128,128),Image.Resampling.NEAREST)
        board.paste(reward,(830,y+60),reward)
        draw.text((800,y+207),"Награда коллекции",fill=INK,font=font(16))
    board.save(root / "previews/frontier-finds.png")
