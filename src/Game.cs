using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Solitude;

public enum Suit { Clubs, Diamonds, Hearts, Spades }
public enum Scoring { Standard, Vegas, None }
public enum PileKind { Stock, Waste, Foundation, Tableau, FreeCell }
public readonly record struct Position(PileKind Kind, int Pile = 0, int Index = -1);
public readonly record struct Card(int Id, bool FaceUp = true, int Copy = 0)
{
    [JsonIgnore] public int Key => Id + Copy * 52;
    [JsonIgnore] public Suit Suit => (Suit)(Id / 13);
    [JsonIgnore] public int Rank => Id % 13 + 1;
    [JsonIgnore] public bool Red => Suit is Suit.Diamonds or Suit.Hearts;
    [JsonIgnore] public string Name => $"{Rank switch { 1 => "Ace", 11 => "Jack", 12 => "Queen", 13 => "King", _ => Rank.ToString() }} of {Suit}";
}
public sealed class Rules
{
    public Rules Clone() => (Rules)MemberwiseClone();
    public GameKind Kind { get; set; }
    public int SpiderSuits { get; set; } = 1;
    public int UndoLimit { get; set; } = 200;
    public bool FreeCellSupermoves { get; set; } = true;
    public bool FreeCellEasterEggs { get; set; }
    public bool SpiderFullUndo { get; set; } = true;
    public int DealMaximum { get; set; } = 1_000_000;
    public bool KeepVegasScore { get; set; }
    public int DrawCount { get; set; } = 3;
    public Scoring Scoring { get; set; } = Scoring.Standard;
    public bool Timed { get; set; } = true;
    public bool AutoFlip { get; set; }
}
public sealed class GameState
{
    public GameKind Kind { get; set; }
    public int SpiderSuits { get; set; } = 1;
    public List<List<Card>> FreeCells { get; set; } = [[], [], [], []];
    public int UndoCount { get; set; }
    public int Seed { get; set; }
    public List<Card> Stock { get; set; } = [];
    public List<Card> Waste { get; set; } = [];
    public List<List<Card>> Foundations { get; set; } = [[], [], [], []];
    public List<List<Card>> Tableau { get; set; } = [[], [], [], [], [], [], []];
    public int Score { get; set; }
    public int TimeBonus { get; set; }
    public int Moves { get; set; }
    public int Elapsed { get; set; }
    public int Recycles { get; set; }
    public int WasteFan { get; set; }
    public bool Started { get; set; }
    public bool WinRecorded { get; set; }
    public bool Lost { get; set; }
    [JsonIgnore] public bool Won => Foundations.Sum(p => p.Count) == (Kind == GameKind.Spider ? 104 : 52);
    public GameState Clone() => new()
    {
        Kind = Kind, SpiderSuits = SpiderSuits, FreeCells = FreeCells.Select(p => p.ToList()).ToList(), UndoCount = UndoCount,
        Seed = Seed, Lost = Lost, Stock = [.. Stock], Waste = [.. Waste], Foundations = Foundations.Select(p => p.ToList()).ToList(),
        Tableau = Tableau.Select(p => p.ToList()).ToList(), Score = Score, Moves = Moves, Elapsed = Elapsed, TimeBonus=TimeBonus,
        Recycles = Recycles, WasteFan = WasteFan, Started = Started, WinRecorded = WinRecorded
    };
}
public sealed partial class Game
{
    public GameState State { get; private set; }
    public Rules Rules { get; }
    public List<GameState> History { get; } = [];
    public bool CanUndo => History.Count != 0 && !State.Won && !State.Lost;
    public bool CanRecycle => Rules.Kind == GameKind.Klondike && State.Stock.Count == 0 && State.Waste.Count != 0 &&
        (Rules.Scoring != Scoring.Vegas || State.Recycles < (Rules.DrawCount == 1 ? 0 : 2));
    public Game(Rules rules, int? seed = null)
    {
        if (rules.DrawCount is not (1 or 3) || !Enum.IsDefined(rules.Scoring) || !Enum.IsDefined(rules.Kind) || rules.SpiderSuits is not (1 or 2 or 4) || rules.UndoLimit is < 0 or > 200 || rules.DealMaximum is < 1 or > 1_000_000) throw new ArgumentException("Invalid game rules.");
        Rules = rules;
        State = Deal(seed ?? RandomNumberGenerator.GetInt32(1, rules.Kind == GameKind.FreeCell ? rules.DealMaximum + 1 : int.MaxValue));
    }
    public static Game Restore(Rules rules, GameState state, List<GameState>? history = null)
    {
        Validate(state);
        if (state.Kind != rules.Kind || state.Kind == GameKind.Spider && state.SpiderSuits != rules.SpiderSuits) throw new InvalidDataException("Saved game and rules do not match.");
        var result = new Game(rules, state.Seed) { State = state.Clone() };
        foreach (var s in history?.TakeLast(rules.UndoLimit==0?int.MaxValue:rules.UndoLimit) ?? []) { Validate(s); if (s.Kind != state.Kind || s.Seed != state.Seed || s.SpiderSuits != state.SpiderSuits) throw new InvalidDataException("Invalid undo history."); result.History.Add(s.Clone()); }
        if(rules.Kind==GameKind.Spider && !rules.SpiderFullUndo)
        {
            int barrier=result.History.FindLastIndex(s=>s.Stock.Count!=state.Stock.Count || s.Foundations.Sum(p=>p.Count)!=state.Foundations.Sum(p=>p.Count));
            if(barrier>=0)result.History.RemoveRange(0,barrier+1);
        }
        return result;
    }
    private GameState Deal(int seed)
    {
        if (Rules.Kind == GameKind.FreeCell) return DealFreeCell(seed);
        if (Rules.Kind == GameKind.Spider) return DealSpider(seed);
        var deck = Enumerable.Range(0, 52).Select(i => new Card(i, false)).ToList();
        // Fixed PRNG keeps deals repeatable between runtime versions. These are Solitude deal numbers.
        uint random = unchecked((uint)seed + 0x9E3779B9u);
        if (random == 0) random = 0xA341316Cu;
        for (int i = 51; i > 0; i--)
        {
            random ^= random << 13; random ^= random >> 17; random ^= random << 5;
            int j = (int)(random % (uint)(i + 1));
            (deck[i], deck[j]) = (deck[j], deck[i]);
        }
        var state = new GameState { Seed = seed, Score = Rules.Scoring == Scoring.Vegas ? -52 : 0 };
        for (int row = 0; row < 7; row++)
            for (int col = row; col < 7; col++)
            {
                var card = deck[^1]; deck.RemoveAt(deck.Count - 1);
                state.Tableau[col].Add(card with { FaceUp = col == row });
            }
        state.Stock = deck;
        return state;
    }
    private void Remember()
    {
        History.Add(State.Clone());
        if (Rules.UndoLimit>0 && History.Count > Rules.UndoLimit) History.RemoveAt(0);
        State.Started = true;
        State.Moves++;
        if (Rules.Kind == GameKind.Spider) State.Score--;
    }
    private void Score(int standard, int vegas = 0)
    {
        if (Rules.Kind != GameKind.Klondike) return;
        if (Rules.Scoring == Scoring.Standard) State.Score = Math.Max(0, State.Score + standard);
        else if (Rules.Scoring == Scoring.Vegas) State.Score += vegas;
    }
    public bool Draw()
    {
        if (Rules.Kind == GameKind.FreeCell) return false;
        if (Rules.Kind == GameKind.Spider) return DrawSpider();
        if (State.Won || State.Lost) return false;
        if (State.Stock.Count == 0)
        {
            if (!CanRecycle) return false;
            Remember();
            State.Stock = State.Waste.AsEnumerable().Reverse().Select(c => c with { FaceUp = false }).ToList();
            State.Waste.Clear(); State.WasteFan = 0; State.Recycles++;
            if (Rules.DrawCount == 1) Score(-100);
            else if (State.Recycles >= 3) Score(-20);
            return true;
        }
        Remember();
        int count = Math.Min(Rules.DrawCount, State.Stock.Count);
        for (int i = 0; i < count; i++)
        {
            State.Waste.Add(State.Stock[^1] with { FaceUp = true }); State.Stock.RemoveAt(State.Stock.Count - 1);
        }
        State.WasteFan = count;
        return true;
    }
    public List<Card>? Pile(Position p) => p.Kind switch
    {
        PileKind.Stock => State.Stock,
        PileKind.Waste => State.Waste,
        PileKind.Foundation when p.Pile >= 0 && p.Pile < State.Foundations.Count => State.Foundations[p.Pile],
        PileKind.Tableau when p.Pile >= 0 && p.Pile < State.Tableau.Count => State.Tableau[p.Pile],
        PileKind.FreeCell when Rules.Kind == GameKind.FreeCell && p.Pile is >= 0 and < 4 => State.FreeCells[p.Pile],
        _ => null
    };
    public int Index(Position p) => p.Index < 0 ? (Pile(p)?.Count ?? 0) - 1 : p.Index;
    public bool CanPick(Position p)
    {
        if (State.Won || State.Lost || p.Kind == PileKind.Stock) return false;
        if (Rules.Kind != GameKind.Klondike && p.Kind == PileKind.Foundation) return false;
        var pile = Pile(p); int index = Index(p);
        if (pile == null || index < 0 || index >= pile.Count || !pile[index].FaceUp) return false;
        if (p.Kind != PileKind.Tableau) return index == pile.Count - 1;
        for (int i = index + 1; i < pile.Count; i++)
            if (!pile[i].FaceUp || (Rules.Kind == GameKind.Spider ? pile[i].Suit != pile[i-1].Suit : pile[i].Red == pile[i - 1].Red) || pile[i].Rank != pile[i - 1].Rank - 1) return false;
        return true;
    }
    public bool CanMove(Position from, Position to)
    {
        if (!CanPick(from) || (from.Kind == to.Kind && from.Pile == to.Pile)) return false;
        var source = Pile(from)!; var dest = Pile(to); int index = Index(from);
        if (dest == null) return false;
        var card = source[index];
        if (to.Kind == PileKind.FreeCell) return Rules.Kind == GameKind.FreeCell && dest.Count == 0 && index == source.Count - 1;
        if (to.Kind == PileKind.Foundation)
            return Rules.Kind != GameKind.Spider && index == source.Count - 1 && (dest.Count == 0 ? card.Rank == 1 : dest[^1].Suit == card.Suit && dest[^1].Rank + 1 == card.Rank);
        if (to.Kind == PileKind.Tableau)
        {
            if (Rules.Kind == GameKind.FreeCell && source.Count-index > FreeCellCapacity(to.Pile)) return false;
            return dest.Count == 0 ? Rules.Kind != GameKind.Klondike || card.Rank == 13 : dest[^1].FaceUp && (Rules.Kind == GameKind.Spider || dest[^1].Red != card.Red) && dest[^1].Rank == card.Rank + 1;
        }
        return false;
    }
    public bool Move(Position from, Position to, bool autoHome = true)
    {
        if (!CanMove(from, to)) return false;
        Remember();
        var source = Pile(from)!; var dest = Pile(to)!; int index = Index(from);
        dest.AddRange(source.Skip(index)); source.RemoveRange(index, source.Count - index);
        if (Rules.Kind == GameKind.Spider) { CompleteSpiderRuns(); return true; }
        if (Rules.Kind == GameKind.FreeCell) { if(autoHome)AutoHomeFreeCell(); return true; }
        if (from.Kind == PileKind.Waste) State.WasteFan = Math.Min(State.Waste.Count, State.WasteFan > 1 ? State.WasteFan - 1 : Rules.DrawCount);
        // Reordering the same foundation card does not earn it a second time.
        if (to.Kind == PileKind.Foundation) { if (from.Kind != PileKind.Foundation) Score(10, 5); }
        else if (from.Kind == PileKind.Foundation) Score(-15, -5);
        else if (from.Kind == PileKind.Waste) Score(5);
        if (Rules.AutoFlip && from.Kind == PileKind.Tableau && source.Count > 0 && !source[^1].FaceUp)
        { source[^1] = source[^1] with { FaceUp = true }; Score(5); }
        if (State.Won && Rules.Timed && Rules.Scoring == Scoring.Standard && State.Elapsed > 30)
            { State.TimeBonus=700000 / State.Elapsed; State.Score += State.TimeBonus; }
        return true;
    }
    public bool Flip(int column)
    {
        if (column < 0 || column >= State.Tableau.Count || State.Won || Rules.Kind != GameKind.Klondike) return false;
        var pile = State.Tableau[column];
        if (pile.Count == 0 || pile[^1].FaceUp) return false;
        Remember(); pile[^1] = pile[^1] with { FaceUp = true }; Score(5); return true;
    }
    public bool ToFoundation(Position from, bool safe = false)
    {
        if (from.Kind==PileKind.Foundation || !CanPick(from)) return false;
        var source = Pile(from)!; var card = source[Index(from)];
        if (safe && card.Rank > 2)
        {
            // Do not hide a needed tableau card during automatic collection.
            foreach (Suit suit in Enum.GetValues<Suit>())
                if (suit != card.Suit &&
                    State.Foundations.Where(p => p.Count > 0 && p[0].Suit == suit).Select(p => p.Count).DefaultIfEmpty(0).Max() < card.Rank - (new Card((int)suit * 13).Red != card.Red ? 1 : 2))
                    return false;
        }
        for (int i = 0; i < 4; i++) if (Move(from, new(PileKind.Foundation, i),autoHome:false)) return true;
        return false;
    }
    public bool AutoStep()
    {
        if (Rules.Kind == GameKind.Spider) return false;
        if (Rules.Kind == GameKind.FreeCell) return AutoHomeFreeCellAction();
        if (ToFoundation(new(PileKind.Waste), true)) return true;
        for (int i = 0; i < 7; i++) if (ToFoundation(new(PileKind.Tableau, i), true)) return true;
        return false;
    }
    public bool Undo()
    {
        if (!CanUndo) return false;
        if (Rules.Kind == GameKind.Spider) return UndoSpider();
        int elapsed = State.Elapsed;
        State = History[^1].Clone(); History.RemoveAt(History.Count - 1);
        int extraPenalties = elapsed / 10 - State.Elapsed / 10;
        State.Elapsed = elapsed; State.Started = true; Score(-2 - (Rules.Timed ? extraPenalties * 2 : 0)); return true;
    }
    public void Tick()
    {
        if (!State.Started || State.Won || State.Lost || !Rules.Timed) return;
        State.Elapsed++;
        if (State.Elapsed % 10 == 0) Score(-2);
    }
    public (Position From, Position To, string Text)? Hint()
    {
        if (Rules.Kind != GameKind.Klondike) return OtherHint();
        for (int i = 0; i < 7; i++)
        {
            var p = State.Tableau[i];
            if (p.Count > 0 && !p[^1].FaceUp) return (new(PileKind.Tableau, i), new(PileKind.Tableau, i), $"Turn over the card in column {i + 1}.");
        }
        var candidates = new List<Position> { new(PileKind.Waste) };
        for (int col = 0; col < 7; col++)
            for (int i = 0; i < State.Tableau[col].Count; i++) candidates.Add(new(PileKind.Tableau, col, i));
        foreach (var from in candidates)
            for (int i = 0; i < 4; i++) if (CanMove(from, new(PileKind.Foundation, i)))
                return (from, new(PileKind.Foundation, i), $"Move the {Pile(from)![Index(from)].Name} to a foundation.");
        foreach (var from in candidates)
            for (int i = 0; i < 7; i++)
            {
                if (from.Kind == PileKind.Tableau && Index(from) == 0 && State.Tableau[i].Count == 0) continue;
                if (CanMove(from, new(PileKind.Tableau, i)))
                    return (from, new(PileKind.Tableau, i), $"Move the {Pile(from)![Index(from)].Name} to column {i + 1}.");
            }
        if (State.Stock.Count > 0 || CanRecycle) return (new(PileKind.Stock), new(PileKind.Stock), State.Stock.Count > 0 ? "Draw from the stock." : "Turn the waste over to use the stock again.");
        // Foundations can also supply a card needed to free a tableau column.
        for (int f = 0; f < 4; f++) for (int t = 0; t < 7; t++)
            if (CanMove(new(PileKind.Foundation, f), new(PileKind.Tableau, t)))
                return (new(PileKind.Foundation, f), new(PileKind.Tableau, t), $"Move the {State.Foundations[f][^1].Name} back to column {t + 1}.");
        return null;
    }
    public static void Validate(GameState s)
    {
        if (s != null && s.Kind != GameKind.Klondike) { ValidateOther(s); return; }
        if (s == null || s.Stock == null || s.Waste == null || s.Foundations?.Count != 4 || s.Tableau?.Count != 7 || s.Foundations.Any(p => p == null) || s.Tableau.Any(p => p == null))
            throw new InvalidDataException("The saved game is incomplete.");
        var cards = s.Stock.Concat(s.Waste).Concat(s.Foundations.SelectMany(p => p)).Concat(s.Tableau.SelectMany(p => p)).ToArray();
        if (cards.Length != 52 || cards.Any(c => c.Id is < 0 or > 51 || c.Copy != 0) || cards.Select(c => c.Id).Distinct().Count() != 52 || s.FreeCells == null || s.FreeCells.Count != 4 || s.FreeCells.Any(p => p == null || p.Count != 0))
            throw new InvalidDataException("The saved game must contain all 52 distinct cards.");
        if (s.Stock.Any(c => c.FaceUp) || s.Waste.Any(c => !c.FaceUp) || s.TimeBonus < 0 || s.Elapsed < 0 || s.Moves < 0 || s.Recycles < 0 || s.WasteFan < 0 || s.WasteFan > 3)
            throw new InvalidDataException("The saved game contains invalid state.");
        foreach (var pile in s.Foundations)
            for (int i = 0; i < pile.Count; i++) if (!pile[i].FaceUp || pile[i].Rank != i + 1 || pile[i].Suit != pile[0].Suit)
                throw new InvalidDataException("The saved foundation is invalid.");
        foreach (var pile in s.Tableau)
        {
            bool faceUp = false;
            for (int i = 0; i < pile.Count; i++)
            {
                if (faceUp && (!pile[i].FaceUp || pile[i].Red == pile[i - 1].Red || pile[i].Rank != pile[i - 1].Rank - 1))
                    throw new InvalidDataException("The saved tableau is invalid.");
                faceUp |= pile[i].FaceUp;
            }
        }
    }
}
