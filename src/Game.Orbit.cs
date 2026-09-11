namespace Solitude;

public sealed partial class Game
{
    private sealed record CachedBoardKey(string Value);
    private readonly System.Runtime.CompilerServices.ConditionalWeakTable<GameState,CachedBoardKey> orbitHistoryKeys=new();
    public bool OrbitAutoMove(Position from)
    {
        if(Rules.Kind!=GameKind.Klondike || from.Kind is not (PileKind.Tableau or PileKind.Waste) || !CanPick(from))return false;
        if(ToFoundation(from))return true;
        // Prefer an existing column, then the leftmost legal empty one. Moving
        // a whole King column to another empty slot would make no progress.
        foreach(int column in Enumerable.Range(0,State.Tableau.Count).OrderBy(i=>State.Tableau[i].Count==0))
        {
            if(State.Tableau[column].Count==0 && from.Kind==PileKind.Tableau && Index(from)==0)continue;
            var destination=new Position(PileKind.Tableau,column);
            if(CanMove(from,destination))return Move(from,destination);
        }
        return false;
    }
    // ORBIT guidance is deliberately separate from historical hint policies.
    public List<(Position From,Position To,string Text)> OrbitHints()
    {
        if(Rules.Kind!=GameKind.Klondike || State.Won)return [];
        // History boards are immutable. Weak keys let Undo and new deals free
        // discarded entries, and avoid rebuilding thousands of strings per hint.
        var seen=History.Select(s=>orbitHistoryKeys.GetValue(s,static board=>new(BoardKey(board))).Value).ToHashSet();seen.Add(BoardKey(State));
        var choices=new List<(Position From,Position To,string Text,int Priority)>();
        void Offer(Position from,Position to,string text,int priority)
        {
            var trial=Restore(Rules.Clone(),State);
            bool moved=from.Kind==PileKind.Stock?trial.Draw():from==to?trial.Flip(from.Pile):trial.Move(from,to);
            if(moved && !seen.Contains(BoardKey(trial.State)))choices.Add((from,to,text,priority));
        }
        for(int i=0;i<7;i++)if(State.Tableau[i] is {Count:>0} p && !p[^1].FaceUp)
            Offer(new(PileKind.Tableau,i),new(PileKind.Tableau,i),$"Reveal the card in column {i+1}.",100);
        var sources=new List<Position>{new(PileKind.Waste)};
        for(int c=0;c<7;c++)for(int i=0;i<State.Tableau[c].Count;i++)sources.Add(new(PileKind.Tableau,c,i));
        foreach(var from in sources.Where(CanPick))
        {
            var card=Pile(from)![Index(from)];
            for(int f=0;f<4;f++)if(CanMove(from,new(PileKind.Foundation,f)))
            {
                var trial=Restore(Rules.Clone(),State);bool safe=trial.ToFoundation(from,true);
                if(safe)Offer(from,new(PileKind.Foundation,f),$"Move the {card.Name} safely to a foundation.",90);
            }
            for(int c=0;c<7;c++)if(CanMove(from,new(PileKind.Tableau,c)))
            {
                bool reveals=from.Kind==PileKind.Tableau && Index(from)>0 && !Pile(from)![Index(from)-1].FaceUp;
                bool empties=from.Kind==PileKind.Tableau && Index(from)==0;
                if(empties && State.Tableau[c].Count==0)continue;
                Offer(from,new(PileKind.Tableau,c),$"Move the {card.Name} to column {c+1}."+(reveals?" Reveal a hidden card.":empties?" Free a column for a King.":""),reveals?85:from.Kind==PileKind.Waste?75:empties?65:40);
            }
        }
        if(State.Stock.Count>0 || CanRecycle)Offer(new(PileKind.Stock),new(PileKind.Stock),State.Stock.Count>0?"Draw to look for another useful card.":"Recycle the waste to explore the next pass.",30);
        for(int f=0;f<4;f++)for(int c=0;c<7;c++)if(CanMove(new(PileKind.Foundation,f),new(PileKind.Tableau,c)))
            Offer(new(PileKind.Foundation,f),new(PileKind.Tableau,c),$"Bring the {State.Foundations[f][^1].Name} back to column {c+1} to explore another route.",10);
        return choices.OrderByDescending(c=>c.Priority).Select(c=>(c.From,c.To,c.Text)).ToList();
    }
    private static string BoardKey(GameState s)=>string.Join('|',new[]{s.Stock,s.Waste}.Concat(s.Foundations).Concat(s.Tableau).Select(p=>string.Join(',',p.Select(c=>c.Id+(c.FaceUp?0:52)))));

    public bool CanCollectToWin()
    {
        if(Rules.Kind!=GameKind.Klondike || State.Won || State.Stock.Count!=0 || State.Tableau.Any(p=>p.Any(c=>!c.FaceUp)))return false;
        var trial=Restore(Rules.Clone(),State);
        while(trial.AutoStep()){}
        return trial.State.Won;
    }
}
