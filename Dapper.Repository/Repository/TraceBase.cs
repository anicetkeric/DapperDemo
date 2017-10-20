using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace Dapper.Repository.Repository
{
    public abstract class TraceBase
    {
        protected void Trace(string message, string category, params object[] args)
        {
            if (HttpContext.Current != null)
            {
                if (HttpContext.Current.Trace.IsEnabled)
                {
                    HttpContext.Current.Trace.Write(category, String.Format(message, args));
                }
            }
            else
            {
                // do some other tracing maybe
            }
        }
    }
}
