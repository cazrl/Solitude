namespace Solitude;

public sealed partial class GameWindow
{
    private void AnimateClassicVictory()
    {
        // Trail stamps are part of the effect's simulation, not display frames.
        // Otherwise 120/144 Hz paints nearly stationary borders over themselves
        // and changes both the bounce path and the density of the trails.
        const double tick=1.0/40;
        double now=MotionNow;
        victoryAccumulator+=Math.Clamp(now-lastVictoryTime,0,.2);
        lastVictoryTime=now;
        if(victoryAccumulator+1e-9<tick)return;
        using var g=Graphics.FromImage(victoryTrail!);
        skin.Configure(g);g.SetClip(Table);
        float density=art.RenderScale;
        try
        {
            // This bitmap is in logical pixels, even when the main window uses
            // device-resolution text/cards (Windows 2000 and XP).
            art.RenderScale=1;
            while(victoryAccumulator+1e-9>=tick)
            {
                victoryAccumulator=Math.Max(0,victoryAccumulator-tick);victoryFrame++;
                // Let a card leave the table before the next one starts. The
                // previous 175 ms launch timer interleaved up to 14 trajectories.
                if(flying.Count==0 && victoryDealt<52)
                {
                    int foundation=victoryDealt%4,rank=12-victoryDealt/4;
                    var origin=TopCard(foundation+3);var card=Game.State.Foundations[foundation][rank];
                    float vx=(victoryDealt%2==0?-1:1)*(2+(victoryDealt*7%5));
                    flying.Add(new(card,origin.X,origin.Y,vx,-2-(victoryDealt%4)));victoryDealt++;
                }
                foreach(var card in flying)
                {
                    card.X+=card.Vx;card.Y+=card.Vy;card.Vy+=.55f;
                    if(card.Y+CardHeight>=Table.Bottom)
                    {card.Y=Table.Bottom-CardHeight;card.Vy=-Math.Abs(card.Vy)*.77f;}
                    art.Draw(g,card.Card,new(card.X,card.Y,CardWidth,CardHeight),Preferences.Era,Preferences.CardBack);
                }
                flying.RemoveAll(card=>card.X < -CardWidth || card.X>WorldWidth);
                if(victoryDealt==52 && flying.Count==0)break;
            }
        }
        finally{art.RenderScale=density;}
        // A serial procession takes longer than the old overlapping burst.
        // Finish after all cards leave, or immediately on the existing skip input.
        if(victoryDealt==52 && flying.Count==0)FinishVictory();
    }
}
