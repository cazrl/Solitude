using System.Windows.Forms.Automation;

namespace Solitude;

public sealed partial class GameWindow
{
    private string? orbitAccessibleFocus;
    protected override AccessibleObject CreateAccessibilityInstance()=>new OrbitAccessibleRoot(this,this,false);
    private string OrbitPileName(Position p)
    {
        var pile=Game.Pile(p)!;
        return p.Kind switch
        {
            PileKind.Stock=>$"Stock, {pile.Count} cards. "+(pile.Count>0?"Draw.":Game.CanRecycle?"Recycle.":"Empty."),
            PileKind.Waste=>pile.Count==0?"Waste, empty.":$"Waste, {pile[^1].Name}.",
            PileKind.Foundation=>$"Foundation {p.Pile+1}, "+(pile.Count==0?"empty.":pile[^1].Name+"."),
            PileKind.FreeCell=>$"Free cell {p.Pile+1}, "+(pile.Count==0?"empty.":pile[^1].Name+"."),
            _=>$"Column {p.Pile+1}, {pile.Count} cards, {pile.Count(c=>!c.FaceUp)} hidden. "+(pile.Count==0?"Empty.":pile[^1].FaceUp?pile[^1].Name+" on top.":"Face-down card on top.")
        };
    }
    private void AnnounceOrbit(string text)
    {
        AccessibleDescription=text;
        if(ephemeral || !IsHandleCreated)return;
        var root=dialogHost?.AccessibilityObject??AccessibilityObject;
        try
        {
            root.RaiseAutomationNotification(AutomationNotificationKind.Other,AutomationNotificationProcessing.MostRecent,text);
            AccessibilityNotifyClients(AccessibleEvents.Reorder,-1);
        }
        catch(System.Runtime.InteropServices.COMException){ }
    }
    private void ActivateOrbitPosition(Position p)
    {
        if(editionMorph!=null || dialog!=DialogPage.None || showingVictory)return;
        if(p.Kind==PileKind.Stock){DrawCards();return;}
        if(selection is {} from)
        {
            from=SelectedMove(from,p);
            if(Game.CanMove(from,p))
            {if(!RequestFreeCellColumnMove(from,p))Changed(Game.Move(from,p));return;}
        }
        if(p.Kind==PileKind.Tableau && Game.Flip(p.Pile)){Changed(true);return;}
        if(ClassicFreeCell && p.Kind==PileKind.Tableau && p.Index<0)p=FreeCellSelection(p.Pile);
        if(Game.CanPick(p)){selection=p;AnnounceOrbit(Game.Pile(p)![Game.Index(p)].Name+" selected. Choose a destination.");}
        else AnnounceOrbit(selection.HasValue?"That destination is not allowed.":OrbitPileName(p));
        Invalidate();
    }
    private void PaintOrbitAccessibleFocus(Graphics g)
    {
        if(orbitAccessibleFocus==null || dialog!=DialogPage.None)return;
        var item=OrbitAccessItems(false).FirstOrDefault(i=>i.Key==orbitAccessibleFocus);
        if(item is {} focused && focused.Bounds.Width>0 && focused.Bounds.Height>0)
        {using var pen=new Pen(Color.White,1){DashStyle=System.Drawing.Drawing2D.DashStyle.Dot};var r=focused.Bounds;g.DrawRectangle(pen,r.X,r.Y,r.Width,r.Height);}
    }
    private sealed record OrbitAccessItem(string Key,string Name,RectangleF Bounds,AccessibleRole Role,bool Enabled,bool Selected,Action Action,Position? Position=null);
    private RectangleF AccessiblePileBounds(Position p)
    {
        if(p.Kind==PileKind.FreeCell)return CellRect(p.Pile,false);
        if(p.Kind==PileKind.Foundation && Kind==GameKind.FreeCell)return CellRect(p.Pile,true);
        if(p.Kind!=PileKind.Tableau)return FuturePosition(p);
        var first=TableauCard(p.Pile,0);var last=TableauCard(p.Pile,Math.Max(0,Game.Pile(p)!.Count-1));
        return RectangleF.Intersect(RectangleF.Union(first,last),new(Table.Left,TableauY,Table.Width,TableauBottom-TableauY+6));
    }
    private List<OrbitAccessItem> OrbitAccessItems(bool modal)
    {
        var items=new List<OrbitAccessItem>();if(skin==null || editionMorph!=null)return items;
        foreach(var h in hotspots)
        {
            bool isDialog=h.Id.StartsWith("dialog-") || h.Id=="modal-close";
            if(dialog!=DialogPage.None ? !isDialog : modal || (menu>=0 && !h.Id.StartsWith("menu-") && h.Id!="future-menu"))continue;
            string name=h.Label??h.Id switch{"modal-close" or "close"=>"Close","system"=>"Window menu","maximize"=>"Maximize or restore","minimize"=>"Minimize","settings"=>"Settings","bar-0"=>"Game menu","bar-1"=>"Help menu","bar-3"=>"Deal","spider-score"=>"Hint","dialog-number"=>"Game number","dialog-help-keyword"=>"Help keyword",_=>h.Id.Replace("dialog-","").Replace('-', ' ')};
            if(h.Id is "dialog-volume-down" or "dialog-volume-up")name=(h.Id.EndsWith("down")?"Decrease":"Increase")+$" sound volume. Current volume {draft?.FutureVolume??Preferences.FutureVolume} percent.";
            var role=h.Id is "dialog-number" or "dialog-help-keyword"?AccessibleRole.Text:h.Role;
            items.Add(new(h.Id,name,h.Bounds,role,h.Enabled,h.Checked,h.Action));
        }
        if(dialog!=DialogPage.None)
        {
            for(int i=0;i<dialogReadText.Count;i++)
            {var text=dialogReadText[i];items.Add(new("text-"+i,text.Text,text.Bounds,AccessibleRole.StaticText,true,false,()=>{}));}
            return items;
        }
        if(modal || menu>=0)return items;
        foreach(var p in KeyboardPiles())
        {
            var position=p;var rect=AccessiblePileBounds(p);
            items.Add(new("pile-"+p.Kind+"-"+p.Pile,OrbitPileName(p),rect,AccessibleRole.List,!showingVictory && !Game.State.Won && !Game.State.Lost,false,()=>ActivateOrbitPosition(position),position));
            if(p.Kind!=PileKind.Tableau)continue;
            var pile=Game.Pile(p)!;
            for(int i=0;i<pile.Count;i++)
            {
                if(!pile[i].FaceUp)continue;var cardPosition=p with{Index=i};var card=pile[i];
                var r=TableauCard(p.Pile,i);if(i<pile.Count-1)r.Height=TableauCard(p.Pile,i+1).Y-r.Y;r=RectangleF.Intersect(r,rect);
                items.Add(new("card-"+card.Key,card.Name+$", column {p.Pile+1}, {pile.Count-i} cards from here",r,AccessibleRole.ListItem,!showingVictory && Game.CanPick(cardPosition),selection==cardPosition,()=>ActivateOrbitPosition(cardPosition),cardPosition));
            }
        }
        return items;
    }
    private sealed class OrbitAccessibleRoot : ControlAccessibleObject
    {
        internal readonly GameWindow Game;
        private readonly Control surface;
        private readonly bool modal;
        private readonly Dictionary<string,OrbitAccessibleChild> children=[];
        internal OrbitAccessibleRoot(Control surface,GameWindow game,bool modal):base(surface){this.surface=surface;Game=game;this.modal=modal;}
        internal List<OrbitAccessItem> Items=>Game.OrbitAccessItems(modal);
        private bool Active=>Game.skin!=null;
        public override string? Name{get=>Active?(modal?surface.AccessibleName??surface.Text:Game.skin.Future?"ORBIT Solitaire":Game.GameTitle):base.Name;set=>base.Name=value;}
        public override string? Description=>Game.dialog!=DialogPage.None?string.Join("\n",Game.dialogReadText.Select(t=>t.Text)):Game.AccessibleDescription??$"{Game.StateSummary}";
        public override AccessibleRole Role=>Active?AccessibleRole.Client:base.Role;
        public override int GetChildCount()=>Active?Items.Count:base.GetChildCount();
        public override AccessibleObject? GetChild(int index)
        {
            if(!Active)return base.GetChild(index);var items=Items;if(index<0 || index>=items.Count)return null;
            string key=items[index].Key;if(!children.TryGetValue(key,out var child))children[key]=child=new(this,key);return child;
        }
        public override AccessibleObject? GetFocused()
        {
            if(!Active)return base.GetFocused();var items=Items;
            int index=items.FindIndex(i=>i.Key==Game.orbitAccessibleFocus);
            if(Game.dialog!=DialogPage.None && Game.keyboardFocus>=0)
            {var controls=Game.hotspots.Where(h=>h.Enabled && h.Id.StartsWith("dialog-")).ToList();if(Game.keyboardFocus<controls.Count)index=items.FindIndex(i=>i.Key==controls[Game.keyboardFocus].Id);}
            return index>=0?GetChild(index):this;
        }
        public override AccessibleObject? GetSelected(){if(!Active)return base.GetSelected();var items=Items;int i=items.FindIndex(x=>x.Selected);return i>=0?GetChild(i):null;}
        public override AccessibleObject? HitTest(int x,int y){var items=Items;for(int i=items.Count-1;i>=0;i--)if(ScreenBounds(items[i].Bounds).Contains(x,y))return GetChild(i);return base.HitTest(x,y);}
        public override AccessibleObject? Navigate(AccessibleNavigation direction)=>direction==AccessibleNavigation.FirstChild?GetChild(0):direction==AccessibleNavigation.LastChild?GetChild(GetChildCount()-1):base.Navigate(direction);
        internal Rectangle ScreenBounds(RectangleF r)
        {
            if(r.Width<=0 || r.Height<=0)return Rectangle.Empty;
            float scale=Game.ScaleFactor;var bounds=Game.dialogBounds;PointF origin=modal?bounds.Location:PointF.Empty;
            var rect=Rectangle.Ceiling(new RectangleF((r.X-origin.X)*scale,(r.Y-origin.Y)*scale,r.Width*scale,r.Height*scale));return surface.RectangleToScreen(rect);
        }
    }
    private sealed class OrbitAccessibleChild(OrbitAccessibleRoot root,string key):AccessibleObject
    {
        private OrbitAccessItem? Item=>root.Items.FirstOrDefault(i=>i.Key==key);
        public override AccessibleObject Parent=>root;
        public override string? Name{get=>Item?.Name??"Unavailable";set{}}
        public override string? Description=>Item?.Position is {} p?root.Game.OrbitPileName(p):Name;
        public override AccessibleRole Role=>Item?.Role??AccessibleRole.None;
        public override Rectangle Bounds=>Item is {} i?root.ScreenBounds(i.Bounds):Rectangle.Empty;
        public override string DefaultAction=>Item?.Role==AccessibleRole.StaticText?"":Item?.Role is AccessibleRole.CheckButton or AccessibleRole.RadioButton?"Choose":"Activate";
        public override AccessibleStates State=>Item is {} i?(i.Role==AccessibleRole.StaticText?AccessibleStates.ReadOnly:i.Enabled?AccessibleStates.Focusable:AccessibleStates.Unavailable)|(i.Selected?(i.Role is AccessibleRole.CheckButton or AccessibleRole.RadioButton?AccessibleStates.Checked:AccessibleStates.Selected):AccessibleStates.None)|(root.Game.orbitAccessibleFocus==key?AccessibleStates.Focused:AccessibleStates.None)|(Bounds.IsEmpty?AccessibleStates.Offscreen:AccessibleStates.None):AccessibleStates.Unavailable;
        public override void DoDefaultAction(){if(Item is {Enabled:true,Role:not AccessibleRole.StaticText} i){Select(AccessibleSelection.TakeFocus);i.Action();root.Game.Invalidate();}}
        public override void Select(AccessibleSelection flags)
        {
            if(Item is not {} i || !i.Enabled || i.Role==AccessibleRole.StaticText)return;root.Game.orbitAccessibleFocus=key;
            if(i.Position is {} p){int index=root.Game.KeyboardPiles().FindIndex(v=>v.Kind==p.Kind && v.Pile==p.Pile);if(index>=0)root.Game.keyboardPile=index;}
            else {var controls=root.Game.hotspots.Where(h=>h.Enabled && h.Id.StartsWith("dialog-")).ToList();root.Game.keyboardFocus=controls.FindIndex(h=>h.Id==key);}
            if((flags&AccessibleSelection.TakeSelection)!=0 && i.Position is {} position && root.Game.Game.CanPick(position))root.Game.selection=position;
            root.Game.AccessibilityNotifyClients(AccessibleEvents.Focus,root.Items.FindIndex(x=>x.Key==key));root.Game.Invalidate();
        }
        public override AccessibleObject? Navigate(AccessibleNavigation direction)
        {
            int index=root.Items.FindIndex(i=>i.Key==key);
            return direction switch{AccessibleNavigation.Next or AccessibleNavigation.Down or AccessibleNavigation.Right=>root.GetChild(index+1),AccessibleNavigation.Previous or AccessibleNavigation.Up or AccessibleNavigation.Left=>root.GetChild(index-1),_=>null};
        }
        public override string? Value
        {
            get=>key switch{"dialog-number"=>root.Game.dealText,"dialog-help-keyword"=>root.Game.helpKeyword,_=>base.Value};
            set
            {
                if(Item is not {Enabled:true})return;
                if(key=="dialog-number" && value is {Length:<=7} && (value.All(char.IsAsciiDigit) || root.Game.ClassicFreeCell && value is "-1" or "-2"))root.Game.dealText=value;
                else if(key=="dialog-help-keyword" && value is {Length:<=80})root.Game.helpKeyword=value;
                root.Game.Invalidate();
            }
        }
    }
    private string StateSummary=>$"{GameTitle}. Score {Game.State.Score}. {Game.State.Elapsed} seconds. {Game.State.Moves} moves. {Game.State.Foundations.Sum(p=>p.Count)} cards in foundations.";
}
