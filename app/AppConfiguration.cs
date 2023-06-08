namespace App
{
    public class AppConfiguration
    {
        public readonly string OpenAIServiceName;
        public readonly string OpenAIServiceKey;
        public readonly string OpenAIServiceDeployName;

        public AppConfiguration(IConfiguration config)
        {
            OpenAIServiceName = config["OPENAI_SERVICE_NAME"];
            OpenAIServiceKey = config["OPENAI_SERVICE_KEY"];
            OpenAIServiceDeployName = config["OPENAI_SERVICE_DEPLOY_NAME"];
        }
    }
}
