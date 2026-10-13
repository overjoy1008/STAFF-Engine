from PIL import Image, ImageDraw, ImageFont, ImageFilter
from pathlib import Path
import random, math
root=Path(__file__).resolve().parents[1]/'Textures'
root.mkdir(exist_ok=True)
font=ImageFont.truetype('/System/Library/Fonts/Supplemental/Arial Narrow Bold.ttf',64)
labels=['PARABOLA  BOOKS','LEAF  BOOKS','BOTANICA','STAFF  OFFICE','KISSA  CAFE','FLOWER  SHOP','RECORDS','ATELIER','BAKERY','TEA  ROOM','STATION  WALK','PAPER  SHOP','B\nO\nO\nK\nS','SHOP','DRINKS','BOOKS','CAFE','INFINITY METRO']
for label in labels:
    key=label.replace(' ','_').replace('\n','_')
    probe=ImageDraw.Draw(Image.new('RGBA',(1,1)))
    box=probe.multiline_textbbox((0,0),label,font=font,align='center',spacing=8)
    im=Image.new('RGBA',(int(box[2]-box[0]+24),int(box[3]-box[1]+16)),(255,255,255,0))
    ImageDraw.Draw(im).multiline_text((12-box[0],8-box[1]),label,font=font,fill=(255,255,255,255),align='center',spacing=8)
    im.save(root/(key+'.png'))
rng=random.Random(181)
for name in ['Asphalt','Plaster']:
    n=512;im=Image.new('RGB',(n,n));pixels=im.load()
    for y in range(n):
        for x in range(n):
            noise=rng.gauss(0,9 if name=='Asphalt' else 2.8)
            tone=int(max(160,min(255,234+noise)))
            pixels[x,y]=(tone,tone,tone)
    dr=ImageDraw.Draw(im)
    if name=='Asphalt':
        for j in range(1500):
            x,y=rng.randrange(n),rng.randrange(n);v=rng.randint(170,250);dr.point((x,y),fill=(v,v,v))
    else:
        for j in range(24):
            x=rng.randrange(n);y=rng.randrange(n);h=rng.randrange(10,110);dr.line((x,y,x+rng.randrange(-2,3),min(n,y+h)),fill=(220,220,220),width=1)
        im=im.filter(ImageFilter.GaussianBlur(.4))
    im.save(root/(name+'.png'))
