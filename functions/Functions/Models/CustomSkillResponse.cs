using System.Collections.Generic;

namespace Functions
{
    class CustomSkillResponse
    {
        public List<OutputValue> Values { get; set; }
    }

    class OutputValue
    {
        public string RecordId { get; set; }
        public OutputsData Data { get; set; }
    }

    class OutputsData
    {
        // ここのフィールド定義を "Microsoft.Skills.Custom.WebApiSkill" の "inputs" で指定した要素の "name" に合わせる
        public float[] Output { get; set; }
    }
}
