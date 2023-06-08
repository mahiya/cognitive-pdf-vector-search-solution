#!/bin/bash -e

# 変数を定義する
region='japaneast'    # デプロイ先のリージョン
resourceGroupName=$1  # デプロイ先のリソースグループ (スクリプトの引数から取得する)
pdfsBlobContainerName='pdfs'
ocrResultsBlobContainerName='ocrresults'

# Cognitive Search に関する名前を定義する
cognitiveSearchIndexName="pdf-search"
cognitiveSearchDataSourceName="pdf-search"
cognitiveSearchSkillSetName="pdf-search"
cognitiveSearchIndexerName="pdf-search"

# Azure Functions 関数の環境変数に使用する Azure OpenAI Service 情報を env_openai.json から取得する
openAIServiceName=$(cat env_openai.json | jq '.NAME' | sed "s/\"//g")
openAIServiceKey=$(cat env_openai.json | jq '.KEY' | sed "s/\"//g")
openAIServiceDeployName=$(cat env_openai.json | jq '.DEPLOY_NAME' | sed "s/\"//g")

# リソースグループを作成する
az group create \
    --location $region \
    --resource-group $resourceGroupName

# Azure リソースをデプロイする
outputs=($(az deployment group create \
            --resource-group $resourceGroupName \
            --template-file biceps/deploy.bicep \
            --parameters storageContainerNames=[\"$pdfsBlobContainerName\",\"$ocrResultsBlobContainerName\"] \
            --query 'properties.outputs.*.value' \
            --output tsv))
subscriptionId=`echo ${outputs[0]}` # 文末の \r を削除する
storageAccountName=`echo ${outputs[1]}` # 文末の \r を削除する
functionAppName=`echo ${outputs[2]}` # 文末の \r を削除する
cognitiveSearchName=`echo ${outputs[3]}` # 文末の \r を削除する
computerVisionName=${outputs[4]}

# Cognitive Service の API キーを取得する
computerVisionKey=`az cognitiveservices account keys list --name $computerVisionName --resource-group $resourceGroupName --query 'key1' --output tsv`

# Cognitive Search の API キーを取得する
cognitiveSearchApiKey=`az search admin-key show --service-name $cognitiveSearchName --resource-group $resourceGroupName --query 'primaryKey' --output tsv`

# Azure Functions のアプリケーション設定を設定する
az functionapp config appsettings set \
    --resource-group $resourceGroupName \
    --name $functionAppName \
    --settings "STORAGE_ACCOUNT_NAME=$storageAccountName" \
               "DEST_STORAGE_CONTAINER_NAME=$ocrResultsBlobContainerName" \
               "COMPUTER_VISION_NAME=$computerVisionName" \
               "COMPUTER_VISION_KEY=$computerVisionKey" \
               "COGNITIVE_SEARCH_NAME=$cognitiveSearchName" \
               "COGNITIVE_SEARCH_API_KEY=$cognitiveSearchApiKey" \
               "COGNITIVE_SEARCH_INDEXER_NAME=$cognitiveSearchIndexerName" \
               "OPENAI_SERVICE_NAME=$openAIServiceName" \
               "OPENAI_SERVICE_KEY=$openAIServiceKey" \
               "OPENAI_SERVICE_DEPLOY_NAME=$openAIServiceDeployName"

# Azure Functions のアプリケーションをデプロイする
pushd functions
sleep 10 # Azure Functions App リソースの作成からコードデプロイが早すぎると「リソースが見つからない」エラーが発生する場合があるので、一時停止する
func azure functionapp publish $functionAppName --csharp
popd

# Azure Functions の Cognitive Search のスキルセットで使用する関数のエンドポイント(キー付き)を取得する
functionCode=`az functionapp function keys list \
    --resource-group $resourceGroupName \
    --name $functionAppName \
    --function-name 'EmbeddingsSkillSet' \
    --query "default" \
    --output tsv`
functionApiUri=`echo https://$functionAppName.azurewebsites.net/api/EmbeddingsSkillSet?code=$functionCode`

# EventGrid をデプロイする
az deployment group create \
    --resource-group $resourceGroupName \
    --template-file biceps/post-deploy.bicep \
    --parameters storageAccountName=$storageAccountName \
                 blobContainerName=$blobContainerName \
                 functionAppName=$functionAppName \
                 functionName='ProcessUploadedData'

# 使用する Cognitive Search REST API のバージョンを指定する
cognitiveSearchApiVersion='2023-07-01-Preview'

# Cognitive Search インデックスを作成する
curl -X PUT https://$cognitiveSearchName.search.windows.net/indexes/$cognitiveSearchIndexName?api-version=$cognitiveSearchApiVersion \
    -H 'Content-Type: application/json' \
    -H 'api-key: '$cognitiveSearchApiKey \
    -d @cogsearch/index.json

# Cognitive Search データソースを作成する
curl -X PUT https://$cognitiveSearchName.search.windows.net/datasources/$cognitiveSearchDataSourceName?api-version=$cognitiveSearchApiVersion \
    -H 'Content-Type: application/json' \
    -H 'api-key: '$cognitiveSearchApiKey \
    -d "$(sed -e "s|{{CONNECTION_STRING}}|ResourceId=/subscriptions/$subscriptionId/resourceGroups/$resourceGroupName/providers/Microsoft.Storage/storageAccounts/$storageAccountName;|; \
                  s|{{CONTAINER_NAME}}|$ocrResultsBlobContainerName|;" \
                  "cogsearch/datasource.json")"

# Cognitive Search スキルセットを作成する
curl -X PUT https://$cognitiveSearchName.search.windows.net/skillsets/$cognitiveSearchSkillSetName?api-version=$cognitiveSearchApiVersion \
    -H 'Content-Type: application/json' \
    -H 'api-key: '$cognitiveSearchApiKey \
    -d "$(sed -e "s|{{CUSTOM_WEB_API_URI}}|$functionApiUri|;" \
                  "cogsearch/skillset.json")"

# Cognitive Search インデクサーを作成する
curl -X PUT https://$cognitiveSearchName.search.windows.net/indexers/$cognitiveSearchIndexerName?api-version=$cognitiveSearchApiVersion \
    -H 'Content-Type: application/json' \
    -H 'api-key: '$cognitiveSearchApiKey \
    -d "$(sed -e "s|{{DATASOURCE_NAME}}|$cognitiveSearchDataSourceName|; \
                  s|{{SKILLSET_NAME}}|$cognitiveSearchSkillSetName|; \
                  s|{{INDEX_NAME}}|$cognitiveSearchIndexName|;" \
                  "cogsearch/indexer.json")"

# Cognitive Search へアクセスするためのクエリキーを取得する
cognitiveSearchQueryKey=`az search query-key list --resource-group $resourceGroupName --service-name $cognitiveSearchName --query "[0].key" --output tsv`

# ローカルで動かす .NET Core アプリのために "app/wwwroot/settings.js" ファイルを作成する
sed -e "s/{{NAME}}/$cognitiveSearchName/g" \
    -e "s/{{KEY}}/$cognitiveSearchQueryKey/g" \
    -e "s/{{INDEX_NAME}}/$cognitiveSearchIndexName/g" \
    "app/wwwroot/settings_template.js" > app/wwwroot/settings.js

# ローカルで動かす .NET Core アプリのために local.settings.json を生成する
echo "
{
  \"OPENAI_SERVICE_NAME\": \"$openAIServiceName\",
  \"OPENAI_SERVICE_KEY\": \"$openAIServiceKey\",
  \"OPENAI_SERVICE_DEPLOY_NAME\": \"$openAIServiceDeployName\"
}" > app/local.settings.json