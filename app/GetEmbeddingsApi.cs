using App;
using Azure;
using Azure.AI.OpenAI;
using Microsoft.AspNetCore.Mvc;

namespace CognitiveSearch.App
{
    [ApiController]
    [Route("api/embeds")]
    public class GetEmbeddingsApi : ControllerBase
    {
        readonly OpenAIClient _openAIClient;
        readonly string _openAIDeployName;

        public GetEmbeddingsApi(AppConfiguration config, OpenAIClient openAIClient)
        {
            _openAIClient = openAIClient;
            _openAIDeployName = config.OpenAIServiceDeployName;
        }

        [HttpGet]
        public async Task<float[]> Get([FromQuery(Name = "t")] string text)
        {
            var tryCount = 0;
            while (true)
            {
                try
                {
                    var resp = await _openAIClient.GetEmbeddingsAsync(_openAIDeployName, new EmbeddingsOptions(text));
                    var embeds = resp.Value.Data[0].Embedding.ToArray();
                    return embeds;
                }
                catch (RequestFailedException e)
                {
                    if (e.Status != 429) throw e;
                    await Task.Delay(500);
                }
                if (++tryCount > 10) throw new Exception("Could not get embeddings using Azure OpenAI Service.");
            }
        }
    }
}