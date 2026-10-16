globalThis.createVitalDisplay = (options) => {
  'use strict';
  const section = document.getElementById(options.sectionId);
  const $ = name => document.getElementById(options.prefix + '-' + name);
  const NS = 'http://www.w3.org/2000/svg';
  const MAX = 12;
  const recovery = {align:.12, launchEnd:.30, splitDuration:.82, speed:480, growth:.66};
  // Exactly six radii, with 1 / 2 / 3 / 1 / 2 / 3 slots: twelve hit points.
  const radii = [52,74,96,118,140,162];
  // A larger minimum angular span on inner rings keeps damage readable.
  const minimumSpans = [110,82,60,48,36,28];
  // Fixed composition, not random: [orbit index, start angle, end angle].
  const shapes = [
    [2,-58,20], [4,135,207], [1,-160,-98], [5,-45,14],
    [0,185,284], [3,25,150], [2,104,139], [5,135,169],
    [1,32,63], [4,-53,-26], [2,235,280], [5,230,304]
  ].map(([orbit,start,end])=>{
    const middle=(start+end)/2;
    const span=Math.max(end-start,minimumSpans[orbit]);
    return [orbit,middle-span/2,middle+span/2];
  });
  // One integrated angle and speed per orbit, including recovery boosts.
  const {velocity:orbitVelocity}=globalThis.VitalMotion;
  const guideAngles = [-178,-162,-144,-124,-90,-63,-53,-43,-27,0,12,25,38,54,74,94,106,121,137,154];
  const charts = [];
  const motionPreference = matchMedia('(prefers-reduced-motion: reduce)');
  let hp = MAX, previous = null, speed = 1, visible = false, initialized = false;
  let playing = !motionPreference.matches;
  const clamp = value => Math.max(0,Math.min(1,value));
  const ease = value => 1-Math.pow(1-clamp(value),3);

  function element(tag, attrs, parent) {
    const node = document.createElementNS(NS, tag);
    for (const [key,value] of Object.entries(attrs)) node.setAttribute(key,value);
    parent.append(node); return node;
  }
  function point(r,a) { const t=a*Math.PI/180; return [200+r*Math.cos(t),200+r*Math.sin(t)]; }
  function chart(container, dark) {
    const health=MAX, live=true;
    const figure = document.createElement('figure');
    figure.className = 'vital-chart' + (live ? ' vital-live-chart' : '');
    const svg = element('svg',{viewBox:'0 0 400 400',role:'img','aria-label':`체력 ${health} / ${MAX}`},figure);
    const assembly = element('g',{class:'vital-assembly'},svg);
    if(options.frame)element('circle',{cx:200,cy:200,r:184,fill:'none',stroke:'currentColor','stroke-width':2.8,class:'vital-frame'},assembly);
    const tracks=element('g',{class:'vital-orbit-tracks',fill:'none','stroke-width':.45},assembly);
    radii.forEach((radius,i)=>element('circle',{cx:200,cy:200,r:radius,'data-orbit':i},tracks));
    // One reference-style filled fan, independent of all six health orbits.
    const fanStart=point(28,40),fanOuterStart=point(151,40);
    const fanOuterEnd=point(151,60),fanEnd=point(28,60);
    const sector=element('path',{
      d:`M${fanStart}L${fanOuterStart}A151 151 0 0 1 ${fanOuterEnd}L${fanEnd}A28 28 0 0 0 ${fanStart}Z`,
      class:'vital-sector','aria-hidden':'true'
    },assembly);
    const guides = element('g',{'class':'vital-guides','stroke-width':'.6',fill:'none'},assembly);
    guideAngles.forEach(a => {
      const p=point(28,a),q=point(180,a);
      element('line',{x1:p[0],y1:p[1],x2:q[0],y2:q[1],...(a===0?{class:'vital-accent-guide'}:{})},guides);
    });
    const arcs = shapes.map(([orbit,start,end],i) => {
      const radius=radii[orbit];
      const p=point(radius,start),q=point(radius,end);
      return element('path',{d:`M${p}A${radius} ${radius} 0 0 1 ${q}`,fill:'none',stroke:'currentColor','stroke-width':9.5,'stroke-linecap':'butt',pathLength:1,'stroke-dasharray':'1 1',class:'vital-hp-arc','data-point':i+1,'data-orbit':orbit,'data-start':start,'data-span':end-start},assembly);
    });
    const label = document.createElement('figcaption');
    label.textContent = `${health} / ${MAX}`;
    figure.append(label); container.append(figure);
    const states = shapes.map((_,i)=>({value:i<health?1:0,from:i<health?1:0,target:i<health?1:0,age:1,duration:0}));
    const orbits=radii.map(()=>({angle:0,boost:0,age:1,duration:1.05}));
    const result = {figure,svg,assembly,sector,sectorAngle:0,arcs,states,orbits,label,hp:health,live,motionTime:0,hitAge:1,hitStrength:0};
    charts.push(result);
  }

  options.mounts.forEach(mount => chart(document.getElementById(mount.id),Boolean(mount.dark)));

  function setHP(value) {
    const oldHP=hp;
    hp=Math.max(0,Math.min(MAX,Math.round(Number(value))));
    $('hp').value=hp;
    $('hp-value').textContent=`${hp} / ${MAX}`;
    $('damage').disabled=hp===0;
    $('heal').disabled=hp===MAX;
    section.querySelectorAll('[data-vital-preset]').forEach(button=>button.setAttribute('aria-pressed',String(Number(button.dataset.vitalPreset)===hp)));
    charts.forEach(c=>{
      if(c.live)c.hp=hp;
      c.label.textContent=`${c.hp} / ${MAX}`;
      c.svg.setAttribute('aria-label',`체력 ${c.hp} / ${MAX}, 호 ${c.hp}개`);
      const batchRecovery=initialized&&hp>oldHP&&!motionPreference.matches;
      // An orbit is existing even while its last damaged arc is still fading.
      const occupiedBefore=new Set();
      c.states.forEach((state,i)=>{if(state.target===1||state.value>.00001)occupiedBefore.add(shapes[i][0])});
      const launchOrbits=new Set();
      // Only retire an orbit's recovery when all of its slots are being removed.
      // Unaffected orbits retain their current movement and any in-flight launch.
      if(initialized&&hp!==oldHP)c.orbits.forEach((orbit,index)=>{
        if(!shapes.some((shape,i)=>shape[0]===index&&i<hp)){orbit.batch=null;orbit.boost=0}
      });
      if(batchRecovery){
        const anchors=new Map();
        for(let i=oldHP;i<hp;i++){
          const index=shapes[i][0];
          if(!anchors.has(index))anchors.set(index,shapes[i][1]);
        }
        anchors.forEach((start,index)=>{
          const isNewOrbit=!occupiedBefore.has(index);
          if(isNewOrbit)launchOrbits.add(index);
          const orbit=c.orbits[index];
          orbit.boost=0;
          // New orbits align their leading tips. Existing ones launch from their
          // exact current position, sharing the same clockwise recovery burst.
          const bearing=-90;
          const delta=isNewOrbit?((bearing-start-orbit.angle+180)%360+360)%360-180:0;
          orbit.batch={age:0,startAngle:orbit.angle,targetAngle:orbit.angle+delta,delta,anchor:start};
        });
      }
      c.states.forEach((state,i)=>{
        const target=i<c.hp?1:0;
        if(!initialized){state.value=state.from=state.target=target;return}
        if(target===state.target)return;
        // Retarget from the exact visible length, including interrupted effects.
        state.from=state.value;
        state.target=target;
        state.age=0;
        state.delay=target&&launchOrbits.has(shapes[i][0])?recovery.align:0;
        state.duration=motionPreference.matches?.13:target?recovery.growth:.58;
        c.arcs[i].style.display='';
      });
      if(initialized&&c.live&&hp<oldHP&&!motionPreference.matches){
        c.hitAge=0;
        c.hitStrength=Math.min(7,3.8+(oldHP-hp)*.65);
      }
    });
    initialized=true;
    paint(0);
    $('announcement').textContent=`체력 ${hp} / ${MAX}. 호 ${hp}개 표시.`;
  }
  function paint(dt) {
    charts.forEach(c=>{
      const motionDt=playing?dt*speed:0;
      const motionMidpoint=c.motionTime+motionDt/2;
      c.motionTime+=motionDt;
      c.hitAge+=dt;
      const envelope=Math.pow(Math.max(0,1-c.hitAge/.48),2);
      const kick=c.hitStrength*envelope;
      const x=kick*(Math.cos(c.hitAge*83)*.72+Math.cos(c.hitAge*137)*.28);
      const y=kick*Math.sin(c.hitAge*109)*.7;
      const twist=kick*Math.sin(c.hitAge*67)*.23;
      c.assembly.setAttribute('transform',`translate(${x.toFixed(4)} ${y.toFixed(4)}) rotate(${twist.toFixed(4)} 200 200)`);
      c.figure.dataset.feedback=envelope>0?'damage':'idle';
      // Health changes and recovery boosts never modify the fan's angular rate.
      c.sectorAngle+=dt*(playing?-20*speed:0);
      c.sector.setAttribute('transform',`rotate(${c.sectorAngle.toFixed(4)} 200 200)`);
      c.orbits.forEach((orbit,index)=>{
        const midpoint=orbit.age+dt/2;
        const boost=orbit.boost*Math.pow(Math.max(0,1-midpoint/orbit.duration),2);
        const velocity=playing?orbitVelocity(index,motionMidpoint)*speed:0;
        if(orbit.batch){
          const batch=orbit.batch;
          const oldAge=batch.age;
          batch.age+=dt;
          const {align,launchEnd,splitDuration,speed:launchSpeed}=recovery;
          if(batch.age<=align){
            orbit.angle=batch.startAngle+batch.delta*ease(batch.age/align)+launchSpeed*batch.age;
          }else if(batch.age<=launchEnd){
            orbit.angle=batch.targetAngle+launchSpeed*batch.age;
          }else{
            if(oldAge<launchEnd)orbit.angle=batch.targetAngle+launchSpeed*launchEnd;
            const start=Math.max(oldAge,launchEnd);
            const blend=clamp(((start+batch.age)/2-launchEnd)/splitDuration);
            const smooth=blend*blend*(3-2*blend);
            orbit.angle+=(batch.age-start)*(launchSpeed*(1-smooth)+velocity*smooth);
            if(batch.age>=launchEnd+splitDuration)orbit.batch=null;
          }
        }else orbit.angle+=dt*(velocity+(orbit.boostDirection||1)*boost);
        orbit.age+=dt;
      });
      c.arcs.forEach((arc,i)=>{
        const state=c.states[i];
        state.age+=dt;
        const progress=state.duration?clamp((state.age-(state.delay||0))/state.duration):1;
        state.value=state.from+(state.target-state.from)*ease(progress);
        // Sharing the orbit angle preserves relative spacing during recovery too.
        const orbit=c.orbits[shapes[i][0]];
        arc.setAttribute('transform',`rotate(${orbit.angle.toFixed(4)} 200 200)`);
        arc.setAttribute('stroke-dasharray',`${state.value.toFixed(5)} 1`);
        arc.style.opacity=state.value>=.999?'1':String(Math.min(1,state.value*2));
        arc.style.display=state.value<=.00001&&state.target===0?'none':'';
        arc.dataset.phase=progress<1?(state.target?'healing':'damage'):'idle';
      });
    });
  }
  function toggle() {
    playing=!playing;
    $('play').textContent=playing?'Ⅱ PAUSE':'▷ PLAY';
    $('play').setAttribute('aria-label',playing?'체력 호 애니메이션 일시정지':'체력 호 애니메이션 재생');
    $('play').setAttribute('aria-pressed',String(!playing));
  }
  $('hp').addEventListener('input',event=>setHP(event.target.value));
  section.querySelectorAll('[data-vital-preset]').forEach(button=>button.addEventListener('click',()=>setHP(button.dataset.vitalPreset)));
  $('damage').addEventListener('click',()=>setHP(hp-1));
  $('heal').addEventListener('click',()=>setHP(hp+1));
  $('restore').addEventListener('click',()=>setHP(MAX));
  $('play').addEventListener('click',toggle);
  $('speed').addEventListener('input',event=>{
    speed=Number(event.target.value); $('speed-value').textContent=speed.toFixed(1)+'×';
  });
  document.addEventListener('keydown',event=>{
    if(document.body.dataset.activeStudy!==section.id||event.target.matches('input,button,textarea,a,[role="button"]')||event.ctrlKey||event.metaKey||event.altKey)return;
    if(event.code==='Space'){event.preventDefault();toggle()}
  });
  new IntersectionObserver(entries=>{visible=entries[0].isIntersecting;previous=null}).observe(section);
  document.addEventListener('visibilitychange',()=>{previous=null});
  function frame(now) {
    if(previous!==null&&visible&&!document.hidden)paint(Math.min((now-previous)/1000,.08));
    previous=now;requestAnimationFrame(frame);
  }
  if(!playing){$('play').textContent='▷ PLAY';$('play').setAttribute('aria-label','체력 호 애니메이션 재생');$('play').setAttribute('aria-pressed','true')}
  setHP(options.initialHP ?? MAX);requestAnimationFrame(frame);
  return Object.freeze({setHP,getHP:()=>hp,toggle});
};
