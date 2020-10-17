using b2clone_lib.Models;
using Memstate;

namespace b2clone_lib.Commands
{
    public class UpdateFileObject : Command<DbRepo,FileObject>
    {
        private FileObject FileObject { get; }
        
        public UpdateFileObject(FileObject fileObject)
        {
            this.FileObject = fileObject;
        }
        
        public override FileObject Execute(DbRepo model)
        {
            model.Files[FileObject.FilePath] = this.FileObject;
            return this.FileObject;
        }
    }
}