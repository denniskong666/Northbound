using System;
using System.IO;
using UnityEngine;

namespace Northbound.Narrative
{
    public sealed class SaveGameService
    {
        public const string SaveFileName = "northbound-save.json";

        public string SavePath { get; }

        public SaveGameService()
            : this(Path.Combine(Application.persistentDataPath, SaveFileName))
        {
        }

        public SaveGameService(string savePath)
        {
            if (string.IsNullOrWhiteSpace(savePath))
            {
                throw new ArgumentException("A save path is required.", nameof(savePath));
            }

            SavePath = savePath;
        }

        public bool Save(NarrativeState state)
        {
            var temporaryPath = SavePath + ".tmp";

            try
            {
                var directoryPath = Path.GetDirectoryName(SavePath);
                if (!string.IsNullOrEmpty(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }

                File.WriteAllText(temporaryPath, (state ?? new NarrativeState()).ToJson());

                if (File.Exists(SavePath))
                {
                    File.Replace(temporaryPath, SavePath, null);
                }
                else
                {
                    File.Move(temporaryPath, SavePath);
                }

                return true;
            }
            catch (IOException)
            {
                DeleteTemporaryFile(temporaryPath);
                return false;
            }
            catch (UnauthorizedAccessException)
            {
                DeleteTemporaryFile(temporaryPath);
                return false;
            }
            catch (NotSupportedException)
            {
                DeleteTemporaryFile(temporaryPath);
                return false;
            }
        }

        public NarrativeState LoadOrNew()
        {
            try
            {
                if (!File.Exists(SavePath))
                {
                    return new NarrativeState();
                }

                return NarrativeState.FromJson(File.ReadAllText(SavePath));
            }
            catch (IOException)
            {
                return new NarrativeState();
            }
            catch (UnauthorizedAccessException)
            {
                return new NarrativeState();
            }
        }

        public void Delete()
        {
            try
            {
                if (File.Exists(SavePath))
                {
                    File.Delete(SavePath);
                }

                DeleteTemporaryFile(SavePath + ".tmp");
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }

        private static void DeleteTemporaryFile(string temporaryPath)
        {
            try
            {
                if (File.Exists(temporaryPath))
                {
                    File.Delete(temporaryPath);
                }
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }
}
