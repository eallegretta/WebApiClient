using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace WebApiClient.Consumer
{
    public class TextPlainMediaTypeFormatter: MediaTypeFormatter
    {
        public TextPlainMediaTypeFormatter()
        {
            base.SupportedMediaTypes.Add(new MediaTypeHeaderValue("text/plain"));
        }

        public override bool CanReadType(Type type)
        {
            return true;
        }

        public override bool CanWriteType(Type type)
        {
            return true;
        }

        public override System.Threading.Tasks.Task<object> ReadFromStreamAsync(Type type, System.IO.Stream readStream, System.Net.Http.HttpContent content, IFormatterLogger formatterLogger)
        {
            return Task.Factory.StartNew<object>(() =>
                {
                    using (var reader = new StreamReader(readStream))
                    {
                        return reader.ReadToEnd();
                    }
                });
        }

        public override Task WriteToStreamAsync(Type type, object value, Stream writeStream, System.Net.Http.HttpContent content, System.Net.TransportContext transportContext)
        {
            return Task.Factory.StartNew(() =>
                {
                    using (var streamWriter = new StreamWriter(writeStream))
                    {
                        streamWriter.Write(value != null ? value.ToString() : string.Empty);
                    }
                });
        }
    }
}
