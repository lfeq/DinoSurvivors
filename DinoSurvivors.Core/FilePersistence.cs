using System;
using System.IO;
using System.Text.Json;

namespace DinoSurvivors.Core;

public class FilePersistence : IPersistence {
    private readonly string _filePath;

    public FilePersistence(string filePath) {
        _filePath = filePath;
    }

    public SaveData Load() {
        if (!File.Exists(_filePath)) {
            return new SaveData();
        }
        try {
            string json = File.ReadAllText(_filePath);
            return JsonSerializer.Deserialize<SaveData>(json) ?? new SaveData();
        } catch {
            return new SaveData();
        }
    }

    public void Save(SaveData data) {
        try {
            string? directory = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory)) {
                Directory.CreateDirectory(directory);
            }
            string json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_filePath, json);
        } catch {
            // Ignore write errors
        }
    }
}
