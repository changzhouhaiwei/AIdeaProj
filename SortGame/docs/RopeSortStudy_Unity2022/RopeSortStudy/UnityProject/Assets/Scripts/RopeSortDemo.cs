using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using Game.GamePlay.ColorSort;

// Desktop/Editor study viewer. Gameplay rules are recovered from the supplied APK.
public sealed class RopeSortDemo : MonoBehaviour
{
    [Serializable] public class Entry { public string key, raw, file; public float difficulty; }
    [Serializable] public class Entries { public Entry[] entries; }
    Entry[] index; int cursor, selected=-1, cut=-1; string search="", status="";
    ColorSortLevel level; ColorSortState state; Vector2 scroll;
    readonly Stack<ColorSortState> history=new Stack<ColorSortState>();
    string loadedFile; Entry[] loaded;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot() { if (FindObjectOfType<RopeSortDemo>()==null) new GameObject("Rope Sort Study").AddComponent<RopeSortDemo>(); }
    void Start() {
        flowerAtlas = Resources.Load<Texture2D>("FlowerAtlas");
        try { index=JsonUtility.FromJson<Entries>(File.ReadAllText(Path.Combine(Application.streamingAssetsPath,"Levels/index.json"))).entries; Load(Array.FindIndex(index, e => e.key == "FIXED/1/1")); }
        catch(Exception e) { status=e.ToString(); }
    }
    void Load(int n) {
        try {
            cursor=(n+index.Length)%index.Length; var row=index[cursor];
            if(loadedFile!=row.file) { loaded=JsonUtility.FromJson<Entries>(File.ReadAllText(Path.Combine(Application.streamingAssetsPath,"Levels",row.file))).entries; loadedFile=row.file; }
            var entry=Array.Find(loaded,x=>x.key==row.key);
            level=ColorSortLatticeParser.Parse(entry.raw,null);
            if(!level.Validate(out string error)) throw new Exception(error);
            state=ColorSortRules.MakeState(level); history.Clear(); selected=cut=-1; status="Drag a bead and the beads hanging below it onto another rope.";
        } catch(Exception e) {state=null;status=e.Message;}
    }
    void Move(int dst) {
        if(selected<0)return;
        var reason=ColorSortRules.RejectReason(state,selected,cut,dst);
        if(reason!=ColorSortReject.None){status=reason.ToString();return;}
        history.Push(ColorSortRules.Snapshot(state));
        ColorSortRules.ApplyMove(state,level,selected,cut,dst); selected=cut=-1;
        status=ColorSortRules.IsWin(state,level)?"WIN":ColorSortRules.IsDead(state,level)?"No continuation detected. Undo or restart.":"Moved";
    }
    Color Tint(int id) { return id<0?new Color(.65f,.65f,.65f):Color.HSVToRGB((id*.113f)%1f,.58f,.95f); }
    void OnGUI() {
        GUI.skin.button.fontSize=16; GUI.skin.label.fontSize=16;
        GUILayout.BeginArea(new Rect(15,15,Screen.width-30,Screen.height-30));
        GUILayout.Label("ROPE SORT / Unity 2022 Study Viewer");
        if(index==null){GUILayout.Label(status);GUILayout.EndArea();return;}
        GUILayout.BeginHorizontal();
        if(GUILayout.Button("Previous"))Load(cursor-1);
        if(GUILayout.Button("Next"))Load(cursor+1);
        if(GUILayout.Button("Restart"))Load(cursor);
        if(GUILayout.Button("Undo")&&history.Count>0){state=history.Pop();selected=cut=-1;status="Undone";}
        if(GUILayout.Button("Cancel")){selected=cut=-1;}
        if(GUILayout.Button("Extra rope")&&state!=null){var snap=ColorSortRules.Snapshot(state);if(ColorSortRules.AddRope(state,level.Capacity)>=0)history.Push(snap);}
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal(); search=GUILayout.TextField(search);
        if(GUILayout.Button("Find key",GUILayout.Width(110))){int at=Array.FindIndex(index,e=>e.key.IndexOf(search,StringComparison.OrdinalIgnoreCase)>=0);if(at>=0)Load(at);else status="Key not found";}
        GUILayout.EndHorizontal();
        GUILayout.Label((cursor+1)+" / "+index.Length+"   "+index[cursor].key+"   Difficulty: "+index[cursor].difficulty);
        GUILayout.Label(status);
        if(state!=null){
            GUILayout.Label("Collected: "+state.Done+" / "+level.GroupsTotal+"    Capacity: "+level.Capacity+"    Queue remaining: "+Math.Max(0,level.Incoming.Count-state.Done));
            DrawRopes(GUILayoutUtility.GetRect(1, 10000, 120, 10000, GUILayout.ExpandHeight(true)));
        }
        GUILayout.EndArea();GUI.backgroundColor=Color.white;
    }
    Vector2 grabOffset;
    void DrawRopes(Rect area)
    {
        Event input = Event.current;
        float spacing = area.width / state.Cols.Count;
        float size = Mathf.Min(64, spacing * .78f, area.height / (level.Capacity + 3));
        Vector2 pointer = input.mousePosition;
        int target = area.Contains(pointer) ? Mathf.FloorToInt((pointer.x - area.x) / spacing) : -1;
        for (int c = 0; c < state.Cols.Count; c++)
        {
            if (state.Gone[c]) continue;
            float x = area.x + spacing * (c + .5f);
            Rect pot = new Rect(x-size*.5f,area.y,size,size*.65f);
            DrawPiece(new Rect(x-size*.065f,pot.yMax,size*.13f,size*level.Capacity),
                new Rect(.218f,.027f,.065f,.448f),Color.white);
            DrawPiece(pot,new Rect(.515f,.55f,.47f,.395f),Tint(state.Jars[c]));
            DrawMark(pot,state.Jars[c]<0?"–":state.Jars[c].ToString());
            bool locked = ColorSortRules.IsLocked(state,c);
            for (int j=0;j<state.Cols[c].Count;j++)
            {
                Rect bead = new Rect(x-size*.46f,area.y+size*(.75f+j),size*.92f,size*.92f);
                if (selected==c && j>=cut) continue;
                DrawBead(bead,state.Cols[c][j]);
                if(input.type==EventType.MouseDown && input.button==0 && bead.Contains(pointer) && !locked)
                {
                    selected=c;cut=j;grabOffset=pointer-bead.position;input.Use();
                }
            }
            if(locked) GUI.Label(new Rect(x-size*.5f,area.y+size*(level.Capacity+1),spacing,size),"LOCK "+state.Locks[c].Count);
        }
        if(selected<0) return;
        for(int j=cut;j<state.Cols[selected].Count;j++)
        {
            Vector2 location=pointer-grabOffset+Vector2.up*(size*(j-cut));
            DrawBead(new Rect(location,new Vector2(size*.92f,size*.92f)),state.Cols[selected][j]);
        }
        if(input.rawType==EventType.MouseUp)
        {
            if(target>=0 && target<state.Cols.Count && target!=selected && !state.Gone[target]) Move(target);
            selected=cut=-1;
            if(input.type!=EventType.Used)input.Use();
        }
    }
    void DrawBead(Rect rect,int value)
    {
        bool hidden=ColorSortLevel.IsHidden(value);
        Rect XSort = hidden ? new Rect(.535f,.035f,.43f,.44f) : new Rect(.018f,.524f,.468f,.452f);
        DrawPiece(rect,XSort,hidden?Color.white:Tint(value));
        DrawMark(rect,hidden?"?":value.ToString());
    }
    Texture2D flowerAtlas;
    GUIStyle markStyle;
    void DrawPiece(Rect target, Rect uv, Color tint)
    {
        Color previous = GUI.color;
        GUI.color = tint;
        if (flowerAtlas != null) GUI.DrawTextureWithTexCoords(target,flowerAtlas,uv,true);
        else GUI.DrawTexture(target,Texture2D.whiteTexture);
        GUI.color = previous;
    }
    void DrawMark(Rect rect, string text)
    {
        if (markStyle == null)
        {
            markStyle = new GUIStyle(GUI.skin.label);
            markStyle.alignment = TextAnchor.MiddleCenter;
            markStyle.fontStyle = FontStyle.Bold;
            markStyle.normal.textColor = new Color(.13f,.09f,.14f);
        }
        markStyle.fontSize = Mathf.Clamp(Mathf.RoundToInt(rect.height*.32f),10,20);
        GUI.Label(rect,text,markStyle);
    }
}
