using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebApiClient.Consumer
{
    public interface IQuoter
    {
        string Random();
        Task<string> RandomAsync();
    }

    public class Test
    {
        public void Test1()
        {
            var quoter = ClientFactory.CreateClient<IQuoter>("http://www.iheartquotes.com/api/v1/");

            string quote1 = quoter.Random();
            string quote2 = quoter.RandomAsync().Result;

            Debug.WriteLine(quote1);
            Debug.WriteLine(quote2);
        }
    }
}
