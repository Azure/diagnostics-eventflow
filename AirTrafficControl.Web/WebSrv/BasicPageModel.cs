using Microsoft;
using Nancy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Validation;

namespace AirTrafficControl.Web.WebSrv
{
    internal class BasicPageModel
    {
        protected Request request;

        public BasicPageModel(Request request)
        {
            Requires.NotNull(request, "request");
            this.request = request;
        }

        public string ApplicationRoot
        {
            get
            {
                var basePath = this.request.Url.BasePath;
                return (basePath == null) ? "/" : basePath.TrimStart('/');
            }
        }
    }
}
