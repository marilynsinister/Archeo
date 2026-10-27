"""Original museum rewards drawn directly at 32x32; enlargements are previews only."""
from PIL import Image,ImageDraw
from gallery_art import furniture
from pixel_art import PALETTE,rgba,INK,render_icon
from art_preview import font

NAMES=("scholar_case","museum_banner","curator_case")
def sprite(name):
    image=furniture("gallery_case").copy() if name.endswith("case") else Image.new("RGBA",(32,32))
    d=ImageDraw.Draw(image)
    def rect(box,c):d.rectangle(box,fill=rgba(PALETTE[c]))
    def line(points,c):d.line(points,fill=rgba(PALETTE[c]))
    if name.endswith("case"):
        colors={"e":"v","b":"g","m":"G","h":"C"} if name=="curator_case" else {"e":"s","b":"b","m":"h","h":"w"}
        original=image.copy()
        for y in range(32):
            for x in range(32):
                for old,new in colors.items():
                    if original.getpixel((x,y))==rgba(PALETTE[old]):image.putpixel((x,y),rgba(PALETTE[new]));break
        # Brass gallery labels, corner fittings and a small carved crest.
        for x in (5,14,23):rect((x,21,x+3,22),"l")
        rect((12,0,19,4),"#");rect((13,1,18,4),"h" if name=="scholar_case" else "G")
        line([(15,1),(15,3),(17,3)],"s")
        for x in (2,28):rect((x,6,x+1,8),"l")
        line([(4,25),(27,25)],"h")
    elif name=="museum_banner":
        rect((3,1,28,3),"#");rect((4,2,27,2),"h")
        d.polygon([(6,4),(25,4),(25,25),(16,30),(6,25)],fill=rgba(PALETTE["d"]),outline=rgba(INK))
        d.polygon([(8,5),(23,5),(23,24),(16,28),(8,24)],fill=rgba(PALETTE["t"]))
        line([(9,6),(22,6),(22,23),(16,26),(9,23),(9,6)],"h")
        # Open book, sun and three points representing the first exhibition.
        rect((11,13,20,19),"#");rect((12,14,15,18),"w");rect((16,14,19,18),"h")
        line([(15,14),(15,19)],"b")
        for x in (12,16,20):rect((x,10,x,10),"l")
        line([(13,22),(18,22)],"l")
    else:raise ValueError(name)
    alpha=image.getchannel("A")
    for y in range(32):
        for x in range(32):
            if alpha.getpixel((x,y)) and any(not (0<=x+dx<32 and 0<=y+dy<32) or not alpha.getpixel((x+dx,y+dy)) for dx,dy in ((-1,0),(1,0),(0,-1),(0,1))):image.putpixel((x,y),rgba(INK))
    return image

def generate(root):
    board=Image.new("RGB",(850,350),"#f4e2b8");d=ImageDraw.Draw(board)
    d.text((24,16),"Живой музей · памятные награды",fill=INK,font=font(26))
    labels=("Первый заказ · витрина друга музея","Первая выставка · музейный вымпел","Три выставки · витрина хранителя")
    for i,name in enumerate(NAMES):
        native=sprite(name);native.save(root/"assets"/f"{name}.png")
        large=native.resize((192,192),Image.Resampling.NEAREST)
        if name.endswith("case"):
            for slot,aid in enumerate(("settler_cup","copper_brooch","herbalist_flask")):
                icon=render_icon(aid).resize((48,48),Image.Resampling.NEAREST);large.paste(icon,(18+slot*54,66),icon)
        board.paste(large,(40+i*275,74),large)
        for n,line in enumerate(labels[i].split(" · ")):d.text((24+i*275,282+n*24),line,fill=INK,font=font(17))
    board.save(root/"previews/living-museum.png")
