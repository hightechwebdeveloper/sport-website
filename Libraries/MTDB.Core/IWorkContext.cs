
using MTDB.Core.Domain;

namespace MTDB.Core
{
    public interface IWorkContext
    {
        User CurrentUser { get; }
    }
}
