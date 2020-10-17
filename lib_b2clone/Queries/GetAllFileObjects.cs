using System.Collections.Generic;
using System.Linq;
using b2clone_lib.Models;
using Memstate;

namespace b2clone_lib.Queries
{
    public class GetAllFileObjects : Query<DbRepo, IDictionary<string, FileObject>>
    {
        public override IDictionary<string, FileObject> Execute(DbRepo model)
        {
            return model.Files;
        }
    }
}