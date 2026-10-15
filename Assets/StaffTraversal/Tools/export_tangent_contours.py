"""Export real ManimGL LaTeX contours and timing from the supplied Tangent Studio study.
Contour sampling matches study_tangent.contour_transform (128 arc-length stations).
"""
import os,json
from pathlib import Path
import numpy as np
engine=Path(__file__).resolve().parents[3];root=engine.parent/'STAFF/Manim';os.chdir(root)
os.environ['PATH']=str(root/'.tools/TinyTeX/bin/universal-darwin')+os.pathsep+os.environ['PATH']
from manimlib import Tex,Write,smooth
SAMPLES=128

def area(p):return np.sum(p[:,0]*np.roll(p[:,1],-1)-p[:,1]*np.roll(p[:,0],-1))/2

def paths(mob):
 result=[]
 for points in mob.get_subpaths():
  curves=np.array([points[k:k+3] for k in range(0,len(points)-2,2)])
  u=np.linspace(0,1,18,endpoint=False)[None,:,None]
  dense=((1-u)**2*curves[:,0,None,:]+2*u*(1-u)*curves[:,1,None,:]+u**2*curves[:,2,None,:]).reshape(-1,3)
  dense=np.vstack([dense,dense[0]]);distance=np.r_[0,np.cumsum(np.linalg.norm(np.diff(dense,axis=0),axis=1))]
  stations=np.linspace(0,distance[-1],SAMPLES,endpoint=False)
  result.append(np.column_stack([np.interp(stations,distance,dense[:,j]) for j in range(3)]))
 return sorted(result,key=lambda p:-abs(area(p)))
base=Tex(r"y=f'(a)(x-a)+f(a)",font_size=72,template='basic')
keys=['y','=','f',"'",'(','a',')','(','x','-','a',')','+','f','(','a',')']
assert len(base)==len(keys)
base.shift(-base[0].get_center());baseline=base[0].get_center()[1]
lookup={};entries=[]
for key,m in zip(keys,base):
 if key not in lookup:lookup[key]=m.copy()
numbers=Tex(r'y=0123456789.',font_size=72,template='basic');numbers.shift(base[0].get_center()-numbers[0].get_center())
for key,m in zip('0123456789.',list(numbers)[2:]):lookup[key]=m.copy()
for key,m in lookup.items():
 center=m.get_center();local=m.copy().shift(np.array([-center[0],0,0]));pp=paths(local)
 entries.append(dict(key=key,width=float(m.get_width()),contours=[dict(points=[dict(x=float(p[0]),y=float(p[1])) for p in path]) for path in pp]))
# Keep the original layout advances, including TeX mathematical spacing.
slots=[]
for i,(key,m) in enumerate(zip(keys,base)):
 gap=float(base[i+1].get_left()[0]-m.get_right()[0]) if i+1<len(base) else 0
 slots.append(dict(key=key,gap=gap))
# Actual Write interpolation yields border coverage and fill for each source glyph.
working=base.copy();write=Write(working,lag_ratio=.045,rate_func=smooth);write.begin();frames=[]
for i in range(121):
 write.interpolate(i/120);glyphs=[]
 for k,m in enumerate(working):
  ps=paths(m);length=sum(np.linalg.norm(np.diff(p,axis=0),axis=1).sum() for p in ps)
  full=paths(base[k]);fullLength=sum(np.linalg.norm(np.diff(p,axis=0),axis=1).sum() for p in full)
  glyphs.append(dict(fill=float(m.get_fill_opacity()),stroke=float(m.get_stroke_width()),coverage=float(min(1,length/max(fullLength,1e-8)))))
 frames.append(dict(glyphs=glyphs))
out=engine/'Assets/StaffTraversal/Resources/TangentMath/contours.json'
out.write_text(json.dumps(dict(source='TangentStudioReference.py study_tangent; pinned 3b1b/manim Tex + Write; contour_transform 128-point arc length, winding and phase alignment',glyphs=entries,slots=slots,width=float(base.get_width()),bottom=float(base.get_bottom()[1]),height=float(base.get_height()),writeFrames=frames,smooth=[float(smooth(i/120)) for i in range(121)]),separators=(',',':')))
print('Exported',len(entries),'real TeX glyphs,',len(frames),'actual Manim Write frames:',out)
