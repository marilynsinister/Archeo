"""Hand-authored pixel grids. A source cell is exactly one game pixel.

No supersampling, filtering, or resampling is used to create game assets.
The shared ink and upper-left lighting unify the collection; individual
silhouettes and material ramps keep objects legible at their native 16x16.
"""
from textwrap import dedent

from PIL import Image

INK = "#281b26"
PALETTE = {
    ".": "#00000000",
    "#": INK,
    "s": "#593441",     # warm inner shadow
    "e": "#75452f",     # wood / old copper shadow
    "b": "#a45e37",     # copper
    "m": "#d38b48",     # brass
    "h": "#f2be6e",     # warm upper planes
    "l": "#ffdfa1",     # gold glint
    "w": "#fff1cc",     # specular / ivory
    "v": "#315443",     # leaf / patina shadow
    "g": "#638b4d",
    "G": "#a5bf67",
    "d": "#294e66",     # cold inner shadow
    "t": "#3f889c",
    "T": "#78c9cf",
    "c": "#729aaf",     # blue ceramic
    "C": "#b8d6d3",
    "x": "#535164",     # stone / steel
    "a": "#8b8c9b",
    "A": "#c4c3c6",
    "B": "#b19c8a",     # aged ivory
    "u": "#514173",     # crystal / pearl shadow
    "p": "#9165ad",
    "P": "#cf9cd5",
    "r": "#a5474f",     # small enamel accent
    "R": "#e48476",
}

