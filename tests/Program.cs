using Solitude;
using System.Text.Json;

int checks=0, failures=0;
void Test(string name,Action test)
{
    try{test();checks++;Console.WriteLine("PASS "+name);}catch(Exception ex){failures++;Console.WriteLine("FAIL "+name+": "+ex.Message);}
}
void Assert(bool result,string message="Assertion failed"){if(!result)throw new Exception(message);}
string Cards(GameState state)=>string.Join(",",state.Stock.Select(c=>c.Id))+"|"+string.Join(",",state.Waste.Select(c=>c.Id))+"|"+string.Join(";",state.Tableau.Select(p=>string.Join(",",p.Select(c=>$"{c.Id}:{c.FaceUp}"))))+"|"+string.Join(";",state.Foundations.Select(p=>string.Join(",",p.Select(c=>c.Id))));
Game Fixture(List<Card>[]? tableau=null,List<Card>[]? foundations=null,List<Card>? waste=null,Rules? rules=null)
{
    var state=new GameState();
    if(tableau!=null)for(int i=0;i<tableau.Length;i++)state.Tableau[i]=tableau[i];
    if(foundations!=null)for(int i=0;i<foundations.Length;i++)state.Foundations[i]=foundations[i];
    state.Waste=waste??[];state.WasteFan=Math.Min(3,state.Waste.Count);
    var used=state.Waste.Concat(state.Tableau.SelectMany(p=>p)).Concat(state.Foundations.SelectMany(p=>p)).Select(c=>c.Id).ToHashSet();
    state.Stock=Enumerable.Range(0,52).Where(id=>!used.Contains(id)).Select(id=>new Card(id,false)).ToList();
    return Game.Restore(rules??new Rules(),state);
}
Card C(Suit suit,int rank,bool up=true)=>new((int)suit*13+rank-1,up);
Test("500 deals preserve every card and the seven-column deal",()=>
{
    for(int seed=0;seed<500;seed++)
    {
        var game=new Game(new Rules(),seed);Game.Validate(game.State);
        Assert(game.State.Stock.Count==24);
        for(int col=0;col<7;col++){Assert(game.State.Tableau[col].Count==col+1);Assert(game.State.Tableau[col].Count(c=>c.FaceUp)==1);Assert(game.State.Tableau[col][^1].FaceUp);}
    }
});
Test("Deals are repeatable",()=>Assert(Cards(new Game(new(),12345).State)==Cards(new Game(new(),12345).State)));
Test("Draw three reveals exactly three; only top waste is playable",()=>
{
    var game=new Game(new(),4);Assert(game.Draw());Assert(game.State.Stock.Count==21 && game.State.Waste.Count==3 && game.State.WasteFan==3);
    Assert(!game.CanPick(new(PileKind.Waste,0,0)));Assert(game.CanPick(new(PileKind.Waste)));
});
Test("Draw one and complete stock recycling preserve order",()=>
{
    var game=new Game(new(){DrawCount=1},27);var initial=game.State.Stock.Select(c=>c.Id).ToArray();
    for(int i=0;i<24;i++)Assert(game.Draw());Assert(game.CanRecycle);Assert(game.Draw());
    Assert(game.State.Stock.Select(c=>c.Id).SequenceEqual(initial));Assert(game.State.Waste.Count==0);Game.Validate(game.State);
});
Test("Draw-three remainder cannot invent cards",()=>
{
    var game=Fixture(waste:[C(Suit.Clubs,1)]);while(game.State.Stock.Count>3)game.Draw();
    int remaining=game.State.Stock.Count;game.Draw();Assert(game.State.WasteFan==remaining);Game.Validate(game.State);
});
Test("Vegas draw one allows one pass",()=>
{
    var game=new Game(new(){Scoring=Scoring.Vegas,DrawCount=1},8);for(int i=0;i<24;i++)game.Draw();
    Assert(!game.CanRecycle && !game.Draw());Assert(game.State.Score==-52);
});
Test("Vegas draw three allows three passes",()=>
{
    var game=new Game(new(){Scoring=Scoring.Vegas},8);
    for(int pass=0;pass<3;pass++){for(int draw=0;draw<8;draw++)Assert(game.Draw());if(pass<2)Assert(game.Draw());}
    Assert(!game.CanRecycle && !game.Draw());Assert(game.State.Recycles==2);
});
Test("Only Kings can occupy empty tableau columns",()=>
{
    var game=Fixture(tableau:[[C(Suit.Clubs,12)],[C(Suit.Spades,13)]]);
    Assert(!game.Move(new(PileKind.Tableau,0),new(PileKind.Tableau,2)));
    Assert(game.Move(new(PileKind.Tableau,1),new(PileKind.Tableau,2)));
});
Test("Tableau requires alternating colours and descending ranks",()=>
{
    var game=Fixture(tableau:[[C(Suit.Clubs,8)],[C(Suit.Diamonds,9)],[C(Suit.Spades,9)],[C(Suit.Hearts,10)]]);
    Assert(!game.CanMove(new(PileKind.Tableau,0),new(PileKind.Tableau,2)));
    Assert(!game.CanMove(new(PileKind.Tableau,0),new(PileKind.Tableau,3)));
    Assert(game.Move(new(PileKind.Tableau,0),new(PileKind.Tableau,1)));Game.Validate(game.State);
});
Test("Move a full valid sequence and undo without losing cards",()=>
{
    var game=Fixture(tableau:[[C(Suit.Clubs,8),C(Suit.Hearts,7),C(Suit.Spades,6)],[C(Suit.Diamonds,9)]]);
    string before=Cards(game.State);Assert(game.Move(new(PileKind.Tableau,0,0),new(PileKind.Tableau,1)));
    Assert(game.State.Tableau[1].Count==4);Assert(game.Undo());Assert(Cards(game.State)==before);Game.Validate(game.State);
});
Test("Hidden cards and non-top foundations cannot be picked",()=>
{
    var game=Fixture(tableau:[[C(Suit.Hearts,8,false),C(Suit.Clubs,7)]],foundations:[[C(Suit.Spades,1),C(Suit.Spades,2)]]);
    Assert(!game.CanPick(new(PileKind.Tableau,0,0)));Assert(!game.CanPick(new(PileKind.Foundation,0,0)));
});
Test("Foundations require Ace, same suit, ascending order",()=>
{
    var game=Fixture(tableau:[[C(Suit.Hearts,2)],[C(Suit.Hearts,1)],[C(Suit.Clubs,2)]]);
    Assert(!game.Move(new(PileKind.Tableau,0),new(PileKind.Foundation,0)));
    Assert(game.Move(new(PileKind.Tableau,1),new(PileKind.Foundation,0)));
    Assert(!game.Move(new(PileKind.Tableau,2),new(PileKind.Foundation,0)));
    Assert(game.Move(new(PileKind.Tableau,0),new(PileKind.Foundation,0)));Assert(game.State.Score==20);Game.Validate(game.State);
});
Test("Classic exposes a face-down card until clicked",()=>
{
    var game=Fixture(tableau:[[C(Suit.Clubs,8,false),C(Suit.Hearts,1)]]);
    game.ToFoundation(new(PileKind.Tableau,0));Assert(!game.State.Tableau[0][0].FaceUp);
    Assert(game.Flip(0));Assert(game.State.Score==15);Assert(!game.Flip(0));
});
Test("Automatic turning is part of the same undoable move",()=>
{
    var game=Fixture(tableau:[[C(Suit.Clubs,8,false),C(Suit.Hearts,1)]],rules:new(){AutoFlip=true});
    string before=Cards(game.State);game.ToFoundation(new(PileKind.Tableau,0));Assert(game.State.Tableau[0][0].FaceUp && game.State.Score==15);
    game.Undo();Assert(Cards(game.State)==before);
});
Test("Waste to tableau scores five; foundation back costs fifteen",()=>
{
    var game=Fixture(tableau:[[C(Suit.Hearts,9)]],waste:[C(Suit.Clubs,8)]);
    game.Move(new(PileKind.Waste),new(PileKind.Tableau,0));Assert(game.State.Score==5);
    var other=Fixture(tableau:[[C(Suit.Hearts,2)]],foundations:[[C(Suit.Clubs,1)]]);other.State.Score=50;
    Assert(other.Move(new(PileKind.Foundation,0),new(PileKind.Tableau,0)));Assert(other.State.Score==35);
});
Test("Vegas foundation moves gain and return five dollars",()=>
{
    var game=Fixture(tableau:[[C(Suit.Hearts,2)]],waste:[C(Suit.Clubs,1)],rules:new(){Scoring=Scoring.Vegas});
    game.State.Score=-52;game.ToFoundation(new(PileKind.Waste));Assert(game.State.Score==-47);
    game.Move(new(PileKind.Foundation,0),new(PileKind.Tableau,0));Assert(game.State.Score==-52);
});
Test("None scoring stays zero",()=>
{
    var game=Fixture(waste:[C(Suit.Clubs,1)],rules:new(){Scoring=Scoring.None});game.ToFoundation(new(PileKind.Waste));for(int i=0;i<20;i++)game.Tick();Assert(game.State.Score==0);
});
Test("Timer starts after play and keeps elapsed time when undoing",()=>
{
    var game=new Game(new(),3);game.Tick();Assert(game.State.Elapsed==0);game.State.Score=100;game.Draw();for(int i=0;i<20;i++)game.Tick();
    Assert(game.State.Elapsed==20 && game.State.Score==96);game.Undo();Assert(game.State.Elapsed==20 && game.State.Score==94);
});
Test("Untimed game has no timer penalty",()=>
{
    var game=new Game(new(){Timed=false},3);game.State.Score=100;game.Draw();for(int i=0;i<20;i++)game.Tick();Assert(game.State.Elapsed==0 && game.State.Score==100);
});
Test("Safe collection leaves a needed Three on the table",()=>
{
    var game=Fixture(tableau:[[C(Suit.Clubs,3)]],foundations:[[C(Suit.Clubs,1),C(Suit.Clubs,2)]]);
    Assert(!game.ToFoundation(new(PileKind.Tableau,0),true));Assert(game.ToFoundation(new(PileKind.Tableau,0),false));
});
Test("Hint does not peek at hidden card identities",()=>
{
    var game=Fixture(tableau:[[C(Suit.Clubs,8,false)]]);var hint=game.Hint();Assert(hint.HasValue && hint.Value.Text.StartsWith("Turn over"));
});
Test("Undo restores exact stock and waste ordering",()=>
{
    var game=new Game(new(),11);string start=Cards(game.State);game.Draw();game.Draw();game.Undo();game.Undo();Assert(Cards(game.State)==start);
});
Test("A completed legal game is recognized and stops changing",()=>
{
    var foundations=Enum.GetValues<Suit>().Select(s=>Enumerable.Range(1,12).Select(r=>C(s,r)).ToList()).ToArray();
    var tableau=Enum.GetValues<Suit>().Select(s=>new List<Card>{C(s,13)}).ToArray();
    var game=Fixture(tableau,foundations);for(int i=0;i<4;i++)Assert(game.ToFoundation(new(PileKind.Tableau,i)));
    Assert(game.State.Won);Assert(!game.Draw() && !game.Undo());int time=game.State.Elapsed;game.Tick();Assert(game.State.Elapsed==time);Game.Validate(game.State);
});
Test("Malformed saves are rejected",()=>
{
    try{Game.Validate(null!);throw new Exception("Null state accepted");}catch(InvalidDataException){}
    var bad=new Game(new(),11).State.Clone();bad.Stock[0]=bad.Stock[1];
    try{Game.Validate(bad);throw new Exception("Duplicate card accepted");}catch(InvalidDataException){}
    var hidden=Fixture(tableau:[[C(Suit.Clubs,8),C(Suit.Hearts,7)]]).State.Clone();hidden.Tableau[0][1]=hidden.Tableau[0][1] with{FaceUp=false};
    try{Game.Validate(hidden);throw new Exception("Invalid sequence accepted");}catch(InvalidDataException){}
});
Test("Save/reopen preserves preferences, score, cards and undo history",()=>
{
    string dir=Path.GetFullPath("artifacts/test-state/roundtrip");var store=new Store(dir);var game=new Game(new(),998);game.Draw();game.Draw();
    Assert(store.Save(new(){Preferences=new(){Era=Era.Windows31,Scale=200},Game=game.State,History=game.History}));
    var saved=store.Load();Assert(saved.Preferences.Era==Era.Windows31 && saved.Preferences.Scale==200);var restored=Game.Restore(saved.Preferences.Rules,saved.Game!,saved.History);
    Assert(Cards(restored.State)==Cards(game.State));restored.Undo();game.Undo();Assert(Cards(restored.State)==Cards(game.State));
});
Test("Damaged save recovers backup and preserves unreadable input",()=>
{
    string dir=Path.GetFullPath("artifacts/test-state/recovery-"+Guid.NewGuid().ToString("N"));var store=new Store(dir);var game=new Game(new(),99);
    Assert(store.Save(new(){Game=game.State}));game.Draw();Assert(store.Save(new(){Game=game.State}));File.WriteAllText(store.FilePath,"damaged-original");
    var fresh=new Store(dir);var saved=fresh.Load();Assert(fresh.Warning!=null && saved.Game!=null);Assert(fresh.Save(saved));
    var copy=Directory.GetFiles(dir,"*.unreadable-*").Single();Assert(File.ReadAllText(copy)=="damaged-original");
});
Test("200 simulated games preserve invariants (up to 40,000 actions)",()=>
{
    for(int seed=1;seed<=200;seed++)
    {
        var game=new Game(new(){DrawCount=seed%2==0?1:3,AutoFlip=seed%3==0},seed);
        var random=new Random(seed);
        for(int step=0;step<200;step++)
        {
            if(game.State.Won)break;
            if(random.Next(10)==0 && game.CanUndo)game.Undo();
            else
            {
                var hint=game.Hint();
                if(!hint.HasValue)break;
                if(hint.Value.From.Kind==PileKind.Stock)game.Draw();
                else if(hint.Value.From==hint.Value.To)game.Flip(hint.Value.From.Pile);
                else game.Move(hint.Value.From,hint.Value.To);
            }
            Game.Validate(game.State);
        }
    }
});
VariantChecks.Run(Test);
Directory.CreateDirectory("artifacts");
File.WriteAllText("artifacts/game-tests.txt",$"{checks} verification groups passed; {failures} failed.\n");
Console.WriteLine($"\n{checks} verification groups passed; {failures} failed.");
return failures==0?0:1;
