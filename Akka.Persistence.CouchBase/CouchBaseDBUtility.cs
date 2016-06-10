using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Akka.Persistence.CouchBase
{
    static class CouchBaseDBUtility
    {

        public static async Task ForEachAsync<T>(this IEnumerable<T> source, Action<T> processor)
        {
            await Task.Run(()=>{foreach (var item in source)  processor(item);});
        }
    }
}
