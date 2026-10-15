import os,json,subprocess
from pathlib import Path
root=Path(__file__).resolve().parents[3].parent/'STAFF/Manim';os.chdir(root)
os.environ['PATH']=str(root/'.tools/TinyTeX/bin/universal-darwin')+os.pathsep+os.environ['PATH']
from manimlib import Tex,FadeIn,UP
m=Tex('0',template='basic');m.move_to([0,0,0]);animation=FadeIn(m,shift=UP*.25,scale=1.15);animation.begin();frames=[]
for i in range(61):
 animation.interpolate(i/60);frames.append({'alpha':float(m.family_members_with_points()[0].get_fill_opacity()),'y':float(m.get_center()[1]),'scale':float(m.get_height())})
height=frames[-1]['scale']
for f in frames:f['scale']/=height
engine=root.parent.parent/'STAFF Engine';dest=engine/'Assets/StaffTraversal/Resources/TangentMath';dest.mkdir(parents=True,exist_ok=True)
(dest/'motion.json').write_text(json.dumps({'frames':frames,'source':'3b1b/manim manimlib.animation.fading.FadeIn; Tex(0), shift=UP*0.25, scale=1.15; 60 samples; duration 0.45s'}))
expressions={'symbol':r"y=f'(a)(x-a)+f(a)",'prefix':'y=','left':'(x-','right':')+','rightMinus':')-','vertical':'x=','minus':'-','dot':'.',**{str(i):str(i) for i in range(10)}}
for name,formula in expressions.items():
 subprocess.run([str(root/'.venv/bin/python'),str(engine/'Tools/MathTypography/render_latex.py'),'--formula',formula,'--output',str(dest/name)],check=True)
subprocess.run([str(root/'.venv/bin/python'),str(engine/'Tools/MathTypography/render_latex.py'),'--formula',expressions['symbol'],'--output',str(engine/'Assets/StaffTraversal/Mathematics/Tangent')],check=True)

# Preserve actual TeX bounds and baselines for runtime numeric typesetting.
import re
entries=[]
for folder in dest.iterdir():
 if folder.is_dir() and (folder/'symbol.svg').exists():
  x,y,w,h=map(float,re.search(r"viewBox='([^']+)'",(folder/'symbol.svg').read_text()).group(1).split())
  entries.append(dict(key=folder.name,x=x,y=y,width=w,height=h))
(dest/'metrics.json').write_text(json.dumps(dict(entries=entries)))
