using System.Diagnostics;
using System.Drawing.Drawing2D;

namespace Solitude;

public sealed partial class GameWindow
{
    private readonly record struct CardPose(Card Card,RectangleF Rect,Position Position,bool Visible,int Layer);
    private sealed record CardFlight(CardPose From,CardPose To,double Start,double Duration,bool Arc,bool Period=false,CardPose? Via=null,IReadOnlyList<(double Time,CardPose Pose)>? Stops=null,bool Future=false,float FromBank=0)
    {
        public double End=>Start+Duration;
        public float Bank(double now)
        {
            if(!Future || now>=End)return 0;
            float t=(float)Math.Clamp((now-Start)/Duration,0,1),ease=t*t*t*(10+t*(-15+6*t));
            return FromBank*(1-ease)+MathF.Sin(t*MathF.PI)*3.5f*Math.Sign(To.Rect.X-From.Rect.X);
        }
        public CardPose Sample(double now)
        {
            if(now>=End)return To;
            if(Stops!=null){if(now>=End)return To;var current=From;foreach(var stop in Stops){if(now<stop.Time)break;current=stop.Pose;}return current;}
            if(Via.HasValue)return now<Start?From:now<End?Via.Value:To;
            float t=(float)Math.Clamp((now-Start)/Duration,0,1),ease=Period?t:1-MathF.Pow(1-t,3);
            if(Future)ease=t*t*t*(10+t*(-15+6*t));
            var r=new RectangleF(From.Rect.X+(To.Rect.X-From.Rect.X)*ease,From.Rect.Y+(To.Rect.Y-From.Rect.Y)*ease,From.Rect.Width+(To.Rect.Width-From.Rect.Width)*ease,From.Rect.Height+(To.Rect.Height-From.Rect.Height)*ease);
            if(Arc)r.Y-=MathF.Sin(MathF.PI*t)*(Future?Math.Min(35,12+Math.Abs(From.Rect.X-To.Rect.X)*.07f):Math.Min(9,Math.Abs(From.Rect.X-To.Rect.X)*.025f));
            Card card=To.Card;
            if(!Period && From.Card.FaceUp!=To.Card.FaceUp)
            {
                float flip=Math.Clamp((t-.15f)/.7f,0,1),width=Math.Max(1,r.Width*Math.Abs(1-2*flip));
                r.X+=(r.Width-width)/2;r.Width=width;card=flip<.5f?From.Card:To.Card;
            }
            return To with{Card=card,Rect=r};
        }
    }
    private readonly Stopwatch motionClock=Stopwatch.StartNew();
    private double? renderMotionTime;
    private double? frameTime;
    private int motionRevision;
    private readonly Dictionary<int,CardFlight> flights=[];
    private Dictionary<int,CardPose> restingCards=[];
    private readonly Dictionary<int,CardPose> dragPoses=[];
    private bool pendingWin;
    private double nextCollect;
    private double MotionNow=>frameTime??renderMotionTime??motionClock.Elapsed.TotalSeconds;
    private bool MotionActive=>flights.Count!=0;
    private Dictionary<int,CardPose> CaptureLayout()
    {
        EnsureLayout();var result=new Dictionary<int,CardPose>(104);
        void Add(Card c,RectangleF r,Position p,bool visible,int layer)=>result[c.Key]=new(c,r,p,visible,layer);
        foreach(var c in Game.State.Stock)Add(c,StockRect,new(PileKind.Stock),false,-1);
        int fan=Math.Max(1,Math.Min(Game.State.WasteFan,Game.State.Waste.Count));
        for(int i=0;i<Game.State.Waste.Count;i++)
        {
            int slot=i-(Game.State.Waste.Count-fan);var r=TopCard(1);r.X+=Math.Max(0,slot)*WasteStep;
            Add(Game.State.Waste[i],r,new(PileKind.Waste,0,i),slot>=0,100+i);
        }
        for(int col=0;col<Game.State.Tableau.Count;col++)for(int i=0;i<Game.State.Tableau[col].Count;i++)
            Add(Game.State.Tableau[col][i],TableauCard(col,i),new(PileKind.Tableau,col,i),!skin.Future || TableauCard(col,i).IntersectsWith(OrbitColumnViewport(col)),1000+col*110+i);
        for(int f=0;f<Game.State.Foundations.Count;f++)
        {
            var pile=Game.State.Foundations[f];var r=Kind==GameKind.FreeCell?CellRect(f,true):Kind==GameKind.Spider?new RectangleF(Table.Left+16+f*(skin.Modern?CardWidth*.42f:24),Table.Bottom-CardHeight-12,CardWidth,CardHeight):TopCard(f+3);
            for(int i=0;i<pile.Count;i++)Add(pile[i],r,new(PileKind.Foundation,f,i),Kind==GameKind.Spider?i==0:i==pile.Count-1,5000+f*20+(Kind==GameKind.Spider?13-i:i));
        }
        if(Kind==GameKind.FreeCell)for(int f=0;f<4;f++)if(Game.State.FreeCells[f].Count>0)Add(Game.State.FreeCells[f][0],CellRect(f,false),new(PileKind.FreeCell,f),true,4000+f);
        return result;
    }
    private void BeginCardMotion(bool deal=false)
    {
        motionRevision++;
        var targets=CaptureLayout();double now=MotionNow;
        var origins=new Dictionary<int,CardPose>(restingCards);
        var banks=skin.Future?flights.ToDictionary(p=>p.Key,p=>p.Value.Bank(now)):null;
        foreach(var (key,flight) in flights)origins[key]=flight.Sample(now);
        foreach(var (key,pose) in dragPoses)origins[key]=pose;
        flights.Clear();
        if(ClassicFreeCell && !deal && !Preferences.FreeCellQuickPlay)BeginFreeCellSequence(origins,targets,now);
        bool animate=skin.Future?OrbitMotionEnabled:skin.Modern?Preferences.Animate:ClassicSpider && Preferences.SpiderAnimateDeal;
        if(animate)
        {
            int dealt=0,home=0;
            foreach(var target in targets.Values.OrderBy(p=>deal?p.Position.Index*12+p.Position.Pile:p.Layer))
            {
                bool dragged=dragPoses.ContainsKey(target.Card.Key);
                var from=origins.GetValueOrDefault(target.Card.Key,target with{Rect=StockRect,Card=target.Card with{FaceUp=false}});
                if(deal){if(target.Position.Kind!=PileKind.Tableau)continue;from=target with{Rect=StockRect,Card=target.Card with{FaceUp=false},Position=new(PileKind.Stock),Visible=true};}
                if(ClassicSpider && !deal && (from.Position.Kind!=PileKind.Stock || target.Position.Kind!=PileKind.Tableau))continue;
                bool moved=Math.Abs(from.Rect.X-target.Rect.X)+Math.Abs(from.Rect.Y-target.Rect.Y)>0.5f,turned=from.Card.FaceUp!=target.Card.FaceUp;
                if((moved || turned) && (from.Visible || target.Visible || deal))
                {
                    double duration=skin.Future?(deal?.43:.32):skin.Modern?Preferences.MotionDuration/1000.0:.08,delay=deal?dealt++*(skin.Future?.016:skin.Modern?.009:.012):0;
                    if(!deal && target.Position.Kind==PileKind.Foundation && !dragged)
                    {delay=Kind==GameKind.FreeCell?home++*.055:Kind==GameKind.Spider?.1:0;}
                    if(!moved)duration*=.8;
                    flights[target.Card.Key]=new(from,target,now+delay,duration,skin.Modern && !dragged && moved,!skin.Modern,Future:skin.Future,FromBank:banks?.GetValueOrDefault(target.Card.Key)??0);
                }
            }
        }
        dragPoses.Clear();restingCards=targets;ScheduleFrames();
    }
    private void BeginFreeCellSequence(Dictionary<int,CardPose> origins,Dictionary<int,CardPose> targets,double now)
    {
        // Classic Quick play skips the visible single-card transfers through free cells.
        // Normal play flashes those transfers; it does not tween a whole floating column.
        var moved=targets.Values.Where(t=>t.Position.Kind==PileKind.Tableau && origins.TryGetValue(t.Card.Key,out var f) && f.Position.Kind==PileKind.Tableau && f.Position.Pile!=t.Position.Pile).OrderBy(t=>t.Position.Index).ToArray();
        if(moved.Length<2 || moved.Select(t=>t.Position.Pile).Distinct().Count()!=1 || moved.Select(t=>origins[t.Card.Key].Position.Pile).Distinct().Count()!=1)return;
        int source=moved.Select(t=>origins[t.Card.Key].Position.Pile).First(),destination=moved[0].Position.Pile;
        var piles=new Dictionary<(PileKind Kind,int Pile),List<CardPose>>();
        for(int i=0;i<8;i++)piles[(PileKind.Tableau,i)]=origins.Values.Where(p=>p.Position.Kind==PileKind.Tableau && p.Position.Pile==i).OrderBy(p=>p.Position.Index).ToList();
        for(int i=0;i<4;i++)piles[(PileKind.FreeCell,i)]=origins.Values.Where(p=>p.Position.Kind==PileKind.FreeCell && p.Position.Pile==i).ToList();
        var cells=Enumerable.Range(0,4).Where(i=>piles[(PileKind.FreeCell,i)].Count==0).ToArray();
        var columns=Enumerable.Range(0,8).Where(i=>i!=source && i!=destination && piles[(PileKind.Tableau,i)].Count==0).ToArray();
        var paths=new Dictionary<int,List<(double Time,CardPose Pose)>>();double time=now;
        void Transfer((PileKind Kind,int Pile) from,(PileKind Kind,int Pile) to)
        {
            var card=piles[from][^1];piles[from].RemoveAt(piles[from].Count-1);int index=piles[to].Count;
            var rect=to.Kind==PileKind.FreeCell?CellRect(to.Pile,false):new RectangleF(Table.Left+TableMargin+ColumnGap*to.Pile,TableauY+index*StackStep(to.Pile),CardWidth,CardHeight);
            var pose=card with{Rect=rect,Position=new(to.Kind,to.Pile,index),Layer=6000};piles[to].Add(pose);
            if(!paths.TryGetValue(card.Card.Key,out var path))paths[card.Card.Key]=path=[];
            path.Add((time,pose));time+=.065;
        }
        void TransferStack(int count,int from,int to,int[] spare)
        {
            if(count<=cells.Length+1)
            {
                for(int i=0;i<count-1;i++)Transfer((PileKind.Tableau,from),(PileKind.FreeCell,cells[i]));
                Transfer((PileKind.Tableau,from),(PileKind.Tableau,to));
                for(int i=count-2;i>=0;i--)Transfer((PileKind.FreeCell,cells[i]),(PileKind.Tableau,to));
                return;
            }
            // Game.CanMove already enforces the classic linear capacity. The
            // animation decomposes that accepted move into legal single cards.
            int column=spare[0];int[] rest=spare[1..];int chunk=Math.Min(count-1,(cells.Length+1)*(1<<rest.Length));
            TransferStack(chunk,from,column,rest);TransferStack(count-chunk,from,to,rest);TransferStack(chunk,column,to,rest);
        }
        if(moved.Length>(cells.Length+1)*(1<<columns.Length))return;
        TransferStack(moved.Length,source,destination,columns);
        foreach(var to in moved)
            if(paths.TryGetValue(to.Card.Key,out var stops))flights[to.Card.Key]=new(origins[to.Card.Key],to,now,Math.Max(.001,time-now),false,true,Stops:stops);
    }

