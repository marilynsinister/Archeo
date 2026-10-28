"""Verify the native grid, opaque ink contour, and exact atlas packaging."""
import json
from pathlib import Path

from PIL import Image

from pixel_art import GRIDS, INK, render_icon, render_lamp, rgba

root = Path(__file__).resolve().parents[1]
catalog = json.loads((root / "assets/artifacts.json").read_text(encoding="utf-8"))
atlas = Image.open(root / "assets/artifacts.png").convert("RGBA")
assert atlas.size == (128, 96), atlas.size
slots = {p["Id"]: p["Sprite"] for p in catalog}
slots.update(journal=21, surveyor=22, sparkle=23)
assert set(slots) == set(GRIDS)
assert len(set(slots.values())) == len(slots), "overlapping atlas assignments"
for name, index in slots.items():
    icon = Image.open(root / "assets/icons" / f"{name}.png").convert("RGBA")
    assert icon.size == (16, 16), (name, icon.size)
    assert icon.tobytes() == render_icon(name).tobytes(), (name, "source mismatch")
    assert set(icon.getchannel("A").tobytes()) == {0, 255}, (name, "antialiased pixels")
    assert all(not icon.getpixel((x, y))[3] for x, y in [(v, 0) for v in range(16)] + [(v, 15) for v in range(16)] + [(0, v) for v in range(16)] + [(15, v) for v in range(16)]), (name, "atlas-edge bleed")
    for y in range(1, 15):
        for x in range(1, 15):
            pixel = icon.getpixel((x, y))
            if pixel[3] and any(icon.getpixel((x + dx, y + dy))[3] == 0 for dx, dy in ((-1, 0), (1, 0), (0, -1), (0, 1))):
                assert pixel == rgba(INK), (name, "broken dark contour", x, y)
    tile = atlas.crop((index % 8 * 16, index // 8 * 16, index % 8 * 16 + 16, index // 8 * 16 + 16))
    assert tile.tobytes() == icon.tobytes(), (name, "atlas slot mismatch")

lamps = Image.open(root / "assets/lamps.png").convert("RGBA")
assert lamps.size == (32, 32)
for slot, name in enumerate(("keeper_lantern", "forest_heart")):
    tile = lamps.crop((slot * 16, 0, slot * 16 + 16, 32))
    assert tile.tobytes() == render_lamp(name).tobytes(), (name, "placed sprite mismatch")
    assert set(tile.getchannel("A").tobytes()) == {0, 255}
    assert any(tile.getpixel((x, 14))[3] and tile.getpixel((x, 15))[3] for x in range(16)), (name, "disconnected stem")
print(f"PASS: {len(slots)} native 16x16 icons; preserved tool slots; exact atlas assignments; binary alpha; complete dark contours; 2 native 16x32 placed lamps.")

workshop = Image.open(root / "assets/workshop.png").convert("RGBA")
mole = Image.open(root / "assets/mole.png").convert("RGBA")
case = Image.open(root / "assets/display_case.png").convert("RGBA")
assert workshop.size == (96,16) and mole.size == (64,64) and case.size == (16,32)
for sheet in (workshop,mole,case):
    assert set(sheet.getchannel("A").tobytes()) == {0,255}, "non-native transparency in workshop art"
for i in range(6):
    tile = workshop.crop((i*16,0,(i+1)*16,16))
    assert tile.getbbox() is not None
    for y in range(1,15):
        for x in range(1,15):
            pixel = tile.getpixel((x,y))
            if pixel[3] and any(not tile.getpixel((x+dx,y+dy))[3] for dx,dy in ((-1,0),(1,0),(0,-1),(0,1))):
                assert pixel == rgba(INK), ("workshop contour",i,x,y)
for row in range(4):
    frames = [mole.crop((i*16,row*16,(i+1)*16,(row+1)*16)).tobytes() for i in range(4)]
    assert len(set(frames)) > 1, ("animation state is static",row)
print("PASS: 6 native workshop icons, 16 native animated companion frames, and the 16x32 display case.")

from gallery_art import furniture
for name in ("gallery_case", "ancient_map", "mechanical_clock", "sea_mosaic"):
    sprite = Image.open(root / "assets" / f"{name}.png").convert("RGBA")
    assert sprite.size == ((16,32) if name == "mechanical_clock" else (32,32)), (name, sprite.size)
    assert sprite.tobytes() == furniture(name).tobytes(), (name, "native furniture source mismatch")
    assert set(sprite.getchannel("A").tobytes()) == {0,255}, (name, "antialiasing")
    for y in range(sprite.height):
        for x in range(sprite.width):
            pixel = sprite.getpixel((x,y))
            if pixel[3] and any(not (0 <= x+dx < sprite.width and 0 <= y+dy < sprite.height) or not sprite.getpixel((x+dx,y+dy))[3] for dx,dy in ((-1,0),(1,0),(0,-1),(0,1))):
                assert pixel == rgba(INK), (name, "broken outer contour", x, y)
print("PASS: native collection case and 3 furniture rewards; source-exact pixels, binary alpha and complete dark contours.")

from frontier_art import reward_sprite
for name in ("caravan_panel", "island_totem"):
    sprite=Image.open(root / "assets" / f"{name}.png").convert("RGBA")
    assert sprite.size==(32,32) and sprite.tobytes()==reward_sprite(name).tobytes(), name
    assert set(sprite.getchannel("A").tobytes())=={0,255}, name
    for y in range(32):
        for x in range(32):
            if sprite.getpixel((x,y))[3] and any(not (0<=x+dx<32 and 0<=y+dy<32) or not sprite.getpixel((x+dx,y+dy))[3] for dx,dy in ((-1,0),(1,0),(0,-1),(0,1))):
                assert sprite.getpixel((x,y))==rgba(INK), (name,"broken contour",x,y)
print("PASS: 2 native frontier collection rewards; exact sources, binary alpha and dark contours.")

from museum_art import NAMES as MUSEUM_NAMES,sprite as museum_sprite
for name in MUSEUM_NAMES:
    sprite=Image.open(root / "assets" / f"{name}.png").convert("RGBA")
    assert sprite.size==(32,32) and sprite.tobytes()==museum_sprite(name).tobytes(),name
    assert set(sprite.getchannel("A").tobytes())=={0,255},name
    for y in range(32):
        for x in range(32):
            if sprite.getpixel((x,y))[3] and any(not(0<=x+dx<32 and 0<=y+dy<32) or not sprite.getpixel((x+dx,y+dy))[3] for dx,dy in ((-1,0),(1,0),(0,-1),(0,1))):
                assert sprite.getpixel((x,y))==rgba(INK),(name,"broken contour",x,y)
print("PASS: 2 native museum cases and exhibition pennant, exact pixels, binary alpha and dark contours.")

from shipwreck_art import GRIDS as SHIP_GRIDS, icon as ship_icon, placed_bell
ship_atlas=Image.open(root / "assets/shipwreck.png").convert("RGBA")
assert ship_atlas.size == (64,16)
for index,name in enumerate(SHIP_GRIDS):
    sprite=ship_atlas.crop((index*16,0,(index+1)*16,16))
    assert sprite.tobytes()==ship_icon(name).tobytes(), (name,"source mismatch")
    assert sprite.size==(16,16) and set(sprite.getchannel("A").tobytes())=={0,255}
    for y in range(1,15):
        for x in range(1,15):
            if sprite.getpixel((x,y))[3] and any(not sprite.getpixel((x+dx,y+dy))[3] for dx,dy in ((-1,0),(1,0),(0,-1),(0,1))):
                assert sprite.getpixel((x,y)) == rgba(INK), (name,"broken contour",x,y)
bell=Image.open(root / "assets/ship_bell.png").convert("RGBA")
assert bell.size==(16,32) and bell.tobytes()==placed_bell().tobytes()
assert set(bell.getchannel("A").tobytes())=={0,255}
for y in range(32):
    for x in range(16):
        if bell.getpixel((x,y))[3] and any(not (0<=x+dx<16 and 0<=y+dy<32) or not bell.getpixel((x+dx,y+dy))[3] for dx,dy in ((-1,0),(1,0),(0,-1),(0,1))):
            assert bell.getpixel((x,y))==rgba(INK), ("placed bell contour",x,y)
print("PASS: 4 native 16x16 story icons and the native 16x32 ship bell, exact sources, binary alpha and dark contours.")

from unusual_art import effects
tracks=Image.open(root / "assets/past_tracks.png").convert("RGBA")
assert tracks.size == (32,16) and tracks.tobytes() == effects().tobytes()
assert set(tracks.getchannel("A").tobytes()) == {0,255}
for index in range(2):
    effect=tracks.crop((index*16,0,index*16+16,16))
    for y in range(1,15):
        for x in range(1,15):
            if effect.getpixel((x,y))[3] and any(not effect.getpixel((x+dx,y+dy))[3] for dx,dy in ((-1,0),(1,0),(0,-1),(0,1))):
                assert effect.getpixel((x,y)) == rgba(INK), ("spectral effect contour",index,x,y)
print("PASS: 2 native spectral effect sprites, binary alpha, source-exact pixels and dark contours.")

from io import BytesIO
from zipfile import ZipFile
previous=root / "dist/Archeo-0.8.0.zip"
if previous.exists():
    with ZipFile(previous) as release:
        for artifact in catalog[:33]:
            name=artifact["Id"]
            old=Image.open(BytesIO(release.read(f"Archeo/assets/icons/{name}.png"))).convert("RGBA")
            current=Image.open(root / "assets/icons" / f"{name}.png").convert("RGBA")
            assert current.tobytes()==old.tobytes(), (name,"changed 0.8 icon pixels")
    print("PASS: all 33 previously released artifact icons retain their exact 0.8 pixels.")

previous=root / "dist/Archeo-1.2.0.zip"
if previous.exists():
    with ZipFile(previous) as release:
        for artifact in catalog:
            name=artifact["Id"]
            old=Image.open(BytesIO(release.read(f"Archeo/assets/icons/{name}.png"))).convert("RGBA")
            current=Image.open(root / "assets/icons" / f"{name}.png").convert("RGBA")
            assert old.tobytes()==current.tobytes(),(name,"changed 1.2 pixels")
        assert release.read("Archeo/assets/artifacts.json")== (root/"assets/artifacts.json").read_bytes(),"changed 1.2 artifact catalog"
    print("PASS: all 43 artifact icons and the complete 1.2 artifact catalog remain unchanged.")

from expedition_art import part as expedition_part,reward as expedition_reward
expedition_atlas=Image.open(root / "assets/expedition_parts.png").convert("RGBA")
assert expedition_atlas.size==(128,16)
expedition_images=[]
for i in range(8):
    tile=expedition_atlas.crop((i*16,0,(i+1)*16,16))
    assert tile.tobytes()==expedition_part(i).tobytes(),("expedition part",i)
    assert all(not tile.getpixel((x,0))[3] and not tile.getpixel((x,15))[3] and not tile.getpixel((0,x))[3] and not tile.getpixel((15,x))[3] for x in range(16))
    expedition_images.append(tile)
for name in ("river_model","voyage_mobile"):
    tile=Image.open(root / "assets" / f"{name}.png").convert("RGBA")
    assert tile.size==(32,32) and tile.tobytes()==expedition_reward(name).tobytes(),name
    expedition_images.append(tile)
for tile in expedition_images:
    assert set(tile.getchannel("A").tobytes())=={0,255}
    for y in range(tile.height):
        for x in range(tile.width):
            if tile.getpixel((x,y))[3] and any(not(0<=x+dx<tile.width and 0<=y+dy<tile.height) or not tile.getpixel((x+dx,y+dy))[3] for dx,dy in ((-1,0),(1,0),(0,-1),(0,1))):
                assert tile.getpixel((x,y))==rgba(INK),("expedition contour",x,y)
print("PASS: 8 native 16x16 expedition parts and 2 native 32x32 rewards, exact sources, binary alpha and dark contours.")
