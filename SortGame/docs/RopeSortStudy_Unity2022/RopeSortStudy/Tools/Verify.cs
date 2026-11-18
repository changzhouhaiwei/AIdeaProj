using System;
using System.IO;
using Game.GamePlay.ColorSort;
class Verify {
 static void Main(string[] args){int n=0,invalid=0,moves=0; using(var errors=new StreamWriter(args[1])) foreach(var line in File.ReadLines(args[0])) {
  var parts=line.Split('\t'); try {
   var l=ColorSortLatticeParser.Parse(parts[1],null); if(!l.Validate(out string err)){invalid++;errors.WriteLine(parts[0]+"\t"+err);continue;}
   var s=ColorSortRules.MakeState(l); if(n%100==0){var ms=new System.Collections.Generic.List<ColorSortHintMove>();ColorSortRules.CollectMoves(s,ms);
    foreach(var m in ms){var snap=ColorSortRules.Snapshot(s);string key=ColorSortRules.StateKey(s);var rec=ColorSortRules.ApplyMove(s,l,m.Src,m.J,m.Dst);if(rec==null)throw new Exception("listed move rejected");ColorSortRules.UndoMove(s,rec);if(ColorSortRules.StateKey(s)!=key)throw new Exception("undo mismatch");ColorSortRules.Restore(s,snap);moves++;}
   } n++;
  }catch(Exception e){invalid++;errors.WriteLine(parts[0]+"\t"+e.Message);}
 } Console.WriteLine("Validated="+n+" Invalid="+invalid+" MoveUndoChecks="+moves); if(invalid>0)Environment.ExitCode=1;
 }
}
