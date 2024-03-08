using System.Linq;
using System.Threading.Tasks;
using Amazon;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;

namespace McAfeeLabs.Automation.Component.HBGController
{
    class AWSSecretManagerHelper
    {
        private readonly string _region;

        public AWSSecretManagerHelper(string region)
        {
            _region = region;
        }

        public async Task<string> GetSecret(string secretName)
        {
            var config = new AmazonSecretsManagerConfig
            {
                RegionEndpoint = RegionEndpoint.GetBySystemName(_region)
            };

            using (var client = new AmazonSecretsManagerClient(config))
            {
                var request = new GetSecretValueRequest
                {
                    SecretId = secretName
                };

                var response = await client.GetSecretValueAsync(request);

                if (response.SecretString != null)
                {
                    return response.SecretString;
                }
                else
                {
                    // Handle binary secret
                    return System.Text.Encoding.UTF8.GetString(response.SecretBinary.ToArray());
                }
            };


        }
    }

}

