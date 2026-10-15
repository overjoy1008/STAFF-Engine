"""CPU Python port of TangentEquationUI + TangentContourGraphic.
Uses the game's exact Manim-exported contours, Write frames and easing samples.
Run with ../STAFF/Manim/.venv/bin/python; no Unity or C# renderer is invoked.
"""
from pathlib import Path
import json, hashlib
import numpy as np
from PIL import Image, ImageDraw, ImageChops
import imageio_ffmpeg

ROOT = Path(__file__).resolve().parents[2]
DATA = ROOT / 'StaffTraversal/Resources/TangentMath/contours.json'
D = json.loads(DATA.read_text())
G = {g['key']: g for g in D['glyphs']}
FPS = 60
SIZE = (1280, 360)
SS = 2
# Same example as TangentContourReview: source=(0,2,0), target=(5,-.01,0).
CONTACT, HEIGHT, SLOPE = 5., -.01, (-.01-2)/5
CREATE, SUBSTITUTE, ERASE = .65, .85, .35

def number(x):
    return f'{0 if abs(x)<.005 else x:.2f}'

def smooth(t):
    return D['smooth'][round(np.clip(t,0,1)*(len(D['smooth'])-1))]

def layout(stage):
    specs=[]
    def add(key,id,slot): specs.append((key,id,D['slots'][slot]['gap']))
    def num(value,id,slot):
        text=number(value)
        for i,key in enumerate(text): specs.append((key,id,D['slots'][slot]['gap'] if i==len(text)-1 else .035))
    add('y','y',0);add('=','eq',1)
    if stage<3:
        add('f','sf',2);add("'",'sp',3);add('(','so',4)
        if stage==0: add('a','arg1',5)
        else: num(CONTACT,'arg1',5)
        add(')','sc',6)
    else: num(SLOPE,'arg1',6)
    add('(','left',7);add('x','x',8);add('-','minus',9)
    if stage==0: add('a','arg2',10)
    else: num(CONTACT,'arg2',10)
    add(')','right',11);add('-' if stage>=2 and HEIGHT<=-.005 else '+','sign',12)
    if stage<2:
        add('f','hf',13);add('(','ho',14)
        if stage==0: add('a','arg3',15)
        else: num(CONTACT,'arg3',15)
        add(')','hc',16)
    else: num(abs(HEIGHT),'arg3',16)
    pen=-sum(G[key]['width']+gap for key,_,gap in specs)/2
    atoms=[]
    for key,id,gap in specs:
        g=G[key];center=pen+g['width']/2
        paths=[np.array([[p['x']+center,p['y']] for p in c['points']]) for c in g['contours']]
        atoms.append(dict(key=key,id=id,paths=paths));pen+=g['width']+gap
    return atoms

def area(p): return np.sum(p[:,0]*np.roll(p[:,1],-1)-p[:,1]*np.roll(p[:,0],-1))/2

def morph(aa,bb):
    aa=sorted(aa,key=lambda p:-abs(area(p)));bb=sorted(bb,key=lambda p:-abs(area(p)))
    if area(aa[0])*area(bb[0])<0: bb=[p[::-1] for p in bb]
    ca=np.concatenate(aa).mean(0);cb=np.concatenate(bb).mean(0);left=[];right=[]
    for k in range(max(len(aa),len(bb))):
        a=aa[k] if k<len(aa) else np.tile(bb[k].mean(0)-cb+ca,(128,1))
        b=bb[k] if k<len(bb) else np.tile(aa[k].mean(0)-ca+cb,(128,1))
        if k<len(aa) and k<len(bb):
            ac=a-a.mean(0);bc=b-b.mean(0)
            best=min(range(128),key=lambda shift:np.sum((ac-np.roll(bc,-shift,axis=0))**2))
            b=np.roll(b,-best,axis=0)
        left.append(a);right.append(b)
    return left,right

def blend(a,b):
    result=[]
    for id in dict.fromkeys(x['id'] for x in a+b):
        f=[x for x in a if x['id']==id];t=[x for x in b if x['id']==id]
        aa=[p for x in f for p in x['paths']];bb=[p for x in t for p in x['paths']]
        if not t: result.append((aa,aa,'erase'))
        elif not f: result.append((bb,bb,'birth'))
        elif [x['key'] for x in f]==[x['key'] for x in t]:
            # Stable glyphs only translate, exactly as in the game.
            for p,q in zip(aa,bb): assert np.max(np.abs((p-p.mean(0))-(q-q.mean(0))))<1e-6
            result.append((aa,bb,'move'))
        else:
            aa,bb=morph(aa,bb);result.append((aa,bb,'morph'))
    return result

L=[layout(i) for i in range(4)];B=[blend(L[i],L[i+1]) for i in range(3)]

def plot(paths,fill=1,stroke=0,coverage=1): return paths,fill,stroke,coverage

