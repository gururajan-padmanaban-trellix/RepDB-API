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


            DateTime currentDate = DateTime.Now;

            string startDate = "2024-01-31";
            string endDate = currentDate.ToString("yyyy-MM-dd");
            string signatureName = "HEUR/UsesXorEncryption.1";
            string engine = "mcafee-raiden-eng";

            ApiClient apiClient = new ApiClient();
            
            string classificationtypeid = Task.Run(() => apiClient.GetClassificationTypeId("aff31f48e6fd29102554973967a29a44").Result).Result;
            Console.WriteLine($"Classification Type ID:{classificationtypeid}");

            //Dictionary<string, object> result = await apiClient.GetChimeraDriverHitsByVendor(startDate, endDate, signatureName, engine);
            //Dictionary<string, object> result = Task.Run(() => apiClient.GetChimeraDriverHitsByVendor(startDate, endDate, signatureName, engine).Result).Result;
            //Console.WriteLine($"Response:{JsonConvert.SerializeObject(result)}");

            //List<string> md5hashes = (List<string>)result["md5List"];
            //List<SampleProperty> classificationData = await apiClient.GetBulkHashDetailsV2(md5hashes);
            //Console.WriteLine($"Response:{JsonConvert.SerializeObject(classificationData[0])}");
            //foreach (SampleProperty item in classificationData)
            //{
            //    if (!md5hashes.Contains(item.MD5))
            //    {
            //        Console.WriteLine(item.MD5);
            //    }
            //}


            //List<SampleProperty> hashDetails = await apiClient.GetRelatedSamplesV2("aff31f48e6fd29102554973967a29a44");
            //Console.WriteLine($"Response:{JsonConvert.SerializeObject(hashDetails[0])}");
        }
    }
}


