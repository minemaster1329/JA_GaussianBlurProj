using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace JA_GaussianBlurProj
{
    public static class TaskUtilities
    {
        #pragma warning disable RECS0165
        public static async void FireAndForgetSafeAsync(this Task task, IErrorHandler handler = null)
        #pragma warning restore RECS0165
        {
            try
            {
                await task;
            }
            catch (Exception e)
            {
                handler?.HandleError(e);
            }
        }
    }
}
