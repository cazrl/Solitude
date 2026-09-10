using System.Security.Cryptography;

namespace Solitude;

public sealed partial class Game
{
    private GameState DealFreeCell(int seed)
    {
        if ((seed < 1 || seed > Rules.DealMaximum) && !(Rules.FreeCellEasterEggs && seed is -1 or -2)) throw new ArgumentOutOfRangeException(nameof(seed), $"Choose a game from 1 to {Rules.DealMaximum:N0}.");
        var state = new GameState { Kind = GameKind.FreeCell, Seed = seed, Tableau = Enumerable.Range(0, 8).Select(_ => new List<Card>()).ToList() };
        if(Rules.FreeCellEasterEggs && seed is -1 or -2)
        {
            // Preserved XP deal branches at VA 01003233-010032D1, inspected
            // as code bytes only. These deliberately ordered deals are not RNG seeds.
            for(int col=0;col<4;col++)
            {
                int suit=seed==-1?col:3-col;
                if(seed==-1)
                {
                    for(int rank=1;rank<=13;rank+=2)state.Tableau[col].Add(new(suit*13+rank-1));
                    for(int rank=12;rank>=2;rank-=2)state.Tableau[col+4].Add(new(suit*13+rank-1));
                }
                else
                {
                    state.Tableau[col].Add(new(suit*13));
                    for(int rank=13;rank>=8;rank--)state.Tableau[col].Add(new(suit*13+rank-1));
                    for(int rank=7;rank>=2;rank--)state.Tableau[col+4].Add(new(suit*13+rank-1));
                }
            }
            return state;
        }
        // The Microsoft CRT shuffle: rank-major C,D,H,S deck; remove by swapping with the last card.
        var deck = Enumerable.Range(0, 52).Select(i => new Card((i % 4) * 13 + i / 4)).ToList();
        uint random = (uint)seed;
        for (int dealt = 0; dealt < 52; dealt++)
        {
            random = unchecked(random * 214013u + 2531011u);
            int index = (int)((random >> 16) & 0x7fffu) % deck.Count;
            state.Tableau[dealt % 8].Add(deck[index]);
            deck[index] = deck[^1]; deck.RemoveAt(deck.Count - 1);
        }
        return state;
    }

    public int FreeCellCapacity(int destination)
    {
        if (Rules.Kind != GameKind.FreeCell || destination < 0 || destination >= 8) return 0;
        int free = State.FreeCells.Count(p => p.Count == 0);
        int empty = State.Tableau.Where((p, i) => i != destination && p.Count == 0).Count();
        // The old game used only the spare cells for a move to an empty column.
        // Vista handles recursive sequence moves through all available columns.
        return Rules.FreeCellSupermoves ? (free + 1) * (1 << empty)
            : State.Tableau[destination].Count == 0 ? free + 1 : (free + 1) * (empty + 1);
    }

    public int FreeCellAvailableMoves(int stopAfter = 2)
    {
        if (Rules.Kind != GameKind.FreeCell || State.Won) return 0;
        int count=0;
        IEnumerable<Position> Sources()
        {
            for(int col=0;col<8;col++)for(int i=0;i<State.Tableau[col].Count;i++)yield return new(PileKind.Tableau,col,i);
            for(int cell=0;cell<4;cell++)yield return new(PileKind.FreeCell,cell);
        }
        foreach(var from in Sources())
        {
            for(int col=0;col<8;col++)if(CanMove(from,new(PileKind.Tableau,col)) && ++count>=stopAfter)return count;
            // Empty home/free cells are interchangeable destinations for the warning.
            if(Enumerable.Range(0,4).Any(i=>CanMove(from,new(PileKind.FreeCell,i))) && ++count>=stopAfter)return count;
            if(Enumerable.Range(0,4).Any(i=>CanMove(from,new(PileKind.Foundation,i))) && ++count>=stopAfter)return count;
        }
        return count;
    }