# Rows describe the 14x14 drawing area inside a transparent 16x16 tile.
# Trailing transparent cells may be omitted. The one-pixel margin is part
# of the native tile, not a resize or a crop of a larger drawing.
GRIDS = {
    "settler_cup": """
        ..............
        ...######
        ..#CwwCcc#
        .#Cdxxtccc###
        .#cCCCCcc##h#
        .#cwlCccs#.#m#
        .#cClCctt#.#b#
        .#cCCct#d###b#
        .#cCct#ds#bb#
        .#cttt#ds###
        ..#tddds#
        ..#sddss#
        ...#####
        ..............
    """,
    "copper_brooch": """
        ..............
        .......##
        ......#hl#
        .##...#hm#..##
        .#h#.#hlm#.#h#
        ..#h#hlm#.#hm#
        ..#hlhlm##hm#
        ...#hmlmbhm#
        ....#mvTbm#
        ...#bvbmbs#
        ..#bb#mb##
        ..###.#b#
        .......#
        ..............
    """,
    "keeper_lantern": """
        .....####
        ....#hmmh#
        ....#m##b#
        .....#bb#
        ....######
        ...#hmmmmh#
        ..#b######b#
        ..#m#hwwl#b#
        ..#m#lwwh#b#
        ..#m#hhmh#b#
        ..#b#hllh#e#
        ..#b######e#
        ...#mhhhmb#
        ....######
    """,
    "herbalist_flask": """
        .....####
        ....#bhlb#
        .....#be#
        ....#ATTd#
        ....#CTTd#
        ...#CTTTtd#
        ..#CwTGggtd#
        ..#TwGGggvt#
        ..#TGG#gGvd#
        ..#tGg#gGvd#
        ...#gg#Gvd#
        ...#dgvvdd#
        ....######
        ..............
    """,
    "harvest_seal": """
        .....####
        ....#bhlm#
        ....#mhmb#
        .....#be#
        .....#be#
        ...###bb###
        ..#mhllllhm#
        .#mhlmvGGhmb#
        .#hlmvGgGhb#
        .#mlvGgvGmb#
        .#mmvvGvmbbs#
        ..#bbmbbbes#
        ...########
        ..............
    """,
    "memory_musicbox": """
        ....#######
        ...#mhhhmmm#
        ..#hPppppmb#
        ..#hPupppmb#
        ..#mhpppumb#
        ..#mhpppumb#
        ...###bb###
        ..#hlllhlhm#
        .#mhh##hmbbb#
        .#bmmhwmbebb#
        .#bmlw#bembb#
        .#bbbbebbbes#
        ..##e####e##
        ...##....##
    """,
    "forest_heart": """
        ......##
        .....#lh#
        ....#lwhm#
        ...#lwwhhb#
        ..#lwhGhhmb#
        ..#whGGghmb#
        .#lwhGvgGmbe#
        .#lwhhvgmmbs#
        .#hhhmvmmmbs#
        ..#hmmvmbbs#
        ..#mmbebbbs#
        ...#vbeevs#
        ...#bGGvbs#
        ....######
    """,
    "brass_gear": """
        .....####
        .....#hl#
        ..####hm####
        .#mhhlhhhmmb#
        .#hlm####mbb#
        ##hm#hmmh#bs##
        #hhm#m##b#mbs#
        #hmm#m##b#bbs#
        ##mb#bmmb#bs##
        .#mbb####bbs#
        .#bmbbbbbsss#
        ..####bs####
        .....#bs#
        .....####
    """,
    "rune_tablet": """
        ...########
        ..#AAAaaaaa#
        .#AAaaxaaaa#
        .#AatTaaxaax#
        .#AaTdtaxaaa#
        .#aatTtaatxx#
        .#aaaxaatTxx#
        .#aaaTaaTdax#
        .#aatTaatTxx#
        .#aaxTaxaxxx#
        .#aaaaaxxax##
        ..#axxxaxx#
        ...#######
        ..............
    """,
    "miner_goblet": """
        ..##########
        .#mhlwwllhmb#
        .#hbeebbbmbb#
        .#hlhhmmmbbs#
        ..#lmRrmmbs#
        ..#hmr#mbbs#
        ...#hmmmbe#
        ....#hbbs#
        .....#me#
        .....#mb#
        ....#hmbb#
        ...#mhllmb#
        ..#mbbbbbbs#
        ...########
    """,
    "dwarf_thermos": """
        ...######
        ..#xAAaax#
        ...#aaaa#
        ..#AAaaax#
        ..#AwAaax###
        ..#Aaaaax##m#
        ..#amhmmb#.#b#
        ..#amrrmb#.#b#
        ..#ambbmb###s#
        ..#abbebb#bs#
        ..#AAaaax###
        ..#Aaaaaxs#
        ...#mhmbs#
        ....#####
    """,
    "resonant_lens": """
        ...#####
        ..#hlhhm##
        .#hlTdTTmb#
        #hlTwTTtdmb#
        #hTwTTtddmb#
        #hTTTTtdmbs#
        #mTtttdmbbs#
        .#mdddmbbs#
        ..#mbbbs##
        ...#####mh#
        ........#hb#
        .........#bs#
        ..........#s#
        ...........#
    """,
    "clockwork_mole": """
        ....##
        ...#mh#
        ....#b#
        ...##b###
        ..#mhlhmm#
        .#mhlhhmmb##
        #mhhmbbmbAwb#
        #hmbmhmmb#xbs#
        #mbm#sbmbbds#
        .#mbbmbbbbs#
        .#sbbmbbss#
        ..#Ax##xA#
        ..###..###
        ..............
    """,
    "mountain_heart": """
        ......##
        .....#wT#
        ....#wTPu#
        ...#wPTpuu#
        ..#wPPTppuu#
        ..#PPpTppuu#
        ..#PppTppuu#
        ..#pppTpuuu#
        ...#ppTuuu#
        ...#puTuuu#
        ....#uTuu#
        ....#vTb#
        ...#bmbmb#
        ....#####
    """,
    "clay_whistle": """
        ..............
        .....###
        ....#hlb#
        ..###mbb#
        .#hlhmmbb#..##
        #hhlhmbbbs##m#
        #hlhlmbb#Aw#b#
        #mhhmmbbs#sbb#
        .#mmbb#bs##b#
        ..#bbb#bs#.#b#
        ...#bbsss#..##
        ....#####
        ..............
        ..............
    """,
    "sailor_buckle": """
        ..............
        ..##########
        .#mhlhhmhmbb#
        #mhlmmbbmbbbs#
        #hlm######bbs#
        #hmb#....#bbs#
        #mTt#..###mbs#
        #mvt#..##mbbs#
        #mbb#....#bbs#
        #mbb######bbs#
        #bbmmbbbbsbss#
        .#bbbbbbssss#
        ..##########
        ..............
    """,
    "shell_comb": """
        ....######
        ..##wCCTpp##
        .#wCCTpppuup#
        #wwCCppppuuus#
        #wClTpppPuuss#
        #hllllllhmbbs#
        .#hmbmbmbmbs#
        .#h#b#b#b#bs#
        .#h#b#b#b#bs#
        .#m#b#b#b#bs#
        .#b#b#b#b#ss#
        ..#.#.#.#.##
        ..............
        ..............
    """,
    "tide_compass": """
        .....####
        ....#mhmb#
        ....#m##b#
        ...##hbb##
        ..#hhlllmb#
        .#hlwwwwwmb#
        #hlwwd#wwmbs#
        #hlwwdt#wlbs#
        #hlwxTrwwmbs#
        #mmw#tRwwmbs#
        .#mbwwwwwbs#
        ..#bbmbbb##
        ...######
        ..............
    """,
    "pearl_casket": """
        ...########
        ..#hllhmmmb#
        .#hmTvTvtmbs#
        .#hmvTvTvmbs#
        .#b######bbs#
        .##sAPwAss##
        #hl#PwwwApmb#
        #hm#pwwPApmb#
        #mbs#pPpu#bbs#
        #mbmb####bbbs#
        #bmbehmmbbbbs#
        .#bbe#mh#bss#
        ..##e####s##
        ...##....##
    """,
    "moon_shell": """
        ......####
        ....##wCPP#
        ...#wCPPppu#
        ..#wCPp###pu#
        .#wCPp#CTT#u#
        .#CPp#CTtd#u#
        #CPpp#Tt#d#u#
        #Pppp#Ttd#pu#
        #ppup#ddd#pu#
        .#puup###pu#
        .#Ppuuppuu#
        ..#PPpuuu#
        ...#Ppuu#
        ....####
    """,
    "abyss_beacon": """
        .....####
        ....#mhlb#
        ...#mhllmb#
        ..#x######x#
        ..#a#TwwT#x#
        ..#a#dTTt#x#
        ..#a#tTTd###
        ...#bTdtb#
        ....#TTd#
        ....#ttd#
        ...#Amhmb#
        ..#aAmhmbx#
        ..#axxxxxxs#
        ...########
    """,
    "journal": """
        ...########
        ..#hlllllhb#
        .#bwlwwwwlbs#
        .#bhbbbbbbes#
        .#bhmhmmmmbs#
        .#bhm####mbs#
        .#bhm#TT#mbs#
        .#bhm#td#mbs#
        .#bhm####mbs#
        .#bhmmmmmbbs#
        .#bhbbbbbbbs#
        .#bhlhlllmb##
        ..#########
        ..............
    """,
    "surveyor": """
        ..............
        .....###
        ....#mhl#
        .####mbb###
        #hhlhmhm#TT#
        #mbbbmbb#td#
        .####b#####
        .....#ms#
        ....#mbbs#
        ...#mb#ebs#
        ..#mb##e##s#
        ..#b#.#e#.#s#
        .#b#..#s#..#s#
        .##....#....##
    """,
    "sparkle": """
        ......#
        .....#l#
        .....#l#
        ....#hlh#
        ...#hlwlh#
        .##hlwwwlh##
        #hlwwwwwwwlh#
        .##hlwwwlh##
        ...#hlwlh#
        ....#hlh#
        .....#h#
        .....#h#
        ......#
        ..............
    """,
}


