using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Formatting;
using System.Text;

namespace WebApiClient.Consumer
{
    public class WebApiMethodAttribute: Attribute
    {
        public WebApiMethodAttribute(string uri = null, string httpMethod = "GET", Type mediaTypeformatterType = null)
        {
            Uri = uri;
            HttpMethod = "GET";
            
            if (mediaTypeformatterType == null)
                mediaTypeformatterType = typeof (JsonMediaTypeFormatter);

            _mediaTypeFormatterType = mediaTypeformatterType;
        }

        private Type _mediaTypeFormatterType;

        public string Uri { get; set; }
        
        public string HttpMethod { get; set; }

        public Type MediaTypeFormatter
        {
            get { return _mediaTypeFormatterType; }
            set
            {
                if (typeof(MediaTypeFormatter).IsAssignableFrom(value))
                    _mediaTypeFormatterType = value;

                throw new Exception("The specified type is not a MediaTypeFormatter");
            }
        }
    }
}