    private IEnumerable<Position> FreeCellTops()
    {
        for (int i = 0; i < 8; i++) yield return new(PileKind.Tableau, i);
        for (int i = 0; i < 4; i++) yield return new(PileKind.FreeCell, i);
    }
    private bool SafeFreeCellCard(Card card)
    {
        if (card.Rank <= 2) return true;
        return Enum.GetValues<Suit>().Where(s => new Card((int)s * 13).Red != card.Red)
            .All(s => State.Foundations.Any(p => p.Count >= card.Rank - 1 && p[0].Suit == s));
    }
    private (Position From, Position To)? FreeCellHomeMove()
    {
        foreach (var from in FreeCellTops())
        {
            var pile = Pile(from)!;
            if (pile.Count == 0 || !SafeFreeCellCard(pile[^1])) continue;
            for (int f = 0; f < 4; f++) if (CanMove(from, new(PileKind.Foundation, f))) return (from, new(PileKind.Foundation, f));
        }
        return null;
    }
    private void AutoHomeFreeCell()
    {
        while (FreeCellHomeMove() is { } move)
        {
            var source = Pile(move.From)!;
            Pile(move.To)!.Add(source[^1]); source.RemoveAt(source.Count - 1);
        }
    }
    private bool AutoHomeFreeCellAction()
    {
        if (State.Won || FreeCellHomeMove() is not {} move) return false;
        return Move(move.From,move.To,autoHome:false);
    }
    public bool ToFreeCell(Position from)
    {
        if (Rules.Kind != GameKind.FreeCell) return false;
        for (int i = 0; i < 4; i++) if (Move(from, new(PileKind.FreeCell, i),autoHome:false)) return true;
        return false;
    }

