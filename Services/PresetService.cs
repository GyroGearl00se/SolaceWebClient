using Blazored.Toast.Services;
using Blazorise;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace SolaceWebClient.Services
{
    public class PresetModel
    {
        public string Name { get; set; }
        public string Host { get; set; }
        public string VpnName { get; set; }
        public string Username { get; set; }
        public string QueueName { get; set; }
    }

    public class PresetService
    {
        private readonly string presetsPath = "presets";

        public async Task<List<PresetModel>> GetPresetsAsync()
        {
            if (!Directory.Exists(presetsPath))
            {
                Directory.CreateDirectory(presetsPath);
            }

            var presets = new List<PresetModel>();

            foreach (var file in Directory.GetFiles(presetsPath, "*.json"))
            {
                var json = await File.ReadAllTextAsync(file);
                var preset = JsonSerializer.Deserialize<PresetModel>(json);
                presets.Add(preset);
            }

            return presets;
        }

        public async Task SavePresetAsync(PresetModel preset)
        {
            if (!Directory.Exists(presetsPath))
            {
                Directory.CreateDirectory(presetsPath);
            }

            var json = JsonSerializer.Serialize(preset);
            var filePath = Path.Combine(presetsPath, $"{preset.Name}.json");
            await File.WriteAllTextAsync(filePath, json);
        }

        public async Task DeletePresetAsync(string presetName)
        {
            if (!Directory.Exists(presetsPath))
            {
                Directory.CreateDirectory(presetsPath);
            }

            var filePath = Path.Combine(presetsPath, $"{presetName}.json");
            File.Delete(filePath);
        }
    }
}

