using System;
using System.Collections.Generic;
using System.Text;

namespace JA_GaussianBlurProj
{
    public interface IErrorHandler
    {
        void HandleError(Exception ex);
    }
}