    private GameState DealSpider(int seed)
    {
        var deck = new List<Card>();
        Suit[] suits = Rules.SpiderSuits switch { 1 => [Suit.Spades], 2 => [Suit.Hearts, Suit.Spades], _ => Enum.GetValues<Suit>() };
        for (int copy = 0; copy < 8 / suits.Length; copy++)
            foreach (var suit in suits) for (int rank = 0; rank < 13; rank++) deck.Add(new((int)suit * 13 + rank, false, copy));
        uint random = unchecked((uint)seed + 0x9E3779B9u);
        if (random == 0) random = 0xA341316Cu;
        for (int i = deck.Count - 1; i > 0; i--)
        {
            random ^= random << 13; random ^= random >> 17; random ^= random << 5;
            int j = (int)(random % (uint)(i + 1)); (deck[i], deck[j]) = (deck[j], deck[i]);
        }
        var state = new GameState
        {
            Kind = GameKind.Spider, SpiderSuits = Rules.SpiderSuits, Seed = seed, Score = 500,
            Tableau = Enumerable.Range(0, 10).Select(_ => new List<Card>()).ToList(),
            Foundations = Enumerable.Range(0, 8).Select(_ => new List<Card>()).ToList()
        };
        for (int i = 0; i < 54; i++) { state.Tableau[i % 10].Add(deck[^1]); deck.RemoveAt(deck.Count - 1); }
        foreach (var pile in state.Tableau) pile[^1] = pile[^1] with { FaceUp = true };
        state.Stock = deck; return state;
    }
    public bool CanDealSpider => Rules.Kind == GameKind.Spider && !State.Won && State.Stock.Count >= 10 && State.Tableau.All(p => p.Count > 0);
    private bool DrawSpider()
    {
        if (!CanDealSpider) return false;
        Remember();
        foreach (var pile in State.Tableau) { pile.Add(State.Stock[^1] with { FaceUp = true }); State.Stock.RemoveAt(State.Stock.Count - 1); }
        CompleteSpiderRuns();if(!Rules.SpiderFullUndo)History.Clear();return true;
    }
    private void CompleteSpiderRuns()
    {
        foreach (var pile in State.Tableau)
        {
            while (pile.Count > 0)
            {
                pile[^1] = pile[^1] with { FaceUp = true };
                if (pile.Count < 13) break;
                var run = pile.TakeLast(13).ToArray();
                if (!run.Select((card, i) => card.FaceUp && card.Suit == run[0].Suit && card.Rank == 13 - i).All(valid => valid)) break;
                State.Foundations.First(p => p.Count == 0).AddRange(run);
                pile.RemoveRange(pile.Count - 13, 13); State.Score += 100;
                if(!Rules.SpiderFullUndo)History.Clear();
            }
        }
    }
    private bool UndoSpider()
    {
        int elapsed = State.Elapsed, moves = State.Moves + 1, undo = State.UndoCount + 1;
        State = History[^1]; History.RemoveAt(History.Count - 1);
        State.Score = 500+100*State.Foundations.Count(p=>p.Count==13)-moves; State.UndoCount = undo;
        State.Elapsed = elapsed; State.Moves = moves; State.Started = true; return true;
    }
    private (Position From, Position To, string Text)? OtherHint()
    {
        var sources = new List<Position>();
        for (int col = 0; col < State.Tableau.Count; col++)
            for (int index = 0; index < State.Tableau[col].Count; index++) sources.Add(new(PileKind.Tableau, col, index));
        if (Rules.Kind == GameKind.FreeCell)
        {
            for (int f = 0; f < 4; f++) sources.Add(new(PileKind.FreeCell, f));
            foreach (var from in sources) for (int f = 0; f < 4; f++)
                if (CanMove(from, new(PileKind.Foundation, f))) return (from, new(PileKind.Foundation, f), $"Move the {Pile(from)![Index(from)].Name} to a home cell.");
        }
        // Prefer joining Spider cards of the same suit; they can subsequently move together.
        foreach (bool sameSuit in new[] { true, false })
            foreach (var from in sources)
                for (int col = 0; col < State.Tableau.Count; col++)
                {
                    if (from.Kind == PileKind.Tableau && Index(from) == 0 && State.Tableau[col].Count == 0) continue;
                    if (!CanMove(from, new(PileKind.Tableau, col))) continue;
                    if (sameSuit && (State.Tableau[col].Count == 0 || State.Tableau[col][^1].Suit != Pile(from)![Index(from)].Suit)) continue;
                    return (from, new(PileKind.Tableau, col), $"Move the {Pile(from)![Index(from)].Name} to column {col + 1}.");
                }
        if (Rules.Kind == GameKind.FreeCell)
            foreach (var from in sources.Where(p => p.Kind == PileKind.Tableau)) for (int f = 0; f < 4; f++)
                if (CanMove(from, new(PileKind.FreeCell, f))) return (from, new(PileKind.FreeCell, f), $"Move the {Pile(from)![Index(from)].Name} to a free cell.");
        if (CanDealSpider) return (new(PileKind.Stock), new(PileKind.Stock), "Deal a new row of ten cards.");
        return null;
    }
    private static void ValidateOther(GameState s)
    {
        bool spider = s.Kind == GameKind.Spider;
        if (!Enum.IsDefined(s.Kind) || s.SpiderSuits is not (1 or 2 or 4) || s.Stock == null || s.Waste == null || s.Waste.Count != 0 || s.Tableau?.Count != (spider ? 10 : 8) || s.Foundations?.Count != (spider ? 8 : 4) || s.FreeCells?.Count != 4 || s.Tableau.Concat(s.Foundations).Concat(s.FreeCells).Any(p => p == null))
            throw new InvalidDataException("The saved game has invalid piles.");
        var cards = s.Stock.Concat(s.Tableau.SelectMany(p => p)).Concat(s.Foundations.SelectMany(p => p)).Concat(s.FreeCells.SelectMany(p => p)).ToArray();
        int total = spider ? 104 : 52;
        if (cards.Length != total || cards.Select(c => c.Key).Distinct().Count() != total || cards.Any(c => c.Id is < 0 or > 51 || c.Copy < 0 || c.Copy >= (spider ? 8 / s.SpiderSuits : 1)) || s.Stock.Any(c => c.FaceUp) || s.Elapsed < 0 || s.Moves < 0 || s.UndoCount < 0 || s.Recycles != 0 || s.WasteFan != 0)
            throw new InvalidDataException("The saved game has invalid cards or counters.");
        if (spider)
        {
            if (s.Stock.Count > 50 || s.Stock.Count % 10 != 0 || s.FreeCells.Any(p => p.Count != 0) || cards.Any(c => s.SpiderSuits == 1 && c.Suit != Suit.Spades || s.SpiderSuits == 2 && c.Suit is not (Suit.Hearts or Suit.Spades)))
                throw new InvalidDataException("The Spider deck is invalid.");
            foreach (var pile in s.Foundations)
                if (pile.Count != 0 && (pile.Count != 13 || pile.Where((c, i) => !c.FaceUp || c.Rank != 13 - i || c.Suit != pile[0].Suit).Any())) throw new InvalidDataException("Invalid completed Spider run.");
            foreach (var pile in s.Tableau)
            {
                bool up = false;
                foreach (var card in pile) { if (up && !card.FaceUp) throw new InvalidDataException("Hidden card above a face-up card."); up |= card.FaceUp; }
                if (pile.Count > 0 && !pile[^1].FaceUp) throw new InvalidDataException("Spider must expose the top card.");
            }
        }
        else
        {
            if (s.Stock.Count != 0 || s.FreeCells.Any(p => p.Count > 1) || cards.Any(c => !c.FaceUp)) throw new InvalidDataException("FreeCell has four single-card free cells and no hidden cards.");
            foreach (var pile in s.Foundations)
                if (pile.Where((c, i) => c.Rank != i + 1 || c.Suit != pile[0].Suit).Any()) throw new InvalidDataException("Invalid FreeCell home cell.");
        }
    }
}
