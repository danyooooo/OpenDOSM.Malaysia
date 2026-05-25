using System.Collections.Generic;
using System.Threading.Tasks;

namespace OpenDOSM.Malaysia.Services;

public interface IDataCatalogueService
{
    Task<List<T>> GetDatasetAsync<T>(string id, int limit = -1);
}
