using System.Collections.Generic;

namespace Functions
{
    class CustomSkillRequest
    {
        public List<InputValue> Values { get; set; }
    }

    class InputValue
    {
        public string RecordId { get; set; }
        public InputsData Data { get; set; }
    }

    class InputsData
    {
        // ここのフィールド定義を "Microsoft.Skills.Custom.WebApiSkill" の "inputs" で指定した要素の "name" に合わせる
        public string Input { get; set; }
    }
}
