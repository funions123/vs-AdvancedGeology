using System;

namespace AdvancedGeology.WorldGen.ProceduralDeposits;

internal readonly record struct GraphiteVein(double Cs, double Ss, double Sd, double Cd, double Offset, double AlongCenter, double CenterY, double Length, double Height, double Width, double Phase);

internal sealed class VeinGraphitePlan : IAdditionalDepositPlan
{
    private const ulong Salt = 0x4752415048564549UL;
    private readonly int ox, oy, oz;
    private readonly double strike, seed;
    private readonly GraphiteVein[] veins;
    public int HorizontalRadius { get; }
    public int VerticalHalfHeight { get; }
    public int PrincipalCount { get; }
    public int BranchCount { get; }

    private VeinGraphitePlan(in ProceduralDepositInstance i, VeinGraphiteDefinition s, double strike, double seed, GraphiteVein[] veins, int principal)
    { ox=i.CenterX; oy=i.CenterY; oz=i.CenterZ; HorizontalRadius=s.HorizontalRadius; VerticalHalfHeight=s.VerticalHalfHeight; this.strike=strike; this.seed=seed; this.veins=veins; PrincipalCount=principal; BranchCount=veins.Length-principal; }

    public static VeinGraphitePlan Create(in ProceduralDepositInstance i, VeinGraphiteDefinition s)
    {
        var r=new ProceduralDepositRandom(i.FeatureId ^ Salt);
        double strike=r.Range(0,180), seed=r.Range(0,100);
        int n=r.NextInt(s.PrincipalMin,s.PrincipalMax), branches=r.NextInt(s.BranchMin,s.BranchMax);
        var veins=new GraphiteVein[n+branches];
        for(int k=0;k<n;k++) veins[k]=Make(strike+r.Range(-16,16),r.Range(72,89),(k-(n-1)/2.0)*r.Range(7,12)+r.Range(-2,2),r.Range(-7,7),r.Range(-2,2),r.Range(18,29),r.Range(25,35),r.Range(1.6,3.4),seed+k*21);
        for(int k=0;k<branches;k++) veins[n+k]=Make(strike+r.Range(25,45)*(k==0?-1:1),r.Range(68,86),r.Range(-9,9),r.Range(-12,12),r.Range(-4,5),r.Range(10,17),r.Range(17,26),r.Range(1,2),seed+77+k*13);
        return new VeinGraphitePlan(i,s,strike,seed,veins,n);
    }

    private static GraphiteVein Make(double strikeDeg,double dipDeg,double off,double a0,double cy,double len,double h,double w,double p)
    { double sr=strikeDeg*Math.PI/180,dr=dipDeg*Math.PI/180; return new(Math.Cos(sr),Math.Sin(sr),Math.Sin(dr),Math.Cos(dr),off,a0,cy,len,h,w,p); }

    public AdditionalDepositSample Evaluate(int wx,int wy,int wz)
    {
        double x=wx-ox,y=wy-oy,z=wz-oz, r=strike*Math.PI/180, a=x*Math.Cos(r)-z*Math.Sin(r), c=x*Math.Sin(r)+z*Math.Cos(r);
        double warp=1.2*Math.Sin(a*.08+seed)+.7*Math.Cos(c*.11-seed*.35);
        if(a*a/(33*33.0)+c*c/(29*29.0)+(y+2+warp)*(y+2+warp)/(28*28.0)>1) return default;
        bool halo=false,found=false; double best=double.MaxValue,bestSwell=0,bestQ=0;
        foreach(var v in veins){ var h=Sample(x,y,z,v); if(h.inside&&h.ratio<best){found=true;best=h.ratio;bestSwell=h.swell;bestQ=h.q;} if(h.halo)halo=true; }
        double f=Math.Sin(x*.2-z*.17+seed)+.5*Math.Cos(y*.28+x*.06);
        if(found){ double dilation=bestSwell-.35*Math.Abs(bestQ); if(best<.38&&dilation>.8)return new(ProceduralMaterialSlots.Graphite,0,1); if(best<.68)return f>.1?new(ProceduralMaterialSlots.Graphite):new(ProceduralMaterialSlots.Quartz); if(best<.86)return f>.05?new(ProceduralMaterialSlots.Graphite,0,.45):default; return f>.25?new(ProceduralMaterialSlots.Quartz):default; }
        return halo?default:default;
    }

