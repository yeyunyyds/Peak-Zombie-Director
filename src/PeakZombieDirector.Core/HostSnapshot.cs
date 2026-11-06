namespace PeakZombieDirector.Core;
// Photon is deliberately kept out of this parser so sender checks are testable.
public sealed class HostSnapshot
{
    public DirectorConfig Config {get;private set;}=new();
    public Difficulty Difficulty {get;private set;}
    public int Alive {get;private set;}
    public double Elapsed {get;private set;}
    public double NextWave {get;private set;}
    public string Status {get;private set;}="";
    public int[] Owned {get;private set;}=Array.Empty<int>();
    public static bool TryRead(int sender,int master,bool localIsHost,object? payload,string guid,string version,out HostSnapshot? result,out string error)
    {
        result=null;error="";
        if(localIsHost || master<=0 || sender!=master) return false;
        if(!(payload is object[] data) || data.Length!=9 || !(data[0] is string id) || id!=guid) return false;
        if(!(data[1] is string v) || v!=version) {error="Host plugin version mismatch";return false;}
        if(!(data[2] is string text) || text.Length>16000 || !(data[3] is int difficulty) || !Enum.IsDefined(typeof(Difficulty),difficulty) || !(data[4] is int alive) || alive<0 || !(data[5] is double elapsed) || !Finite(elapsed) || !(data[6] is double next) || !Finite(next) || !(data[7] is string status) || status.Length>256 || !(data[8] is int[] owned) || owned.Length>20000 || owned.Any(x=>x<=0)) return false;
        result=new HostSnapshot {Config=ConfigCodec.Decode(text),Difficulty=(Difficulty)difficulty,Alive=alive,Elapsed=elapsed,NextWave=next,Status=status,Owned=owned.ToArray()};return true;
    }
    static bool Finite(double n)=>n>=0 && !double.IsNaN(n) && !double.IsInfinity(n);
}
