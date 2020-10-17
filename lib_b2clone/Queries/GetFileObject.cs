using System.IO;
using b2clone_lib.Models;
using Memstate;

namespace b2clone_lib.Queries
{
    public class GetFileObject : Query<DbRepo, FileObject>
    {
        private string FilePath { get; }
        
        public GetFileObject(string filePath)
        {
            this.FilePath = filePath;
        }

        public override FileObject Execute(DbRepo model)
        {
            return model.Files.ContainsKey(this.FilePath) ? model.Files[this.FilePath] : null;
        }
    }
}