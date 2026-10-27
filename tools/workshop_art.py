"""Native 16px quest icons, 16px animation frames and one 16x32 table."""
from textwrap import dedent

from PIL import Image, ImageDraw

from pixel_art import PALETTE, render_icon, rgba

GRIDS = {
    "blueprint": """
        ..#########
        .#CTTTTTT#T#
        .#TTTTTTT#t#
        .#TTdTTTT###
        .#TdtdddTTt#
        .#TddTdTdTt#
        .#TdddTddTt#
        .#TTdTTdTTt#
        .#TTTTTTTTt#
        .#TddddTTTt#
        .#TTTTddddt#
        .#TddddTTTt#
        .#ttttttttd#
        ..#########
    """,
    "spring": """
        ....#####
        ...#hllhmb#
        ..#hm###mbs#
        ..#mb#.#bbs#
        ...#b#..#b#
        ...#mhmmmb#
        ..#hm###mbs#
        ..#mb#.#bbs#
        ...#b#..#b#
        ...#mhmmmb#
        ..#hm###mbs#
        ..#mb###bbs#
        ...#bbbbb#
        ....#####
    """,
    "lens": """
        ....######
        ...#hlhhmb#
        ..#hTwTTtmb#
        .#hTwTTttmbs#
        .#hTwTttdmbs#
        .#hTTttddmbs#
        .#mTttdddmbs#
        .#mbtdddmbbs#
        ..#mbddmbbs#
        ...#mbbbbs#
        ....######
        ..............
        ..............
        ..............
    """,
    "core": """
        .....####
        ....#hlhmb#
        ...#m####mb#
        ..#mb#GG#bbs#
        ..#mb#Gg#bbs#
        ..#hm#gg#bbs#
        ..#hm#gv#bbs#
        ..#mb#gv#bbs#
        ..#mb####bbs#
        ...#mbbbbbs#
        ....#bbbb#
        .....####
        ..............
        ..............
    """,
    "display_case": """
        ...########
        ..#CTTTTTTt#
        .#CT......td#
        .#Tw......td#
        .#T.......td#
        .#T.......td#
        .#T.......td#
        .#tTTTTTTTdd#
        .#hllllllllb#
        .#mbbbbbbbbs#
        .#bbbbbbbbbs#
        ..#b#....#s#
        ..#b#....#s#
        ...#......#
    """,
    "mole_sleep": """
        ..............
        ..............
        ..............
        ..............
        ....##
        ...#mb#
        ..#mhlmm###
        .#hhlhhmmmb#
        #mhhmbbmmmxb#
        #hmbmhmmb##bs#
        #mbm#sbmbbds#
        .#mbbmbbsss#
        ..#xsssss##
        ...######
    """,
}


def icon(name):
    rows = dedent(GRIDS[name]).strip("\n").splitlines()
    assert len(rows) == 14 and max(map(len, rows)) <= 14, (name, list(map(len, rows)))
    im = Image.new("RGBA", (16, 16))
    for y, row in enumerate(rows, 1):
        for x, token in enumerate(row, 1):
            im.putpixel((x, y), rgba(PALETTE[token]))
    # Every silhouette edge is explicit dark ink, including the little spring curls.
    mask = im.getchannel("A")
    for y in range(1, 15):
        for x in range(1, 15):
            if mask.getpixel((x, y)) and any(not mask.getpixel((x+dx, y+dy)) for dx,dy in ((-1,0),(1,0),(0,-1),(0,1))):
                im.putpixel((x, y), rgba(PALETTE["#"]))
    return im


def generate(root):
    atlas = Image.new("RGBA", (96, 16))
    for slot, name in enumerate(("blueprint", "spring", "lens", "core", "clockwork_mole", "display_case")):
        im = render_icon(name) if name == "clockwork_mole" else icon(name)
        atlas.paste(im, (slot * 16, 0))
    atlas.save(root / "assets/workshop.png")

    case = Image.new("RGBA", (16, 32))
    d = ImageDraw.Draw(case)
    def color(token): return rgba(PALETTE[token])
    # A transparent glass canopy above the normal table's display surface.
    # Vanilla table items begin about 3 native pixels below the top of this
    # 1x2 sprite. Keep the entire 16px artifact above the wooden surface.
    d.line([(0,20),(0,4),(2,2),(13,2),(15,4),(15,20)], fill=color("#"))
    d.line([(1,19),(1,5),(3,3),(12,3),(14,5),(14,19)], fill=color("T"))
    d.line([(2,5),(2,8)], fill=color("w"))
    d.line([(13,5),(13,18)], fill=color("t"))
    d.rectangle((0,20,15,24), fill=color("#"))
    d.rectangle((1,21,14,21), fill=color("h"))
    d.line((2,21,12,21),fill=color("l"))
    d.rectangle((1,22,14,23), fill=color("b"))
    d.line((2,23,13,23),fill=color("e"))
    for x in (3,11):
        d.rectangle((x,24,x+2,30),fill=color("#"))
        d.line((x+1,25,x+1,28),fill=color("b"))
    case.save(root / "assets/display_case.png")

    sheet = Image.new("RGBA", (64,64))
    frames = []
    for row in range(4):
        for frame in range(4):
            im = icon("mole_sleep") if row == 3 else render_icon("clockwork_mole")
            draw = ImageDraw.Draw(im)
            if row == 0 and frame == 2:
                draw.point((11,7),fill=color("#"))  # blink
            if row == 1:
                for foot in ((4,12),(10,12)):
                    draw.rectangle((foot[0],foot[1],foot[0]+1,foot[1]+1), fill=color("#"))
                draw.point((4 if frame % 2 else 10,12),fill=color("A"))
                draw.point((10 if frame % 2 else 4,13),fill=color("a"))
            if row == 2:
                draw.point((13,8),fill=color("A" if frame % 2 else "x"))
                draw.point((11,7),fill=color("l"))
            if row == 3 and frame in (1,2):
                draw.point((8,8),fill=color("h"))
            sheet.paste(im,(frame*16,row*16))
            frames.append(im)
    sheet.save(root / "assets/mole.png")
    out = root / "previews"
    out.mkdir(exist_ok=True)
    preview = Image.new("RGB",(640,200),"#e8dbbe")
    for i in range(6):
        im = atlas.crop((i*16,0,(i+1)*16,16)).resize((80,80),Image.Resampling.NEAREST)
        preview.paste(im,(i*104+12,14),im)
    placed = case.resize((32,64),Image.Resampling.NEAREST)
    preview.paste(placed,(580,118),placed)
    preview.save(out / "workshop.png")
    animation = []
    for frame in frames:
        im = Image.new("RGB",(96,96),"#e8dbbe")
        big = frame.resize((96,96),Image.Resampling.NEAREST)
        im.paste(big,(0,0),big)
        animation.append(im)
    animation[0].save(out / "clockwork-digger.gif",save_all=True,append_images=animation[1:],duration=160,loop=0)
