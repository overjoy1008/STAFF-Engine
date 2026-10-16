'use strict';
// All artwork is real SVG geometry. The supplied bitmap is used only by ORIGINAL.
const NS='http://www.w3.org/2000/svg', CX=368,CY=517;
function el(tag,attrs={},parent){const n=document.createElementNS(NS,tag);for(const [k,v]of Object.entries(attrs))n.setAttribute(k,v);if(parent)parent.append(n);return n}
function circle(parent,x,y,r,attrs={}){return el('circle',{cx:x,cy:y,r,fill:'none',stroke:'#151513','stroke-width':.7,...attrs},parent)}
function line(parent,x1,y1,x2,y2,attrs={}){return el('line',{x1,y1,x2,y2,stroke:'#10100e','stroke-width':.9,...attrs},parent)}
function label(parent,x,y,text,attrs={}){const n=el('text',{x,y,fill:'#5e5e5c','font-family':'Arial, sans-serif','font-size':5,...attrs},parent);n.textContent=text;return n}
function polar(r,a){return[CX+r*Math.cos(a*Math.PI/180),CY+r*Math.sin(a*Math.PI/180)]}
function arc(parent,r,a,b,width=1,color='#151513',extra={}){const p=polar(r,a),q=polar(r,b);return el('path',{d:`M${p} A${r} ${r} 0 ${b-a>180?1:0} 1 ${q}`,fill:'none',stroke:color,'stroke-width':width,...extra},parent)}
const outer=document.querySelector('#outer'),dial=document.querySelector('#dial'),arms=document.querySelector('#arms'),hub=document.querySelector('#hub');
const outerMotion=el('g',{},outer);
// Faint concentric registration lines, with the original's interrupted paper bands.
for(const [r,w,c]of [[484,2,'#dddddb'],[477,1.1,'#e1e1df'],[470,2,'#cdcdcb'],[464,.7,'#bcbcba'],[454,1,'#e2e2e0'],[416,16,'#f0f0ee'],[405,1.6,'#bfbfbd'],[397,.8,'#d5d5d3'],[373,1,'#c2c2c0']])circle(outer,CX,CY,r,{'stroke-width':w,stroke:c});
circle(outer,CX,CY,344,{fill:'#d8d8d6',stroke:'none','fill-opacity':.66});
circle(outer,CX,CY,350,{'stroke-width':8,stroke:'#e8e8e6'});
for(let i=0;i<124;i++){const[x,y]=polar(351,i*360/124);circle(outerMotion,x,y,1.13,{fill:'#a6a6a4',stroke:'none','fill-opacity':.68})}
outer.append(outerMotion);
circle(dial,CX,CY,291,{'stroke-width':1.15,stroke:'#696967'});
circle(dial,CX,CY,275,{'stroke-dasharray':'1.2 2.25','stroke-width':1.2,stroke:'#6a6a68'});
circle(dial,CX,CY,258,{fill:'#eeeeec','fill-opacity':.77,stroke:'none'});
const arcGroups=[];
for(const [r,a,b,w,v]of [[265,224,270,3.1,-.27],[258,73,84,3,.3],[219,267,349,2,.62],[218,81,206,1.5,-.38],[212,201,275,1.55,.48],[210,137,175,1.35,-.3],[219,302,416,2.05,.2]]){const g=el('g',{},dial);arc(g,r,a,b,w);arcGroups.push({g,v})}
const textOrbit=el('g',{},dial);
const orbitPath=arc(textOrbit,265,-62,61,.3,'none');orbitPath.id='orbit-type';
const ot=el('text',{'font-family':'Arial, sans-serif','font-size':4.3,'letter-spacing':.17,fill:'#61615f'},textOrbit);
const tp=el('textPath',{href:'#orbit-type'},ot);tp.textContent='ORRERY / THE RECURRING RELATIONSHIP BETWEEN PLANETS AND THEIR ORBITAL PATHS. THE PLANETS SYNCHRONIZE. THE SAME SYSTEM IN ANOTHER MOMENT.';
for(const [x,y,a,count]of [[109,496,0,3],[114,564,-16,8],[335,768,-90,6],[238,737,-62,6],[311,263,79,3],[185,330,47,3]]){const g=el('g',{transform:`translate(${x} ${y}) rotate(${a})`},dial);for(let i=0;i<count;i++)label(g,0,i*8,'PLANET SYNC A.',{'font-size':5})}
// Endpoints and individual planet positions hand-mapped from the reference.
// A planet tuple is: distance along axis, radius, opacity, perpendicular offset.
const axes=[
 {end:[613,706],id:'01',v:1.4,dots:[[.84,15,.22,0],[.85,27,0,-4],[.96,14,.8,0],[.43,16,.11,-4],[.43,6,.95,0],[.48,2.7,.95,0],[.25,2.1,.7,0],[.18,10,.62,0]],rings:[[.85,27]]},
 {end:[434,629],id:'02',v:-1.9,dots:[[.27,9,.16,0],[.5,5,.55,0],[.73,4.5,.7,0]]},
 {end:[428,707],id:'03',v:.82,dots:[[.33,9,.35,0],[.47,4.5,.8,0],[.64,4.2,.65,0],[.84,4.6,.16,0]]},
 {end:[402,662],id:'04',v:1.9,dots:[[.23,9,.26,0],[.49,5.3,.42,0],[.61,4.7,.9,0],[.85,3.2,.85,0]]},
 {end:[348,622],id:'05',v:-1.35,dots:[[.2,3.5,.7,0],[.33,5,.85,0],[.46,3.2,.9,0],[.81,12,.57,0]],rings:[[.8,26],[.8,19]]},
 {end:[322,689],id:'06',v:1.1,dots:[[.3,4.7,.55,0],[.48,6,.7,0],[.71,5.5,.7,0],[.89,3,.95,0]]},
 {end:[225,861],id:'07',v:.48,dots:[[.31,11,.12,0],[.39,12,.25,0],[.5,6.7,.65,0],[.64,10,.4,0],[.86,6.5,.6,0]]},
 {end:[174,905],id:'08',v:-.43,dots:[[.32,15,.28,0],[.35,10,.39,0],[.45,6.5,.65,0],[.58,25,.23,0],[.77,9,.25,0],[.79,4.8,.8,0]],rings:[[.57,34]]},
 {end:[245,636],id:'09',v:1.62,dots:[[.18,3,.8,0],[.57,9,.42,0],[.95,6.2,.7,0]]},
 {end:[175,649],id:'10',v:-.75,dots:[[.27,2.6,.85,0],[.54,6,.47,0],[.77,9,.6,0]]},
 {end:[160,482],id:'11',v:1.13,dots:[[.16,8,.4,0],[.24,4,.7,0],[.68,7,.65,0]]},
 {end:[100,383],id:'12',v:-.43,dots:[[.21,2.4,.7,0],[.33,3,.8,0],[.56,3.2,.6,0],[.76,23,.38,0],[.81,17,.35,0],[.87,15,.25,0]],rings:[[.43,21]]},
 {end:[271,376],id:'13',v:1.27,dots:[[.47,3,.5,0],[.61,4,.2,0],[.76,3.5,.58,0],[.89,10,.13,0]]},
 {end:[437,146],id:'14',v:.28,dots:[[.22,16,.25,0],[.3,7.8,.7,0],[.42,8,.26,0],[.44,5.5,.95,0],[.6,11,.26,0],[.63,3.3,.6,0],[.82,3.7,.95,0]],rings:[[.31,21],[.31,16],[.18,29],[.18,31]]},
 {end:[444,357],id:'15',v:-1.34,dots:[[.29,5.5,.3,0],[.56,11,.85,0],[.73,5,.33,0],[.82,6,.75,0]]},
 {end:[471,335],id:'16',v:.65,dots:[[.48,4,.65,0],[.64,5.6,.5,0],[.71,11,.17,0],[.72,6.2,.75,0]]},
 {end:[476,418],id:'17',v:-1.08,dots:[[.32,5,.4,0],[.64,7,.2,0],[.8,6,.6,0],[.88,5.8,.24,0]]},
 {end:[455,452],id:'18',v:1.57,dots:[[.49,6,.28,0],[.85,5,.65,0]]}
];
const animatedAxes=[];
axes.forEach((axis,i)=>{const g=el('g',{},arms),dx=axis.end[0]-CX,dy=axis.end[1]-CY,len=Math.hypot(dx,dy),nx=-dy/len,ny=dx/len;
 const pt=(f,o=0)=>[CX+dx*f+nx*o,CY+dy*f+ny*o];
 const p=pt(.055);line(g,...p,...axis.end,{'stroke-width':i===7?2.1:i===13?1.7:.9});
 axis.dots.forEach(([f,r,opacity,o])=>{const[x,y]=pt(f,o);circle(g,x,y,r,{fill:'#141412',stroke:'none','fill-opacity':opacity})});
 (axis.rings||[]).forEach(([f,r])=>{const[x,y]=pt(f);circle(g,x,y,r,{'stroke-width':.7,stroke:'#565654'})});
 circle(g,...axis.end,i===13?6.8:i===15?4:2.5,{fill:'#0b0b09',stroke:'none'});
 const tx=axis.end[0]+(dx<0?-12:5),ty=axis.end[1]+(dy>0?5:0);label(g,tx,ty,axis.id,{'font-size':5.3,'letter-spacing':.3});
 if([0,3,6,11,13].includes(i)){const[x,y]=pt(i===13?.62:.58);label(g,x+12,y-7,'ORRERY',{'font-size':5})}
 animatedAxes.push({g,v:axis.v});
});
// Tiny radial annotations, with concentric construction circles at the origin.
for(let i=0;i<25;i++){const a=-126+i*9.2,[x,y]=polar(117,a);label(dial,x,y,String(i+1).padStart(2,'0'),{'font-size':4,fill:'#91918f',transform:`rotate(${a+90} ${x} ${y})`})}
for(const r of [22,58,70,77])circle(hub,CX,CY,r,{'stroke-width':r===58?.7:.6,stroke:r===58?'#363634':'#9a9a98','stroke-opacity':r===58?1:.7});
const centerMotion=el('g',{},hub);arc(centerMotion,58,-90,104,3.1,'#10100e');arc(centerMotion,69,15,96,4,'#ababa9',{'stroke-opacity':.45});
circle(hub,CX,CY,22,{fill:'#eeeeec','fill-opacity':.72,stroke:'#545452','stroke-width':.7});
line(hub,CX-8,CY+6,CX+8,CY-6,{'stroke-width':.35,opacity:.4});
let elapsed=0,last=null,playing=!matchMedia('(prefers-reduced-motion: reduce)').matches,speed=4,comparing=false;
const play=document.querySelector('#play'),status=document.querySelector('#status');
function sync(){document.body.classList.toggle('is-paused',!playing);play.setAttribute('aria-label',playing?'일시정지':'재생');document.querySelector('#play-label').textContent=playing?'PAUSE':'PLAY';document.querySelector('#play-icon').setAttribute('d',playing?'M5 3V13M11 3V13':'M5 3 12 8 5 13Z');status.textContent=comparing?'ORIGINAL REFERENCE':playing?'SYSTEM IN MOTION':'SYSTEM AT REST'}
function toggle(){playing=!playing;sync()}
function render(){animatedAxes.forEach(({g,v})=>g.setAttribute('transform',`rotate(${elapsed*v*.35} ${CX} ${CY})`));arcGroups.forEach(({g,v})=>g.setAttribute('transform',`rotate(${elapsed*v} ${CX} ${CY})`));outerMotion.setAttribute('transform',`rotate(${-elapsed*.16} ${CX} ${CY})`);centerMotion.setAttribute('transform',`rotate(${elapsed*1.35} ${CX} ${CY})`);textOrbit.setAttribute('transform',`rotate(${elapsed*.045} ${CX} ${CY})`);document.querySelector('#time').textContent=elapsed.toFixed(2).padStart(6,'0')}
function frame(now){if(last!==null&&playing&&!comparing&&!document.hidden)elapsed+=Math.min((now-last)/1000,.08)*speed;last=now;render();requestAnimationFrame(frame)}
play.addEventListener('click',toggle);
document.querySelector('#speed').addEventListener('input',e=>{speed=Number(e.target.value);document.querySelector('#speed-value').textContent=speed.toFixed(1)+'×'});
function compare(){comparing=!comparing;document.querySelector('#reference').hidden=!comparing;document.querySelector('#mode-note').hidden=!comparing;document.querySelector('#compare').setAttribute('aria-pressed',String(comparing));sync()}
document.querySelector('#compare').addEventListener('click',compare);
function reset(){elapsed=0;render()}
document.querySelector('#reset').addEventListener('click',reset);
async function fullscreen(){try{if(document.fullscreenElement)await document.exitFullscreen();else if(document.documentElement.requestFullscreen)await document.documentElement.requestFullscreen()}catch{status.textContent='FULLSCREEN UNAVAILABLE'}}
document.querySelector('#fullscreen').addEventListener('click',fullscreen);
if(!document.documentElement.requestFullscreen)document.querySelector('#fullscreen').hidden=true;
document.addEventListener('keydown',e=>{if(document.body.dataset.activeStudy&&document.body.dataset.activeStudy!=='cyclical')return;if(e.target.matches('input,button,textarea')||e.ctrlKey||e.metaKey||e.altKey)return;if(e.code==='Space'){e.preventDefault();toggle()}if(e.key.toLowerCase()==='c')compare();if(e.key.toLowerCase()==='r')reset();if(e.key.toLowerCase()==='f')fullscreen()});
document.addEventListener('visibilitychange',()=>{last=null});
sync();render();requestAnimationFrame(frame);
