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
        public string Topic { get; set; }
    }

    public class PresetService
    {
        private readonly string presetsFilePath = Path.Combine("presets", "presets.json");

        public async Task<List<PresetModel>> GetPresetsAsync()
        {
            if (!File.Exists(presetsFilePath))
            {
                return new List<PresetModel>();
            }

            var json = await File.ReadAllTextAsync(presetsFilePath);
            var presets = JsonSerializer.Deserialize<List<PresetModel>>(json);

            return presets ?? new List<PresetModel>();
        }

        public async Task SavePresetsAsync(List<PresetModel> presets)
        {
            var json = JsonSerializer.Serialize(presets, new JsonSerializerOptions { WriteIndented = true });
            var directory = Path.GetDirectoryName(presetsFilePath);

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            await File.WriteAllTextAsync(presetsFilePath, json);
        }
    }
}