def frame(phase,t=0):
    if phase=='erase': return [plot(a['paths'],coverage=1-smooth(t/ERASE)) for a in L[3]]
    if phase=='substitute':
        t=np.clip(t/SUBSTITUTE,0,1);i=0 if t<2.8/6.8 else 1 if t<4.8/6.8 else 2
        begin=[0,2.8/6.8,4.8/6.8][i];duration=2.8/6.8 if i==0 else 2/6.8
        local=np.clip((t-begin)/duration,0,1);s=smooth(local);out=[]
        for aa,bb,kind in B[i]:
            if kind=='erase': out.append(plot(aa,coverage=1-smooth(np.clip(local/.325,0,1))))
            elif kind=='birth': out.append(plot(bb,coverage=s))
            else: out.append(plot([a+(b-a)*s for a,b in zip(aa,bb)]))
        return out
    if phase=='write':
        wf=D['writeFrames'][round(np.clip(t/CREATE,0,1)*(len(D['writeFrames'])-1))]['glyphs']
        return [plot(a['paths'],v['fill'],v['stroke'],v['coverage']) for a,v in zip(L[0],wf)]
    if phase=='hidden': return []
    return [plot(a['paths']) for a in L[3 if phase=='numeric' else 0]]

def partial(paths,t):
    if t>=.9999: return paths
    if t<=0: return []
    out=[]
    for p in paths:
        count=t*len(p);n=int(count)
        out.append(np.vstack((p[:n+1],p[n%len(p)]+(p[(n+1)%len(p)]-p[n%len(p)])*(count-n))))
    return out

SX=720*.94/D['width'];SY=120*.61/D['height'];BASE=D['bottom']+D['height']/2

def render(plots):
    image=Image.new('L',(SIZE[0]*SS,SIZE[1]*SS),0)
    for paths,fill,stroke,coverage in plots:
        paths=partial(paths,coverage)
        points=[[(float((x*SX+SIZE[0]/2)*SS),float((SIZE[1]/2-(y-BASE)*SY)*SS)) for x,y in p] for p in paths]
        if fill>0:
            # Even-odd fill, matching the UI's paired scanline crossings (including holes).
            mask=Image.new('1',image.size)
            for p in points:
                if len(p)<3: continue
                contour=Image.new('1',image.size);ImageDraw.Draw(contour).polygon(p,fill=1);mask=ImageChops.logical_xor(mask,contour)
            layer=Image.new('L',image.size,round(255*fill));image=Image.composite(layer,image,mask)
        if stroke>0 or fill>0:
            width=max(stroke*max(.15,SY/120),fill*np.clip(SY/170,.45,.9))
            draw=ImageDraw.Draw(image)
            # C# emits a rectangular quad for each segment, with a .2px half-width minimum.
            half=max(.2,width*.5)*SS
            for p in points:
                for a,b in zip(p,p[1:]):
                    a=np.array(a);b=np.array(b);d=b-a;n=np.linalg.norm(d)
                    if n<.0001: continue
                    v=np.array([-d[1],d[0]])/n*half
                    draw.polygon([tuple(a+v),tuple(b+v),tuple(b-v),tuple(a-v)],fill=255)
    return image.resize(SIZE,Image.Resampling.LANCZOS).convert('RGB')

def timeline(t):
    if t<CREATE: return 'write',t
    if t<1.2: return 'symbolic',0
    if t<2.05: return 'substitute',t-1.2
    if t<3.3: return 'numeric',0
    if t<3.65: return 'erase',t-3.3
    return 'hidden',0

if __name__=='__main__':
    out=ROOT.parent/'docs/images';out.mkdir(parents=True,exist_ok=True)
    writer=imageio_ffmpeg.write_frames(str(out/'tangent-equation.mp4'),SIZE,fps=FPS,codec='libx264',pix_fmt_out='yuv420p',macro_block_size=1,output_params=['-crf','17','-movflags','+faststart']);writer.send(None)
    checkpoints={16:'write',60:'symbolic',90:'substitute',150:'numeric',207:'erase'}
    for i in range(240):
        im=render(frame(*timeline(i/FPS)));writer.send(im.tobytes())
        if i in checkpoints: im.save('/tmp/game-tangent-'+checkpoints[i]+'.png')
    writer.close()
    assert ''.join(a['key'] for a in L[3])=='y=-0.40(x-5.00)-0.01'
    assert not frame('hidden')
    audit={'renderer':'Python port of current Unity equations; Pillow rasterization','contours_sha256':hashlib.sha256(DATA.read_bytes()).hexdigest(),'timings':{'write':CREATE,'substitution':SUBSTITUTE,'uncreate':ERASE},'formula':'y=-0.40(x-5.00)-0.01','background':'#000000','ink':'#FFFFFF','frames':240,'fps':FPS}
    (out/'tangent-equation-render.json').write_text(json.dumps(audit,indent=2)+'\n')
    print('PASS runtime layout, negative sign, rigid glyph translation; rendered 240 Python frames',flush=True)
