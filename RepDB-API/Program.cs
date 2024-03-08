using RepDB_API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace RepDB_API
{

    internal class Program
    {

        //public static async Task Main(string[] args)
        public static void Main(string[] args)
        {
            ApiClient apiClient = new ApiClient();
            string classificationtypeid = Task.Run(() => apiClient.GetClassificationTypeId("aff31f48e6fd29102554973967a29a44").Result).Result;
            Console.WriteLine($"Classification Type ID:{classificationtypeid}");
        }
    }
}


