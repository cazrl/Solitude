using System.Drawing.Drawing2D;
using System.Media;

namespace Solitude;

public sealed partial class GameWindow
{
    private Preferences? futureOptionsParent;
    private void OpenFutureAtmosphere()
    {
        var parent=draft!;OpenDialog(DialogPage.Deck);futureOptionsParent=parent;draft=parent.Clone();
    }
    private bool ReturnToFutureOptions()
    {
        if(!skin.Future || dialog!=DialogPage.Deck || futureOptionsParent==null)return false;
        var parent=futureOptionsParent;OpenDialog(DialogPage.Options);draft=parent;return true;
    }
    private void ApplyFuturePalette()
    {
        if(futureOptionsParent!=null){futureOptionsParent.FuturePalette=draft!.FuturePalette;CloseDialog();return;}
        Preferences.FuturePalette=draft!.FuturePalette;shared.Capture(Preferences);CloseDialog();Save();Invalidate();
    }
    private void PaintFutureDialog(Graphics g)
    {
        string title=dialog switch{DialogPage.Options=>"Experience",DialogPage.Deck=>"Atmosphere",DialogPage.Won=>"Orbit complete",DialogPage.Help=>"Flight manual",_=>"About ORBIT"};
        int width=520,height=dialog==DialogPage.Options?458:dialog==DialogPage.Deck?295:dialog==DialogPage.Won?338:360;
        var bounds=new RectangleF(MathF.Round((WorldWidth-width)/2+dialogOffset.X),MathF.Round((WorldHeight-height)/2+dialogOffset.Y),width,height);dialogBounds=bounds;
        if(dialogHost!=null){dialogHost.Text=title;dialogHost.AccessibleName=title;}
        var buttons=skin.Frame(g,bounds,title,true,true,mouse,pressedHotspot!=null);Add("modal-close",buttons[2],CloseDialog);
        var body=new RectangleF(bounds.X+1,bounds.Y+36,bounds.Width-2,bounds.Height-37);Skin.Fill(g,skin.Face,body);
        float x=body.X+24,y=body.Y+18,w=body.Width-48,bottom=body.Bottom;
        if(dialog==DialogPage.Options)
        {
            skin.Group(g,"Cards per draw",new(x,y,190,82));
            Check(g,"draw1",new(x+13,y+22,160,23),"Draw one",draft!.Rules.DrawCount==1,()=>{draft.Rules.DrawCount=1;Invalidate();},true);
            Check(g,"draw3",new(x+13,y+48,160,23),"Draw three",draft.Rules.DrawCount==3,()=>{draft.Rules.DrawCount=3;Invalidate();},true);
            skin.Group(g,"Scoring",new(x+207,y,w-207,105));
            for(int i=0;i<3;i++){int score=i;Check(g,"score"+i,new(x+220,y+21+i*25,w-230,24),((Scoring)i).ToString(),draft.Rules.Scoring==(Scoring)i,()=>{draft.Rules.Scoring=(Scoring)score;Invalidate();},true);}
            Check(g,"animations",new(x,y+123,w,30),"Spatial card &motion",draft.Animate,()=>{draft.Animate=!draft.Animate;Invalidate();});
            Orbit.Text(g,"Switch off for instant, reduced-motion play.",new(x+23,y+154,w-23,19),10,FutureArt.Muted);
            Check(g,"future-lights",new(x,y+188,w,30),"Light trails and docking &waves",draft.FutureAtmosphere,()=>{draft.FutureAtmosphere=!draft.FutureAtmosphere;Invalidate();});
            Check(g,"sound",new(x,y+230,w,30),"Synthesized &sound",draft.Sound,()=>{draft.Sound=!draft.Sound;Invalidate();});
            Orbit.Text(g,"Auto-save is on. Changing draw or scoring starts a new deal.",new(x,y+278,w,21),10,FutureArt.Muted);
            DialogButton(g,"ok",new(x+w-190,bottom-48,90,31),"Apply",()=>{GameCatalog.ApplyPeriodPresentation(draft!);ApplyOptions();if(!Preferences.Animate){StopCardMotion();futurePulses.Clear();}ScheduleFrames();},true);
            DialogButton(g,"cancel",new(x+w-90,bottom-48,90,31),"Cancel",CloseDialog);
            DialogButton(g,"appearance",new(x,bottom-48,118,31),"Atmosphere…",OpenFutureAtmosphere);
        }
        else if(dialog==DialogPage.Deck)
        {
            string[] names=["Aurora","Solstice","Nebula"];Color[] colors=[Color.FromArgb(119,232,218),Color.FromArgb(239,191,126),Color.FromArgb(175,179,255)];
            Orbit.Text(g,"Choose the light beyond the glass.",new(x,y,w,25),14);
            for(int i=0;i<3;i++)
            {
                int choice=i;var r=new RectangleF(x+i*(w+12)/3,y+44,(w-24)/3,96);
                FutureArt.Panel(g,r,Color.FromArgb(9,22,34),draft!.FuturePalette==i?colors[i]:Color.FromArgb(55,74,92),9);
                FutureArt.Glow(g,new(r.X+r.Width/2,r.Y+40),39,Color.FromArgb(95,colors[i]));FutureArt.OrbitMark(g,new(r.X+r.Width/2-18,r.Y+14,36,36),colors[i]);
                Check(g,"futurepalette"+i,new(r.X+11,r.Bottom-32,r.Width-16,26),names[i],draft.FuturePalette==i,()=>{draft.FuturePalette=choice;Invalidate();},true);
            }
            DialogButton(g,"ok",new(x+w-190,bottom-47,90,31),"Apply",ApplyFuturePalette,true);
            DialogButton(g,"cancel",new(x+w-90,bottom-47,90,31),"Cancel",CloseDialog);
        }
        else if(dialog==DialogPage.Won)
        {
            FutureArt.OrbitMark(g,new(x+w/2-27,y,54,54),Orbit.Accent);
            Orbit.Text(g,"All in alignment.",new(x,y+65,w,39),28,FutureArt.Ink,true);
            Orbit.Text(g,$"{Game.State.Moves} moves   /   {Game.State.Elapsed/60:00}:{Game.State.Elapsed%60:00}   /   Score {Game.State.Score}",new(x,y+112,w,24),13,Orbit.Accent,true);
            Orbit.Text(g,"A small moment of order in an infinite universe.",new(x,y+151,w,23),11,FutureArt.Muted,true);
            DialogButton(g,"ok",new(x+w/2-141,bottom-49,160,32),"Another orbit",()=>NewGame(),true);
            DialogButton(g,"cancel",new(x+w/2+31,bottom-49,110,32),"Stay here",CloseDialog);
        }
        else if(dialog==DialogPage.Help)
        {
            Orbit.Text(g,"A familiar game. A different horizon.",new(x,y,w,33),20);
            string[] lines=["Build descending columns in alternating red and black suits.","Move Aces home, then build each foundation up to King.","Only Kings may enter empty columns. Draw from the left stack.","Drag cards, or select a card and click its destination.","Double-click sends just that card home. Right-click collects.","H: hint   ·   Ctrl+Z: undo   ·   F2: new deal   ·   F6: editions"];
            for(int i=0;i<lines.Length;i++)Orbit.Text(g,lines[i],new(x,y+48+i*25,w,23),12,i==5?Orbit.Accent:FutureArt.Ink);
            Orbit.Text(g,"Automatic flips. Full-session Undo. A saved place in every era.",new(x,y+212,w,23),10,FutureArt.Muted);
            DialogButton(g,"ok",new(x+w-100,bottom-45,100,30),"Ready",CloseDialog,true);
        }
        else
        {
            FutureArt.OrbitMark(g,new(x,y+4,48,48),Orbit.Accent);Orbit.Text(g,"ORBIT / 2126",new(x+65,y,w-65,36),25);
            skin.Wrapped(g,"Solitude, imagined one century from now.\n\nOriginal geometric cards, spatial choreography and synthesized sound. The rules remain Klondike.\n\nPart of Solitude 0.10.0.",new(x,y+73,w,156));
            DialogButton(g,"ok",new(x+w-100,bottom-45,100,30),"Continue",CloseDialog,true);
        }
    }
    private void PlayFutureSound(string name)
    {
        if(!skin.Future || !Preferences.Sound)return;
        string key="orbit/"+name;lastPeriodSound=key;if(ephemeral)return;
        if(!vistaSounds.TryGetValue(key,out var player))
        {
            double frequency=name switch{"SHARED_LIFTOFF"=>390,"SHARED_MOVETOHOME"=>784,"SHARED_UNDO"=>330,"SHARED_HINTSHOWN"=>587,"SHARED_HINTNOMOVE"=>220,"ORBIT_WIN"=>523.25,_=>440};
            bool win=name=="ORBIT_WIN";double duration=win?1.3:.19;const int rate=22050;int count=(int)(rate*duration);
            var stream=new MemoryStream(44+count*2);using(var writer=new BinaryWriter(stream,System.Text.Encoding.ASCII,true))
            {
                writer.Write("RIFF"u8);writer.Write(36+count*2);writer.Write("WAVEfmt "u8);writer.Write(16);writer.Write((short)1);writer.Write((short)1);writer.Write(rate);writer.Write(rate*2);writer.Write((short)2);writer.Write((short)16);writer.Write("data"u8);writer.Write(count*2);
                for(int i=0;i<count;i++)
                {
                    double t=(double)i/rate,envelope=Math.Min(1,t/.012)*Math.Pow(1-t/duration,2.4),wave=Math.Sin(t*frequency*Math.Tau)+.24*Math.Sin(t*frequency*2*Math.Tau);
                    if(win)wave+=.5*Math.Sin(t*frequency*1.25*Math.Tau)+.38*Math.Sin(t*frequency*1.5*Math.Tau);
                    writer.Write((short)(wave*envelope*2700));
                }
            }
            stream.Position=0;vistaSounds[key]=player=new SoundPlayer(stream);
        }
        try{player.Play();}catch(Exception e) when(e is InvalidOperationException or IOException or TimeoutException){}
    }
}
