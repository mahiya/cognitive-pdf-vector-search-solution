using Microsoft.Extensions.Configuration;

namespace Functions
{
    public class FunctionConfiguration
    {
        public readonly string StorageAccountName;
        public readonly string DestinationStorageContainerName;

        public readonly string ComputerVisionServiceName;
        public readonly string ComputerVisionServiceKey;

        public readonly string CognitiveSearchName;
        public readonly string CognitiveSearchApiKey;
        public readonly string CognitiveSearchIndexerName;

        public readonly string OpenAIServiceName;
        public readonly string OpenAIServiceKey;
        public readonly string OpenAIServiceDeployName;


        public FunctionConfiguration(IConfiguration config)
        {
            StorageAccountName = config["STORAGE_ACCOUNT_NAME"];
            DestinationStorageContainerName = config["DEST_STORAGE_CONTAINER_NAME"];

            ComputerVisionServiceName = config["COMPUTER_VISION_NAME"];
            ComputerVisionServiceKey = config["COMPUTER_VISION_KEY"];

            CognitiveSearchName = config["COGNITIVE_SEARCH_NAME"];
            CognitiveSearchApiKey = config["COGNITIVE_SEARCH_API_KEY"];
            CognitiveSearchIndexerName = config["COGNITIVE_SEARCH_INDEXER_NAME"];

            OpenAIServiceName = config["OPENAI_SERVICE_NAME"];
            OpenAIServiceKey = config["OPENAI_SERVICE_KEY"];
            OpenAIServiceDeployName = config["OPENAI_SERVICE_DEPLOY_NAME"];
        }
    }
}
