using Avert.Automation.Support.HBGCommon;
using McAfeeLabs.Automation.Component.HBGDriverEval;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace McAfeeLabs.Automation.Component.HBGController
{
    public class HashProperty
    {
        public string MD5 { get; set; }
        public string ClassificationName { get; set; }
        public int? ClassificationTypeId { get; set; }
        public string ClassificationType { get; set; }
        public string SourceName { get; set; }
        public long? Size { get; set; }

        public HashProperty(string md5, string cname, int? ctypeid, string ctype, string source, long? size)
        {
            MD5 = md5;
            ClassificationName = cname;
            ClassificationTypeId = ctypeid;
            ClassificationType = ctype;
            SourceName = source;
            Size = size;
        }
    }

    public class ApiClient
    {
        private readonly string rootUrl;
        private readonly string env;
        private readonly string apiKey;
        private readonly string engine;
        private readonly string startDate;
        private readonly string endDate;

        public ApiClient()
        {
            DateTime currentDate = DateTime.Now;

            string secretName = ConfigurationManager.AppSettings["repdb_creds"];
            string secret_region = ConfigurationManager.AppSettings["repdb_creds_region"];
            var awsSecretManagerHelper = new AWSSecretManagerHelper(secret_region);
            string secretValue = Task.Run(() => awsSecretManagerHelper.GetSecret(secretName).Result).Result;
            var secretObject = JObject.Parse(secretValue);

            rootUrl = secretObject["base-url"]?.ToString();
            env = ConfigurationManager.AppSettings["env"];
            apiKey = secretObject["rep-auth-key"]?.ToString();
            engine = ConfigurationManager.AppSettings["engine"];
            startDate = ConfigurationManager.AppSettings["detection_start_date"];
            endDate = currentDate.ToString("yyyy-MM-dd");
        }

        public async Task<string> GetClassificationTypeId(string md5)
        {
            DataAccessHBGeneric_CType classificationtype;
            string classificationtypeid = string.Empty;
            string url = $"{rootUrl}/file-classification/v1/md5/{md5}";

            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("rep-auth-key", apiKey);
                try
                {
                    HttpResponseMessage response = await client.GetAsync(url);
                    string responseBody = await response.Content.ReadAsStringAsync();
                    dynamic parsedResponse = JsonConvert.DeserializeObject(responseBody);
                    classificationtypeid = parsedResponse.classificationtypeid;
                    classificationtype = (DataAccessHBGeneric_CType)Enum.Parse(typeof(DataAccessHBGeneric_CType), classificationtypeid);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                    return null;
                }
                return classificationtype.ToString();
            }
        }

        public async Task<Dictionary<string, object>> GetChimeraDriverHitsByVendor(string signatureName)
        {

            //Console.WriteLine($"{rootUrl} - {signatureName}");
            int offset = 0;
            int limit = 49;//max 49
            int totalCount = 0;

            Dictionary<string, object> result = new Dictionary<string, object>();
            List<string> md5List = new List<string>();

            do
            {
                string apiUrl;
                if (!string.IsNullOrEmpty(engine))
                {

                    apiUrl = $"{rootUrl}/detection/search/auto-classification?start_date={startDate}&end_date={endDate}&signature_name={signatureName}&engine={engine}&offset={offset}&limit={limit}";
                }
                else
                {
                    apiUrl = $"{rootUrl}/detection/search/auto-classification?start_date={startDate}&end_date={endDate}&signature_name={signatureName}&offset={offset}&limit={limit}";
                }

                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("rep-auth-key", apiKey);
                    try
                    {

                        HttpResponseMessage response = await client.GetAsync(apiUrl);
                        string responseBody = await response.Content.ReadAsStringAsync();

                        // Parse the response to check the total count and offset
                        dynamic parsedResponse = JsonConvert.DeserializeObject(responseBody);


                        foreach (var item in parsedResponse.items)
                        {
                            string md5 = item.md5;
                            md5List.Add(md5);

                        }
                        totalCount = parsedResponse.total;
                        offset += limit;

                        // Break the loop if all items have been fetched
                        if (offset >= totalCount)
                        {
                            break;
                        }

                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.ToString());
                        return null;
                    }
                }
            } while (true);

            List<string> unique_md5s = md5List.Distinct().ToList();

            Console.WriteLine($"GetChimeraDriverHitsByVendorAPI - Received Hit Count:{totalCount} | Distinct Hit Count:{unique_md5s.Count}");
            result["total"] = unique_md5s.Count;
            result["md5List"] = unique_md5s;
            return result;
        }



        public async Task<List<HashProperty>> GetBulkHashDetailsV2(List<string> md5Hashes)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    List<HashProperty> results = new List<HashProperty>();
                    List<string> unique_hashes = md5Hashes.Distinct().ToList();
                    string url = $"{rootUrl}/bulk-hash-details/v2";
                    int max_hash_count = 200;
                    int batch_count = (int)Math.Ceiling((double)unique_hashes.Count / max_hash_count);
                    // Console.WriteLine($"Hash Count: {unique_hashes.Count}|Batch Count:{batch_count.ToString()}");

                    for (int batch_index = 0; batch_index < batch_count; batch_index++)
                    {
                        // Console.WriteLine($"Slice|{batch_index * max_hash_count}:{(batch_index * max_hash_count) + max_hash_count}");
                        string jsonBody = $"{{ \"md5\":" +
                                $" {JsonConvert.SerializeObject(unique_hashes.Skip(batch_index * max_hash_count).Take(max_hash_count).ToList())} }}";

                        client.DefaultRequestHeaders.Add("rep-auth-key", apiKey);
                        HttpResponseMessage response = await client.PostAsync(url, new StringContent(jsonBody, null, "application/json"));
                        response.EnsureSuccessStatusCode();

                        string result = await response.Content.ReadAsStringAsync();
                        dynamic parsedResult = JsonConvert.DeserializeObject(result);

                        int count = 0;
                        foreach (var item in parsedResult.data)
                        {
                            string md5 = item.md5;
                            int? classificationTypeId = (int?)item.classificationtypeid;
                            string classificationType = item.classificationtype;
                            string classificationName = item.classificationname;
                            string sourceName = item.recordsource;
                            long? fileSize = item.filesize;
                            HashProperty hashProperty = new HashProperty(md5,
                                classificationName, classificationTypeId, classificationType, sourceName, fileSize);
                            results.Add(hashProperty);
                            count++;
                            //Console.WriteLine(JsonConvert.SerializeObject(hashProperty));
                        }
                        // Console.WriteLine($"Received Data Count:{parsedResult.data.Count} | Parsed Data count:{count}");

                    }
                    return results;
                }
            }
            catch (Exception ex)
            {
                // Handle the error here
                Console.WriteLine($"GetBulkHashDetailsV2 -  Error : {ex.Message}");
                return null;
            }
        }

        public async Task<List<HashProperty>> GetRelatedSamplesV2(string md5)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    List<HashProperty> results = new List<HashProperty>();
                    List<string> md5List = new List<string>();

                    string url = $"{rootUrl}/fingerprint/related-samples/v2";
                    int max_clean_sample_count = 300;
                    int limit = 300; //max = 1000
                    int sleep_for_secs = 60;
                    int retry = 22;

                    // Console.WriteLine($"MD5 : {md5}");
                    string jsonBody = $"{{ \"md5\": \"{md5}\", \"reputation\": {{ \"clean\": {max_clean_sample_count}, \"dirty\": 0, \"assumed_dirty\": 0  }}, \"limit\": {limit} }}";
                    // Console.WriteLine(jsonBody);

                    client.DefaultRequestHeaders.Add("rep-auth-key", apiKey);
                    HttpResponseMessage post_response = await client.PostAsync(url, new StringContent(jsonBody, null, "application/json"));
                    post_response.EnsureSuccessStatusCode();

                    string result = await post_response.Content.ReadAsStringAsync();
                    dynamic parsedResult = JsonConvert.DeserializeObject(result);

                    string request_id = parsedResult.request_id;
                    string getUrl = $"{url}?request_id={request_id}";
                    string status = string.Empty;
                    int attempt = 0;

                    while (attempt < retry)
                    {
                        attempt++;
                        Console.WriteLine($"request_id:{request_id} | Attempt:{attempt}");
                        try
                        {
                            HttpResponseMessage get_response = await client.GetAsync(getUrl);
                            get_response.EnsureSuccessStatusCode();

                            string responseBody = await get_response.Content.ReadAsStringAsync();
                            dynamic parsedResponse = JsonConvert.DeserializeObject(responseBody);
                            status = parsedResponse.status;

                            if (status == "completed")
                            {
                                Console.WriteLine($"GetRelatedSamplesV2API- Related Sample Count:{parsedResponse.data.Count}");
                                foreach (var elm in parsedResponse.data)
                                {
                                    if (!md5List.Contains((string)elm.samplemd5))
                                    {
                                        md5List.Add((string)elm.samplemd5);
                                    }
                                }
                                break;
                            }
                            await Task.Delay(1000 * sleep_for_secs);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine($"GetRelatedSamplesV2 | Error:{e.ToString()}");
                            return null;
                        }
                    }

                    //include parent md5
                    // Console.WriteLine($"Related MD5s Count:{md5List.Count}");
                    md5List.Add(md5);

                    results = await GetBulkHashDetailsV2(md5List);
                    return results;
                }
            }
            catch (Exception ex)
            {
                // Handle the error here
                Console.WriteLine($"GetBulkHashDetailsV2 -  Error : {ex.Message}");
                return null;
            }
        }
    }
}


