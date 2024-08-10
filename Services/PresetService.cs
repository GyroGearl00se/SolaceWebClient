using Blazored.Toast.Services;
using Blazorise;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace SolaceWebClient.Services
{
    public class PresetGroupModel
    {
        public string GroupName { get; set; }
        public List<PresetModel> Presets { get; set; }
    }
    public class PresetModel
    {
        public string Name { get; set; }
        public string Host { get; set; }
        public string VpnName { get; set; }
        public string Username { get; set; }
        public string QueueName { get; set; }
        public string Topic { get; set; }
        public string SempUrl { get; set; }
        public string sempUsername { get; set; }
    }

    public class PresetService
    {
        private readonly string presetsFilePath = Path.Combine("presets", "presets.json");

        public async Task<List<PresetGroupModel>> GetPresetGroupsAsync()
        {
            if (!File.Exists(presetsFilePath))
            {
                return new List<PresetGroupModel>();
            }

            var json = await File.ReadAllTextAsync(presetsFilePath);
            var presetGroups = JsonSerializer.Deserialize<List<PresetGroupModel>>(json);

            return presetGroups ?? new List<PresetGroupModel>();
        }
    }

}
