new Vue({
    el: '#app',
    data: {
        settings: {},
        search: "*",
        searching: false,
        docs: []
    },
    // 画面表示時の処理
    async mounted() {
        this.settings = settings;
        await this.searchDocuments();
    },
    watch: {
        // 検索テキストボックスの値が変更された時の処理
        search: function () {
            this.searchDocuments();
        }
    },
    methods: {
        // 指定したテキストの埋め込みを取得する
        getEmbeddings: async function (text) {
            const url = `api/embeds?t=${encodeURI(text)}`;
            const resp = await axios.get(url);
            return resp.data;
        },
        // 検索結果を取得する
        searchDocuments: async function () {
            this.docs = [];
            if (!this.search) return;
            this.searching = true;

            const url = `https://${this.settings.name}.search.windows.net/indexes/${this.settings.indexName}/docs/search?api-version=${this.settings.apiVersion}`;
            const headers = {
                "Content-Type": "application/json",
                "api-key": this.settings.key
            };
            const body = {
                "top": this.settings.searchTop,
                "select": this.settings.select,
                "vector": {
                    "value": await this.getEmbeddings(this.search),
                    "fields": "contentVector",
                }
            };
            const resp = await axios.post(url, body, { headers });

            this.searching = false;
            this.docs = resp.data.value;
        }
    }
});

