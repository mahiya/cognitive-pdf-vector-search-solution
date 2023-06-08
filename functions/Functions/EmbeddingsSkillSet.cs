using Azure;
using Azure.AI.OpenAI;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Functions
{
    public class EmbeddingsSkillSet
    {
        readonly OpenAIClient _openAIClient;
        readonly string _openAIDeployName;

        public EmbeddingsSkillSet(
            FunctionConfiguration config,
            OpenAIClient openAIClient)
        {
            _openAIClient = openAIClient;
            _openAIDeployName = config.OpenAIServiceDeployName;
        }

        [FunctionName(nameof(EmbeddingsSkillSet))]
        public async Task<IActionResult> RunAsync(
            [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req,
            ILogger log)
        {
            // Azure Cognitive Search からの入力を読み込む
            var body = await new StreamReader(req.Body).ReadToEndAsync();
            log.LogInformation("Input from Azure Cognitive Search Skill Set:");
            log.LogInformation(body);

            // Azure Cognitive Search からの入力(JSON)を解析する
            var inputValues = JsonConvert.DeserializeObject<CustomSkillRequest>(body).Values;

            // 出力を生成する何かしらの処理を行う (レコードIDごとに)
            var outputValues = new List<OutputValue>();
            foreach (var inputValue in inputValues)
            {
                var embeddings = await GetEmbeddingsAsync(inputValue.Data.Input);
                var outputValue = new OutputValue
                {
                    RecordId = inputValue.RecordId,
                    Data = new OutputsData { Output = embeddings }
                };
                outputValues.Add(outputValue);
            }

            // 処理結果を Output として返す
            return new OkObjectResult(new CustomSkillResponse { Values = outputValues });
        }

        async Task<float[]> GetEmbeddingsAsync(string text)
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