from weather_content import GRIDS as WEATHER_GRIDS
GRIDS.update(WEATHER_GRIDS)
from unusual_content import GRIDS as UNUSUAL_GRIDS
GRIDS.update(UNUSUAL_GRIDS)
from nature_content import GRIDS as NATURE_GRIDS
GRIDS.update(NATURE_GRIDS)
from resident_content import GRIDS as RESIDENT_GRIDS
GRIDS.update(RESIDENT_GRIDS)
from frontier_content import GRIDS as FRONTIER_GRIDS
GRIDS.update(FRONTIER_GRIDS)


def rgba(value):
    value = value.lstrip("#")
    if len(value) == 6:
        value += "ff"
    return tuple(int(value[i:i + 2], 16) for i in (0, 2, 4, 6))


def render_icon(name):
    rows = dedent(GRIDS[name]).strip("\n").splitlines()
    if len(rows) != 14 or any(len(row) > 14 for row in rows):
        raise ValueError(f"{name}: expected 14 source rows of at most 14 cells; got {[len(row) for row in rows]}")
    result = Image.new("RGBA", (16, 16))
    for y, row in enumerate(rows, start=1):
        for x, token in enumerate(row, start=1):
            if token not in PALETTE:
                raise ValueError(f"{name}: unknown palette token {token!r} at {x}, {y}")
            result.putpixel((x, y), rgba(PALETTE[token]))
    if name in UNUSUAL_GRIDS or name in NATURE_GRIDS or name in RESIDENT_GRIDS or name in FRONTIER_GRIDS:
        mask = result.getchannel("A")
        for y in range(1,15):
            for x in range(1,15):
                if mask.getpixel((x,y)) and any(not mask.getpixel((x+dx,y+dy)) for dx,dy in ((-1,0),(1,0),(0,-1),(0,1))):
                    result.putpixel((x,y), rgba(INK))
    return result


def render_lamp(name):
    """Placed objects use the engine's 16x32 format, drawn natively too."""
    result = Image.new("RGBA", (16, 32))
    # The inventory artwork is reused 1:1, never stretched into a world sprite.
    result.paste(render_icon(name), (0, 0))
    rows = {
        "keeper_lantern": """
            .....#hm#
            .....#mb#
            .....#mb#
            .....#be#
            .....#be#
            .....#be#
            .....#be#
            .....#bs#
            .....#bs#
            .....#bs#
            .....#bs#
            ....#hmbb#
            ...#mhmmmb#
            ..#mhllllmb#
            .#mbbbbbbbbs#
            ..##########
        """,
        "forest_heart": """
            .....#bvs#
            ..##.#bvs#.##
            .#Gg##bvs##gG#
            .#vGgvbvvgGgv#
            ..##vGbgGgv##
            ....#vbgv##
            .....#be#
            .....#be#
            .....#be#
            .....#be#
            ....#bbe#
            ...#mbebb#
            ..#mbbeebb#
            .#mbeebebbs#
            ..#ebbsbbs#
            ...#######
        """,
    }[name]
    for y, row in enumerate(dedent(rows).strip("\n").splitlines(), start=15):
        if len(row) > 14:
            raise ValueError(f"{name} lamp row too wide: {row}")
        for x, token in enumerate(row, start=1):
            result.putpixel((x, y), rgba(PALETTE[token]))
    return result
