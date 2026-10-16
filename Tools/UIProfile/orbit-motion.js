(() => {
  'use strict';
  // Authored, deterministic velocity cycles; no random number generator or seed.
  // Values are angular velocities (degrees/second), not orbital positions.
  const patterns = [
    {period:6.8, phase:.03, stops:[90,10,-75,-20,110,90]},
    {period:9.1, phase:.41, stops:[-35,-125,-8,65,18,-35]},
    {period:5.7, phase:.19, stops:[18,135,30,-100,-15,18]},
    {period:11.3,phase:.66, stops:[-100,-10,72,110,-30,-100]},
    {period:8.3, phase:.28, stops:[60,-15,-90,-8,120,60]},
    {period:7.4, phase:.83, stops:[-25,95,12,-140,-45,-25]}
  ];
  function velocity(index,time) {
    const p=patterns[index];
    const cycle=((time/p.period+p.phase)%1+1)%1;
    const segment=cycle*(p.stops.length-1);
    const step=Math.min(p.stops.length-2,Math.floor(segment));
    const t=segment-step;
    const smooth=t*t*(3-2*t);
    return p.stops[step]+(p.stops[step+1]-p.stops[step])*smooth;
  }
  globalThis.VitalMotion=Object.freeze({velocity,patterns});
})();
