(() => {
  'use strict';
  globalThis.profileDisplay=createVitalDisplay({
    sectionId:'profile',prefix:'profile',mounts:[{id:'profile-health'}],
    initialHP:9,frame:true
  });
  const NS='http://www.w3.org/2000/svg';
  const svg=document.getElementById('profile-geometry');
  function el(tag,attrs,parent=svg){
    const node=document.createElementNS(NS,tag);
    for(const [key,value] of Object.entries(attrs))node.setAttribute(key,value);
    parent.append(node);return node;
  }
  function triangle(x,y,width,down){
    const height=width*Math.sqrt(3)/2;
    return down?`${x},${y} ${x+width},${y} ${x+width/2},${y+height}`:
      `${x},${y+height} ${x+width/2},${y} ${x+width},${y+height}`;
  }
  // One 60-degree lattice for every diagonal. Alternating triangles nest
  // along their sloped sides instead of occupying separate rectangular cells.
  const chargeCount=8;
  const top=68,smallWidth=68,largeWidth=168,gap=12,strokeWidth=4.5;
  const smallPitch=smallWidth/2+gap,largePitch=largeWidth/2+gap;
  const smallHeight=smallWidth*Math.sqrt(3)/2;
  const largeHeight=largeWidth*Math.sqrt(3)/2;
  const largeStart=(chargeCount-1)*smallPitch+smallWidth+gap-largeWidth/2;
  const gaugeBottom=top+largeHeight,gaugeTop=top+smallHeight+gap;
  const gaugeSkew=(gaugeBottom-gaugeTop)/Math.sqrt(3);
  // End on a downward triangle so the chain nests against the first large one.
  // Continue the first and last downward triangles' right-hand diagonals below.
  const firstDown=chargeCount%2===1;
  const gaugeLeft=(firstDown?smallWidth:smallWidth/2)-(gaugeBottom-top)/Math.sqrt(3);
  const gaugeRight=largeStart-gap;
  const gaugeWidth=gaugeRight-gaugeLeft;
  const viewLeft=gaugeLeft-9;
  const viewRight=largeStart+2*largePitch+largeWidth+14;
  svg.setAttribute('viewBox',`${viewLeft} 50 ${viewRight-viewLeft} 180`);
  const defs=el('defs',{});
  const filter=el('filter',{id:'profile-neon-glow',x:'-60%',y:'-60%',width:'220%',height:'220%'},defs);
  el('feGaussianBlur',{stdDeviation:8},filter);
  const scan=el('pattern',{id:'profile-scanlines',width:5,height:5,patternUnits:'userSpaceOnUse'},defs);
  el('rect',{x:0,y:0,width:5,height:1,fill:'#03100d','fill-opacity':.32},scan);
  if(!matchMedia('(prefers-reduced-motion: reduce)').matches)el('animateTransform',{
    attributeName:'patternTransform',type:'translate',from:'0 0',to:'0 5',dur:'1.6s',repeatCount:'indefinite'
  },scan);
  function inset(points,padding){
    const vertices=points.split(' ').map(p=>p.split(',').map(Number));
    const width=Math.max(...vertices.map(p=>p[0]))-Math.min(...vertices.map(p=>p[0]));
    const scale=1-padding/(width*Math.sqrt(3)/6);
    const center=vertices.reduce((sum,p)=>[sum[0]+p[0]/3,sum[1]+p[1]/3],[0,0]);
    return vertices.map(p=>p.map((v,i)=>center[i]+(v-center[i])*scale).join(',')).join(' ');
  }
  function neonTriangle(group,points,outlineClass,attrs={}){
    // Use the same physical inset on both sizes, rather than a percentage that
    // made the large purple triangles look like a different double border.
    const inner=inset(points,3.1);
    el('polygon',{points,class:'charge-glow',fill:'var(--neon)',filter:'url(#profile-neon-glow)'},group);
    el('polygon',{points,class:'charge-face'},group);
    el('polygon',{points:inner,class:'charge-energy',fill:'var(--neon)'},group);
    el('polygon',{points:inner,class:'charge-scanlines',fill:'url(#profile-scanlines)'},group);
    el('polygon',{points,class:`${outlineClass} charge-border`,fill:'none',stroke:'currentColor','stroke-width':strokeWidth,'stroke-linejoin':'miter','stroke-miterlimit':10,...attrs},group);
  }
  const chargeLayer=el('g',{class:'profile-charges'});
  const lights=Array(chargeCount).fill(false);
  const buttons=[];
  for(let i=0;i<chargeCount;i++){
    const g=el('g',{class:'profile-charge profile-neon',role:'button',tabindex:0,'data-charge':i+1,'aria-pressed':String(lights[i]),style:`--neon:#ff5062;--neon-phase:${-i*.23}s`},chargeLayer);
    const points=triangle(i*smallPitch,top,smallWidth,(i%2===0)===firstDown);
    neonTriangle(g,points,'charge-outline');
    // Pointer gestures paint the row below. Keep synthetic clicks accessible.
    g.addEventListener('click',event=>{if(event.detail===0){lights[i]=!lights[i];syncLights()}});
    g.addEventListener('keydown',event=>{
      if(event.key==='Enter'||event.key===' '){event.preventDefault();lights[i]=!lights[i];syncLights()}
    });
    buttons.push(g);
  }
  const largeLayer=el('g',{class:'profile-large-charges'});
  const largeColors=Array.from({length:3},()=>({color:'#cb90ff',name:'보라색'}));
  const largeLights=Array(3).fill(false),largeButtons=[];
  largeColors.forEach(({color,name},i)=>{
    const group=el('g',{
      class:'profile-large-charge profile-neon',role:'button',tabindex:0,
      'aria-pressed':'false','aria-label':`큰 삼각형 ${i+1} ${name} 불빛 켜기`,
      'data-large-charge':i+1,style:`--neon:${color};--neon-phase:${-i*.7}s`
    },largeLayer);
    neonTriangle(group,triangle(largeStart+i*largePitch,top,largeWidth,i%2===1),'large-outline',{'data-large-triangle':i+1});
    largeButtons.push(group);
    function toggleLarge(){
      largeLights[i]=!largeLights[i];syncLarge();tryConvert();
    }
    group.addEventListener('click',toggleLarge);
    group.addEventListener('keydown',event=>{if(event.key==='Enter'||event.key===' '){event.preventDefault();toggleLarge()}});
  });
  function syncLarge(){
    largeButtons.forEach((group,i)=>{
      group.classList.toggle('is-lit',largeLights[i]);
      group.setAttribute('aria-pressed',String(largeLights[i]));
      group.setAttribute('aria-label',`큰 삼각형 ${i+1} 보라색 불빛 ${largeLights[i]?'끄기':'켜기'}`);
    });
  }
  const gauge=el('g',{class:'profile-gauge',role:'slider',tabindex:0,'aria-label':'에너지','aria-valuemin':0,'aria-valuemax':100,'aria-valuenow':0,style:'--neon:#7f95ff'});
  const gaugeGlow=el('polygon',{class:'profile-gauge-glow',fill:'var(--neon)',filter:'url(#profile-neon-glow)'},gauge);
  const gaugeFace=el('polygon',{class:'profile-gauge-face',fill:'#17211f'},gauge);
  const gaugeFill=el('polygon',{class:'profile-gauge-fill',fill:'var(--neon)'},gauge);
  const gaugeScan=el('polygon',{class:'profile-gauge-scan',fill:'url(#profile-scanlines)'},gauge);
  const gaugeCore=el('polygon',{class:'profile-gauge-core',fill:'none',stroke:'var(--neon)','stroke-width':strokeWidth},gauge);
  const gaugeLayers=[gaugeGlow,gaugeFace,gaugeFill,gaugeScan,gaugeCore];
  el('polygon',{
    points:`${gaugeLeft+gaugeSkew},${gaugeTop} ${gaugeRight+gaugeSkew},${gaugeTop} ${gaugeRight},${gaugeBottom} ${gaugeLeft},${gaugeBottom}`,
    fill:'none',stroke:'currentColor','stroke-width':strokeWidth,'stroke-linejoin':'miter',
    'stroke-miterlimit':10,'data-parallelogram':'gauge'
  },gauge);
  // Draw the complete filled perimeter over the static black track outline.
  gauge.append(gaugeCore);
  function syncLights(){
    buttons.forEach((g,i)=>{
      g.classList.toggle('is-lit',lights[i]);
      g.setAttribute('aria-pressed',String(lights[i]));
      g.setAttribute('aria-label',`작은 삼각형 ${i+1} 불빛 ${lights[i]?'끄기':'켜기'}`);
    });
    const count=lights.filter(Boolean).length;
    document.getElementById('profile-charges').value=count;
    document.getElementById('profile-charges-value').textContent=`${count} / ${chargeCount}`;
    tryConvert();
  }
  let energyValue=0,energyTarget=0,energyVelocity=0,energyFrame=null,energyPrevious=null;
  let conversionQueued=false;
  const reduceMotion=matchMedia('(prefers-reduced-motion: reduce)');
  function canConvert(){
    return lights.every(Boolean)&&energyTarget===100&&energyValue>=99.98&&largeLights.includes(false);
  }
  function tryConvert(){
    if(conversionQueued||!canConvert())return;
    conversionQueued=true;
    // Run after the current render: changing a spring target inside its final
    // frame must not accidentally cancel the next animation.
    queueMicrotask(()=>{
      conversionQueued=false;
      if(!canConvert())return;
      const slot=largeLights.indexOf(false);
      lights.fill(false);syncLights();
      largeLights[slot]=true;syncLarge();
      setEnergy(0);
      if(!reduceMotion.matches)largeButtons[slot].animate([{opacity:.35},{opacity:1}],{duration:360,easing:'ease-out'});
      document.getElementById('profile-announcement').textContent='빨강과 파랑을 소모해 보라색 충전 1개를 획득했습니다.';
    });
  }
  function renderEnergy(level){
    const width=gaugeWidth*level/100;
    const points=`${gaugeLeft+gaugeSkew},${gaugeTop} ${gaugeLeft+gaugeSkew+width},${gaugeTop} ${gaugeLeft+width},${gaugeBottom} ${gaugeLeft},${gaugeBottom}`;
    gaugeLayers.forEach(layer=>{
      layer.setAttribute('points',points);
      layer.style.visibility=level>.0001?'visible':'hidden';
    });
    gauge.dataset.rendered=level.toFixed(4);
    tryConvert();
  }
  function animateEnergy(now){
    const dt=energyPrevious===null?1/60:Math.min((now-energyPrevious)/1000,.05);
    energyPrevious=now;
    // Exact critically damped spring integration keeps position and velocity
    // continuous when the slider changes direction mid-animation.
    const omega=13,offset=energyValue-energyTarget;
    const impulse=energyVelocity+omega*offset;
    const decay=Math.exp(-omega*dt);
    energyValue=Math.max(0,Math.min(100,energyTarget+(offset+impulse*dt)*decay));
    energyVelocity=(energyVelocity-omega*impulse*dt)*decay;
    if(Math.abs(energyValue-energyTarget)<.02&&Math.abs(energyVelocity)<.1){
      energyValue=energyTarget;energyVelocity=0;energyFrame=null;energyPrevious=null;
      renderEnergy(energyValue);return;
    }
    renderEnergy(energyValue);
    energyFrame=requestAnimationFrame(animateEnergy);
  }
  function setEnergy(value){
    energyTarget=Math.max(0,Math.min(100,Number(value)));
    gauge.setAttribute('aria-valuenow',energyTarget);
    document.getElementById('profile-energy').value=energyTarget;
    document.getElementById('profile-energy-value').textContent=energyTarget+'%';
    if(reduceMotion.matches){
      if(energyFrame!==null)cancelAnimationFrame(energyFrame);
      energyFrame=null;energyPrevious=null;energyVelocity=0;energyValue=energyTarget;
      renderEnergy(energyValue);
    }else if(energyFrame===null&&energyValue!==energyTarget){
      energyFrame=requestAnimationFrame(animateEnergy);
    }
  }
  let redGesture=null;
  function redPoint(event){
    const matrix=svg.getScreenCTM();
    return matrix?new DOMPoint(event.clientX,event.clientY).matrixTransform(matrix.inverse()):null;
  }
  function paintRed(event){
    const point=redPoint(event);
    if(!point||!redGesture)return;
    const previous=redGesture.previous||point;
    const steps=Math.max(1,Math.ceil(Math.hypot(point.x-previous.x,point.y-previous.y)/4));
    for(let n=0;n<=steps;n++){
      const sample=new DOMPoint(previous.x+(point.x-previous.x)*n/steps,previous.y+(point.y-previous.y)*n/steps);
      buttons.forEach((button,i)=>{
        if(!redGesture.visited.has(i)&&button.querySelector('.charge-face').isPointInFill(sample)){
          lights[i]=redGesture.value;redGesture.visited.add(i);
        }
      });
    }
    redGesture.previous=point;syncLights();
  }
  chargeLayer.addEventListener('pointerdown',event=>{
    const button=event.target.closest('[data-charge]');
    if(!button||redGesture||!event.isPrimary||event.button!==0)return;
    event.preventDefault();button.focus({preventScroll:true});
    const index=Number(button.dataset.charge)-1;
    redGesture={pointer:event.pointerId,value:!lights[index],visited:new Set(),previous:null};
    chargeLayer.setPointerCapture(event.pointerId);paintRed(event);
  });
  chargeLayer.addEventListener('pointermove',event=>{if(redGesture?.pointer===event.pointerId)paintRed(event)});
  function endRed(event){
    if(redGesture?.pointer!==event.pointerId)return;
    if(event.type==='pointerup')paintRed(event);
    redGesture=null;
    if(chargeLayer.hasPointerCapture(event.pointerId))chargeLayer.releasePointerCapture(event.pointerId);
  }
  ['pointerup','pointercancel','lostpointercapture'].forEach(type=>chargeLayer.addEventListener(type,endRed));
  // Transparent hit surface includes the unfilled area. Pointer capture allows
  // a touch or mouse drag to continue smoothly outside the slanted boundary.
  const gaugeHit=el('polygon',{
    points:`${gaugeLeft+gaugeSkew},${gaugeTop} ${gaugeRight+gaugeSkew},${gaugeTop} ${gaugeRight},${gaugeBottom} ${gaugeLeft},${gaugeBottom}`,
    class:'profile-gauge-hit',fill:'transparent','pointer-events':'all'
  },gauge);
  let gaugePointer=null;
  function energyFromPointer(event){
    const matrix=svg.getScreenCTM();
    if(!matrix)return;
    const point=new DOMPoint(event.clientX,event.clientY).matrixTransform(matrix.inverse());
    const y=Math.max(gaugeTop,Math.min(gaugeBottom,point.y));
    const left=gaugeLeft+(gaugeBottom-y)/(gaugeBottom-gaugeTop)*gaugeSkew;
    setEnergy(Math.round(100*(point.x-left)/gaugeWidth));
  }
  gaugeHit.addEventListener('pointerdown',event=>{
    if(gaugePointer!==null||!event.isPrimary||event.button!==0)return;
    event.preventDefault();gaugePointer=event.pointerId;
    gauge.focus({preventScroll:true});
    gaugeHit.setPointerCapture(event.pointerId);energyFromPointer(event);
  });
  gaugeHit.addEventListener('pointermove',event=>{
    if(event.pointerId===gaugePointer)energyFromPointer(event);
  });
  function endGaugePointer(event){
    if(event.pointerId!==gaugePointer)return;
    if(event.type==='pointerup')energyFromPointer(event);
    gaugePointer=null;
    if(gaugeHit.hasPointerCapture(event.pointerId))gaugeHit.releasePointerCapture(event.pointerId);
  }
  ['pointerup','pointercancel','lostpointercapture'].forEach(type=>gaugeHit.addEventListener(type,endGaugePointer));
  gauge.addEventListener('keydown',event=>{
    const changes={ArrowRight:1,ArrowUp:1,ArrowLeft:-1,ArrowDown:-1,PageUp:10,PageDown:-10};
    if(event.key==='Home'){event.preventDefault();setEnergy(0)}
    else if(event.key==='End'){event.preventDefault();setEnergy(100)}
    else if(event.key in changes){event.preventDefault();setEnergy(energyTarget+changes[event.key])}
  });
  document.getElementById('profile-charges').addEventListener('input',event=>{
    lights.forEach((_,i)=>{lights[i]=i<Number(event.target.value)});syncLights();
  });
  document.getElementById('profile-energy').addEventListener('input',event=>setEnergy(event.target.value));
  syncLarge();syncLights();renderEnergy(0);setEnergy(0);
})();
