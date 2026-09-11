using System.Collections;
using System.Diagnostics;
using System.Drawing;
using System.Text.Json;
using Solitude;

internal static partial class UiProgram
{
    private static T CardMotionValue<T>(object value,string name)=>(T)value.GetType().GetProperty(name)!.GetValue(value)!;
    private static GameState CardVictoryState()
    {
        var state=new GameState{Seed=1989,Started=true,Elapsed=185,Score=4254,TimeBonus=3780};
        for(int f=0;f<4;f++)state.Foundations[f]=Enumerable.Range(f*13,13).Select(id=>new Card(id)).ToList();
        return state;
    }
    private static void ProfileOrbitVictory()
    {
        foreach(int scale in new[]{100,200})
        {
            using var form=new GameWindow(new Store("artifacts/orbit-card-motion/profile"),Era.Future2126,1989,scale,true);
            form.Preferences.Sound=false;form.SetRenderState(CardVictoryState());Set(form,"renderMotionTime",100.0);Call(form,"StartVictory");
            using var frame=new Bitmap(form.Width,form.Height,System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
            foreach(bool atmosphere in new[]{true,false})foreach(double time in new[]{1.0,2.8,5.0,6.6})
            {
                form.Preferences.FutureAtmosphere=atmosphere;Set(form,"renderMotionTime",100+time);DrawFrame(form,frame);
                var watch=Stopwatch.StartNew();for(int i=0;i<30;i++)DrawFrame(form,frame);
                Console.WriteLine($"{scale}% time {time} atmosphere {atmosphere}: {watch.Elapsed.TotalMilliseconds/30:F2} ms");
            }
        }
    }
    private static void CheckOrbitCardMotion()
    {
        const string output="artifacts/orbit-card-motion";
        Directory.CreateDirectory(output);Directory.CreateDirectory(output+"/win");Directory.CreateDirectory(output+"/deal");
        foreach(int scale in new[]{100,125,150,200})
        {
            using var form=new GameWindow(new Store(output+"/isolated"),Era.Future2126,1989,scale,true);
            form.Preferences.Sound=false;
            Paint(form);Set(form,"renderMotionTime",100.0);Call(form,"BeginCardMotion",true);
            var flights=(IDictionary)Field(form,"flights")!;
            Check(flights.Count==28,"Deal did not animate all 28 tableau cards");
            var stock=(RectangleF)typeof(GameWindow).GetProperty("StockRect",Private)!.GetValue(form)!;
            foreach(object flight in flights.Values)
            {
                var from=CardMotionValue<object>(flight,"From");
                Check(CardMotionValue<Position>(from,"Position").Kind==PileKind.Stock && CardMotionValue<RectangleF>(from,"Rect")==stock,"Dealt card does not originate at the visible stock");
                Check(!CardMotionValue<Card>(from,"Card").FaceUp,"Dealt card starts face up inside the stock");
            }
            // Isolate one departing card. Its upper half must actually be
            // painted between stock and tableau, outside the destination clip.
            object selected=flights.Values.Cast<object>().First(f=>CardMotionValue<Position>(CardMotionValue<object>(f,"To"),"Position")==new Position(PileKind.Tableau,6,0));
            int key=CardMotionValue<Card>(CardMotionValue<object>(selected,"To"),"Card").Key;
            flights.Clear();flights.Add(key,selected);
            double start=CardMotionValue<double>(selected,"Start");
            using(var moving=new Bitmap(form.Width,form.Height))
            {
                Set(form,"renderMotionTime",start-.001);
                using(var g=Graphics.FromImage(moving)){g.ScaleTransform(scale/100f,scale/100f);Call(form,"PaintMovingCards",g);}
                Check(FramePixels(moving).All(p=>p==0),"Unreleased deal sprite paints over departing cards");
                Set(form,"renderMotionTime",start+.17);
                var pose=selected.GetType().GetMethod("Sample")!.Invoke(selected,[start+.17])!;
                var rect=CardMotionValue<RectangleF>(pose,"Rect");
                float tableauY=(float)typeof(GameWindow).GetProperty("TableauY",Private)!.GetValue(form)!;
                var point=new PointF(rect.X+rect.Width/2,rect.Y+rect.Height*.15f);
                Check(point.Y<tableauY && point.X>stock.Right,"Deal visibility probe is not between stock and tableau");
                using(var g=Graphics.FromImage(moving)){g.ScaleTransform(scale/100f,scale/100f);Call(form,"PaintMovingCards",g);}
                Check(moving.GetPixel((int)(point.X*scale/100),(int)(point.Y*scale/100)).A>240,"Dealt card is clipped above the tableau");
            }
            Call(form,"StopCardMotion");
            if(scale==100)
            {
                Set(form,"renderMotionTime",100.0);Call(form,"BeginCardMotion",true);
                using var frame=new Bitmap(form.Width,form.Height);
                for(int i=0;i<=35;i++){Set(form,"renderMotionTime",100+i/30.0);Call(form,"Animate");DrawFrame(form,frame);frame.Save($"{output}/deal/deal-{i:000}.png");}
            }
            foreach(bool compact in new[]{false,true})
            {
                form.ClientSize=new((compact?800:1120)*scale/100,(compact?540:720)*scale/100);
                form.SetRenderState(CardVictoryState());Set(form,"renderMotionTime",100.0);Call(form,"StartVictory");
                string initial=JsonSerializer.Serialize(form.Game.State);
                using var frame=new Bitmap(form.Width,form.Height);
                var timings=new List<double>();int settledSpriteCount=0;
                for(int i=0;i<=152;i++)
                {
                    double time=i/20.0;Set(form,"renderMotionTime",100+time);var watch=Stopwatch.StartNew();DrawFrame(form,frame);watch.Stop();
                    if(i>20)timings.Add(watch.Elapsed.TotalMilliseconds);
                    var poses=((IEnumerable)Field(form,"orbitVictoryPoses")!).Cast<object>().ToArray();
                    Check(poses.Length==52 && poses.Select(p=>CardMotionValue<Card>(p,"Card").Key).Distinct().Count()==52,"Victory lost or duplicated a card");
                    Check(poses.All(p=>
                    {
                        var center=CardMotionValue<PointF>(p,"Center");float width=CardMotionValue<float>(p,"Width"),height=CardMotionValue<float>(p,"Height");
                        return float.IsFinite(center.X+center.Y+width+height) && width>0 && height>0 && center.X-width*.7f>=0 && center.X+width*.7f<=form.Width/(scale/100f) && center.Y-height*.7f>=36 && center.Y+height*.7f<form.Height/(scale/100f)-35;
                    }),"Invalid victory projection or card outside the playing area");
                    if(i==100){var art=typeof(GameWindow).GetProperty("Orbit",Private)!.GetValue(form)!;settledSpriteCount=((IDictionary)typeof(FutureArt).GetField("cards",Private)!.GetValue(art)!).Count;}
                    if(scale==100 && !compact)frame.Save($"{output}/win/win-{i:000}.png");
                    if(i==130)frame.Save($"{output}/fans-{scale}-{compact}.png");
                }
                Check(JsonSerializer.Serialize(form.Game.State)==initial,"Victory animation changed the won deal");
                var orbit=(FutureArt)typeof(GameWindow).GetProperty("Orbit",Private)!.GetValue(form)!;
                var sprites=(IDictionary)typeof(FutureArt).GetField("cards",Private)!.GetValue(orbit)!;
                Check(sprites.Count==settledSpriteCount && sprites.Count<=192,"Victory rasterizes new sprites as the cards settle");
                Set(form,"renderMotionTime",107.599);Call(form,"Animate");Check((bool)Field(form,"showingVictory")!,"Victory cuts off before its final hold");
                Set(form,"renderMotionTime",107.601);Call(form,"Animate");Check(!(bool)Field(form,"showingVictory")! && (DialogPage)Field(form,"dialog")! ==DialogPage.Won,"Victory did not finish on elapsed time");
                Console.WriteLine($"PASS card victory {scale}% {(compact?"compact":"normal")}: warmed paint median {timings.Order().ElementAt(timings.Count/2):F2} ms, p95 {timings.Order().ElementAt((int)(timings.Count*.95)):F2} ms");
                Call(form,"CloseDialog");
            }
            Set(form,"renderMotionTime",200.0);Call(form,"StartVictory");Key(form,Keys.Escape);
            Check(!(bool)Field(form,"showingVictory")! && (DialogPage)Field(form,"dialog")! ==DialogPage.Won,"Escape did not skip card victory");
            Call(form,"CloseDialog");Call(form,"StartVictory");Click(form,new RectangleF(150,220,20,20));
            Check(!(bool)Field(form,"showingVictory")!,"Click did not skip card victory");
            Call(form,"CloseDialog");form.Preferences.Animate=false;Call(form,"StartVictory");
            Check(!(bool)Field(form,"showingVictory")! && (DialogPage)Field(form,"dialog")! ==DialogPage.Won,"Reduced motion still runs the card victory");
        }
        using(var form=new GameWindow(new Store(output+"/last-card"),Era.Future2126,1989,100,true))
        {
            form.Preferences.Sound=false;var state=CardVictoryState();state.Tableau[0].Add(state.Foundations[0][^1]);state.Foundations[0].RemoveAt(12);
            form.SetRenderState(state);Paint(form);Set(form,"renderMotionTime",100.0);
            bool moved=form.Game.Move(new(PileKind.Tableau,0,0),new(PileKind.Foundation,0));Check(moved,"Last-card fixture move failed");Call(form,"Changed",moved);
            Set(form,"renderMotionTime",100.1);Call(form,"Animate");Check(!(bool)Field(form,"showingVictory")!,"Victory starts before the last card lands");
            Set(form,"renderMotionTime",101.0);Call(form,"Animate");Check((bool)Field(form,"showingVictory")!,"Last foundation arrival did not start victory");
            string won=JsonSerializer.Serialize(form.Game.State);
            using var frame=new Bitmap(form.Width,form.Height);
            foreach(int palette in new[]{0,1,2})
            {
                form.Preferences.FuturePalette=palette;
                foreach(double time in new[]{2.8,6.5}){Set(form,"renderMotionTime",101+time);DrawFrame(form,frame);frame.Save($"{output}/palette-{palette}-{time:F1}.png");}
            }
            Check(JsonSerializer.Serialize(form.Game.State)==won,"Victory palette rendering changes the win");
            Set(form,"renderMotionTime",108.7);Call(form,"Animate");Check((DialogPage)Field(form,"dialog")! ==DialogPage.Won,"Last-card victory did not reach results");
        }
        Console.WriteLine("PASS stock-to-tableau pixels, all 52 victory identities, bounded sprites, four scales, compact sizing and skip/reduced-motion lifecycle");
    }
}
