using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
namespace PS5SavefileBatcher;
// Scriptable diagnostics use the same connection and batch engine as the GUI.
public static class DumperCli
{
    public static void Run(string[] args)
    {
        string output = args.Length>1 ? args[1] : Path.Combine(AppContext.BaseDirectory,"diagnostic-result.json");
        try
        {
            if(args[0]=="--smoke")
            {
                using var form=new DumperForm();
                form.CreateControl();
                // Exercise the native SQLite library after single-file extraction too.
                using var db=new Microsoft.Data.Sqlite.SqliteConnection("Data Source=:memory:");
                db.Open();
                using var command=db.CreateCommand();
                command.CommandText="SELECT sqlite_version()";
                File.WriteAllText(output,"GUI construction OK; SQLite "+command.ExecuteScalar()+"; runtime "+Environment.Version);
                return;
            }
            if(args.Length<3)throw new ArgumentException("Usage: --inventory|--dump result.json host [userHex game save1,save2 destination]");
            DumperSettings s;
            try { s=JsonSerializer.Deserialize<DumperSettings>(File.ReadAllText(Path.Combine(AppContext.BaseDirectory,"settings.json"))) ?? new(); }
            catch { s=new(); }
            s.Host=args[2];
            using var client=DumperConnection.Connect(s,s.SendPayload,_=>{});
            var users=client.GetUsers();
            if(args[0]=="--inventory")
            {
                var inventory=users.Select(u=>new { UserId=u.id.ToString("x"),u.name,Games=client.ListSaves(u.id).Select(g=>new { Id=g,Saves=client.Search(u.id,g).Select(v=>new { v.DirName,v.Title,v.Subtitle }) }).ToArray() }).ToArray();
                File.WriteAllText(output,JsonSerializer.Serialize(inventory,DumpRunner.JsonOptions));
            }
            else if(args[0]=="--dump"&&args.Length==7)
            {
                int uid=Convert.ToInt32(args[3],16);
                var found=client.Search(uid,args[4]);
                var names=args[5].Split(',');
                if(names.Any(n=>!found.Any(f=>f.DirName==n)))throw new ArgumentException("Requested save not found");
                var jobs=names.Select(n=>new SaveJob(uid,args[4],n)).ToArray();
                var report=new DumpRunner(new PayloadMount(client),new SaveFtp(s.Host,s.TransferPort(client.EmbeddedFtpPort))).Run(jobs,args[6],_=>{},(_,_)=>{},CancellationToken.None,
                    new DumpOptions { UserInRunName=true, ConsoleUserName=users.First(u=>u.id==uid).name });
                File.WriteAllText(output,JsonSerializer.Serialize(report,DumpRunner.JsonOptions));
                if(report.Status!="Complete")Environment.ExitCode=1;
            }
            else throw new ArgumentException("Unknown command or argument count");
            client.Disconnect();
        }
        catch(Exception ex){File.WriteAllText(output,ex.ToString());Environment.ExitCode=1;}
    }
}
