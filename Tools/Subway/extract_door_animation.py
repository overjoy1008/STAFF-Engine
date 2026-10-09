"""Extract authored GLB translation tracks for the baked FBX scene. No geometry edits."""
import hashlib, json, struct, sys
from pathlib import Path
source=Path(sys.argv[1]); output=Path(sys.argv[2]); raw=source.read_bytes()
assert raw[:4]==b'glTF'
size=struct.unpack_from('<I',raw,12)[0]; doc=json.loads(raw[20:20+size]); blob=raw[28+size:]
def accessor(index):
 a=doc['accessors'][index]; v=doc['bufferViews'][a['bufferView']]; n={'SCALAR':1,'VEC3':3}[a['type']]
 assert a['componentType']==5126 and 'sparse' not in a
 return [struct.unpack_from('<'+'f'*n,blob,v.get('byteOffset',0)+a.get('byteOffset',0)+i*v.get('byteStride',n*4)) for i in range(a['count'])]
def vec(v):return dict(zip(('x','y','z'),v))
nodes=doc['nodes']; parents={child:i for i,n in enumerate(nodes) for child in n.get('children',[])}
def mul(a,b):return [[sum(a[i][k]*b[k][j] for k in range(4)) for j in range(4)] for i in range(4)]
def apply(m,v,w=1):return [sum(m[i][k]*list(v+[w])[k] for k in range(4)) for i in range(3)]
def world(i):
 n=nodes[i]
 if 'matrix' in n:m=[[n['matrix'][j*4+i] for j in range(4)] for i in range(4)]
 else:
  x,y,z,w=n.get('rotation',[0,0,0,1]);sc=n.get('scale',[1,1,1]);t=n.get('translation',[0,0,0])
  rot=[[1-2*y*y-2*z*z,2*x*y-2*z*w,2*x*z+2*y*w],[2*x*y+2*z*w,1-2*x*x-2*z*z,2*y*z-2*x*w],[2*x*z-2*y*w,2*y*z+2*x*w,1-2*x*x-2*y*y]]
  m=[[rot[i][j]*sc[j] for j in range(3)]+[t[i]] for i in range(3)]+[[0,0,0,1]]
 return mul(world(parents[i]),m) if i in parents else m
lo=[float('inf')]*3;hi=[float('-inf')]*3
for i,n in enumerate(nodes):
 if 'mesh' not in n:continue
 matrix=world(i)
 for prim in doc['meshes'][n['mesh']]['primitives']:
  a=doc['accessors'][prim['attributes']['POSITION']]
  import itertools
  for corner in itertools.product(*zip(a['min'],a['max'])):
   pt=apply(matrix,list(corner))
   for k in range(3):lo[k]=min(lo[k],pt[k]);hi[k]=max(hi[k],pt[k])
animation=next(a for a in doc['animations'] if a['name']=='Doors_All_OpenClose');tracks=[]
for channel in animation['channels']:
 assert channel['target']['path']=='translation'
 sampler=animation['samplers'][channel['sampler']];assert sampler.get('interpolation','LINEAR')=='LINEAR'
 values=accessor(sampler['output']);times=[v[0] for v in accessor(sampler['input'])]
 assert times[0]>=0 and times[-1]>=3.99
 tracks.append({'node':nodes[channel['target']['node']]['name'],'times':times,'deltas':[vec(apply(world(parents[channel['target']['node']]),[v[k]-values[0][k] for k in range(3)],0)) for v in values]})
output.write_text(json.dumps({'source':source.name,'sha256':hashlib.sha256(raw).hexdigest(),'clip':animation['name'],'openTime':1.5,'sourceSize':vec([hi[k]-lo[k] for k in range(3)]),'sourceCenter':vec([(hi[k]+lo[k])/2 for k in range(3)]),'tracks':tracks},indent=2)+'\n')
print('Extracted',len(tracks),'authored LINEAR tracks; source bounds', [hi[k]-lo[k] for k in range(3)])