    private static (bool inside,bool halo,double ratio,double q,double swell) Sample(double x,double y,double z,GraphiteVein v)
    { double a=x*v.Cs-z*v.Ss,c=x*v.Ss+z*v.Cs-v.Offset,dy=y-v.CenterY,plane=-dy*v.Cd/Math.Max(.12,v.Sd),bend=Math.Sin(a*.11+y*.06+v.Phase)*1.8+Math.Cos(a*.27+v.Phase*1.2)*.7,relay=1.3*Math.Tanh((a-v.AlongCenter)/3),signed=(c-plane)*v.Sd-bend-relay,u=(a-v.AlongCenter)/v.Length,q=dy/v.Height,wp=.11*Math.Sin(u*4+v.Phase)*Math.Cos(q*3),foot=Math.Sqrt(u*u+q*q)+wp,tap=AdditionalDepositMath.Clamp(1-foot*foot,0,1),swell=.62+.55*Math.Pow(.5+.5*Math.Sin(a*.18+v.Phase),2),half=v.Width*.5*swell*Math.Sqrt(tap),d=Math.Abs(signed); return(foot<1.03&&d<half,foot<1.12&&d<half+1.6*Math.Sqrt(Math.Max(tap,.04)),d/Math.Max(.1,half),q,swell); }
}

internal sealed class FlakeGraphiteSchistPlan : IAdditionalDepositPlan
{
    private const ulong Salt=0x464C414B45475241UL;
    private readonly int ox,oy,oz; private readonly double cs,ss,amp,freq,dip,seed; private readonly Horizon[] horizons; private readonly Shear[] shears;
    public int HorizontalRadius{get;} public int VerticalHalfHeight{get;} public int HorizonCount=>horizons.Length; public int ShearCount=>shears.Length;
    private readonly record struct Horizon(double Level,double Half,double Phase); private readonly record struct Shear(double Ss,double Cs,double Offset,double Width);
    private FlakeGraphiteSchistPlan(in ProceduralDepositInstance i,FlakeGraphiteSchistDefinition s,double strike,double amp,double freq,double dip,double seed,Horizon[] h,Shear[] sh){ox=i.CenterX;oy=i.CenterY;oz=i.CenterZ;HorizontalRadius=s.HorizontalRadius;VerticalHalfHeight=s.VerticalHalfHeight;double r=strike*Math.PI/180;cs=Math.Cos(r);ss=Math.Sin(r);this.amp=amp;this.freq=freq;this.dip=dip;this.seed=seed;horizons=h;shears=sh;}
    public static FlakeGraphiteSchistPlan Create(in ProceduralDepositInstance i,FlakeGraphiteSchistDefinition s){var r=new ProceduralDepositRandom(i.FeatureId^Salt);double strike=r.Range(0,180),amp=r.Range(7,12),freq=r.Range(.075,.105),dip=r.Range(-.045,.045),seed=r.Range(0,100);int n=r.NextInt(s.HorizonMin,s.HorizonMax);var h=new Horizon[n];for(int k=0;k<n;k++)h[k]=new(-10+k*r.Range(8,11)+r.Range(-2,2),r.Range(1.8,3.2),seed+k*19);int sn=r.NextInt(s.ShearMin,s.ShearMax);var sh=new Shear[sn];for(int k=0;k<sn;k++){double a=(strike+r.Range(28,65)*(k%2==0?-1:1))*Math.PI/180;sh[k]=new(Math.Sin(a),Math.Cos(a),r.Range(-16,16),r.Range(2.5,4.5));}return new(i,s,strike,amp,freq,dip,seed,h,sh);}
    public AdditionalDepositSample Evaluate(int wx,int wy,int wz){double x=wx-ox,y=wy-oy,z=wz-oz,a=x*cs-z*ss,c=x*ss+z*cs,warp=1.3*Math.Sin(a*.09+seed)+.8*Math.Cos(c*.12-seed*.4);if(a*a/(31*31.0)+c*c/(27*27.0)+(y+2+warp)*(y+2+warp)/(26*26.0)>1)return default;double sy=y-amp*Math.Sin(c*freq+seed)-1.2*Math.Sin(c*freq*2.1-seed*.3)-dip*a,sh=9;foreach(var s in shears)sh=Math.Min(sh,Math.Abs(x*s.Ss+z*s.Cs-s.Offset)/s.Width);foreach(var h in horizons){double d=Math.Abs(sy-h.Level),hinge=.5+.5*Math.Cos(c*freq+seed),th=h.Half*(.75+.55*hinge);if(d<th){double grade=1-d/th+.45*hinge+.55*AdditionalDepositMath.Clamp(1-sh,0,1);return grade>1.38?new(ProceduralMaterialSlots.Graphite,0,1):grade>.84?new(ProceduralMaterialSlots.Graphite,0,.6):new(ProceduralMaterialSlots.Graphite,0,.3);}}return default;}
}
