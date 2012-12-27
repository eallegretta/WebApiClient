using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Formatting;
using System.Text;

namespace WebApiClient.Consumer
{
    public static class GlobalConfiguration
    {
        static GlobalConfiguration()
        {
            LowercaseUris = true;
            MediaTypeFormatters = new MediaTypeFormatter[] { new JsonMediaTypeFormatter(), new XmlMediaTypeFormatter(), new FormUrlEncodedMediaTypeFormatter(), new TextPlainMediaTypeFormatter() };
        }

        public static bool LowercaseUris { get; set; }

        public static IEnumerable<MediaTypeFormatter> MediaTypeFormatters { get; set; }
    }
}
