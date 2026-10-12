// Altered HLSL port of Anderson Mancini's lensflare-threejs-vanilla (CC0).
// Original glare, drawflare, colored ghosts and hexagonal aperture functions.
#define iTime _FlareOptions.z
#define glareSize _FlareParameters.x
#define flareSize _FlareParameters.y
#define starPoints _FlareParameters.z
#define ghostScale _FlareParameters.w
#define animated false
#define anamorphic (_FlareOptions.y > 0.5)
#define flareShape 1.2
#define flareSpeed 0.0
#define colorGain float3(230.0,205.0,170.0)
    float rand(float n){return frac(sin(n) * 43758.5453123);}

    float noise(float p){
        float fl = floor(p);
        float fc = frac(p);
        return lerp(rand(fl),rand(fl + 1.0), fc);
    }

    float3 hsv2rgb(float3 c)
    {
        float4 k = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
        float3 p = abs(frac(c.xxx + k.xyz) * 6.0 - k.www);
        return c.z * lerp(k.xxx, clamp(p - k.xxx, 0.0, 1.0), c.y);
    }

    float saturate2(float x)
    {
        return clamp(x, 0.,1.);
    }


    float2 rotateUV(float2 uv, float rotation)
    {
        return float2(
            cos(rotation) * uv.x + sin(rotation) * uv.y,
            cos(rotation) * uv.y - sin(rotation) * uv.x
        );
    }

    // Based on https://www.shadertoy.com/view/XtKfRV
    float3 drawflare(float2 p, float intensity, float rnd, float speed, int id)
    {
        float flarehueoffset = (1. / 32.) * float(id) * 0.1;
        float lingrad = distance(((float2)(0.)), p);
        float expgrad = 1. / exp(lingrad * (frac(rnd) * 0.66 + 0.33));
        float3 colgrad = hsv2rgb(float3( frac( (expgrad * 8.) + speed * flareSpeed + flarehueoffset), pow(saturate(1.-abs(expgrad*2.-1.)), 0.45), 20.0 * expgrad * intensity)); //rainbow spectrum effect

        float internalStarPoints;

        if(anamorphic){
            internalStarPoints = 1.0;
        } else{
            internalStarPoints = starPoints;
        }
        
        float blades = length(p * flareShape * sin(internalStarPoints * atan2(p.x, p.y))); //draw 6 blades
        
        float comp = pow(1.-saturate2(blades), ( anamorphic ? 100. : 12.));
        comp += saturate2(expgrad-0.9) * 3.;
        comp = pow(comp * expgrad, 8. + (1.-intensity) * 5.);
        
        if(flareSpeed > 0.0){
            return ((float3)(comp)) * colgrad;
        } else{
            return ((float3)(comp)) * flareSize * 15.;
        }
    }

    float dist(float3 a, float3 b) { return abs(a.x - b.x) + abs(a.y - b.y) + abs(a.z - b.z); }

    float glare(float2 uv, float2 pos, float size)
    {
        float2 main;

        if(animated){
        main = rotateUV(uv-pos, iTime * 0.1);      
        } else{
        main = uv-pos;     
        }
        
        float ang = atan2(main.y, main.x) * (anamorphic ? 1.0 : starPoints);
        float dist = length(main); 
        dist = pow(dist, .9);
        
        float f0 = 1.0/(length(uv-pos)*(1.0/size*16.0)+.2);

        return f0+f0*(sin((ang))*.2 +.3);
    }

    //https://www.shadertoy.com/view/Xd2GR3
    float sdHex(float2 p){
        p = abs(p);
        float2 q = float2(p.x*2.0*0.5773503, p.y + p.x*0.5773503);
        return dot(step(q.xy,q.yx), 1.0-q.yx);
    }

    //fakes x^n for specular effects (k is 0-1)
    float fpow(float x, float k){
        return x > k ? pow((x-k)/(1.0-k),2.0) : 0.0;
    }

    float3 renderhex(float2 uv, float2 p, float s, float3 col){
        uv -= p;
        if (abs(uv.x) < 0.2*s && abs(uv.y) < 0.2*s){
            return lerp(((float3)(0)),lerp(((float3)(0)),col,0.1 + fpow(length(uv/s),0.1)*10.0),smoothstep(0.0,0.1,sdHex(uv*20.0/s)));
        }
        return ((float3)(0));
    }

    float3 LensFlare(float2 uv, float2 pos)
    {
        float2 main = uv-pos;
        float2 uvd = uv*(length(uv));
        
        float ang = atan2(main.x,main.y);
        
        float f0 = .3/(length(uv-pos)*16.0+1.0);
        
        f0 = f0*(sin(noise(sin(ang*3.9-(animated ? iTime : 0.0) * 0.3) * starPoints))*.2 );
        
        float f1 = max(0.01-pow(length(uv+1.2*pos),1.9),.0)*7.0;

        float f2 = max(.9/(10.0+32.0*pow(length(uvd+0.99*pos),2.0)),.0)*0.35;
        float f22 = max(.9/(11.0+32.0*pow(length(uvd+0.85*pos),2.0)),.0)*0.23;
        float f23 = max(.9/(12.0+32.0*pow(length(uvd+0.95*pos),2.0)),.0)*0.6;
        
        float2 uvx = lerp(uv,uvd, 0.1);
        
        float f4 = max(0.01-pow(length(uvx+0.4*pos),2.9),.0)*4.02;
        float f42 = max(0.0-pow(length(uvx+0.45*pos),2.9),.0)*4.1;
        float f43 = max(0.01-pow(length(uvx+0.5*pos),2.9),.0)*4.6;
        
        uvx = lerp(uv,uvd,-.4);
        
        float f5 = max(0.01-pow(length(uvx+0.1*pos),5.5),.0)*2.0;
        float f52 = max(0.01-pow(length(uvx+0.2*pos),5.5),.0)*2.0;
        float f53 = max(0.01-pow(length(uvx+0.1*pos),5.5),.0)*2.0;
        
        uvx = lerp(uv,uvd, 2.1);
        
        float f6 = max(0.01-pow(length(uvx-0.3*pos),1.61),.0)*3.159;
        float f62 = max(0.01-pow(length(uvx-0.325*pos),1.614),.0)*3.14;
        float f63 = max(0.01-pow(length(uvx-0.389*pos),1.623),.0)*3.12;
        
        float3 c = ((float3)(glare(uv,pos, glareSize)));

        float2 prot;

        if(animated){
            prot = rotateUV(uv - pos, (iTime * 0.1));  
        } else if(anamorphic){
            prot = rotateUV(uv - pos, 1.570796);     
        } else {
            prot = uv - pos;
        }

        c += drawflare(prot, (anamorphic ? flareSize * 10. : flareSize), 0.1, iTime, 1);
        
        c.r+=f1+f2+f4+f5+f6; c.g+=f1+f22+f42+f52+f62; c.b+=f1+f23+f43+f53+f63;
        c = c*1.3 * ((float3)(length(uvd)+.09)); // Vignette
        c+=((float3)(f0));
        
        return c;
    }

    float3 cc(float3 color, float factor,float factor2)
    {
        float w = color.x+color.y+color.z;
        return lerp(color,((float3)(w))*factor,w*factor2);
    }    

    float rnd(float2 p)
    {
        float f = frac(sin(dot(p, float2(12.1234, 72.8392) )*45123.2));
        return f;   
    }

    float rnd(float w)
    {
        float f = frac(sin(w)*1000.);
        return f;   
    }

    float regShape(float2 p, int N)
    {
        float f;
        
        float a=atan2(p.x,p.y)+.2;
        float b=6.28319/float(N);
        f=smoothstep(.5,.51, cos(floor(.5+a/b)*b-a)*length(p.xy)* 2.0  -ghostScale);
            
        return f;
    }

    // Based on https://www.shadertoy.com/view/Xlc3D2
    float3 circle(float2 p, float size, float decay, float3 color, float3 color2, float dist, float2 mouse)
    {
        float l = length(p + mouse*(dist*2.))+size/2.;
        float l2 = length(p + mouse*(dist*4.))+size/3.;
        
        float c = max(0.04-pow(length(p + mouse*dist), size*ghostScale), 0.0)*10.;
        float c1 = max(0.001-pow(max(l-0.3, 0.0), 1./40.)+sin(l*20.), 0.0)*3.;
        float c2 =  max(0.09/pow(length(p-mouse*dist/.5)*1., .95), 0.0)/20.;
        float s = max(0.02-pow(regShape(p*5. + mouse*dist*5. + decay, 6) , 1.), 0.0)*1.5;
        
        color = cos(((float3)(colorGain))*16. + dist/8.)*0.5+.5;
        float3 f = c*color;
        f += c1*color;
        f += c2*color;  
        f +=  s*color;
        return f;
    }


float3 StaffLensFlare(float2 screenUV,float2 sunUV) {
 float aspect=_SolarScreen.y/_SolarScreen.x;
 float2 uv=(screenUV-.5)*float2(1,aspect);
 float2 pos=(sunUV-.5)*float2(1,aspect);
 float3 c=LensFlare(uv,pos)*20.0*colorGain/256.0;
 if(_FlareOptions.x>.5){
  c+=renderhex(uv,-pos*.5,ghostScale*1.4,float3(.45,.32,.2));
  c+=renderhex(uv,pos*.5,ghostScale*.5,float3(.25,.35,.45));
  c+=renderhex(uv,pos*.2,ghostScale*1.6,float3(.15,.3,.35));
  c+=renderhex(uv,-pos*2.5,ghostScale*3,float3(.2,.15,.3));
 }
 // Additive intensity bounded to keep the original singular core finite.
 return min(max(c,0),8.0)*_FlareOptions.w*.08;
}
