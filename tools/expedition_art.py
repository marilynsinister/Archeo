"""Eight native 16px parts and two original 32px expedition keepsakes."""
from PIL import Image,ImageDraw
from pixel_art import PALETTE,rgba,INK
from art_preview import font

def contour(image):
    alpha=image.getchannel("A")
    for y in range(image.height):
        for x in range(image.width):
            if alpha.getpixel((x,y)) and any(not(0<=x+dx<image.width and 0<=y+dy<image.height) or not alpha.getpixel((x+dx,y+dy)) for dx,dy in ((-1,0),(1,0),(0,-1),(0,1))):image.putpixel((x,y),rgba(INK))
    return image

def part(index):
    im=Image.new("RGBA",(16,16));d=ImageDraw.Draw(im)
    def rect(box,c):d.rectangle(box,fill=rgba(PALETTE[c]))
    def line(points,c,width=1):d.line(points,fill=rgba(PALETTE[c]),width=width)
    if index==0:
        rect((4,1,11,14),"m");rect((5,2,10,13),"h")
        for y in (4,7,10):line([(7,y),(10,y)],"b")
        line([(6,3),(6,12)],"w")
    elif index==1:
        rect((2,6,13,9),"x");rect((6,2,9,13),"x");rect((7,3,8,12),"A");rect((3,7,12,7),"a")
        rect((6,6,9,9),"m")
    elif index==2:
        d.ellipse((3,2,12,13),fill=rgba(PALETTE["b"]));d.ellipse((4,3,11,10),fill=rgba(PALETTE["h"]))
        line([(6,4),(6,8)],"l");rect((6,12,9,14),"e")
    elif index in (3,4):
        rect((2,2,13,13),"m" if index==3 else "b");rect((5,5,10,10),".")
        line([(3,3),(12,3)],"h");line([(3,4),(3,11)],"h")
        for x in (3,11):rect((x,11,x+1,12),"l")
    elif index==5:
        d.polygon([(2,6),(4,3),(8,2),(12,4),(13,8),(10,12),(6,13),(3,10)],fill=rgba(PALETTE["C"]))
        d.ellipse((5,5,11,11),fill=rgba(PALETTE["c"]));d.ellipse((7,6,10,9),fill=rgba(PALETTE["w"]))
        rect((4,10,5,11),"b")
    elif index==6:
        line([(4,2),(10,3),(6,6),(11,9),(5,13)],"b",3)
        line([(5,3),(8,4),(7,6),(10,9),(6,12)],"h",2)
        for x,y in ((6,5),(9,8),(6,11)):rect((x-1,y-1,x+1,y+1),"m")
    elif index==7:
        d.ellipse((2,2,13,13),fill=rgba(PALETTE["t"]));d.ellipse((3,3,11,11),fill=rgba(PALETTE["T"]))
        d.ellipse((6,5,10,9),fill=rgba(INK));rect((4,4,5,5),"w")
    else:raise ValueError(index)
    return contour(im)

def reward(name):
    im=Image.new("RGBA",(32,32));d=ImageDraw.Draw(im)
    def rect(box,c):d.rectangle(box,fill=rgba(PALETTE[c]))
    def line(points,c,width=1):d.line(points,fill=rgba(PALETTE[c]),width=width)
    if name=="river_model":
        rect((2,26,29,30),"e");rect((3,27,28,28),"h")
        rect((5,7,8,25),"b");rect((23,4,27,25),"m");rect((24,5,26,23),"h")
        line([(7,8),(25,8)],"x",3);line([(7,9),(24,9)],"A")
        rect((11,14,21,24),"d");rect((12,15,20,23),"t");line([(12,17),(20,17)],"C")
        rect((15,8,17,18),"m");d.ellipse((12,16,20,22),fill=rgba(PALETTE["h"]))
        for y in range(8,24,4):line([(24,y),(26,y)],"e")
        line([(6,26),(6,28)],"s");line([(25,26),(25,28)],"s")
    elif name=="voyage_mobile":
        line([(15,1),(4,7),(27,7),(15,1)],"b",2);line([(5,7),(26,7)],"h")
        for x,y in ((7,19),(16,25),(25,18)):line([(x,8),(x,y)],"m",2)
        d.ellipse((2,15,12,25),fill=rgba(PALETTE["C"]));d.ellipse((5,17,10,22),fill=rgba(PALETTE["c"]))
        d.ellipse((12,24,21,30),fill=rgba(PALETTE["t"]));rect((14,25,16,26),"T")
        d.polygon([(21,15),(26,12),(29,17),(27,23),(22,22)],fill=rgba(PALETTE["G"]))
        line([(23,16),(26,15)],"l");rect((14,10,18,12),"b")
    else:raise ValueError(name)
    return contour(im)

def generate(root):
    atlas=Image.new("RGBA",(128,16))
    for i in range(8):atlas.paste(part(i),(i*16,0))
    atlas.save(root/"assets/expedition_parts.png")
    board=Image.new("RGB",(980,500),"#f4e2b8");d=ImageDraw.Draw(board)
    from expedition_content import RU
    for row,story in enumerate(("river","tide")):
        y=row*245;d.text((24,y+14),RU["exp.title."+story],fill=INK,font=font(25))
        for i in range(4):
            icon=part(row*4+i).resize((96,96),Image.Resampling.NEAREST);board.paste(icon,(30+i*155,y+65),icon)
            d.text((28+i*155,y+174),RU[f"exp.piece.{story}.{i}"],fill=INK,font=font(18))
        name=("river_model","voyage_mobile")[row];native=reward(name);native.save(root/"assets"/f"{name}.png")
        large=native.resize((160,160),Image.Resampling.NEAREST);board.paste(large,(745,y+48),large)
        d.text((696,y+212),RU["exp.reward."+story],fill=INK,font=font(17))
    board.save(root/"previews/riddle-expeditions.png")
