using System.Text.Json;

namespace Comms.Helpers
{
    public class SnakeCaseNamingPolicy : JsonNamingPolicy
    {
        public override string ConvertName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return name;

            var result = new System.Text.StringBuilder();
            
            for (int i = 0; i < name.Length; i++)
            {
                if (char.IsUpper(name[i]))
                {
                    if (i > 0)
                        result.Append('_');
                    result.Append(char.ToLower(name[i]));
                }
                else
                {
                    result.Append(name[i]);
                }
            }
            
            return result.ToString();
        }
    }
}