    private void StopCardMotion(){motionRevision++;flights.Clear();dragPoses.Clear();restingCards=Game==null?[]:CaptureLayout();}
    private bool IsDragged(Card card)
    {
        if(!dragging || selection is not {} selected)return false;
        var pile=Game.Pile(selected);if(pile==null)return false;
        for(int i=Game.Index(selected);i<pile.Count;i++)if(pile[i].Key==card.Key)return true;
        return false;
    }
    private int SettledTop(List<Card> pile,Position position)
    {
        int index=pile.Count-1-(dragging && IsSelected(position)?1:0);
        while(index>=0 && flights.ContainsKey(pile[index].Key))index--;
        return index;
    }
    private void DrawGameCard(Graphics g,Card card,RectangleF r)
    {
        if(showingVictory && skin.Modern && Kind==GameKind.Klondike)return;
        // The board cache must not depend on the clock. The animation layer
        // owns a card until UpdateMotions removes its flight and rebuilds it.
        if(flights.ContainsKey(card.Key) && !IsDragged(card))return;
        bool inverted=Kind==GameKind.FreeCell && !skin.Modern && !dragging && selection is {} selected && Game.Pile(selected) is {Count:>0} pile && pile[^1].Key==card.Key;
        art.Draw(g,card,r,Preferences.Era,Preferences.CardBack,invert:inverted);
    }
    private void PaintMovingCards(Graphics g)
    {
        double now=MotionNow;var saved=g.Save();g.SetClip(Table,CombineMode.Intersect);
        if(skin.Future)g.SetClip(new RectangleF(Table.X,Table.Y,Table.Width,TableauBottom-Table.Y),CombineMode.Intersect);
        foreach(var flight in flights.Values.OrderBy(f=>f.To.Layer))
        {
            // Undealt cards are still inside the visible stock, not a second
            // pile of sprites painted over cards that have already departed.
            if(skin.Future && flight.From.Position.Kind==PileKind.Stock && now<flight.Start)continue;
            if(IsDragged(flight.To.Card) || now>=flight.End && !flight.To.Visible)continue;
            var pose=flight.Sample(now);
            if(skin.Future)
            {
                var cardClip=g.Save();
                if(flight.From.Position.Kind==PileKind.Tableau && flight.To.Position.Kind==PileKind.Tableau && flight.From.Position.Pile==flight.To.Position.Pile)
                    g.SetClip(OrbitColumnViewport(flight.To.Position.Pile),CombineMode.Intersect);
                float t=(float)Math.Clamp((now-flight.Start)/flight.Duration,0,1),bank=flight.Bank(now);
                if(Preferences.FutureAtmosphere && t>0 && t<1 && Math.Abs(flight.To.Rect.X-flight.From.Rect.X)>30)
                    for(int i=4;i>=1;i--){var tail=flight.Sample(Math.Max(flight.Start,now-i*.017)).Rect;using var b=new SolidBrush(Color.FromArgb(55-i*9,Orbit.Accent));g.FillEllipse(b,tail.X+tail.Width/2-1.4f,tail.Y+tail.Height/2-1.4f,2.8f,2.8f);}
                Orbit.DrawCard(g,pose.Card,pose.Rect,ScaleFactor,flight.To.Rect.Width,bank);
                g.Restore(cardClip);
            }
            else art.Draw(g,pose.Card,pose.Rect,Preferences.Era,Preferences.CardBack,flight.To.Rect.Width);
        }
        g.Restore(saved);
    }
    private (Position Position,RectangleF Rect)? HitMovingCard(PointF p)
    {
        double now=MotionNow;
        foreach(var flight in flights.Values.OrderByDescending(f=>f.To.Layer))
        {
            if(now>=flight.End || flight.To.Position.Kind==PileKind.Stock)continue;
            var pose=flight.Sample(now);if(pose.Rect.Contains(p) && pose.Card.FaceUp && Game.CanPick(flight.To.Position) && Game.Pile(flight.To.Position)?[Game.Index(flight.To.Position)].Key==flight.To.Card.Key)return (flight.To.Position,pose.Rect);
        }
        return null;
    }
    private void RememberDragPose(Position source)
    {
        var pile=Game.Pile(source)!;int start=Game.Index(source);
        for(int i=start;i<pile.Count;i++)
        {
            var card=pile[i];var r=new RectangleF(mouse.X-dragOffset.X,mouse.Y-dragOffset.Y+(i-start)*(source.Kind==PileKind.Tableau?StackStep(source.Pile):18),CardWidth,CardHeight);
            dragPoses[card.Key]=new(card,r,source with{Index=i},true,7000+i-start);
        }
    }
    private void ReturnDrag(Position source)
    {
        RememberDragPose(source);BeginCardMotion();
    }
    private void UpdateMotions()
    {
        double now=MotionNow;
        foreach(int key in flights.Where(pair=>now>=pair.Value.End).Select(pair=>pair.Key).ToArray())flights.Remove(key);
        if(pendingWin && !MotionActive){pendingWin=false;StartVictory();}
    }
    public void RenderMotionSequence(string directory)
    {
        Directory.CreateDirectory(directory);StopCardMotion();BeginCardMotion(deal:true);
        double first=flights.Count==0?MotionNow:flights.Values.Min(f=>f.Start);
        try
        {
            foreach(int ms in new[]{0,100,200,350,500,750})
            {
                renderMotionTime=first+ms/1000.0;boardStamp=null;
                using var bitmap=new Bitmap(ClientSize.Width,ClientSize.Height);
                using(var g=Graphics.FromImage(bitmap))PaintScaled(g);
                bitmap.Save(Path.Combine(directory,$"{Preferences.Era}-{Kind}-{ms:000}.png"));
            }
        }
        finally{renderMotionTime=null;StopCardMotion();}
    }
}
